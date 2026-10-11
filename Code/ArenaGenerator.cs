using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Builds the whole arena at runtime: <see cref="Arena.FloorCount"/> stacked grids of tiles, the
/// tower around them (<see cref="TowerBuilder"/>: walls, colliders, facade, glass roof) and the lava
/// at the bottom of the shaft. Lives on the "Arena" GameObject.
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

	/// <summary>
	/// Unlit-looking emissive white. Tint carries the colour, and with bloom on the camera it
	/// reads as a glowing surface rather than an orange box. Core content, always mounted.
	/// </summary>
	public const string GlowMaterialPath = "materials/dev/primary_white_emissive.vmat";

	/// <summary>Height of the lava glow light above the slab's centre: just over its top face.</summary>
	const float LavaGlowHeight = 32f;

	/// <summary>
	/// Reach of the lava glow: the shaft, the last floor's undersides over it and the sides of its tiles,
	/// and no further. The light casts no shadows, so a longer reach (it was 0.8 of the grid) lit the
	/// sides of the tiles a floor higher through the forge floor, as pink slivers along the dungeon's joints.
	/// </summary>
	const float LavaGlowRadius = 420f;

	/// <summary>
	/// Where the floor plaques hang: centred on every wall, at this height above the tile top — well
	/// over the players' heads, so a crowd never hides it, and a good way under the cornice, where it
	/// reads from across the room rather than as part of the ceiling.
	/// </summary>
	const float PlaqueHeight = 170f;

	/// <summary>Height of the trophy sign's centre above floor 0's tiles: about three hundred units over the roof's ridge.</summary>
	const float TrophyHeight = FloorContext.ClearHeight + 352f;

	/// <summary>
	/// One identity colour per floor, top to bottom. The values live in <see cref="Palette"/>.
	/// </summary>
	public static Color FloorColor( int floor ) => Palette.FloorHue( floor );

	/// <summary>What a stepped-on tile heats towards during its <see cref="Arena.DecayDelay"/>.</summary>
	public static Color WarnColor => Palette.Warn;

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

	static readonly Color SunColor = new Color( 1.00f, 0.95f, 0.88f );
	/// <summary>
	/// The sky's ambient fill, which reaches every room alike — the rooms are closed, but ambient light
	/// is not occluded. A neutral grey at a little over half strength, so each room's own lamps carry its
	/// light and colour (the old blue-lilac fill at 0.8 gave every floor the same cool, flat cast) and
	/// still no corner goes black — at 0.5 the forge's ceiling did.
	/// </summary>
	static readonly Color SunSkyColor = new Color( 0.55f, 0.55f, 0.58f );

	/// <summary>Chamfer on the tile's top edges: catches the light, so neighbouring tiles read as separate blocks.</summary>
	public const float TileBevel = 2f;

	/// <summary>Four atlas cells times four quarter-turns of the top face's UVs.</summary>
	const int TileLooks = 16;

	/// <summary>
	/// Largest per-tile lean toward warm or cool, per channel. On the tiles' own colours that turns
	/// the hue by about two degrees — enough that no two neighbours are quite the same, never enough
	/// to read as a different material.
	/// </summary>
	const float TileHueJitter = 0.02f;

	GameObject[,,] _tiles;
	ModelRenderer[,,] _renderers;

	/// <summary>One theme per floor, top to bottom; each holds the surfaces it painted for this build.</summary>
	ArenaTheme[] _themes;

	/// <summary>[floor, look]: every tile shares one of its floor's sixteen generated slabs.</summary>
	Model[,] _tileModels;

	/// <summary>The tower's own surfaces (outside stone, window parts, roof glass), painted with the themes.</summary>
	Material _facade;
	Material _facadeDetail;
	Material _roofPanes;

	Material _glowMaterial;

	/// <summary>
	/// A doomed tile's glow: <see cref="WarnColor"/> painted into the emissive material's own colour,
	/// with the renderer's tint left as a grey brightness. Tinting the shipped emissive white orange
	/// came out cream — its glow doesn't take the tint's hue — however dim the tint.
	/// </summary>
	Material _warnMaterial;

	/// <summary>Glow brightness of a doomed tile when it starts warning, and when it is about to drop: above about 0.7 the orange blows out toward cream.</summary>
	const float WarnEmber = 0.18f;
	const float WarnGlow = 0.6f;

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
		_glowMaterial = Material.Load( GlowMaterialPath );
		_warnMaterial = new Painter( 16, WarnColor, 1f ).Bake( "arena_tile_warning", GlowMaterialPath, 1f ).Upload();

		PaintThemes();

		_tileModels = new Model[Arena.FloorCount, TileLooks];
		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
		{
			// The underside of a floor is the ceiling of the room below it; the bottom floor's
			// undersides face the lava shaft and wear the forge's own rock.
			var below = _themes[Math.Min( floor + 1, Arena.FloorCount - 1 )];
			for ( var look = 0; look < TileLooks; look++ )
				_tileModels[floor, look] = BuildTileModel( _themes[floor], below, look / 4, look % 4 );
		}

		var tileSize = new Vector3( Arena.TileSize, Arena.TileSize, Arena.TileThickness );

		for ( int floor = 0; floor < Arena.FloorCount; floor++ )
		{
			for ( int x = 0; x < Arena.GridSize; x++ )
			{
				for ( int y = 0; y < Arena.GridSize; y++ )
				{
					var tile = new GameObject( GameObject, true, $"Tile_{floor}_{x}_{y}" );
					tile.LocalPosition = TileLocalPosition( floor, x, y );
					MakeLocalOnly( tile );

					var renderer = tile.AddComponent<ModelRenderer>();
					renderer.Model = _tileModels[floor, TileLook( floor, x, y )];
					renderer.Tint = TileColor( floor, x, y );

					var collider = tile.AddComponent<BoxCollider>();
					collider.Scale = tileSize;
					collider.Static = true;

					_tiles[floor, x, y] = tile;
					_renderers[floor, x, y] = renderer;
				}
			}
		}

		new TowerBuilder( GameObject, _themes, _facade, _facadeDetail, _roofPanes )
			.Build( LavaCenterZ + LavaThickness * 0.5f, LavaCenterZ - LavaThickness * 0.5f );
		CreateLava( boxModel, span );
		CreateClouds();
		CreateSigns();
		ConfigureLighting();
		ConfigureCamera();

		Log.Info( $"Arena: {Arena.FloorCount} floors x {Arena.GridSize}x{Arena.GridSize} = {Arena.FloorCount * Arena.GridSize * Arena.GridSize} tiles" );
	}

	/// <summary>
	/// How many surfaces paint at once. One parallel loop over all of them let the thread pool run
	/// some thirty-five side by side, each holding its canvases (tens of megabytes), and the build
	/// swung between 2.5 and 4.7 s from one start to the next; twelve workers taking jobs in turn ran
	/// it in 2.3 s every time on the twelve-thread machine it is tuned on (fix round 1 of Task 6b).
	/// </summary>
	const int BakeWorkers = 12;

	/// <summary>
	/// Paints every surface of the arena — the one expensive step of the build, so it is timed and
	/// logged once (budget: 4 s in the editor's unoptimised build, well under that published).
	///
	/// Each surface is an independent job (its own canvas, its own seeded streams), so they paint side
	/// by side on worker threads and come out identical whatever order they finish in; the main thread
	/// waits — the arena must be whole before anyone can stand on it — and then uploads them, since
	/// textures and materials are engine resources made on the main thread only. A fixed set of
	/// <see cref="BakeWorkers"/> workers takes the jobs in turn, biggest first.
	/// </summary>
	void PaintThemes()
	{
		var clock = System.Diagnostics.Stopwatch.StartNew();

		_themes = new ArenaTheme[Arena.FloorCount];
		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
			_themes[floor] = ArenaTheme.Create( floor );

		var bakes = new List<Func<PaintedSurface>>();
		var uploads = new List<Action<PaintedSurface>>();
		void Job( Func<PaintedSurface> bake, Action<PaintedSurface> upload ) { bakes.Add( bake ); uploads.Add( upload ); }

		// The biggest surfaces first — the walls, then the floors — so no worker is left starting one at
		// the end; the walls from the bottom floor up, since the forge's and the dungeon's are the slowest.
		foreach ( var surface in new[] { ThemeSurface.Wall, ThemeSurface.Floor, ThemeSurface.Ceiling, ThemeSurface.Trim } )
			foreach ( var theme in surface == ThemeSurface.Wall ? _themes.Reverse() : _themes )
				for ( var variant = 0; variant < (surface == ThemeSurface.Wall ? theme.WallCount : 1); variant++ )
				{
					var v = variant;
					Job( () => theme.Bake( surface, v ), baked => theme.Apply( surface, baked, v ) );
				}

		Job( TowerBuilder.PaintFacade, baked => _facade = baked.Upload() );
		Job( TowerBuilder.PaintFacadeDetail, baked => _facadeDetail = baked.Upload() );
		Job( TowerBuilder.PaintPanes, baked => _roofPanes = baked.Upload() );

		var painted = new PaintedSurface[bakes.Count];
		var next = -1;
		Sandbox.Utility.Parallel.For( 0, Math.Min( bakes.Count, BakeWorkers ), _ =>
		{
			for ( var i = Interlocked.Increment( ref next ); i < bakes.Count; i = Interlocked.Increment( ref next ) )
				painted[i] = bakes[i]();
		} );

		var surfaces = 0;
		for ( var i = 0; i < painted.Length; i++ )
		{
			uploads[i]( painted[i] );
			if ( painted[i] is not null ) surfaces++;
		}

		Log.Info( $"Arena textures: {surfaces} surfaces in {clock.ElapsedMilliseconds} ms" );
	}

	/// <summary>
	/// One generated slab, 56 x 56 x 12, chamfered on its top edges only, in two materials:
	///   * top and chamfers: the floor surface, one atlas cell unfolded over both — the top face
	///     takes the cell's middle, each chamfer the strip along the cell's edge on its side at true
	///     slant length (<see cref="ArenaTheme.UnfoldedSpan"/>), so the surface runs on over the arris
	///     and the cell's edge, where the joint is painted, lands at the chamfer's foot. UVs are
	///     turned by <paramref name="turn"/> quarter-turns with the tangent following them, so the
	///     normal map stays lit from the right side; each chamfer's UV trapezoid has the shape of the
	///     chamfer itself, so its two triangles share one mapping and nothing kinks.
	///   * sides: a sliver from the middle of the same strip, the stone at its arris, stretched down
	///     the side — no decal from the middle of the tile can land on them, and the joint colour
	///     stays on the chamfer's foot, where it reads from above.
	///   * underside: the ceiling of the room below, full size and unchamfered, so neighbouring
	///     undersides meet edge to edge and read as one ceiling; the cell with the same index as the
	///     top, unturned, so boards and stripes keep one direction across the room — or turned with the
	///     top where the ceiling has no direction (<see cref="ArenaTheme.CeilingTurns"/>).
	/// Built from quads rather than MeshKit.BevelBox: a bevel box chamfers the bottom edges too, and
	/// those strips framed every ceiling panel in the floor's colour.
	/// </summary>
	static Model BuildTileModel( ArenaTheme self, ArenaTheme below, int cell, int turn )
	{
		var kit = new MeshKit();
		var half = Arena.TileSize * 0.5f;
		var inner = half - TileBevel;
		var top = Arena.TileThickness * 0.5f;
		var shoulder = top - TileBevel;
		var unfolded = ArenaTheme.UnfoldedSpan * 0.5f;
		var region = ArenaTheme.Cell( cell % 2, cell / 2, 2 );
		var surface = self.FloorSurface;

		// A point on the unfolded top (units from the tile centre) to its place in the cell:
		// texture right is +X and texture up is +Y before the turn.
		Vector2 Uv( float x, float y )
		{
			for ( var k = 0; k < turn; k++ ) (x, y) = (-y, x);
			return new Vector2( region.Left + region.Width * (x + unfolded) / (unfolded * 2f), region.Top + region.Height * (unfolded - y) / (unfolded * 2f) );
		}

		kit.Quad( surface,
			new Vector3( -inner, -inner, top ), new Vector3( inner, -inner, top ), new Vector3( inner, inner, top ), new Vector3( -inner, inner, top ),
			Uv( -inner, -inner ), Uv( inner, -inner ), Uv( inner, inner ), Uv( -inner, inner ) );

		// The +X side, then the same quarter-turned three times; geometry and unfolded UV points are
		// turned by the same rotation, so every side samples its own edge of the cell.
		// A side is seen only at grazing angles, a pixel or less wide at the far end of a seam, where
		// it alternates with the chamfer above it pixel by pixel: in the dark joint colour it drew the
		// far seams as dashed lines (on the dungeon and forge floors most). It takes the middle of the
		// chamfer's strip instead — the stone's own colour at its arris, which the Rim rule keeps free
		// of decals — and is lit as if it faced up, like the top: facing the camera it caught the lamps
		// behind it and drew the seams as bright dashes instead. Whichever a pixel lands on, it is the
		// same stone in the same light. (In a hole the sides read as evenly lit stone, which they are.)
		var sideOuter = inner + (unfolded - inner) * 0.55f;
		var sideInner = inner + (unfolded - inner) * 0.35f;
		for ( var side = 0; side < 4; side++ )
		{
			var rotation = Rotation.FromYaw( side * 90f );
			Vector3 P( float x, float y, float z ) => rotation * new Vector3( x, y, z );
			Vector2 T( float x, float y ) { var p = rotation * new Vector3( x, y, 0f ); return Uv( p.x, p.y ); }

			kit.Quad( surface,
				P( half, -half, shoulder ), P( half, half, shoulder ), P( inner, inner, top ), P( inner, -inner, top ),
				T( unfolded, -half ), T( unfolded, half ), T( inner, inner ), T( inner, -inner ) );
			// The side's bottom edge (a, b) takes the outer UVs and its top edge (c, d) the inner ones,
			// as on the chamfer above, where going up the face also moves inward on the unfolded cell.
			// For the +X face that gives +U pointing down the face and +V along −Y: U × V = −X, the
			// same sign against the outward normal as the top face (U × V = −Z) and the chamfer. The
			// other way round the tangent frame is mirrored and the normal map's slopes flip.
			kit.Quad( surface,
				P( half, -half, -top ), P( half, half, -top ), P( half, half, shoulder ), P( half, -half, shoulder ),
				T( sideOuter, -half ), T( sideOuter, half ), T( sideInner, half ), T( sideInner, -half ), Vector3.Up );
		}

		// Anticlockwise seen from below. A ceiling with no direction turns with the top (CeilingTurns):
		// the cell's corners, as the unturned quad maps them, quarter-turned about the cell's middle.
		var under = new[] { new Vector3( -half, half, -top ), new Vector3( half, half, -top ), new Vector3( half, -half, -top ), new Vector3( -half, -half, -top ) };
		if ( !below.CeilingTurns )
			kit.Quad( new Skin( below.Ceiling, region ), under[0], under[1], under[2], under[3] );
		else
		{
			Vector2 Under( float u, float v )
			{
				var (x, y) = (u - 0.5f, v - 0.5f);
				for ( var k = 0; k < turn; k++ ) (x, y) = (-y, x);
				return new Vector2( region.Left + (x + 0.5f) * region.Width, region.Top + (y + 0.5f) * region.Height );
			}
			kit.Quad( below.Ceiling, under[0], under[1], under[2], under[3], Under( 0f, 1f ), Under( 1f, 1f ), Under( 1f, 0f ), Under( 0f, 0f ) );
		}

		return kit.Build( $"arena_tile_{self.Floor}_{cell}_{turn}" );
	}

	/// <summary>
	/// Per-tile hash. The spatial hash alone repeats every four tiles in its low bits (x times an
	/// odd number keeps x's low bits), which is exactly where the look is read from — a visible
	/// lattice. One avalanche round (murmur's finaliser) spreads every input bit over the output.
	/// </summary>
	static uint TileHash( int floor, int x, int y )
	{
		var h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(floor * 83492791);
		h ^= h >> 16; h *= 0x7feb352du;
		h ^= h >> 15; h *= 0x846ca68bu;
		h ^= h >> 16;
		return h;
	}

	/// <summary>Which of the floor's sixteen slabs a tile wears: atlas cell from bits 0–1, UV turn from bits 2–3.</summary>
	static int TileLook( int floor, int x, int y )
	{
		var h = TileHash( floor, x, y );
		return (int)(h & 3) * 4 + (int)((h >> 2) & 3);
	}

	/// <summary>
	/// The resting tint of a tile, from its index alone so a reset can restore any tile without
	/// storing it: value within ±3 % of 0.97 and a slight warm/cool and green/magenta lean, so no
	/// two neighbours match exactly. The colour itself lives in the floor's textures; every channel
	/// stays at or below 1 because ModelRenderer.Tint wraps above it.
	/// </summary>
	public static Color TileColor( int floor, int x, int y )
	{
		var h = TileHash( floor, x, y ) >> 4;
		var value = 0.94f + (h & 0xFF) / 255f * 0.06f;
		var warmth = ((h >> 8) & 0xFF) / 255f * 2f - 1f;
		var lean = ((h >> 16) & 0xFF) / 255f * 2f - 1f;

		var r = 1f + warmth * TileHueJitter;
		var g = 1f + lean * TileHueJitter * 0.5f;
		var b = 1f - warmth * TileHueJitter;
		var scale = value / MathF.Max( r, MathF.Max( g, b ) );
		return new Color( r * scale, g * scale, b * scale );
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

		renderer.MaterialOverride = _warnMaterial;

		// Orange on every floor from the first frame — blending from the floor's own colour made a
		// teal tile go beige, which reads as nothing. Dim ember to full glow over the delay.
		var eased = t * t;
		var glow = WarnEmber + (WarnGlow - WarnEmber) * eased;
		renderer.Tint = new Color( glow, glow, glow );
		renderer.GameObject.LocalPosition = TileLocalPosition( floor, x, y ) + Vector3.Down * (6f * eased);
	}

	/// <summary>Undo <see cref="SetTileWarning"/>: the slab's own materials, resting colour and height.</summary>
	public void RestoreTile( int floor, int x, int y )
	{
		var renderer = _renderers?[floor, x, y];
		if ( renderer is null ) return;

		renderer.MaterialOverride = null;
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
		// Inside the rooms it only takes the edge off the far wall; outside it softens the tower against the sky.
		var fog = GameObject.AddComponent<GradientFog>();
		fog.Color = Palette.Haze;
		fog.StartDistance = 2500f;
		fog.EndDistance = 9000f;
		fog.FalloffExponent = 2f;
		fog.Height = 400f;
		fog.VerticalFalloffExponent = 1f;
	}

	/// <summary>
	/// Post-processing on the scene's one camera. Bloom is what makes the emissive lava and the
	/// roof glass glow; ambient occlusion darkens the seams between tiles, which is where the
	/// grid's readability lives.
	/// </summary>
	void ConfigureCamera()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

		camera.FieldOfView = 75f;
		camera.BackgroundColor = Palette.Sky;

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

	void CreateLava( Model boxModel, float span )
	{
		var z = LavaCenterZ;

		// Exactly the grid: it fills the shaft between the last floor's walls, whose faces stand on the grid edge.
		var lavaSpan = span;

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

		CreateLavaGlow( z );
	}

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
	/// Signage in the world: a plaque with the floor's number centred on every wall of every room,
	/// hung on the wall's middle pier facing into the room, so you can read your depth wherever you
	/// look; and the trophy over the tower — Kenney's gold medal and the one rule — for the menu orbit
	/// and for anyone looking up. No colliders.
	/// </summary>
	void CreateSigns()
	{
		var signs = new GameObject( GameObject, true, "Signs" );
		MakeLocalOnly( signs );

		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
		{
			var tileTop = -floor * Arena.FloorGap + Arena.TileThickness * 0.5f;
			for ( var side = 0; side < 4; side++ )
			{
				// One unit in front of the pier, which stands flush with the collider plane on the grid edge.
				var into = FloorContext.Out( side );
				var position = into * (FloorContext.HalfSpan - 1f) + Vector3.Up * (tileTop + PlaqueHeight);
				AddSign( signs, position, Rotation.LookAt( -into, Vector3.Up ), new Vector2( 420f, 420f ), 2.4f, floor, false );
			}
		}

		AddSign( signs, Vector3.Up * TrophyHeight, Rotation.Identity, new Vector2( 1500f, 1300f ), 4f, -1, true );
	}

	void AddSign( GameObject parent, Vector3 localPosition, Rotation localRotation, Vector2 panelSize, float scale, int floor, bool lookAtCamera )
	{
		var sign = new GameObject( parent, true, floor < 0 ? "Trophy" : $"Floor Sign {floor + 1}" );
		sign.LocalPosition = localPosition;
		sign.LocalRotation = localRotation;
		sign.LocalScale = new Vector3( scale );
		MakeLocalOnly( sign );

		var world = sign.AddComponent<WorldPanel>();
		world.PanelSize = panelSize;
		world.LookAtCamera = lookAtCamera;

		sign.AddComponent<ArenaSign>().Floor = floor;
	}

	static readonly Color CloudColor = new Color( 0.60f, 0.60f, 0.64f );

	/// <summary>
	/// One point light over the lava so the bottom floor is lit from below and the drop reads as
	/// dangerous rather than as an unlit void.
	///
	/// Parented to the arena, not to the lava: the lava slab carries a large non-uniform
	/// LocalScale, and a child would inherit it — both its offset and the light's radius would
	/// come out stretched.
	/// </summary>
	void CreateLavaGlow( float lavaZ )
	{
		var glow = new GameObject( GameObject, true, "Lava Glow" );
		glow.LocalPosition = new Vector3( 0f, 0f, lavaZ + LavaGlowHeight );
		MakeLocalOnly( glow );

		_lavaLight = glow.AddComponent<PointLight>();
		_lavaLight.LightColor = LavaGlowColor;
		_lavaLight.Radius = LavaGlowRadius;

		// Thousands of tiles between this light and the camera. Shadow-casting it would be the single
		// most expensive thing in the scene, and it lights an area nobody stands in.
		_lavaLight.Shadows = false;
	}

	/// <summary>
	/// Generated geometry is never serialised into the scene and never enters a network
	/// snapshot. Tile state travels as batched collapse indices instead (CLAUDE.md, decision 3).
	/// </summary>
	public static void MakeLocalOnly( GameObject go )
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
