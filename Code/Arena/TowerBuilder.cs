using System;

/// <summary>
/// Builds the tower around the tiles: per floor a wall segment on all four sides — wall face, slab
/// edge, the theme's structural front layer, the outer facade — and on top the greenhouse's glass
/// roof; then four static box colliders that run the whole height of the shaft. Each floor is one
/// model (one draw call per material) on its own GameObject; everything is local presentation,
/// rebuilt identically on every machine from constants.
///
/// The segments stack: a floor's wall runs from its tile underside (the lava top, for the last floor)
/// up to the next floor's underside, so looking down a hole by a wall shows the room below, never sky.
/// </summary>
public sealed class TowerBuilder
{
	/// <summary>World units per repeat of the facade stone: about a storey, so courses never line up with the floors.</summary>
	const float FacadeRepeat = 256f;

	const int FacadeSize = 512;
	const int DetailSize = 512;
	const int PaneSize = 256;

	/// <summary>Windows per side per floor on the facade, their size and their sill above the tile top.</summary>
	const int WindowsPerSide = 7;
	const float WindowWidth = 60f;
	const float WindowHeight = 96f;
	const float WindowSill = 58f;

	/// <summary>How far the facade's frames, sills and courses stand proud of the stone.</summary>
	const float Proud = 5f;

	/// <summary>Greenhouse roof: eave beam on the wall tops, the glass rising to a ridge along X.</summary>
	const float EaveHeight = 8f;
	const float RoofRise = 40f;
	const float BarSpacing = (FloorContext.Span + 2f * FloorContext.WallThickness) / 43f;
	const float PaneLength = 150f;

	/// <summary>Self-lit frosted glass: dimmed so it reads as bright overcast sky rather than blowing out under bloom.</summary>
	static Color PaneTint => new( 0.34f, 0.37f, 0.39f );

	/// <summary>
	/// Detail atlas cells (4 × 4 of 128 px): 0–7 frame paint per floor, dressed stone, three tones of
	/// glass, a dark pane (an unlit room), a warmly lit one, a slatted blind and a linen curtain.
	/// </summary>
	const int StoneCell = 9;
	static readonly int[] GlassCells = { 8, 10, 11 };
	const int DarkCell = 12;
	const int LitCell = 13;
	const int BlindCell = 14;
	const int CurtainCell = 15;

	readonly GameObject _arena;
	readonly ArenaTheme[] _themes;
	readonly Material _facade;
	readonly Material _detail;
	readonly Material _panes;

	public TowerBuilder( GameObject arena, ArenaTheme[] themes, Material facade, Material detail, Material panes )
	{
		_arena = arena;
		_themes = themes;
		_facade = facade;
		_detail = detail;
		_panes = panes;
	}

	/// <summary>Builds every floor. Heights are local to the arena; the lava top is where the last floor's wall ends.</summary>
	public void Build( float lavaTopZ, float lavaBottomZ )
	{
		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
			BuildFloor( floor, lavaTopZ, lavaBottomZ );

		AddColliders( lavaTopZ, Arena.TileThickness * 0.5f + FloorContext.ClearHeight );
	}

	void BuildFloor( int floor, float lavaTopZ, float lavaBottomZ )
	{
		var last = floor == Arena.FloorCount - 1;
		var tileTop = -floor * Arena.FloorGap + Arena.TileThickness * 0.5f;

		var root = new GameObject( _arena, true, $"Floor {floor} Shell" );
		ArenaGenerator.MakeLocalOnly( root );

		var room = new FloorContext
		{
			Floor = floor,
			TileTopZ = tileTop,
			CeilingZ = tileTop + FloorContext.ClearHeight,
			BottomZ = last ? lavaTopZ : tileTop - Arena.TileThickness,
			Kit = new MeshKit(),
			Theme = _themes[floor],
			Root = root,
		};

		BuildShell( room );
		BuildFacade( room, lavaTopZ, lavaBottomZ );
		room.Theme.Dress( room );
		if ( floor == 0 ) BuildRoof( room );

		var renderer = root.AddComponent<ModelRenderer>();
		renderer.Model = room.Kit.Build( $"tower_floor{floor}" );
	}

	/// <summary>
	/// The structural shell in the theme's surfaces: the slab edge — a ledge at the tile line whose
	/// front face fills the strip between this floor's tiles and the cornice of the room below. The
	/// last floor's segment runs on down the lava shaft as a plain face on the grid edge, so the lava,
	/// which fits the grid exactly, meets the wall without a gap. The wall face itself is laid by the
	/// theme's front layer, in lengths that meet behind its posts.
	/// </summary>
	void BuildShell( FloorContext room )
	{
		var theme = room.Theme;
		var stock = ArenaTheme.TrimRegion( TrimBand.Stock );
		var slab = ArenaTheme.TrimRegion( TrimBand.SlabEdge );
		var bottom = room.BottomZ - room.TileTopZ;
		const float Repeat = ArenaTheme.WallRepeat;
		const float Span = FloorContext.Span;
		const float Recess = FloorContext.Recess;

		for ( var side = 0; side < 4; side++ )
		{
			var offset = (float)new Random( 9001 + room.Floor * 4 + side ).NextDouble() * Repeat;
			Vector2 WallUv( float u, float v ) => new( (u + offset) / Repeat, (Repeat - v) / Repeat );

			const float Deep = Recess + FloorContext.Embed;
			var (u0, u1) = side % 2 == 0 ? (-Deep, Span + Deep) : (0f, Span);
			room.WallBox( side, theme.Trim, slab, stock, false, u0, u1, -Arena.TileThickness, FloorContext.LedgeTop, 0f, Deep, BoxFaces.Front | BoxFaces.Top );

			if ( room.BottomZ < room.TileTopZ - Arena.TileThickness )
				room.WallFace( side, theme.Wall, 0f, Span, bottom, -Arena.TileThickness, 0f, false, WallUv );
		}
	}

	/// <summary>
	/// The outside: one calm stone face per side, a band of windows framed in the floor's identity
	/// colour with dark glass, a dressed course on every floor line, a crowning cornice on top and a
	/// plinth under the last floor that hides the lava slab's edge.
	/// </summary>
	void BuildFacade( FloorContext room, float lavaTopZ, float lavaBottomZ )
	{
		const float Span = FloorContext.Span;
		const float Thick = FloorContext.WallThickness;
		var bottom = room.BottomZ - room.TileTopZ;
		var stone = new Skin( _detail, Cell( StoneCell ) );
		var frame = new Skin( _detail, Cell( room.Floor ) );
		var last = room.Floor == Arena.FloorCount - 1;

		for ( var side = 0; side < 4; side++ )
		{
			room.WallFace( side, _facade, -Thick, Span + Thick, bottom, FloorContext.ClearHeight, Thick, true,
				( u, v ) => new Vector2( (Span - u) / FacadeRepeat, -(room.TileTopZ + v) / FacadeRepeat ) );

			var facing = room.WallFacing( side );
			for ( var w = 0; w < WindowsPerSide; w++ )
			{
				var u = (w + 0.5f) * Span / WindowsPerSide;
				AddWindow( room, side, u, facing, frame, stone, new Random( 5101 + room.Floor * 211 + side * 37 + w ) );
			}

			// Courses and cornices: sides 0 and 2 run through the corners, 1 and 3 stop at them.
			if ( room.Floor == 0 )
				Course( room, side, FloorContext.ClearHeight - 12f, FloorContext.ClearHeight + EaveHeight + 2f, 12f, stone );
			if ( !last )
				Course( room, side, -Arena.TileThickness - 7f, -Arena.TileThickness + 7f, 8f, stone );
			else
				Course( room, side, lavaBottomZ - 12f - room.TileTopZ, lavaTopZ + 24f - room.TileTopZ, 10f, stone );
		}

		// The underside of the plinth, so the tower ends in stone from any angle.
		if ( last )
		{
			var h = FloorContext.HalfSpan + Thick + 10f;
			var z = lavaBottomZ - 12f;
			room.Kit.Quad( stone, new Vector3( -h, h, z ), new Vector3( h, h, z ), new Vector3( h, -h, z ), new Vector3( -h, -h, z ) );
		}
	}

	/// <summary>A dressed stone band standing <paramref name="proud"/> off the facade between heights v0 and v1.</summary>
	static void Course( FloorContext room, int side, float v0, float v1, float proud, Skin stone )
	{
		const float Thick = FloorContext.WallThickness;
		var reach = side % 2 == 0 ? Thick + proud : Thick;
		var length = FloorContext.Span + reach * 2f;
		var centre = room.OnWall( side, FloorContext.HalfSpan, (v0 + v1) * 0.5f, Thick + proud * 0.5f );
		room.Kit.Box( stone, centre, new Vector3( proud, length, v1 - v0 ), room.WallFacing( side ) );
	}

	/// <summary>
	/// One facade window: frame, a cross of glazing bars and a stone sill standing proud of the wall,
	/// dark glass a unit in front of the stone. The rotation's Forward points into the room, so the
	/// boxes' local X is depth, Y runs along the wall and Z is up.
	///
	/// The grid of windows is regular, as real fenestration is; what differs is behind the glass
	/// (ruling R25), drawn per window from its own seeded stream: one of three glass tones, mirrored
	/// or not so the reflection falls differently, now and then an unlit room or a warmly lit one, and
	/// in some a blind or a curtain let down to its own height. All of it quiet — the facade stays calm.
	/// </summary>
	void AddWindow( FloorContext room, int side, float u, Rotation facing, Skin frame, Skin stone, Random random )
	{
		const float Thick = FloorContext.WallThickness;
		var kit = room.Kit;
		var top = WindowSill + WindowHeight;
		var middle = WindowSill + WindowHeight * 0.5f;
		const float Rail = 7f;

		Vector3 At( float du, float v, float proud ) => room.OnWall( side, u + du, v, Thick + proud * 0.5f );

		kit.BevelBox( frame, At( 0f, top - Rail * 0.5f, Proud ), new Vector3( Proud, WindowWidth, Rail ), 0.8f, facing );
		kit.BevelBox( frame, At( 0f, WindowSill + Rail * 0.5f, Proud ), new Vector3( Proud, WindowWidth, Rail ), 0.8f, facing );
		kit.BevelBox( frame, At( -(WindowWidth - Rail) * 0.5f, middle, Proud ), new Vector3( Proud, Rail, WindowHeight - Rail * 2f ), 0.8f, facing );
		kit.BevelBox( frame, At( (WindowWidth - Rail) * 0.5f, middle, Proud ), new Vector3( Proud, Rail, WindowHeight - Rail * 2f ), 0.8f, facing );

		var transom = WindowSill + WindowHeight * 0.62f;
		kit.BevelBox( frame, At( 0f, middle, Proud * 0.6f ), new Vector3( Proud * 0.6f, 2.5f, WindowHeight - Rail * 2f ), 0.5f, facing );
		kit.BevelBox( frame, At( 0f, transom, Proud * 0.6f ), new Vector3( Proud * 0.6f, WindowWidth - Rail * 2f, 2.5f ), 0.5f, facing );

		kit.BevelBox( stone, At( 0f, WindowSill - 2.5f, Proud + 3f ), new Vector3( Proud + 3f, WindowWidth + 12f, 5f ), 1f, facing );

		var pick = random.NextDouble();
		var glass = pick < 0.07 ? DarkCell : pick < 0.13 ? LitCell : GlassCells[random.Next( GlassCells.Length )];
		var mirror = random.NextDouble() < 0.5;

		var cover = random.NextDouble();
		var screen = cover < 0.3 ? BlindCell : cover < 0.45 ? CurtainCell : -1;
		var drop = screen < 0 ? 0f : 0.15f + (float)random.NextDouble() * 0.5f;

		// Glass below the blind's foot, the blind above it, on one plane and never overlapping.
		var bottom = WindowSill + Rail;
		var high = top - Rail;
		var foot = high - (high - bottom) * drop;
		Pane( room, side, Cell( glass ), u, Rail, bottom, foot, 1f - drop, mirror );
		if ( screen >= 0 ) Pane( room, side, Cell( screen ), u, Rail, foot, high, drop, mirror );
	}

	/// <summary>
	/// The lower <paramref name="fraction"/> of an atlas cell fitted to one strip (v0..v1) of a window's
	/// opening, facing out: a part of the picture keeps its scale, so a blind let half down shows its
	/// bottom rail and half its slats rather than all of them squeezed.
	/// </summary>
	void Pane( FloorContext room, int side, Rect cell, float u, float rail, float v0, float v1, float fraction, bool mirror )
	{
		var half = WindowWidth * 0.5f - rail;
		room.WallFace( side, _detail, u - half, u + half, v0, v1, FloorContext.WallThickness + 1f, true, ( pu, pv ) =>
		{
			var x = (u + half - pu) / (half * 2f);
			if ( mirror ) x = 1f - x;
			var y = 1f - fraction + fraction * (v1 - pv) / (v1 - v0);
			return new Vector2( cell.Left + x * cell.Width, cell.Top + y * cell.Height );
		} );
	}

	/// <summary>
	/// The greenhouse roof on floor 0: an eave beam capping the wall tops all round, then a lattice of
	/// glazing bars pitched up <see cref="RoofRise"/> to a ridge along X — light bars, a heavier rafter
	/// every sixth, purlins under them — closed at both gables. The bars are painted stock of the
	/// greenhouse's own trim and cast shadows; the panes are opaque frosted glass on a separate renderer
	/// that casts none, so the sun still lights the room in stripes (controller ruling R6 — no working
	/// translucency in this engine build).
	/// </summary>
	void BuildRoof( FloorContext room )
	{
		var kit = room.Kit;
		var trim = room.Theme.Trim;
		var stock = ArenaTheme.TrimRegion( TrimBand.Stock );
		var paint = new Skin( trim, stock );
		const float Span = FloorContext.Span;
		const float Thick = FloorContext.WallThickness;
		var outer = FloorContext.HalfSpan + Thick;
		var eaveTop = room.CeilingZ + EaveHeight;
		var ridge = room.CeilingZ + RoofRise;

		for ( var side = 0; side < 4; side++ )
		{
			var (u0, u1) = side % 2 == 0 ? (-Thick, Span + Thick) : (0f, Span);
			room.WallBox( side, trim, stock, stock, false, u0, u1, FloorContext.ClearHeight, FloorContext.ClearHeight + EaveHeight, 0f, Thick,
				BoxFaces.Front | BoxFaces.Top | BoxFaces.Back );
		}

		// The glass plane falls from the ridge (y = 0) to the eave tops (|y| = outer); bars hang under it.
		float Plane( float y ) => ridge - (ridge - eaveTop) * MathF.Abs( y ) / outer;

		var panes = new MeshKit();
		var slopeLength = MathF.Sqrt( outer * outer + (ridge - eaveTop) * (ridge - eaveTop) );
		for ( var sign = -1; sign <= 1; sign += 2 )
		{
			var normal = new Vector3( 0f, sign * (ridge - eaveTop), outer ).Normal;
			var slope = Rotation.LookAt( Vector3.Forward, normal );
			var midY = sign * outer * 0.5f;

			var bars = (int)MathF.Round( (outer * 2f) / BarSpacing );
			for ( var k = 1; k < bars; k++ )
			{
				var x = -outer + k * BarSpacing;
				var heavy = k % 6 == 0;
				var depth = heavy ? 9f : 5f;
				var centre = new Vector3( x, midY, Plane( midY ) ) - normal * (depth * 0.5f);
				kit.BevelBox( paint, centre, new Vector3( heavy ? 6f : 3f, slopeLength, depth ), heavy ? 1f : 0.6f, slope );
			}

			foreach ( var along in new[] { 0.3f, 0.64f } )
			{
				var y = sign * outer * along;
				var centre = new Vector3( 0f, y, Plane( y ) ) - normal * 7.5f;
				kit.BevelBox( paint, centre, new Vector3( outer * 2f, 5f, 6f ), 0.8f, slope );
			}

			// The slope's glass, a hair above the bars: seen from inside (facing down) and from the menu orbit (facing up).
			var lift = normal * 0.3f;
			var a = new Vector3( -outer, sign * outer, eaveTop ) + lift;
			var b = new Vector3( outer, sign * outer, eaveTop ) + lift;
			var c = new Vector3( outer, 0f, ridge ) + lift;
			var d = new Vector3( -outer, 0f, ridge ) + lift;
			var uvA = new Vector2( 0f, 0f ); var uvB = new Vector2( outer * 2f / BarSpacing, 0f );
			var uvC = new Vector2( outer * 2f / BarSpacing, slopeLength / PaneLength ); var uvD = new Vector2( 0f, slopeLength / PaneLength );
			QuadFacing( panes, _panes, -normal, a, b, c, d, uvA, uvB, uvC, uvD );
			QuadFacing( panes, _panes, normal, a, b, c, d, uvA, uvB, uvC, uvD );
		}

		kit.BevelBox( paint, new Vector3( 0f, 0f, ridge - 5f ), new Vector3( outer * 2f, 10f, 10f ), 1.2f );

		// Gables: a triangle of glass at each end, with upright bars.
		var glass = new Skin( _panes, BarSpacing );
		for ( var end = -1; end <= 1; end += 2 )
		{
			var x = end * (outer - 1.5f);
			var left = new Vector3( x, -outer, eaveTop );
			var right = new Vector3( x, outer, eaveTop );
			var apex = new Vector3( x, 0f, ridge );
			panes.Tri( glass, left, right, apex );
			panes.Tri( glass, right, left, apex );

			for ( var y = -outer + 84f; y < outer - 40f; y += 84f )
			{
				var height = Plane( y ) - eaveTop;
				kit.BevelBox( paint, new Vector3( end * (outer - 2f), y, eaveTop + height * 0.5f ), new Vector3( 4f, 3f, height ), 0.6f );
			}
		}

		var holder = new GameObject( room.Root, true, "Roof Panes" );
		ArenaGenerator.MakeLocalOnly( holder );
		var renderer = holder.AddComponent<ModelRenderer>();
		renderer.Model = panes.Build( "tower_roof_panes" );
		renderer.Tint = PaneTint;
		renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
	}

	/// <summary>An explicit-UV quad wound to face <paramref name="normal"/> whatever order the corners came in.</summary>
	static void QuadFacing( MeshKit kit, Material material, Vector3 normal, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD )
	{
		if ( Vector3.Dot( Vector3.Cross( b - a, d - a ), normal ) >= 0f ) kit.Quad( material, a, b, c, d, uvA, uvB, uvC, uvD );
		else kit.Quad( material, d, c, b, a, uvD, uvC, uvB, uvA );
	}

	/// <summary>
	/// One solid static box per side for the whole shaft, from the lava top to floor 0's ceiling, inner
	/// face exactly on the grid edge (centre at depth +32, 64 thick), long enough to close the corners.
	/// One box rather than one per floor, so a player sliding down the wall meets no seam at a floor
	/// line to snag on (ruling R24). Never a mesh collider: the player and the camera boom slide along
	/// a box face. A lid over the roof too, so a launch can't carry anyone over the wall tops and the
	/// camera boom pulls in under the glass.
	/// </summary>
	void AddColliders( float bottomZ, float topZ )
	{
		var holder = new GameObject( _arena, true, "Tower Walls" );
		ArenaGenerator.MakeLocalOnly( holder );

		const float Thick = FloorContext.WallThickness;
		var height = topZ - bottomZ;
		var middle = (topZ + bottomZ) * 0.5f;
		var reach = FloorContext.HalfSpan + Thick * 0.5f;
		var length = FloorContext.Span + Thick * 2f;

		for ( var side = 0; side < 4; side++ )
		{
			var collider = holder.AddComponent<BoxCollider>();
			collider.Center = FloorContext.Out( side ) * reach + Vector3.Up * middle;
			collider.Scale = side % 2 == 0 ? new Vector3( Thick, length, height ) : new Vector3( length, Thick, height );
			collider.Static = true;
		}

		var lid = holder.AddComponent<BoxCollider>();
		lid.Center = Vector3.Up * (topZ + RoofRise * 0.5f);
		lid.Scale = new Vector3( length, length, RoofRise );
		lid.Static = true;
	}

	/// <summary>Detail atlas cell <paramref name="index"/> of 4 × 4, inset two pixels.</summary>
	static Rect Cell( int index )
	{
		const float Inset = 2f / DetailSize;
		return new Rect( (index % 4) * 0.25f + Inset, (index / 4) * 0.25f + Inset, 0.25f - Inset * 2f, 0.25f - Inset * 2f );
	}

	/// <summary>
	/// Calm dressed stone for the facade, in the palette's paper neutrals: courses of different heights,
	/// blocks of different lengths starting at different places in every course, fine joints, a soft
	/// tone drift between blocks, faint weathering streaks. Safe on any thread.
	/// </summary>
	public static PaintedSurface PaintFacade()
	{
		var mortar = ArenaTheme.Mix( Palette.PaperShade, Palette.InkSoft, 0.12f );
		var painter = new Painter( FacadeSize, mortar, 0.9f );
		var random = new Random( 4111 );
		float Next() => (float)random.NextDouble();

		var y = 0f;
		while ( y < FacadeSize - 1f )
		{
			var course = 50f + Next() * 34f;
			if ( FacadeSize - (y + course) < 40f ) course = FacadeSize - y;

			var x = Next() * FacadeSize;
			var end = x + FacadeSize;
			while ( x < end - 1f )
			{
				var length = 110f + Next() * 160f;
				if ( end - (x + length) < 60f ) length = end - x;
				var tone = Next() * 2f - 1f;
				var block = tone < 0f ? ArenaTheme.Mix( Palette.Paper, Palette.PaperShade, -tone * 0.3f ) : ArenaTheme.Mix( Palette.Paper, Color.White, tone * 0.12f );
				block = ArenaTheme.Mix( block, Palette.Hex( 0xD8CFC6 ), Next() * 0.12f );
				var joint = 1.6f + Next() * 1.2f;
				painter.Rect( x + joint * 0.5f, y + joint * 0.5f, length - joint, course - joint, block, radius: 2f + Next() * 2f, height: 1f, bevel: 3f + Next() * 2f, roughness: 0.82f + Next() * 0.1f );
				x += length;
			}

			y += course;
		}

		// Weathering: faint darker streaks running down from the courses.
		var streaks = painter.Noise( 4, 4112, 2 );
		var weight = new float[FacadeSize * FacadeSize];
		for ( var py = 0; py < FacadeSize; py++ )
		for ( var px = 0; px < FacadeSize; px++ )
		{
			// Read across: smooth down the facade, thin across it, seamless where the stone repeats.
			var s = ArenaTheme.StreakX( streaks, FacadeSize, py, px, 37 );
			weight[py * FacadeSize + px] = s > 0.55f ? MathF.Min( (s - 0.55f) * 3f, 1f ) * 0.12f : 0f;
		}
		painter.Blend( weight, Palette.Hex( 0xB8AE9E ) );

		painter.HeightNoise( 0.25f, 32, 4113, 3 );
		painter.Mottle( 0.04f, 4, 4114, 4 );
		painter.Cavity( 0.4f, 4 );
		painter.EdgeWear( 0.3f, 3, 4115 );
		painter.Grain( 0.012f, 4116 );
		return painter.Bake( "tower_facade", Painter.Matte, 4f );
	}

	/// <summary>
	/// The facade's small parts in one atlas: window-frame paint in each floor's identity colour, dark
	/// glass in three tones with a faint sky reflection, an unlit and a lamp-lit pane, a blind and a
	/// curtain, and dressed stone for sills and courses.
	/// </summary>
	public static PaintedSurface PaintFacadeDetail()
	{
		var painter = new Painter( DetailSize, Palette.Paper, 0.8f );
		const int Cell = DetailSize / 4;

		for ( var floor = 0; floor < Arena.FloorCount; floor++ )
		{
			var x = (floor % 4) * Cell; var y = (floor / 4) * Cell;
			var paint = Palette.FloorHue( floor );
			painter.Fill( x, y, Cell, Cell, ArenaTheme.Mix( paint, Palette.Ink, 0.25f ), height: 0f );
			painter.Rect( x + 3f, y + 3f, Cell - 6f, Cell - 6f, paint, radius: 6f, height: 1f, bevel: 10f, roughness: 0.45f );
		}

		// Three glass tones: how much sky each reflects, how fast it darkens, where the sheen falls.
		var random = new Random( 4124 );
		float Next() => (float)random.NextDouble();
		foreach ( var glass in GlassCells )
		{
			var (x, y) = ((glass % 4) * Cell, (glass / 4) * Cell);
			var sky = ArenaTheme.Mix( Palette.Ink, Palette.Sky, 0.26f + Next() * 0.16f );
			var fall = 0.55f + Next() * 0.4f;
			for ( var row = 0; row < Cell; row++ )
				painter.Fill( x, y + row, Cell, 1, ArenaTheme.Mix( sky, Palette.Ink, MathF.Pow( row / (float)Cell, fall ) ), height: 0f, roughness: 0.08f );
			var at = x + 4f + Next() * 40f;
			var width = 14f + Next() * 16f;
			painter.Polygon( new[] { new Vector2( at, y + Cell ), new Vector2( at + width, y + Cell ), new Vector2( at + width + 66f, y ), new Vector2( at + 66f, y ) },
				Palette.Sky, alpha: 0.07f + Next() * 0.06f );
		}

		{
			// A room with no light on: deep glass with only a trace of sky.
			var (x, y) = ((DarkCell % 4) * Cell, (DarkCell / 4) * Cell);
			var deep = ArenaTheme.Mix( Palette.Ink, Color.Black, 0.25f );
			for ( var row = 0; row < Cell; row++ )
				painter.Fill( x, y + row, Cell, 1, ArenaTheme.Mix( ArenaTheme.Mix( deep, Palette.Sky, 0.14f ), deep, row / (float)Cell ), height: 0f, roughness: 0.08f );
		}

		{
			// A room with a lamp on: a muted warm glow, brightest low in the pane where the lamp light falls.
			var (x, y) = ((LitCell % 4) * Cell, (LitCell / 4) * Cell);
			var warm = Palette.Hex( 0xE6C9A2 );
			for ( var row = 0; row < Cell; row++ )
				painter.Fill( x, y + row, Cell, 1, ArenaTheme.Mix( Palette.Ink, warm, 0.3f + 0.22f * row / (float)Cell ), height: 0f, roughness: 0.1f );
		}

		{
			// A slatted blind, its bottom rail at the cell's foot.
			var (x, y) = ((BlindCell % 4) * Cell, (BlindCell / 4) * Cell);
			var slat = ArenaTheme.Mix( Palette.Paper, Palette.PaperShade, 0.35f );
			painter.Fill( x, y, Cell, Cell, ArenaTheme.Mix( slat, Palette.InkSoft, 0.25f ), height: 0.3f, roughness: 0.6f );
			for ( var sy = Cell - 10f; sy > -6f; sy -= 7f )
				painter.Rect( x, y + sy, Cell, 5.5f, slat, height: 0.7f, bevel: 1.5f, roughness: 0.6f );
			painter.Rect( x, y + Cell - 6f, Cell, 6f, ArenaTheme.Mix( slat, Palette.InkSoft, 0.15f ), height: 0.9f, bevel: 1.5f, roughness: 0.5f );
		}

		{
			// A linen curtain: soft vertical folds of uneven width, a hem at its foot.
			var (x, y) = ((CurtainCell % 4) * Cell, (CurtainCell / 4) * Cell);
			var linen = Palette.Hex( 0xE4D8C6 );
			var phase = Next() * MathF.Tau;
			for ( var col = 0; col < Cell; col++ )
			{
				var fold = MathF.Sin( col / (float)Cell * MathF.Tau * 4.5f + phase + MathF.Sin( col * 0.11f ) * 0.8f );
				painter.Fill( x + col, y, 1, Cell, ArenaTheme.Mix( linen, Palette.InkSoft, 0.12f + fold * 0.1f ), height: 0.6f + fold * 0.2f, roughness: 0.95f );
			}
			painter.Fill( x, y + Cell - 7, Cell, 7, ArenaTheme.Mix( linen, Palette.InkSoft, 0.2f ), height: 0.7f, roughness: 0.95f );
		}

		{
			// Dressed stone for sills and courses, with the facade's own mottle and pitting so it never reads as plastic.
			var (x0, y0) = ((StoneCell % 4) * Cell, (StoneCell / 4) * Cell);
			painter.Fill( x0, y0, Cell, Cell, ArenaTheme.Mix( Palette.Paper, Palette.PaperShade, 0.3f ), height: 0.5f, roughness: 0.85f );
			var mottle = painter.Noise( 16, 4125, 4 );
			var pits = painter.Noise( 64, 4126, 2 );
			var dark = new float[DetailSize * DetailSize];
			var light = new float[DetailSize * DetailSize];
			var relief = new float[DetailSize * DetailSize];
			for ( var y = y0; y < y0 + Cell; y++ )
			for ( var x = x0; x < x0 + Cell; x++ )
			{
				var i = y * DetailSize + x;
				var m = (mottle[i] - 0.5f) * 2f;
				dark[i] = m < 0f ? -m * 0.18f : 0f;
				light[i] = m > 0f ? m * 0.12f : 0f;
				relief[i] = pits[i] < 0.3f ? (pits[i] - 0.3f) * 1.5f : (mottle[i] - 0.5f) * 0.3f;
			}
			painter.Blend( dark, Palette.Hex( 0xC9BCA6 ) );
			painter.Blend( light, Color.White );
			painter.AddHeight( relief, 1f );
		}

		painter.HeightNoise( 0.15f, 32, 4121, 3 );
		painter.Mottle( 0.03f, 8, 4122, 3 );
		painter.Grain( 0.01f, 4123 );
		return painter.Bake( "tower_facade_detail", Painter.Matte, 3f );
	}

	/// <summary>
	/// One frosted pane, repeated between the roof's bars: pale sky-tinted glass with a soft milky
	/// frost, a few broad highlights, and grime gathered toward the bars and the lower edge.
	/// Painted onto the self-lit dev material, since the pane stands in for daylight coming through.
	/// </summary>
	public static PaintedSurface PaintPanes()
	{
		var glass = ArenaTheme.Mix( Palette.Sky, Palette.Paper, 0.45f );
		// Rough, as frost is: at 0.3 the roof threw a blown white highlight across the menu orbit.
		var painter = new Painter( PaneSize, glass, 0.92f );

		var frost = ArenaTheme.Ramp( painter.Noise( 6, 4131, 4 ), 0.35f, 0.75f, 0.35f );
		painter.Blend( frost, Color.White );

		var random = new Random( 4132 );
		for ( var k = 0; k < 3; k++ )
		{
			var x = (float)random.NextDouble() * PaneSize;
			var width = 16f + (float)random.NextDouble() * 30f;
			painter.Polygon( new[] { new Vector2( x, PaneSize ), new Vector2( x + width, PaneSize ), new Vector2( x + width + 90f, 0f ), new Vector2( x + 90f, 0f ) },
				Color.White, alpha: 0.12f );
		}

		var grime = new float[PaneSize * PaneSize];
		for ( var y = 0; y < PaneSize; y++ )
		for ( var x = 0; x < PaneSize; x++ )
		{
			var edge = MathF.Min( x, PaneSize - 1 - x );
			var foot = y / (float)PaneSize;
			grime[y * PaneSize + x] = MathF.Max( 0f, 1f - edge / 22f ) * 0.3f + MathF.Max( 0f, foot - 0.82f ) * 1.4f;
		}
		painter.Blend( grime, ArenaTheme.Mix( glass, GreenhouseTheme.MossLight, 0.5f ) );

		// The bar under each pane edge, as seen through the glass from above: painted rather than modelled
		// proud of it, because three-unit bars at the menu orbit's distance shimmer into moiré.
		var bar = new float[PaneSize * PaneSize];
		for ( var y = 0; y < PaneSize; y++ )
		for ( var x = 0; x < PaneSize; x++ )
		{
			var edge = MathF.Min( x, PaneSize - 1 - x );
			bar[y * PaneSize + x] = edge < 8 ? 0.55f : edge < 11 ? 0.55f * (11 - edge) / 3f : 0f;
		}
		painter.Blend( bar, Palette.Hex( 0xA9B5A2 ) );
		painter.Mottle( 0.03f, 4, 4133, 3 );
		return painter.Bake( "tower_roof_panes", ArenaGenerator.GlowMaterialPath, 1f );
	}
}
