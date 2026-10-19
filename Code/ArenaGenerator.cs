using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

	/// <summary>
	/// Resting tints a tile can wear. Each is a tinted copy of the floor's surfaces in every chunk, so
	/// each costs a draw call per chunk: four keep neighbours from matching without multiplying the
	/// draws (a chunk is one model, so the tint can't be the renderer's).
	/// </summary>
	const int TintCount = 4;

	/// <summary>Tiles per side of a <see cref="TileChunk"/>: nine chunks per floor.</summary>
	const int ChunkTiles = 10;
	const int ChunksPerSide = Arena.GridSize / ChunkTiles;

	GameObject[,,] _tiles;

	/// <summary>
	/// A tile's own renderer, made the first time it warns: while it glows and sags it is drawn on its
	/// own and hidden in its chunk. Null for a tile that never warned.
	/// </summary>
	ModelRenderer[,,] _renderers;

	/// <summary>True while a tile is drawn by its own renderer rather than its chunk.</summary>
	bool[,,] _warning;

	/// <summary>[floor, chunk x, chunk y]: what draws the tiles.</summary>
	TileChunk[,,] _chunks;

	/// <summary>The floor's surfaces in each resting tint, by surface and tint index.</summary>
	readonly Dictionary<(Material, int), Material> _tinted = new();

	/// <summary>One theme per floor, top to bottom; each holds the surfaces it painted for this build.</summary>
	ArenaTheme[] _themes;

	/// <summary>[floor, look]: every tile wears one of its floor's sixteen generated slabs, copied into its chunk.</summary>
	MeshKit[,] _tileSlabs;

	/// <summary>[floor, look]: the same slabs as models, made on first use for a warning tile's own renderer.</summary>
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
		if ( _bakes is not null ) PumpBuild();

		if ( !_lavaRenderer.IsValid() ) return;

		var t = Time.Now;
		var pulse = 0.5f + 0.3f * MathF.Sin( t * 1.3f ) + 0.2f * MathF.Sin( t * 3.1f + 1.7f );

		_lavaRenderer.Tint = Color.Lerp( LavaColor, LavaHotColor, pulse );
		_lavaLight.LightColor = LavaGlowColor * (0.8f + 0.35f * pulse);

		// The whole cloud ring turns very slowly — enough to feel alive behind a standing player.
		if ( _clouds.IsValid() )
			_clouds.LocalRotation = Rotation.FromYaw( t * 0.6f );
	}

	/// <summary>
	/// Builds the arena in two halves. Everything a player can touch is made here, at once: every tile
	/// with its collider, the tower's colliders, the lava, the light. Everything that is only seen
	/// streams in after (<see cref="PumpBuild"/>): the surfaces paint on worker threads while the game
	/// runs, and each part of the tower is built as soon as the surfaces it wears are in — the facade
	/// first, since the menu sees nothing else, then the rooms from the top down, where players spawn.
	/// Painting every surface used to hold the first frame for three to five seconds; now nothing waits
	/// on it.
	/// </summary>
	void Generate()
	{
		_buildClock = System.Diagnostics.Stopwatch.StartNew();

		_tiles = new GameObject[Arena.FloorCount, Arena.GridSize, Arena.GridSize];
		_renderers = new ModelRenderer[Arena.FloorCount, Arena.GridSize, Arena.GridSize];
		_warning = new bool[Arena.FloorCount, Arena.GridSize, Arena.GridSize];
		_chunks = new TileChunk[Arena.FloorCount, ChunksPerSide, ChunksPerSide];
		_tileSlabs = new MeshKit[Arena.FloorCount, TileLooks];
		_tileModels = new Model[Arena.FloorCount, TileLooks];

		var span = Arena.GridSize * Arena.TileSize;

		var boxModel = Model.Load( "models/dev/box.vmdl" );
		_glowMaterial = Material.Load( GlowMaterialPath );
		_warnMaterial = new Painter( 16, WarnColor, 1f ).Bake( "arena_tile_warning", GlowMaterialPath, 1f ).Upload();

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

					var collider = tile.AddComponent<BoxCollider>();
					collider.Scale = tileSize;
					collider.Static = true;

					_tiles[floor, x, y] = tile;
				}
			}
		}

		TowerBuilder.AddColliders( GameObject, LavaTopLocalZ );
		CreateLava( boxModel, span );
		CreateClouds();
		CreateSigns();
		ConfigureLighting();
		ConfigureCamera();

		// Last: eleven workers painting at full tilt slowed the tile loop above threefold.
		StartPainting();

		Log.Info( $"Arena: {Arena.FloorCount} floors x {Arena.GridSize}x{Arena.GridSize} = {Arena.FloorCount * Arena.GridSize * Arena.GridSize} tiles, playable in {_buildClock.ElapsedMilliseconds} ms" );
	}

	static float LavaTopLocalZ => LavaCenterZ + LavaThickness * 0.5f;

	/// <summary>
	/// Most surfaces painting at once: half the hardware threads. With all but one painting, the main
	/// thread (and the engine's own) were starved — uploads waited 400 ms for a frame, the menu stuttered
	/// and the arena took longer to dress (3.0 s against 2.0 s on the twelve-thread machine it is tuned
	/// on); more than twelve only held more canvases, tens of megabytes each (fix round 1 of Task 6b).
	/// </summary>
	static int BakeWorkers => Math.Clamp( (int)Sandbox.Engine.SystemInfo.ProcessorCount / 2, 1, 12 );

	/// <summary>How long a frame may spend uploading painted surfaces; at least one goes up every frame.</summary>
	const double UploadBudgetMs = 4.0;

	/// <summary>The streaming build: one job per surface, which part of the arena waits for it, and how far each has got.</summary>
	List<Func<PaintedSurface>> _bakes;
	List<Action<PaintedSurface>> _uploads;
	List<int> _gates;
	PaintedSurface[] _painted;
	int[] _paintedDone;
	bool[] _uploaded;
	int _nextJob = -1;
	int _surfaces;
	Task _painting;

	/// <summary>Set to 1 when the arena goes away mid-build, so the workers stop taking jobs.</summary>
	int _abandoned;

	/// <summary>
	/// Jobs still to upload per part: <see cref="FacadeGate"/> for the tower's own surfaces, then one per
	/// floor (gate 1 + floor). A floor waits for its theme's surfaces and for the ceiling of the room
	/// below it, which its tiles' undersides wear.
	/// </summary>
	int[] _gateLeft;
	const int FacadeGate = 0;

	TowerBuilder _tower;

	/// <summary>The next floor to dress; floors are built in order, one per frame.</summary>
	int _nextFloor;

	System.Diagnostics.Stopwatch _buildClock;

	/// <summary>
	/// Lists every surface of the arena as a job and sets workers painting them. Each surface is an
	/// independent job (its own canvas, its own seeded streams), so they paint side by side and come out
	/// identical whatever order they finish in. The jobs run in the order the arena needs them: the
	/// tower's own surfaces, then floor by floor from the top.
	/// </summary>
	void StartPainting()
	{
		_themes = new ArenaTheme[Arena.FloorCount];
		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
			_themes[floor] = ArenaTheme.Create( floor );

		_bakes = new List<Func<PaintedSurface>>();
		_uploads = new List<Action<PaintedSurface>>();
		_gates = new List<int>();
		_gateLeft = new int[1 + Arena.FloorCount];
		void Job( int gate, Func<PaintedSurface> bake, Action<PaintedSurface> upload )
		{
			_bakes.Add( bake );
			_uploads.Add( upload );
			_gates.Add( gate );
			_gateLeft[gate]++;
		}

		Job( FacadeGate, TowerBuilder.PaintFacade, baked => _facade = baked.Upload() );
		Job( FacadeGate, TowerBuilder.PaintFacadeDetail, baked => _facadeDetail = baked.Upload() );
		Job( FacadeGate, TowerBuilder.PaintPanes, baked => _roofPanes = baked.Upload() );

		foreach ( var theme in _themes )
		{
			// A room's ceiling is the undersides of the tiles above it, so it is the floor above that
			// waits for it; the greenhouse paints none, and the forge's is also the bottom floor's own.
			var ceilingGate = 1 + Math.Max( theme.Floor - 1, 0 );
			Job( ceilingGate, () => theme.Bake( ThemeSurface.Ceiling ), baked => theme.Apply( ThemeSurface.Ceiling, baked ) );

			var gate = 1 + theme.Floor;
			for ( var variant = 0; variant < theme.WallCount; variant++ )
			{
				var v = variant;
				Job( gate, () => theme.Bake( ThemeSurface.Wall, v ), baked => theme.Apply( ThemeSurface.Wall, baked, v ) );
			}
			foreach ( var surface in new[] { ThemeSurface.Floor, ThemeSurface.Trim, ThemeSurface.Glow } )
				Job( gate, () => theme.Bake( surface ), baked => theme.Apply( surface, baked ) );
		}

		_painted = new PaintedSurface[_bakes.Count];
		_paintedDone = new int[_bakes.Count];
		_uploaded = new bool[_bakes.Count];

		// Parallel's own workers rather than game tasks: a game task that runs for over a second without
		// yielding is reported as stuck, and one surface alone can take that long.
		_painting = Sandbox.Utility.Parallel.ForAsync( 0, Math.Min( _bakes.Count, BakeWorkers ), default, ( _, _ ) =>
		{
			PaintJobs();
			return ValueTask.CompletedTask;
		} );
	}

	/// <summary>A worker: takes the next unpainted job until none are left. Touches no engine resource.</summary>
	void PaintJobs()
	{
		var bakes = _bakes;
		var painted = _painted;
		var done = _paintedDone;
		for ( var i = Interlocked.Increment( ref _nextJob ); i < bakes.Count && Interlocked.CompareExchange( ref _abandoned, 0, 0 ) == 0; i = Interlocked.Increment( ref _nextJob ) )
		{
			painted[i] = bakes[i]();
			Interlocked.Exchange( ref done[i], 1 );
		}
	}

	/// <summary>
	/// Once a frame on the main thread while the arena streams in: uploads what the workers have
	/// painted (textures and materials are engine resources, made on the main thread only) within
	/// <see cref="UploadBudgetMs"/>, then builds at most one part whose surfaces are all in.
	/// </summary>
	void PumpBuild()
	{
		var started = System.Diagnostics.Stopwatch.GetTimestamp();
		for ( var i = 0; i < _bakes.Count; i++ )
		{
			if ( _uploaded[i] || Interlocked.CompareExchange( ref _paintedDone[i], 0, 0 ) == 0 ) continue;

			if ( _painted[i] is not null ) _surfaces++;
			_uploads[i]( _painted[i] );
			_painted[i] = null;
			_uploaded[i] = true;
			_gateLeft[_gates[i]]--;

			if ( System.Diagnostics.Stopwatch.GetElapsedTime( started ).TotalMilliseconds > UploadBudgetMs ) break;
		}

		if ( _painting.IsFaulted )
		{
			Log.Error( _painting.Exception );
			EndBuild();
			return;
		}

		if ( _tower is null )
		{
			if ( _gateLeft[FacadeGate] > 0 ) return;
			_tower = new TowerBuilder( GameObject, _themes, _facade, _facadeDetail, _roofPanes, LavaTopLocalZ, LavaCenterZ - LavaThickness * 0.5f );
			_tower.BuildFacades();
			return;
		}

		if ( _gateLeft[1 + _nextFloor] > 0 ) return;
		DressFloor( _nextFloor++ );

		if ( _nextFloor < Arena.FloorCount ) return;
		_tower.EnableReflections();
		Log.Info( $"Arena: {_surfaces} surfaces painted and every floor dressed {_buildClock.ElapsedMilliseconds} ms after the build started" );
		EndBuild();
	}

	/// <summary>Drops the job lists: their closures would otherwise outlive the build (and trip the editor's hotload).</summary>
	void EndBuild()
	{
		_bakes = null;
		_uploads = null;
		_gates = null;
		_painted = null;
		_painting = null;
	}

	/// <summary>
	/// A floor's visible half: its sixteen tile slabs, the chunks that draw its tiles (starting from
	/// whatever has already collapsed or is warning), and its room.
	/// </summary>
	void DressFloor( int floor )
	{
		// The underside of a floor is the ceiling of the room below it; the bottom floor's
		// undersides face the lava shaft and wear the forge's own rock.
		var below = _themes[Math.Min( floor + 1, Arena.FloorCount - 1 )];
		for ( var look = 0; look < TileLooks; look++ )
			_tileSlabs[floor, look] = BuildTileSlab( _themes[floor], below, look / 4, look % 4 );

		for ( var cx = 0; cx < ChunksPerSide; cx++ )
			for ( var cy = 0; cy < ChunksPerSide; cy++ )
				_chunks[floor, cx, cy] = new TileChunk( ChunkTiles * ChunkTiles );

		for ( var x = 0; x < Arena.GridSize; x++ )
		{
			for ( var y = 0; y < Arena.GridSize; y++ )
			{
				var tint = TileTintIndex( floor, x, y );
				var chunk = ChunkOf( floor, x, y );
				chunk.Add( ChunkSlot( x, y ), _tileSlabs[floor, TileLook( floor, x, y )], TileLocalPosition( floor, x, y ), surface => Tinted( surface, tint ) );
				chunk.SetShown( ChunkSlot( x, y ), _tiles[floor, x, y].Enabled && !_warning[floor, x, y] );
			}
		}

		for ( var cx = 0; cx < ChunksPerSide; cx++ )
			for ( var cy = 0; cy < ChunksPerSide; cy++ )
				_chunks[floor, cx, cy].Build( GameObject, $"Tiles {floor} {cx} {cy}" );

		_tower.BuildFloor( floor );
	}

	protected override void OnDestroy()
	{
		Interlocked.Exchange( ref _abandoned, 1 );
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
	static MeshKit BuildTileSlab( ArenaTheme self, ArenaTheme below, int cell, int turn )
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

		return kit;
	}

	TileChunk ChunkOf( int floor, int x, int y ) => _chunks[floor, x / ChunkTiles, y / ChunkTiles];

	static int ChunkSlot( int x, int y ) => (x % ChunkTiles) * ChunkTiles + y % ChunkTiles;

	/// <summary>
	/// A copy of one of the floor's surfaces in resting tint <paramref name="tint"/>, through the
	/// material's own colour tint: the copy shares the surface's textures, so it costs no memory.
	/// </summary>
	Material Tinted( Material surface, int tint )
	{
		if ( _tinted.TryGetValue( (surface, tint), out var material ) ) return material;

		material = surface.CreateCopy( $"{surface.Name}_tint{tint}" );
		material.Set( "g_vColorTint", TileTint( tint ) );
		_tinted[(surface, tint)] = material;
		return material;
	}

	/// <summary>
	/// Keeps the chunks in step with the tiles once a frame, before anything is drawn: a tile is drawn
	/// by its chunk while it stands and isn't warning. Reading the flags back rather than being told
	/// keeps every way a tile goes down or comes back (decay, abilities, snapshots, resets) on the one
	/// rule of CLAUDE.md — a collapsed tile is a disabled GameObject.
	/// </summary>
	protected override void OnPreRender()
	{
		if ( _chunks is null ) return;

		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
		{
			if ( _chunks[floor, 0, 0] is null ) continue;

			for ( var cx = 0; cx < ChunksPerSide; cx++ )
				for ( var cy = 0; cy < ChunksPerSide; cy++ )
				{
					var chunk = _chunks[floor, cx, cy];
					var changed = false;
					for ( var x = cx * ChunkTiles; x < (cx + 1) * ChunkTiles; x++ )
						for ( var y = cy * ChunkTiles; y < (cy + 1) * ChunkTiles; y++ )
							changed |= chunk.SetShown( ChunkSlot( x, y ), _tiles[floor, x, y].Enabled && !_warning[floor, x, y] );

					if ( changed ) chunk.Upload();
				}
		}
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

	/// <summary>Which of the <see cref="TintCount"/> resting tints a tile wears, from bits 4–5 of its hash (the look takes 0–3).</summary>
	static int TileTintIndex( int floor, int x, int y ) => (int)((TileHash( floor, x, y ) >> 4) & (TintCount - 1));

	/// <summary>
	/// Resting tint <paramref name="tint"/>: lighter or darker (value 0.955 or 0.985), warm or cool,
	/// with a slight green/magenta lean, so neighbours rarely match. The colour itself lives in the
	/// floor's textures; every channel stays at or below 1.
	/// </summary>
	static Color TileTint( int tint )
	{
		var value = 0.94f + ((tint & 1) == 0 ? 0.25f : 0.75f) * 0.06f;
		var warmth = (tint & 2) == 0 ? -0.6f : 0.6f;
		var lean = ((tint ^ (tint >> 1)) & 1) == 0 ? -0.5f : 0.5f;

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
		if ( _tiles is null ) return;

		// Not dressed yet: the floor's tiles are still waiting for their surfaces.
		var look = TileLook( floor, x, y );
		if ( _tileSlabs[floor, look] is null ) return;

		var renderer = _renderers[floor, x, y];
		if ( renderer is null )
		{
			renderer = _renderers[floor, x, y] = _tiles[floor, x, y].AddComponent<ModelRenderer>();
			renderer.Model = _tileModels[floor, look] ??= _tileSlabs[floor, look].Build( $"arena_tile_{floor}_{look}" );
			renderer.MaterialOverride = _warnMaterial;
		}

		_warning[floor, x, y] = true;
		renderer.Enabled = true;

		// Orange on every floor from the first frame — blending from the floor's own colour made a
		// teal tile go beige, which reads as nothing. Dim ember to full glow over the delay.
		var eased = t * t;
		var glow = WarnEmber + (WarnGlow - WarnEmber) * eased;
		renderer.Tint = new Color( glow, glow, glow );
		renderer.GameObject.LocalPosition = TileLocalPosition( floor, x, y ) + Vector3.Down * (6f * eased);
	}

	/// <summary>Undo <see cref="SetTileWarning"/>: back in its chunk, at its resting height.</summary>
	public void RestoreTile( int floor, int x, int y )
	{
		if ( _tiles is null || !_warning[floor, x, y] ) return;

		_warning[floor, x, y] = false;
		_renderers[floor, x, y].Enabled = false;
		_tiles[floor, x, y].LocalPosition = TileLocalPosition( floor, x, y );
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
		_lavaRenderer.MaterialOverride = CreateLavaMaterial( GlowMaterialPath );
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

	/// <summary>The lava's surface: the user's Kenney blob pattern from the project.</summary>
	const string LavaTexturePath = "textures/kenney/lava_blobs.png";

	/// <summary>Logical pixels per side of the generated crust the lava falls back to.</summary>
	const int CrustArtSize = 16;

	/// <summary>Texture pixels per logical crust pixel. Upscaling in code keeps the pixels crisp.</summary>
	const int CrustUpscale = 8;

	const int CrustSize = CrustArtSize * CrustUpscale;

	/// <summary>
	/// Cracked crust for the lava, on a copy of the emissive material: bright seams between dark
	/// plates, so the slab glows through its cracks instead of as one flat orange sheet. Greyscale,
	/// so <see cref="LavaColor"/> in the tint carries the colour and its pulse. Built once per arena
	/// and held by the renderer — never cached in a static, which would outlive a hotload.
	/// </summary>
	static Material CreateLavaMaterial( string baseMaterial )
	{
		// The Kenney blob pattern when it loads; the generated crust otherwise. Both tile seamlessly.
		var material = LoadLavaTexture( baseMaterial ) ?? CreateCrustMaterial( LavaPixels( 4242 ), baseMaterial );

		// The slab is one box ~1900 units across; repeat the 16-pixel crust so a crack is a crack,
		// not a 200-unit stripe. LavaPixels wraps, so the repeats meet without seams.
		material.Set( "g_vTexCoordScale", new Vector2( 10f, 10f ) );
		return material;
	}

	/// <summary>
	/// A copy of <paramref name="baseMaterial"/> wearing the lava texture file, or null if it didn't
	/// load — the caller falls back to the generated crust, so a missing file degrades the look
	/// rather than painting the lava in the engine's error checkerboard.
	/// </summary>
	static Material LoadLavaTexture( string baseMaterial )
	{
		var texture = Texture.LoadFromFileSystem( LavaTexturePath, FileSystem.Mounted, true );
		if ( texture is null || texture.IsError )
		{
			Log.Warning( $"ArenaGenerator: couldn't load {LavaTexturePath}, using the generated crust instead" );
			return null;
		}

		var material = Material.Load( baseMaterial ).CreateCopy( "tnt_lava" );
		material.Set( "Color", texture );
		material.Set( "g_flModelTintAmount", 1f );
		return material;
	}

	/// <summary>The generated crust as a material: each logical pixel upscaled to a crisp block.</summary>
	static Material CreateCrustMaterial( float[,] art, string baseMaterial )
	{
		var data = new byte[CrustSize * CrustSize * 4];

		for ( var y = 0; y < CrustSize; y++ )
		{
			for ( var x = 0; x < CrustSize; x++ )
			{
				var v = (byte)(art[x / CrustUpscale, y / CrustUpscale].Clamp( 0f, 1f ) * 255f);
				var i = (y * CrustSize + x) * 4;

				data[i + 0] = v;
				data[i + 1] = v;
				data[i + 2] = v;
				data[i + 3] = 255;
			}
		}

		var texture = Texture.Create( CrustSize, CrustSize, ImageFormat.RGBA8888 )
			.WithName( "tnt_lava" )
			.WithData( data )
			.Finish();

		// A copy of the emissive dev surface rather than a bare Material.Create: the copy keeps every
		// parameter that already works and only the colour texture changes. Full model tint, so the
		// lava colour isn't washed toward grey.
		var material = Material.Load( baseMaterial ).CreateCopy( "tnt_lava" );
		material.Set( "Color", texture );
		material.Set( "g_flModelTintAmount", 1f );
		return material;
	}

	/// <summary>
	/// Lava crust: scattered cell centres, and each pixel's brightness rises as it nears the border
	/// between its two closest cells — a cheap Voronoi crack pattern. Wraps at the edges so the
	/// slab can tile without seams.
	/// </summary>
	static float[,] LavaPixels( int seed )
	{
		var random = new System.Random( seed );
		var cells = new Vector2[10];

		for ( var i = 0; i < cells.Length; i++ )
			cells[i] = new Vector2( (float)random.NextDouble() * CrustArtSize, (float)random.NextDouble() * CrustArtSize );

		var art = new float[CrustArtSize, CrustArtSize];

		for ( var y = 0; y < CrustArtSize; y++ )
		{
			for ( var x = 0; x < CrustArtSize; x++ )
			{
				var nearest = float.MaxValue;
				var second = float.MaxValue;

				foreach ( var cell in cells )
				{
					// Toroidal distance, so the pattern wraps.
					var dx = MathX.Clamp( x + 0.5f - cell.x, -CrustArtSize, CrustArtSize );
					var dy = MathX.Clamp( y + 0.5f - cell.y, -CrustArtSize, CrustArtSize );
					if ( dx > CrustArtSize * 0.5f ) dx -= CrustArtSize;
					if ( dx < -CrustArtSize * 0.5f ) dx += CrustArtSize;
					if ( dy > CrustArtSize * 0.5f ) dy -= CrustArtSize;
					if ( dy < -CrustArtSize * 0.5f ) dy += CrustArtSize;

					var d = dx * dx + dy * dy;
					if ( d < nearest ) { second = nearest; nearest = d; }
					else if ( d < second ) second = d;
				}

				var edge = MathF.Sqrt( second ) - MathF.Sqrt( nearest );
				art[x, y] = edge < 1.1f ? 1.0f : 0.38f + (float)random.NextDouble() * 0.08f;
			}
		}

		return art;
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
