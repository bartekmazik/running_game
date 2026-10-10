using System;

/// <summary>
/// Builds the whole arena at runtime: <see cref="Arena.FloorCount"/> stacked grids of tiles,
/// plus a lava plane under the lowest floor. Lives on the "Arena" GameObject.
/// Floor 0 is the top floor (where players spawn); the last floor is the bottom.
/// </summary>
public sealed class ArenaGenerator : Component
{
	/// <summary>models/dev/box.vmdl is a 50-unit cube, so object scale = desired size / 50.</summary>
	const float BoxModelUnits = 50f;

	const float LavaThickness = 32f;

	/// <summary>Radius of the spawn ring. 24 players at this radius sit ~63 units apart.</summary>
	const float SpawnRingRadius = 240f;

	/// <summary>
	/// Matte dev PBR surface. Takes Tint cleanly and carries no printed texture, so a 56-unit
	/// tile doesn't turn into visual noise. Swap the number (00–100) to change roughness.
	///
	/// Deliberately NOT materials/default.vmat. That material is broken in this engine install:
	/// it asks for materials/default/default_mask_tga_3a4f4d17.generated.vtex, and core ships
	/// only the default_mask.tga source, never the compiled .vtex_c — so every renderer using it
	/// logs "Texture manager doesn't know about texture ... returning error texture". Harmless as
	/// one line, but arena-wide it fired once per tile, thousands of times.
	///
	/// This material's dependency set is complete on disk: dev_nonmetal_rough30.vmat_c plus its
	/// generated tcolor, tnormal and tambientocclusion.
	/// </summary>
	public const string SurfaceMaterialPath = "materials/dev/dev_nonmetal_rough30.vmat";

	/// <summary>Matte surface for the non-solid cream frame around each floor.</summary>
	const string FrameMaterialPath = SurfaceMaterialPath;

	/// <summary>
	/// Unlit-looking emissive white. Tint carries the colour, and with bloom on the camera it
	/// reads as a glowing surface rather than an orange box. Core content, always mounted.
	/// </summary>
	const string GlowMaterialPath = "materials/dev/primary_white_emissive.vmat";

	/// <summary>Height of the lava glow light above the slab.</summary>
	const float LavaGlowHeight = 120f;

	/// <summary>Frame beam cross-section. Sits just outside the grid, entirely below tile tops.</summary>
	const float FrameWidth = 14f;
	const float FrameDepth = 22f;

	/// <summary>Square corner posts that carry the frames down into the lava.</summary>
	const float PillarSize = 30f;

	/// <summary>How far above floor 0's top face the corner posts rise, and their glowing caps.</summary>
	const float PillarRise = 90f;

	/// <summary>
	/// One identity colour per floor, top to bottom. The values live in <see cref="Palette"/>.
	/// </summary>
	public static Color FloorColor( int floor ) => Palette.FloorHue( floor );

	/// <summary>
	/// Outermost ring of every floor — reads as "edge" before you reach it. Tints the candy-stripe
	/// texture, so the ring comes out as cream-and-coral stripes.
	/// </summary>
	static readonly Color EdgeColor = new Color( 1.00f, 0.52f, 0.50f );

	/// <summary>What a stepped-on tile heats towards during its <see cref="Arena.DecayDelay"/>.</summary>
	public static Color WarnColor => Palette.Warn;

	/// <summary>Cream rails and posts — the "toy" frame the floors sit in.</summary>
	static readonly Color FrameColor = new Color( 0.98f, 0.94f, 0.86f );

	/// <summary>Candy lava: hot coral-pink rather than volcanic orange, so it belongs to the palette.</summary>
	static readonly Color LavaColor = new Color( 0.92f, 0.22f, 0.38f );

	/// <summary>The hot end of the lava's slow pulse.</summary>
	static readonly Color LavaHotColor = new Color( 1.00f, 0.46f, 0.42f );

	/// <summary>
	/// Above 1.0 on purpose — Light.LightColor carries intensity in its magnitude, so an HDR
	/// value is how you get a bright light rather than a merely pink one.
	/// </summary>
	static readonly Color LavaGlowColor = new Color( 2.4f, 0.80f, 1.00f );

	const float ExposureCompensation = 0f;

	/// <summary>Flat pastel sky behind everything — the camera's clear colour.</summary>
	public static readonly Color SkyColor = new Color( 0.60f, 0.82f, 1.00f );

	static readonly Color SunColor = new Color( 1.00f, 0.95f, 0.88f );
	static readonly Color SunSkyColor = new Color( 0.74f, 0.80f, 0.94f );

	/// <summary>Lilac haze for the drop, so the bottom of the shaft is soft rather than black.</summary>
	static readonly Color FogColor = new Color( 0.80f, 0.66f, 0.86f );

	GameObject[,,] _tiles;
	ModelRenderer[,,] _renderers;

	/// <summary>One material per floor — each wears its own Kenney motif.</summary>
	Material[] _floorMaterials;
	Material _hazardMaterial;
	Material _glowMaterial;

	ModelRenderer _lavaRenderer;
	PointLight _lavaLight;

	/// <summary>
	/// Min-X / min-Y corner of the grid, at floor 0's tile-centre height, in world space.
	/// Set in OnAwake — every OnAwake completes during load, so this is readable from any
	/// other component's OnStart regardless of component order.
	/// </summary>
	public Vector3 GridOrigin { get; private set; }

	/// <summary>World-space height of the lava's top face.</summary>
	public float LavaTopZ => WorldPosition.z + LavaCenterZ + LavaThickness * 0.5f;

	static float LavaCenterZ => -((Arena.FloorCount - 1) * Arena.FloorGap) - Arena.LavaOffset;

	protected override void OnAwake()
	{
		// Pure arithmetic, needs no tiles — safe to run before Generate().
		var span = Arena.GridSize * Arena.TileSize;
		GridOrigin = WorldPosition - new Vector3( span * 0.5f, span * 0.5f, 0f );
	}

	protected override void OnStart()
	{
		Generate();
	}

	/// <summary>
	/// A slow, uneven breathing on the lava and its light — two sines out of phase, so it never
	/// settles into an obvious loop. One renderer and one light; nothing per tile.
	/// </summary>
	protected override void OnUpdate()
	{
		if ( !_lavaRenderer.IsValid() ) return;

		var t = Time.Now;
		var pulse = 0.5f + 0.3f * MathF.Sin( t * 1.3f ) + 0.2f * MathF.Sin( t * 3.1f + 1.7f );

		_lavaRenderer.Tint = Color.Lerp( LavaColor, LavaHotColor, pulse );
		_lavaLight.LightColor = LavaGlowColor * (0.8f + 0.35f * pulse);

		// The whole cloud ring turns very slowly — enough to feel alive behind a standing player.
		if ( _clouds.IsValid() )
			_clouds.LocalRotation = Rotation.FromYaw( t * 0.6f );
	}

	void Generate()
	{
		_tiles = new GameObject[Arena.FloorCount, Arena.GridSize, Arena.GridSize];
		_renderers = new ModelRenderer[Arena.FloorCount, Arena.GridSize, Arena.GridSize];

		var span = Arena.GridSize * Arena.TileSize;

		var boxModel = Model.Load( "models/dev/box.vmdl" );
		var fallbackBlock = TileTextures.CreateBlock();
		_floorMaterials = new Material[Arena.FloorCount];
		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
			_floorMaterials[floor] = TileTextures.CreateFromFile( $"tnt_floor{floor}", TileTextures.FloorTexturePath( floor ) ) ?? fallbackBlock;

		_hazardMaterial = TileTextures.CreateFromFile( "tnt_edge", TileTextures.EdgeTexturePath ) ?? TileTextures.CreateHazard();
		_glowMaterial = Material.Load( GlowMaterialPath );
		var tileScale = new Vector3( Arena.TileSize, Arena.TileSize, Arena.TileThickness ) / BoxModelUnits;

		for ( int floor = 0; floor < Arena.FloorCount; floor++ )
		{
			for ( int x = 0; x < Arena.GridSize; x++ )
			{
				for ( int y = 0; y < Arena.GridSize; y++ )
				{
					var tile = new GameObject( GameObject, true, $"Tile_{floor}_{x}_{y}" );
					tile.LocalPosition = TileLocalPosition( floor, x, y );
					tile.LocalScale = tileScale;
					MakeLocalOnly( tile );

					var renderer = tile.AddComponent<ModelRenderer>();
					renderer.Model = boxModel;
					renderer.MaterialOverride = TileMaterial( floor, x, y );
					renderer.Tint = TileColor( floor, x, y );

					var collider = tile.AddComponent<BoxCollider>();
					collider.Scale = new Vector3( BoxModelUnits );
					collider.Static = true;

					_tiles[floor, x, y] = tile;
					_renderers[floor, x, y] = renderer;
				}
			}
		}

		CreateFrame( boxModel, span );
		CreateLava( boxModel, span );
		CreateClouds();
		CreateSigns( span );
		ConfigureLighting();
		ConfigureCamera();

		Log.Info( $"Arena: {Arena.FloorCount} floors x {Arena.GridSize}x{Arena.GridSize} = {Arena.FloorCount * Arena.GridSize * Arena.GridSize} tiles" );
	}

	static bool IsEdge( int x, int y )
	{
		var last = Arena.GridSize - 1;
		return x == 0 || y == 0 || x == last || y == last;
	}

	/// <summary>Code-generated block texture inside, hazard stripes on the outermost ring.</summary>
	Material TileMaterial( int floor, int x, int y ) => IsEdge( x, y ) ? _hazardMaterial : _floorMaterials[floor];

	/// <summary>
	/// The resting colour of a tile. Three layers, all from the index alone so a reset can
	/// restore any tile without storing its colour:
	///   * the floor's identity colour,
	///   * concentric square bands every three tiles out from the centre, so a player can judge
	///     how near the edge they are without looking for it,
	///   * a faint checker inside each band, so individual tiles still read when running,
	/// and a dark outermost ring as the edge warning.
	/// </summary>
	public static Color TileColor( int floor, int x, int y )
	{
		if ( IsEdge( x, y ) )
			return EdgeColor.Darken( floor * 0.03f );

		// Chebyshev distance from the centre, in tiles. GridSize is even, so the centre is a
		// four-tile junction and the innermost band is the 2x2 around it.
		var half = Arena.GridSize / 2;
		var dx = x < half ? half - 1 - x : x - half;
		var dy = y < half ? half - 1 - y : y - half;
		var ring = dx > dy ? dx : dy;

		var band = (ring / 3) % 2 == 0;
		var check = (x + y) % 2 == 0;

		// Pastel needs a lighter hand than the old palette: deep darkening turned butter into mud.
		var shade = (band ? 0f : 0.10f) + (check ? 0f : 0.04f);
		return FloorColor( floor ).Darken( shade );
	}

	/// <summary>
	/// Paint a tile part-way through its collapse. <paramref name="t"/> runs 0..1 over the decay
	/// delay. The tile switches to the emissive material and heats from a dim ember to
	/// <see cref="WarnColor"/>, sagging a few units — on the matte surface the same tint read as
	/// mud, not as a tile about to go. Purely local presentation.
	/// </summary>
	public void SetTileWarning( int floor, int x, int y, float t )
	{
		var renderer = _renderers?[floor, x, y];
		if ( renderer is null ) return;

		renderer.MaterialOverride = _glowMaterial;

		// Orange on every floor from the first frame — blending from the floor's own colour made a
		// teal tile go beige, which reads as nothing. Dim ember to full glow over the delay.
		var eased = t * t;
		renderer.Tint = Color.Lerp( WarnColor.Darken( 0.6f ), WarnColor, eased );
		renderer.GameObject.LocalPosition = TileLocalPosition( floor, x, y ) + Vector3.Down * (6f * eased);
	}

	/// <summary>Undo <see cref="SetTileWarning"/>: resting material, colour and height.</summary>
	public void RestoreTile( int floor, int x, int y )
	{
		var renderer = _renderers?[floor, x, y];
		if ( renderer is null ) return;

		renderer.MaterialOverride = TileMaterial( floor, x, y );
		renderer.Tint = TileColor( floor, x, y );
		renderer.GameObject.LocalPosition = TileLocalPosition( floor, x, y );
	}

	/// <summary>
	/// Aims the scene's directional light. CLAUDE.md puts lighting under this component, and
	/// setting it from code rather than the scene keeps the "everything is generated" rule —
	/// the scene's own values are a starting point, not the shipped look.
	///
	/// A 3/4 angle rather than straight down: overhead light flattens the tile grid into one
	/// grey sheet, and the seams between tiles are the thing a player actually reads.
	/// </summary>
	void ConfigureLighting()
	{
		var sun = Scene.Get<DirectionalLight>();

		sun.WorldRotation = new Angles( 50f, 135f, 0f ).ToRotation();
		sun.LightColor = SunColor;
		sun.SkyColor = SunSkyColor;
		sun.Shadows = true;

		// No photographic sky: every shipped skybox is a real-world photo (clouds, a studio, a sunrise)
		// and each one dragged the palette towards grey. A flat pastel backdrop on the camera reads as
		// a toy world instead; the sun's SkyColor carries the ambient fill the sky used to provide.
		var sky = Scene.Get<SkyBox2D>();
		if ( sky is not null )
			sky.Enabled = false;

		// Haze that thickens with depth below floor 0, so the lower floors and the lava sit in a
		// soft lilac haze instead of hard-edged void. Height is measured from this object's origin.
		var fog = GameObject.AddComponent<GradientFog>();
		fog.Color = FogColor;
		fog.StartDistance = 4500f;
		fog.EndDistance = 20000f;
		fog.FalloffExponent = 2f;
		fog.Height = 400f;
		fog.VerticalFalloffExponent = 1f;
	}

	/// <summary>
	/// Post-processing on the scene's one camera. Bloom is what makes the emissive lava and the
	/// pillar caps glow; ambient occlusion darkens the seams between tiles, which is where the
	/// grid's readability lives.
	/// </summary>
	void ConfigureCamera()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

		camera.FieldOfView = 75f;
		camera.BackgroundColor = SkyColor;

		var bloom = camera.GameObject.GetOrAddComponent<Bloom>();
		bloom.Strength = 0.35f;
		bloom.Threshold = 0.9f;

		var ao = camera.GameObject.GetOrAddComponent<AmbientOcclusion>();
		ao.Intensity = 0.5f;

		// Pastel needs exact colours: auto exposure saw a bright sky and pulled the whole frame down a
		// stop, and the filmic curve then greyed what was left — the palette read as dusk, not candy.
		// Fixed exposure and ACES — which kept the pastels saturated where AgX and the filmic curve
		// greyed them — put the floor and sky colours on screen close to how they're authored.
		var tonemap = camera.GameObject.GetOrAddComponent<Tonemapping>();
		tonemap.AutoExposureEnabled = false;
		tonemap.Mode = Tonemapping.TonemappingMode.ACES;
		tonemap.ExposureCompensation = ExposureCompensation;
	}

	/// <summary>
	/// The boundary treatment: a cream beam around each floor's perimeter and a post at every
	/// corner running from above the top floor down into the lava. All of it sits outside the
	/// grid and below tile tops, and none of it has a collider — CLAUDE.md keeps the play grid's
	/// collision untouched, so this is something you see, never something you stand on.
	/// </summary>
	void CreateFrame( Model boxModel, float span )
	{
		var frameMaterial = TileTextures.CreateFromFile( "tnt_frame", TileTextures.FrameTexturePath, FrameMaterialPath )
			?? TileTextures.CreatePlate( FrameMaterialPath );
		var glowMaterial = Material.Load( GlowMaterialPath );

		var frame = new GameObject( GameObject, true, "Frame" );
		MakeLocalOnly( frame );

		var outer = span * 0.5f + FrameWidth * 0.5f;
		var beamLength = span + FrameWidth * 2f;

		for ( int floor = 0; floor < Arena.FloorCount; floor++ )
		{
			// Top of the beam flush with the tile's underside, so the floor appears to rest on it.
			var z = -floor * Arena.FloorGap - Arena.TileThickness * 0.5f - FrameDepth * 0.5f;

			AddBox( frame, boxModel, frameMaterial, FrameColor, new Vector3( 0f, outer, z ), new Vector3( beamLength, FrameWidth, FrameDepth ) );
			AddBox( frame, boxModel, frameMaterial, FrameColor, new Vector3( 0f, -outer, z ), new Vector3( beamLength, FrameWidth, FrameDepth ) );
			AddBox( frame, boxModel, frameMaterial, FrameColor, new Vector3( outer, 0f, z ), new Vector3( FrameWidth, beamLength, FrameDepth ) );
			AddBox( frame, boxModel, frameMaterial, FrameColor, new Vector3( -outer, 0f, z ), new Vector3( FrameWidth, beamLength, FrameDepth ) );

			// A thin strip in the floor's identity colour along the beam's outer face — the same
			// colour as the HUD's ladder rung, so you can count floors from outside the arena.
			// Tint is stored 8 bits per channel and WRAPS above 1.0 — a 1.6x "brighter" teal came
			// out red. The emissive material plus bloom supplies the brightness; tint stays <= 1.
			var stripColor = FloorColor( floor );
			var stripZ = z - FrameDepth * 0.25f;
			var stripOuter = outer + FrameWidth * 0.5f + 1f;

			AddBox( frame, boxModel, glowMaterial, stripColor, new Vector3( 0f, stripOuter, stripZ ), new Vector3( beamLength, 2f, 5f ) );
			AddBox( frame, boxModel, glowMaterial, stripColor, new Vector3( 0f, -stripOuter, stripZ ), new Vector3( beamLength, 2f, 5f ) );
			AddBox( frame, boxModel, glowMaterial, stripColor, new Vector3( stripOuter, 0f, stripZ ), new Vector3( 2f, beamLength, 5f ) );
			AddBox( frame, boxModel, glowMaterial, stripColor, new Vector3( -stripOuter, 0f, stripZ ), new Vector3( 2f, beamLength, 5f ) );
		}

		// Corner posts: from PillarRise above floor 0 down to just inside the lava.
		var top = PillarRise;
		var bottom = LavaCenterZ;
		var height = top - bottom;
		var corner = span * 0.5f + PillarSize * 0.5f;

		for ( int i = 0; i < 4; i++ )
		{
			var cx = (i & 1) == 0 ? corner : -corner;
			var cy = (i & 2) == 0 ? corner : -corner;

			AddBox( frame, boxModel, frameMaterial, FrameColor, new Vector3( cx, cy, bottom + height * 0.5f ), new Vector3( PillarSize, PillarSize, height ) );
			AddBox( frame, boxModel, glowMaterial, LavaColor, new Vector3( cx, cy, top + 3f ), new Vector3( PillarSize + 2f, PillarSize + 2f, 6f ) );
		}
	}

	void AddBox( GameObject parent, Model model, Material material, Color tint, Vector3 localPosition, Vector3 size )
	{
		var box = new GameObject( parent, true, "Frame Piece" );
		box.LocalPosition = localPosition;
		box.LocalScale = size / BoxModelUnits;
		MakeLocalOnly( box );

		var renderer = box.AddComponent<ModelRenderer>();
		renderer.Model = model;
		renderer.MaterialOverride = material;
		renderer.Tint = tint;
	}

	void CreateLava( Model boxModel, float span )
	{
		var z = LavaCenterZ;

		// Wider than the grid, so the posts and anything falling past the edge both land in it.
		var lavaSpan = span + PillarSize * 4f;

		var lava = new GameObject( GameObject, true, "Lava" );
		lava.LocalPosition = new Vector3( 0f, 0f, z );
		lava.LocalScale = new Vector3( lavaSpan, lavaSpan, LavaThickness ) / BoxModelUnits;
		lava.Tags.Add( "lava" );
		MakeLocalOnly( lava );

		_lavaRenderer = lava.AddComponent<ModelRenderer>();
		_lavaRenderer.Model = boxModel;
		_lavaRenderer.MaterialOverride = TileTextures.CreateLava( GlowMaterialPath );
		_lavaRenderer.Tint = LavaColor;

		// Trigger volume matches the rendered slab exactly — both scale off `lavaSpan`, so a
		// TileSize change moves them together. Deliberately not Static: a static trigger
		// against a static collider may not generate overlap events at all.
		var trigger = lava.AddComponent<BoxCollider>();
		trigger.Scale = new Vector3( BoxModelUnits );
		trigger.IsTrigger = true;

		lava.AddComponent<LavaZone>();

		CreateLavaGlow( z, span );
	}

	/// <summary>
	/// One point light over the lava so the bottom floor is lit from below and the drop reads as
	/// dangerous rather than as an unlit void.
	///
	/// Parented to the arena, not to the lava: the lava slab carries a large non-uniform
	/// LocalScale, and a child would inherit it — both its offset and the light's radius would
	/// come out stretched.
	/// </summary>
	/// <summary>
	/// Puffy toy clouds drifting in a wide ring around the tower, so the flat pastel sky has depth
	/// and the menu orbit has something passing by. Clusters of matte spheres — no models of our
	/// own, no colliders, nothing that could ever be stood on. Deterministic, so every machine
	/// sees the same sky.
	/// </summary>
	void CreateClouds()
	{
		var sphere = Model.Load( "models/dev/sphere.vmdl" );
		var units = MathF.Max( sphere.Bounds.Size.x, 1f );
		var random = new System.Random( 2024 );

		var sky = new GameObject( GameObject, true, "Clouds" );
		MakeLocalOnly( sky );

		const int Count = 16;
		var towerDepth = (Arena.FloorCount - 1) * Arena.FloorGap;

		for ( var i = 0; i < Count; i++ )
		{
			var yaw = i * 360f / Count + (float)random.NextDouble() * 14f;
			var distance = 3400f + (float)random.NextDouble() * 2200f;
			var height = 300f - (float)random.NextDouble() * (towerDepth + 900f);

			var cloud = new GameObject( sky, true, "Cloud" );
			cloud.LocalPosition = Rotation.FromYaw( yaw ).Forward * distance + Vector3.Up * height;
			cloud.LocalRotation = Rotation.FromYaw( (float)random.NextDouble() * 360f );
			MakeLocalOnly( cloud );

			var puffs = 3 + random.Next( 3 );
			for ( var p = 0; p < puffs; p++ )
			{
				var size = 260f + (float)random.NextDouble() * 260f;
				var puff = new GameObject( cloud, true, "Puff" );
				puff.LocalPosition = new Vector3( (p - puffs * 0.5f) * 210f, (float)random.NextDouble() * 120f - 60f, (float)random.NextDouble() * 80f );
				puff.LocalScale = new Vector3( size, size * 0.85f, size * 0.62f ) / units;
				MakeLocalOnly( puff );

				var renderer = puff.AddComponent<ModelRenderer>();
				renderer.Model = sphere;
				// Self-lit: a lit sphere seen from the side is half shadow, and a grey cloud reads as
				// smoke. A dimmed emissive white lands as soft cream under the tonemapper.
				renderer.MaterialOverride = _glowMaterial;
				renderer.Tint = CloudColor;
				renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
			}
		}

		_clouds = sky;
	}

	GameObject _clouds;

	/// <summary>
	/// Signage in the world: a floor-number disc (floor colour + that floor's Kenney motif) on every
	/// corner post of every floor, so you can read your depth looking across the shaft, and the
	/// trophy over the tower — Kenney's gold medal and the one rule — for the menu orbit and for
	/// anyone looking up. Billboards, so they read from any side; no colliders.
	/// </summary>
	void CreateSigns( float span )
	{
		var signs = new GameObject( GameObject, true, "Signs" );
		MakeLocalOnly( signs );

		var corner = span * 0.5f + PillarSize * 0.5f;
		var outward = 34f;

		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
		{
			for ( var i = 0; i < 4; i++ )
			{
				var cx = (i & 1) == 0 ? 1f : -1f;
				var cy = (i & 2) == 0 ? 1f : -1f;
				var position = new Vector3( cx * (corner + outward), cy * (corner + outward), -floor * Arena.FloorGap + 72f );

				AddSign( signs, position, new Vector2( 640f, 640f ), 1f, floor );
			}
		}

		AddSign( signs, Vector3.Up * (PillarRise + 420f), new Vector2( 1500f, 1300f ), 4f, -1 );
	}

	void AddSign( GameObject parent, Vector3 localPosition, Vector2 panelSize, float scale, int floor )
	{
		var sign = new GameObject( parent, true, floor < 0 ? "Trophy" : $"Floor Sign {floor + 1}" );
		sign.LocalPosition = localPosition;
		sign.LocalScale = new Vector3( scale );
		MakeLocalOnly( sign );

		var world = sign.AddComponent<WorldPanel>();
		world.PanelSize = panelSize;
		world.LookAtCamera = true;

		sign.AddComponent<ArenaSign>().Floor = floor;
	}

	static readonly Color CloudColor = new Color( 0.60f, 0.60f, 0.64f );

	void CreateLavaGlow( float lavaZ, float span )
	{
		var glow = new GameObject( GameObject, true, "Lava Glow" );
		glow.LocalPosition = new Vector3( 0f, 0f, lavaZ + LavaGlowHeight );
		MakeLocalOnly( glow );

		_lavaLight = glow.AddComponent<PointLight>();
		_lavaLight.LightColor = LavaGlowColor;
		_lavaLight.Radius = span * 0.8f;

		// Thousands of tiles between this light and the camera. Shadow-casting it would be the single
		// most expensive thing in the scene, and it lights an area nobody stands in.
		_lavaLight.Shadows = false;
	}

	/// <summary>
	/// Generated geometry is never serialised into the scene and never enters a network
	/// snapshot. Tile state travels as batched collapse indices instead (CLAUDE.md, decision 3).
	/// </summary>
	static void MakeLocalOnly( GameObject go )
	{
		go.Flags |= GameObjectFlags.NotSaved;
		go.NetworkMode = NetworkMode.Never;
	}

	Vector3 TileLocalPosition( int floor, int x, int y )
	{
		var half = (Arena.GridSize - 1) * 0.5f;

		return new Vector3(
			(x - half) * Arena.TileSize,
			(y - half) * Arena.TileSize,
			-floor * Arena.FloorGap );
	}

	/// <summary>The tile at [floor, x, y], or null if any index is out of range.</summary>
	public GameObject GetTile( int floor, int x, int y )
	{
		// A collapse RPC can land on a joining client before its own Generate() has run.
		if ( _tiles is null ) return null;

		if ( floor < 0 || floor >= Arena.FloorCount ) return null;
		if ( x < 0 || x >= Arena.GridSize ) return null;
		if ( y < 0 || y >= Arena.GridSize ) return null;

		return _tiles[floor, x, y];
	}

	/// <summary>
	/// Feet-level spawn position on floor 0. Players are spread around a ring so N of them
	/// don't spawn inside each other; a lone player gets dead centre. Lives here rather than
	/// in TntPlayer because both TntPlayer and RoundManager need it, and this class already
	/// owns every other piece of arena geometry.
	/// </summary>
	public Vector3 GetSpawnPosition( int index, int total )
	{
		var target = WorldPosition;

		if ( total > 1 )
		{
			var yaw = index / (float)total * 360f;
			target += Rotation.FromYaw( yaw ).Forward * SpawnRingRadius;
		}

		// Snap to a tile centre. GridSize is even, so the arena's exact centre is a four-tile
		// junction. SpawnRingRadius is well inside the footprint, so these indices can't go out
		// of range.
		var tileX = (int)((target.x - GridOrigin.x) / Arena.TileSize);
		var tileY = (int)((target.y - GridOrigin.y) / Arena.TileSize);

		return new Vector3(
			GridOrigin.x + (tileX + 0.5f) * Arena.TileSize,
			GridOrigin.y + (tileY + 0.5f) * Arena.TileSize,
			GridOrigin.z + Arena.TileThickness * 0.5f + 4f );
	}

	/// <summary>Yaw that faces a spawn slot towards the arena's centre.</summary>
	public float GetSpawnYaw( Vector3 spawnPosition )
	{
		var toCentre = (WorldPosition - spawnPosition).WithZ( 0f );
		if ( toCentre.IsNearZeroLength ) return 0f;

		return Rotation.LookAt( toCentre.Normal ).Yaw();
	}

	/// <summary>
	/// World position to tile index — pure arithmetic, never a trace (CLAUDE.md, decision 2).
	/// This is the inverse of <see cref="TileLocalPosition"/>; keep them in this file together
	/// so they cannot drift apart. Returns false if the position is outside the arena.
	/// Expects a grounded feet position: the floor index comes from height above a tile's top face.
	/// </summary>
	public bool TryGetTileIndex( Vector3 worldPosition, out int floor, out int x, out int y )
	{
		floor = 0;
		x = 0;
		y = 0;

		var local = worldPosition - GridOrigin;

		// Guard the floats before casting: (int) truncates toward zero, so a negative
		// offset just off the min edge would otherwise report index 0.
		var fx = local.x / Arena.TileSize;
		var fy = local.y / Arena.TileSize;
		if ( fx < 0f || fy < 0f ) return false;

		x = (int)fx;
		y = (int)fy;
		if ( x >= Arena.GridSize || y >= Arena.GridSize ) return false;

		if ( !TryGetFloor( local.z, out floor ) ) return false;

		return true;
	}

	bool TryGetFloor( float localZ, out int floor )
	{
		floor = 0;

		var drop = (Arena.TileThickness * 0.5f - localZ) / Arena.FloorGap;
		if ( drop < -0.5f ) return false;

		floor = (int)(drop + 0.5f);
		return floor < Arena.FloorCount;
	}

	/// <summary>
	/// Every tile a grounded player's footprint overlaps — up to four. Same arithmetic as
	/// <see cref="TryGetTileIndex"/>, applied to the corners of a square of half-size
	/// <paramref name="radius"/>.
	///
	/// Marking only the tile under the feet's centre let a player stand on a four-tile junction
	/// indefinitely: the marked tile collapsed, the other three held them up, and since the centre
	/// never left the collapsed tile's index nothing new was ever reported.
	/// </summary>
	public int GetFootprintTiles( Vector3 worldPosition, float radius, int[] packedOut )
	{
		var local = worldPosition - GridOrigin;
		if ( !TryGetFloor( local.z, out var floor ) ) return 0;

		var minX = FloorToTile( local.x - radius );
		var maxX = FloorToTile( local.x + radius );
		var minY = FloorToTile( local.y - radius );
		var maxY = FloorToTile( local.y + radius );

		var count = 0;

		for ( var x = minX; x <= maxX; x++ )
		{
			if ( x < 0 || x >= Arena.GridSize ) continue;

			for ( var y = minY; y <= maxY; y++ )
			{
				if ( y < 0 || y >= Arena.GridSize ) continue;
				if ( count >= packedOut.Length ) return count;

				packedOut[count++] = TileDecay.PackIndex( floor, x, y );
			}
		}

		return count;
	}

	/// <summary>
	/// The floor you would land on from here: the one whose top face is at, or first below, this
	/// height. Unlike <see cref="TryGetTileIndex"/>, which rounds to the nearest floor for a
	/// grounded player, this never answers with the floor above someone who is mid-fall.
	/// </summary>
	public bool TryGetFloorBelow( Vector3 worldPosition, out int floor )
	{
		var local = worldPosition - GridOrigin;
		var drop = (Arena.TileThickness * 0.5f - local.z) / Arena.FloorGap;

		floor = (int)MathF.Ceiling( drop - 0.08f );
		if ( floor < 0 ) floor = 0;
		return floor < Arena.FloorCount;
	}

	/// <summary>Column and row under a world position, ignoring height. False outside the grid.</summary>
	public bool TryGetColumn( Vector3 worldPosition, out int x, out int y )
	{
		var local = worldPosition - GridOrigin;
		x = FloorToTile( local.x );
		y = FloorToTile( local.y );
		return x >= 0 && y >= 0 && x < Arena.GridSize && y < Arena.GridSize;
	}

	/// <summary>World position of the centre of a tile's top face.</summary>
	public Vector3 TileTop( int floor, int x, int y )
	{
		return WorldPosition + TileLocalPosition( floor, x, y ) + Vector3.Up * (Arena.TileThickness * 0.5f);
	}

	/// <summary>True if the tile exists and hasn't collapsed.</summary>
	public bool IsTileStanding( int floor, int x, int y )
	{
		var tile = GetTile( floor, x, y );
		return tile is not null && tile.Enabled;
	}

	/// <summary>Floor, not truncate — a coordinate just off the min edge must land on -1, not 0.</summary>
	static int FloorToTile( float local )
	{
		var f = local / Arena.TileSize;
		var i = (int)f;
		return f < i ? i - 1 : i;
	}

	/// <summary>Re-enable every tile and restore its resting look. Called on round restart.</summary>
	public void ResetAll()
	{
		// A round-state broadcast can land on a joining client before its own Generate() runs.
		if ( _tiles is null ) return;

		for ( int floor = 0; floor < Arena.FloorCount; floor++ )
		{
			for ( int x = 0; x < Arena.GridSize; x++ )
			{
				for ( int y = 0; y < Arena.GridSize; y++ )
				{
					RestoreTile( floor, x, y );
					_tiles[floor, x, y].Enabled = true;
				}
			}
		}
	}
}
