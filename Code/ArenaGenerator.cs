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

	/// <summary>How much darker each floor is than the one above it.</summary>
	const float FloorDarkenStep = 0.15f;

	/// <summary>
	/// Matte dev PBR surface. Takes Tint cleanly and carries no printed texture, so a 56-unit
	/// tile doesn't turn into visual noise. Swap the number (00–100) to change roughness.
	///
	/// Deliberately NOT materials/default.vmat. That material is broken in this engine install:
	/// it asks for materials/default/default_mask_tga_3a4f4d17.generated.vtex, and core ships
	/// only the default_mask.tga source, never the compiled .vtex_c — so every renderer using it
	/// logs "Texture manager doesn't know about texture ... returning error texture". Harmless as
	/// one line, but arena-wide it fired once per tile, 4500 times.
	///
	/// This material's dependency set is complete on disk: dev_nonmetal_rough30.vmat_c plus its
	/// generated tcolor, tnormal and tambientocclusion.
	/// </summary>
	const string SurfaceMaterialPath = "materials/dev/dev_nonmetal_rough30.vmat";

	/// <summary>Height of the lava glow light above the slab.</summary>
	const float LavaGlowHeight = 120f;

	static readonly Color TileColorA = new Color( 0.66f, 0.68f, 0.72f );
	static readonly Color TileColorB = new Color( 0.40f, 0.42f, 0.47f );
	static readonly Color LavaColor = new Color( 0.90f, 0.24f, 0.05f );

	/// <summary>
	/// Above 1.0 on purpose — Light.LightColor carries intensity in its magnitude, so an HDR
	/// value is how you get a bright light rather than a merely orange one.
	/// </summary>
	static readonly Color LavaGlowColor = new Color( 2.4f, 0.70f, 0.15f );

	static readonly Color SunColor = new Color( 1.00f, 0.96f, 0.88f );
	static readonly Color SunSkyColor = new Color( 0.32f, 0.38f, 0.45f );

	GameObject[,,] _tiles;

	/// <summary>
	/// Min-X / min-Y corner of the grid, at floor 0's tile-centre height, in world space.
	/// Set in OnAwake — every OnAwake completes during load, so this is readable from any
	/// other component's OnStart regardless of component order.
	/// </summary>
	public Vector3 GridOrigin { get; private set; }

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

	void Generate()
	{
		_tiles = new GameObject[Arena.FloorCount, Arena.GridSize, Arena.GridSize];

		var span = Arena.GridSize * Arena.TileSize;

		var boxModel = Model.Load( "models/dev/box.vmdl" );
		var material = Material.Load( SurfaceMaterialPath );
		var tileScale = new Vector3( Arena.TileSize, Arena.TileSize, Arena.TileThickness ) / BoxModelUnits;

		for ( int floor = 0; floor < Arena.FloorCount; floor++ )
		{
			var shade = floor * FloorDarkenStep;
			var floorColorA = TileColorA.Darken( shade );
			var floorColorB = TileColorB.Darken( shade );

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
					renderer.MaterialOverride = material;
					renderer.Tint = (x + y) % 2 == 0 ? floorColorA : floorColorB;

					var collider = tile.AddComponent<BoxCollider>();
					collider.Scale = new Vector3( BoxModelUnits );
					collider.Static = true;

					_tiles[floor, x, y] = tile;
				}
			}
		}

		CreateLava( boxModel, material, span );
		ConfigureLighting();

		Log.Info( $"Arena: {Arena.FloorCount} floors x {Arena.GridSize}x{Arena.GridSize} = {Arena.FloorCount * Arena.GridSize * Arena.GridSize} tiles" );
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

		sun.WorldRotation = new Angles( 55f, 135f, 0f ).ToRotation();
		sun.LightColor = SunColor;
		sun.SkyColor = SunSkyColor;
		sun.Shadows = true;
	}

	void CreateLava( Model boxModel, Material material, float span )
	{
		var z = -((Arena.FloorCount - 1) * Arena.FloorGap) - Arena.LavaOffset;

		var lava = new GameObject( GameObject, true, "Lava" );
		lava.LocalPosition = new Vector3( 0f, 0f, z );
		lava.LocalScale = new Vector3( span, span, LavaThickness ) / BoxModelUnits;
		lava.Tags.Add( "lava" );
		MakeLocalOnly( lava );

		var renderer = lava.AddComponent<ModelRenderer>();
		renderer.Model = boxModel;
		renderer.MaterialOverride = material;
		renderer.Tint = LavaColor;

		// Trigger volume matches the rendered slab exactly — both scale off `span`, so a
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
	void CreateLavaGlow( float lavaZ, float span )
	{
		var glow = new GameObject( GameObject, true, "Lava Glow" );
		glow.LocalPosition = new Vector3( 0f, 0f, lavaZ + LavaGlowHeight );
		MakeLocalOnly( glow );

		var light = glow.AddComponent<PointLight>();
		light.LightColor = LavaGlowColor;
		light.Radius = span * 0.8f;

		// 4500 tiles between this light and the camera. Shadow-casting it would be the single
		// most expensive thing in the scene, and it lights an area nobody stands in.
		light.Shadows = false;
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
		// junction — a player stood there straddles four tiles, only one of which gets marked,
		// and the other three hold them up instead of letting them fall. SpawnRingRadius is
		// well inside the footprint, so these indices can't go out of range.
		var tileX = (int)((target.x - GridOrigin.x) / Arena.TileSize);
		var tileY = (int)((target.y - GridOrigin.y) / Arena.TileSize);

		return new Vector3(
			GridOrigin.x + (tileX + 0.5f) * Arena.TileSize,
			GridOrigin.y + (tileY + 0.5f) * Arena.TileSize,
			GridOrigin.z + Arena.TileThickness * 0.5f + 4f );
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

		var drop = (Arena.TileThickness * 0.5f - local.z) / Arena.FloorGap;
		if ( drop < -0.5f ) return false;

		floor = (int)(drop + 0.5f);
		return floor < Arena.FloorCount;
	}

	/// <summary>Re-enable every tile. Called on round restart.</summary>
	public void ResetAll()
	{
		// A round-state broadcast can land on a joining client before its own Generate() runs.
		if ( _tiles is null ) return;

		foreach ( var tile in _tiles )
		{
			tile.Enabled = true;
		}
	}
}
