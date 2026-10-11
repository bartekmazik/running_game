using System;

/// <summary>
/// Floor 5 (index 4): a library. Underfoot, oak herringbone parquet blocks — every plank its own
/// tone, hue and grain, a polished finish worn dull along the walking paths, fine scratches, and a
/// small rosewood inlay pin on one block. Overhead (the undersides of the office floor), dark walnut
/// coffers: mitred beam frames, stepped mouldings, a fielded panel in every bay.
/// </summary>
public sealed class LibraryTheme : ArenaTheme
{
	public override string Name => "Library";
	public override int Floor => 4;
	public override Color LightColor => new( 1.30f, 0.95f, 0.66f );

	static Color Oak => Palette.Hex( 0xB98A62 );
	static Color OakLight => Palette.Hex( 0xCFA27A );
	static Color OakShade => Palette.Hex( 0x9C7050 );

	/// <summary>Late-wood lines of the grain: the same oak, denser and darker.</summary>
	static Color LateWood => Palette.Hex( 0x8A5F40 );

	/// <summary>Planks cut from yellower and from rosier stock — the rose leaning toward the floor's identity hue.</summary>
	static Color Honey => Palette.Hex( 0xC39556 );
	static Color Rosy => Palette.Hex( 0xB07A68 );

	/// <summary>Where the finish has worn through: paler, greyer, duller bare oak.</summary>
	static Color Bare => Palette.Hex( 0xC4A68A );

	/// <summary>Dirt packed in the gaps between planks and between blocks.</summary>
	static Color Gap => Palette.Hex( 0x4E3527 );

	/// <summary>Inlay: rosewood, worn toward the oak around it.</summary>
	static Color Rose => Palette.Hex( 0xC99BB0 );
	static Color Rosewood => Palette.Hex( 0x7A4A44 );

	static Color Walnut => Palette.Hex( 0x765443 );
	static Color WalnutLight => Palette.Hex( 0x8E6A52 );
	static Color WalnutDeep => Palette.Hex( 0x4D3528 );
	static Color WalnutGrain => Palette.Hex( 0x3E2A20 );

	/// <summary>The bookcases' boarded back, in shadow behind the books.</summary>
	static Color CaseBack => Palette.Hex( 0x3A2820 );

	/// <summary>Gilt tooling on the spines: old gold, worn soft.</summary>
	static Color Gilt => Palette.Hex( 0xB89D62 );

	/// <summary>Dust settled along the front of the shelves.</summary>
	static Color Dust => Palette.Hex( 0x9A8E80 );

	/// <summary>One bookcase per module, so every length of wall starts on a case's upright and no bay ends in a sliver of a case.</summary>
	protected override float WallModule => WallRepeat / 4f;

	/// <summary>
	/// Three walls of four bookcases each, worn by the lengths of wall either way round (six looks, never
	/// the one the length before wore): with one, every bay — three and a half to four and a half cases
	/// wide — showed the same four cases in the same order, and a distinctive shelf came round about
	/// thirty times (6b review); with three and no mirror, the lengths ran A-B-C-A-B-C.
	/// </summary>
	public override int WallCount => 3;

	/// <summary>A bookcase has no handedness: mirrored, its books stand in the other order.</summary>
	protected override bool MirrorWalls => true;

	/// <summary>The deep shadow a shelf throws over the books under it, and the dark of a leather band.</summary>
	static Color ShelfShadow => Palette.Hex( 0x1E1512 );

	/// <summary>The faded colour a spine's head turns where the light has reached it for years.</summary>
	static Color Faded => Palette.Hex( 0xCFC3AE );

	/// <summary>Paper labels on cloth spines.</summary>
	static Color Label => Palette.Hex( 0xDCD0B6 );

	/// <summary>The dusty brown every binding drifts toward with age.</summary>
	static Color AgedBinding => Palette.Hex( 0x8A7A6A );

	/// <summary>Plank width in pixels (about 7 units, an 18 cm plank) and its length in widths.</summary>
	const int PlankWidth = 64;
	const int PlankRatio = 4;

	/// <summary>
	/// The structural front layer, and warm lamplight where the chandeliers will hang under the cornice
	/// (the chandeliers, reading lamps and ladders come in Task 9).
	/// </summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 30f, Pier = 78f, Corner = 32f, Cornice = 30f, Skirting = 14f, SkirtingDepth = 6f } );
		AddLamps( room, 5, FloorContext.ClearHeight - 28f, -24f, LightColor * 4.2f, Seed + 900 );
	}

	/// <summary>
	/// Built-in walnut bookcases, one per <see cref="WallModule"/>: a plinth, side uprights shared
	/// with the next case, five shelves set at their own heights in every case (shelves are moved to
	/// fit what is on them), a dark boarded back. Books are generated per shelf: each its own width,
	/// height, colour from a muted set of bindings (oxblood, bottle green, navy, ochre, vellum, plum and
	/// the floor's own dusty rose), cloth or leather — leather with raised bands and gilt, cloth with a
	/// paper label or a pair of gilt rules — and a rounded spine. Now and then a short run of matching
	/// volumes, a few books lying flat, a gap, and in about half the rows the last book leaning on its
	/// neighbour. Each of the <see cref="WallCount"/> walls draws its cases from its own stream.
	/// Spines fade toward the head, where the light reaches; each shelf throws a shadow over the
	/// tops of the books under it.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		const int Case = WallSize / 4;
		const float Upright = 3.4f * Ppu;
		const float Shelf = 2.2f * Ppu;
		float Row( float v ) => (WallRepeat - v) * Ppu;
		var painter = new Painter( WallSize, CaseBack, 0.85f );
		var random = new Random( Seed + 600 + variant * 31 );
		float Next() => (float)random.NextDouble();

		// The case back: vertical boards, each its own shade, in deep shadow behind the books.
		for ( var x = 0f; x < WallSize; )
		{
			var board = (9f + Next() * 5f) * Ppu;
			var w = MathF.Min( board, WallSize - x );
			painter.Rect( x + 0.5f, 0f, w - 1f, WallSize, Mix( CaseBack, WalnutGrain, Next() * 0.5f ), height: 0.2f, roughness: 0.85f );
			x += board;
		}

		var shade = new float[WallSize * WallSize];
		for ( var c = 0; c < 4; c++ )
		{
			var left = c * Case + Upright * 0.5f;
			var right = (c + 1) * Case - Upright * 0.5f;

			// Plinth top and five shelves, every case set at its own heights.
			var plinth = 13f + Next() * 3f;
			var levels = new float[6];
			levels[0] = plinth;
			var pitch = (WallRepeat - 33f - plinth) / 5f;
			for ( var k = 1; k < 6; k++ ) levels[k] = plinth + k * pitch + (k < 5 ? (Next() - 0.5f) * 4f : 0f);

			painter.Rect( left, Row( plinth ), right - left, WallSize - Row( plinth ), Walnut, height: 1.2f, bevel: 3f, roughness: 0.55f );

			for ( var k = 0; k < 5; k++ )
			{
				var floorV = levels[k] + (k == 0 ? 0f : Shelf / Ppu);
				var clear = levels[k + 1] - floorV;
				ShelfOfBooks( painter, random, left + 2f, right - 2f, Row( floorV ), clear * Ppu );

				// The shelf above throws its shadow over the tops of the books.
				var under = Row( levels[k + 1] ) + Shelf;
				for ( var y = (int)under; y < (int)under + 34 && y < WallSize; y++ )
				{
					var w = 0.55f * (1f - (y - under) / 34f);
					for ( var x = (int)left; x < (int)right; x++ ) shade[y * WallSize + x] = MathF.Max( shade[y * WallSize + x], w * w );
				}
			}

			// Shelves: walnut boards with a lit front arris and a grey line of dust along it.
			for ( var k = 1; k < 6; k++ )
			{
				var top = Row( levels[k] );
				var tone = Mix( Walnut, Next() < 0.5f ? WalnutLight : WalnutDeep, Next() * 0.25f );
				painter.Rect( left - 1f, top, right - left + 2f, Shelf, tone, height: 1.25f, bevel: 3f, roughness: 0.5f );
				painter.Rect( left, top + 1f, right - left, 1.5f, Mix( tone, Dust, 0.5f ), alpha: 0.6f );
			}

			// The crown rail over the top shelf, hidden for the most part behind the cornice beam.
			painter.Rect( left - 1f, 0f, right - left + 2f, Row( WallRepeat - 28f ), WalnutDeep, height: 1.1f, bevel: 3f, roughness: 0.6f );
		}

		painter.Blend( shade, ShelfShadow );

		// The uprights, shared by neighbouring cases, their grain running down them.
		var grain = painter.Noise( 8, Seed + 601 + variant, 2 );
		var lines = new float[WallSize * WallSize];
		for ( var c = 0; c < 4; c++ )
		{
			var x0 = c * Case - Upright * 0.5f;
			var tone = Mix( Walnut, WalnutLight, Next() * 0.3f );
			painter.Rect( x0, 0f, Upright, WallSize, tone, height: 1.45f, bevel: 4f, roughness: 0.5f );
			for ( var x = (int)x0 + 2; x < (int)(x0 + Upright) - 2; x++ )
			{
				var px = (x + WallSize) % WallSize;
				for ( var y = 0; y < WallSize; y++ )
				{
					// Read down the upright: thin across it, long along it.
					var g = grain[(px * 37 % WallSize) * WallSize + y];
					lines[y * WallSize + px] = g > 0.58f ? MathF.Min( (g - 0.58f) * 3f, 1f ) * 0.35f : 0f;
				}
			}
		}
		painter.Blend( lines, WalnutGrain );

		painter.Mottle( 0.05f, 6, Seed + 602 + variant, 4 );
		painter.RoughnessNoise( 0.05f, 16, Seed + 603 + variant, 2 );
		painter.Cavity( 0.5f, 4 );
		painter.EdgeWear( 0.15f, 2, Seed + 604 + variant );
		painter.Grain( 0.012f, Seed + 605 + variant );
		return painter.Bake( $"arena_4_wall_{variant}", Painter.Matte, 5f );
	}

	/// <summary>
	/// One shelf's books from <paramref name="left"/> to <paramref name="right"/>, standing on pixel
	/// row <paramref name="foot"/> with <paramref name="clear"/> pixels of headroom.
	/// </summary>
	static void ShelfOfBooks( Painter painter, Random random, float left, float right, float foot, float clear )
	{
		const float Ppu = WallSize / WallRepeat;
		float Next() => (float)random.NextDouble();
		var x = left + Next() * 4f;
		var run = 0;
		var runColor = Bindings[0];
		float runHeight = 0f, runWidth = 0f;
		var runLeather = false;

		// About half the rows end in a book leaning on its neighbour where there is room for it to fall;
		// the rest stand straight to the end.
		var leanAtEnd = Next() < 0.5f;

		while ( x < right - 4f )
		{
			// A gap now and then, where a book is out.
			if ( Next() < 0.035f ) { x += (4f + Next() * 9f) * Ppu; continue; }

			// A few books lying flat, the longest at the bottom.
			if ( Next() < 0.04f && right - x > 30f * Ppu )
			{
				var length = (19f + Next() * 6f) * Ppu;
				var y = foot;
				var count = 2 + random.Next( 3 );
				for ( var k = 0; k < count && foot - y < clear - 10f; k++ )
				{
					var thick = (2.6f + Next() * 2.2f) * Ppu;
					var l = length * (1f - k * 0.08f) - Next() * 6f;
					painter.Rect( x + (length - l) * Next(), y - thick, l, thick - 1f, Binding( random ), radius: 1.5f, height: 0.9f, bevel: thick * 0.4f, roughness: 0.7f );
					y -= thick;
				}
				x += length + 2f;
				continue;
			}

			// A short run of matching volumes now and then, otherwise a single book: long runs made
			// landmarks that recurred in every bay.
			if ( run == 0 )
			{
				run = Next() < 0.12f ? 3 + random.Next( 3 ) : 1;
				runColor = Binding( random );
				runHeight = MathF.Min( clear * (0.62f + Next() * 0.3f), clear - 6f );
				runWidth = (3.2f + Next() * Next() * 4.5f) * Ppu;
				runLeather = Next() < 0.3f;
			}
			run--;

			var width = runWidth * (0.94f + Next() * 0.12f);
			var height = MathF.Min( runHeight + (Next() - 0.5f) * 3f, clear - 4f );
			if ( x + width > right ) break;

			// The last book of a row, with room to fall, leans on its neighbour.
			var lean = 0f;
			var room = right - (x + width);
			if ( leanAtEnd && room < width * 1.6f && room > 4f ) lean = 8f + Next() * 12f;
			else if ( Next() < 0.03f ) lean = (Next() - 0.5f) * 10f;

			var tone = Mix( runColor, Next() < 0.5f ? Color.Black : Color.White, Next() * 0.1f );
			PaintBook( painter, random, x, foot, width, height, tone, runLeather, lean );
			x += width + (lean != 0f ? MathF.Abs( MathF.Sin( lean * MathF.PI / 180f ) ) * height : 0.6f + Next() * 1.2f);
		}
	}

	/// <summary>
	/// One book seen by its spine: a rounded spine (the bevel across its width is the curve), faded
	/// toward its head, leather with raised bands and a gilt panel or cloth with a label or gilt
	/// rules. <paramref name="lean"/> in degrees tips its head toward the book on its left (Painter's
	/// angle turns clockwise in the image), the foot sliding out to the right.
	/// </summary>
	static void PaintBook( Painter painter, Random random, float x, float foot, float width, float height, Color color, bool leather, float lean )
	{
		float Next() => (float)random.NextDouble();
		var radians = lean * MathF.PI / 180f;
		var sin = MathF.Abs( MathF.Sin( radians ) ); var cos = MathF.Cos( radians );

		// Centre placed so the lowest corner of the tipped book rests on the shelf.
		var cx = x + width * 0.5f + (lean > 0f ? height * sin * 0.5f : 0f);
		var cy = foot - (sin * width + cos * height) * 0.5f;
		var rough = leather ? 0.55f : 0.78f;
		painter.Rect( cx - width * 0.5f, cy - height * 0.5f, width - 1f, height, color, radius: 1.2f, height: 1f, bevel: MathF.Min( width * 0.45f, 7f ), roughness: rough, angle: lean );

		// The sun-faded head of the spine.
		painter.Rect( cx - width * 0.5f, cy - height * 0.5f, width - 1f, height * 0.3f, Mix( color, Faded, 0.35f ), alpha: 0.25f + Next() * 0.15f, angle: lean );

		if ( lean != 0f ) return;
		var top = cy - height * 0.5f;
		if ( leather )
		{
			var bands = 3 + random.Next( 2 );
			for ( var b = 0; b < bands; b++ )
			{
				var y = top + height * (0.14f + 0.72f * b / (bands - 1));
				painter.Rect( x + 1f, y, width - 3f, 2.4f, Mix( color, ShelfShadow, 0.35f ), height: 1.15f, bevel: 1f, roughness: 0.5f );
			}
			painter.Rect( x + 2f, top + height * 0.22f, width - 5f, height * 0.13f, Gilt, alpha: 0.35f + Next() * 0.2f, roughness: 0.35f );
		}
		else if ( Next() < 0.45f )
		{
			painter.Rect( x + 2f, top + height * (0.12f + Next() * 0.1f), width - 5f, height * 0.12f, Label, alpha: 0.55f + Next() * 0.2f, roughness: 0.85f );
		}
		else
		{
			foreach ( var at in new[] { 0.08f, 0.9f } )
				painter.Rect( x + 1.5f, top + height * at, width - 4f, 1.6f, Gilt, alpha: 0.4f + Next() * 0.2f, roughness: 0.35f );
		}
	}

	/// <summary>A binding from the muted set, a few percent off its neighbours.</summary>
	static Color Binding( Random random ) => Mix( Bindings[random.Next( Bindings.Length )], AgedBinding, (float)random.NextDouble() * 0.15f );

	/// <summary>Muted bindings: oxblood, bottle green, navy, ochre, tan, plum, the floor's dusty rose, vellum, brown calf, teal, maroon, sage.</summary>
	static readonly Color[] Bindings =
	{
		Palette.Hex( 0x7A3E3E ), Palette.Hex( 0x3F5B49 ), Palette.Hex( 0x3E4A66 ), Palette.Hex( 0xA88452 ),
		Palette.Hex( 0xB59A72 ), Palette.Hex( 0x6A4B66 ), Palette.Hex( 0x9C6F7C ), Palette.Hex( 0xD6C8AA ),
		Palette.Hex( 0x6E4C36 ), Palette.Hex( 0x4D6B6C ), Palette.Hex( 0x5E3A34 ), Palette.Hex( 0x7E8A6A ),
	};

	/// <summary>
	/// Walnut mouldings and posts with continuous grain: the cornice's fascia carries a row of dentils,
	/// the posts are fluted pilasters, the skirting a plinth with a bead, the slab edge a walnut nosing.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		return PaintTrimAtlas( "arena_4_trim", WalnutLight, WalnutDeep, 0.5f, 0.3f, Seed + 700, painter =>
		{
			// Dentils along the fascia: blocks stood proud, the gaps between them in shadow.
			var (top, height) = TrimRows( TrimBand.Cornice );
			var y0 = top + height * 0.6f; var y1 = top + height * 0.9f;
			for ( var x = 0; x < TrimSize; x += 16 )
			{
				painter.Rect( x + 3f, y0, 10f, y1 - y0, WalnutLight, alpha: 0f, height: 0.98f, bevel: 1.5f );
				painter.Rect( x - 3f, y0, 6f, y1 - y0, WalnutDeep, alpha: 0.55f, height: 0.62f );
			}

			// Flutes down the posts: five narrow grooves between wider flat fillets, each a round-bottomed
			// cut with crisp shoulders and a dark hollow. Wide sine grooves with no flat between them read
			// as the folds of a curtain (6b review).
			var (ptop, pheight) = TrimRows( TrimBand.Pilaster );
			var relief = new float[TrimSize * TrimSize];
			var hollow = new float[TrimSize * TrimSize];
			for ( var y = 0; y < pheight; y++ )
			{
				var t = (y + 0.5f) / pheight;
				var groove = 0f;
				for ( var f = 0; f < 5; f++ )
				{
					var u = (t - (0.24f + f * 0.115f)) / 0.06f;
					if ( u > 0f && u < 1f ) groove = MathF.Sqrt( MathF.Sin( u * MathF.PI ) );
				}
				for ( var x = 0; x < TrimSize; x++ )
				{
					relief[(ptop + y) * TrimSize + x] = -groove * 0.3f;
					hollow[(ptop + y) * TrimSize + x] = groove * 0.4f;
				}
			}
			painter.AddHeight( relief, 1f );
			painter.Blend( hollow, WalnutDeep );
		} );
	}

	protected override PaintedSurface PaintFloor()
	{
		const int cell = FloorSize / 2;
		var painter = new Painter( FloorSize, Oak, 0.35f );
		var random = new Random( Seed );
		float Next() => (float)random.NextDouble();

		// Each block's outline is set in height first (the parquet's own relief is added on top of it),
		// and the joint at the chamfer's foot is painted last, as the ring between the cell edge and
		// that same outline, so colour and height agree on where the block ends.
		var outlines = new Vector2[4][];
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			painter.Fill( (int)min.x, (int)min.y, cell, cell, Oak, height: 0.5f );
			outlines[c] = SlabOutline( min, cell, 6f + Next() * 3f, 2f, 5f + Next() * 6f, Seed + c * 101 );
			painter.Polygon( outlines[c], Oak, alpha: 0f, height: 1f, bevel: Rim );
		}

		PaintParquet( painter, random );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );

			// Fine scratches from chair legs and grit, pale where they cut through the finish.
			var scratches = 18 + random.Next( 18 );
			for ( var s = 0; s < scratches; s++ )
			{
				var at = min + new Vector2( 40f + Next() * 432f, 40f + Next() * 432f );
				var angle = Next() * MathF.Tau;
				var length = 8f + Next() * Next() * 46f;
				var to = at + new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) ) * length;
				painter.Line( at, to, 0.7f + Next() * 0.6f, Bare, 0.18f + Next() * 0.2f, roughness: 0.55f );
			}

			// The rosewood inlay pin on variant 3: small, set into a plank, worn into the oak around it.
			if ( c == 3 )
			{
				var at = min + new Vector2( 170f + Next() * 170f, 170f + Next() * 170f );
				var radius = 7f + Next() * 2f;
				painter.Polygon( Organic( at, new Vector2( radius, radius * 0.96f ), 0.1f, Seed + 81, 0f, 24 ), Mix( Mix( Rose, Rosewood, 0.25f ), Oak, 0.35f ), alpha: 0.6f, roughness: 0.4f );
				painter.Polygon( Organic( at + new Vector2( 1.5f, -1f ), new Vector2( radius * 0.6f, radius * 0.45f ), 0.6f, Seed + 82, Next() * MathF.Tau, 16 ), Bare, alpha: 0.25f );
			}

			// The joint between blocks: the ring outside the block outline, dark with packed dirt.
			painter.Polygon( JointRing( min, cell, outlines[c] ), Mix( Gap, OakShade, 0.25f ), alpha: 0.85f, roughness: 0.8f );
		}

		// Traffic: the finish worn dull and paler in broad patches, different on each of the four blocks.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 2 ), 0.55f, 0.85f, 0.45f ), Bare, roughness: 0.62f );

		painter.Mottle( 0.06f, 4, Seed + 31, 3 );
		painter.RoughnessNoise( 0.07f, 8, Seed + 32, 2 );
		painter.Cavity( 0.4f, 4 );
		painter.EdgeWear( 0.3f, 3, Seed + 33 );
		painter.Grain( 0.012f, Seed + 34 );
		return painter.Bake( "arena_4_floor", Painter.Matte, 6f );
	}

	/// <summary>
	/// Herringbone, computed per pixel. In a frame turned 45° the pattern is axis-aligned planks of
	/// <see cref="PlankRatio"/> by 1 (units of a plank width): a plank along U sits where
	/// (U − ⌊V⌋) mod 2n &lt; n, every other point lies on a plank along V. Each plank gets its own tone,
	/// hue lean, slight lift (planks are never perfectly flush), and grain read along its length from
	/// a stretched noise row of its own; a fine gap with a micro-bevel separates the planks. The
	/// pattern's phase differs per block and two blocks are mirrored, so no block is a turned copy of another.
	/// </summary>
	void PaintParquet( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		const float Period = PlankRatio * 2f;
		var grain = painter.Noise( FloorSize / 2, Seed + 20, 1 );
		var figure = painter.Noise( 24, Seed + 21, 1 );
		var planks = new byte[FloorSize * FloorSize];

		// 256 plank colours, ±6 % in value with the hue oak really has: paler planks yellower,
		// darker ones rosier. A plank's hash picks its entry.
		var palette = new Color[256];
		for ( var k = 0; k < 256; k++ )
		{
			var tone = k / 255f * 2f - 1f;
			palette[k] = tone > 0f ? Mix( Oak, Mix( OakLight, Honey, 0.45f ), tone * 0.55f ) : Mix( Oak, Mix( OakShade, Rosy, 0.5f ), -tone * 0.55f );
		}
		var late = new float[FloorSize * FloorSize];
		var gap = new float[FloorSize * FloorSize];
		var relief = new float[FloorSize * FloorSize];
		var scale = 1f / (MathF.Sqrt( 2f ) * PlankWidth);

		// One period of a sine in 256 steps: the grain's waver without a Sin per pixel.
		var sine = new float[256];
		for ( var k = 0; k < 256; k++ ) sine[k] = MathF.Sin( k * MathF.Tau / 256f );
		int lastKey = int.MinValue, lastB = int.MinValue, plankPhase = 0, plankRow = 0;
		byte plankColor = 0;
		float plankLift = 0f, plankStrength = 0f, plankWave = 0f;
		uint plankShift = 0;

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var mirror = c == 1 || c == 2;
			var offsetU = (float)random.NextDouble() * Period;
			var offsetV = (float)random.NextDouble() * Period;
			lastKey = int.MinValue; lastB = int.MinValue;

			for ( var y = 0; y < cell; y++ )
			{
				var py = y + 0.5f;
				var row = ((int)min.y + y) * FloorSize + (int)min.x;
				for ( var x = 0; x < cell; x++ )
				{
					var px = mirror ? cell - x - 0.5f : x + 0.5f;
					var u = (px + py) * scale + offsetU;
					var v = (py - px) * scale + offsetV;

					float along, across; int idA, idB, family;
					var r = MathF.Floor( v );
					var a = u - r;
					var m = a - MathF.Floor( a / Period ) * Period;
					if ( m < PlankRatio )
					{
						along = m; across = v - r; idA = (int)r; idB = (int)MathF.Floor( a / Period ); family = 0;
					}
					else
					{
						var q = MathF.Floor( u );
						var b = v - q - 1f;
						along = b - MathF.Floor( b / Period ) * Period; across = u - q; idA = (int)q; idB = (int)MathF.Floor( b / Period ); family = 1;
					}

					// Per-plank values, recomputed only when the plank changes (runs of pixels share one).
					var key = idA * 2 + family;
					if ( key != lastKey || idB != lastB )
					{
						lastKey = key; lastB = idB;
						var h = Hash( key, idB, Seed + c * 7 );
						var g = Hash( key, idB, Seed + c * 7 + 3 );
						plankColor = (byte)(h & 0xFF);
						plankLift = (Bits( h, 12 ) - 0.5f) * 0.06f;
						plankStrength = 0.22f + Bits( h, 20 ) * 0.28f;
						plankPhase = (int)(g & 0xFF);
						plankWave = 1f + Bits( g, 8 ) * 2f;
						plankRow = (int)((g >> 16) & 511);
						plankShift = (g >> 25) & 127;
					}

					var i = row + x;
					planks[i] = plankColor;

					// Grain: thin late-wood lines along the plank, wavering slightly, read from a stretched
					// noise row (Streak, inlined: this loop runs a million times), over a soft figure.
					var acrossPx = across * PlankWidth;
					var alongPx = along * PlankWidth;
					var wave = sine[((int)(alongPx * 0.65f) + plankPhase) & 255] * plankWave;
					var grainRow = (((int)(acrossPx + wave) + plankRow) & (FloorSize - 1)) * FloorSize;
					var position = (alongPx + plankShift) * (1f / 70f);
					var p0 = (int)position;
					var g0 = grain[grainRow + (p0 & (FloorSize - 1))];
					var s = g0 + (grain[grainRow + ((p0 + 1) & (FloorSize - 1))] - g0) * (position - p0);
					var f = figure[i];
					var lateWood = (s > 0.62f ? MathF.Min( (s - 0.62f) * 6f, 1f ) * plankStrength : 0f) + (f > 0.55f ? (f - 0.55f) * 0.5f : 0f);
					late[i] = lateWood;

					// Gap and micro-bevel between planks.
					var edgeAcross = MathF.Min( across, 1f - across ) * PlankWidth;
					var edgeAlong = MathF.Min( along, PlankRatio - along ) * PlankWidth;
					var edge = MathF.Min( edgeAcross, edgeAlong );
					var shoulder = edge >= 3f ? 1f : edge / 3f;
					shoulder = shoulder * shoulder * (3f - 2f * shoulder);
					gap[i] = edge < 1.1f ? (1f - edge / 1.1f) * 0.45f : 0f;
					relief[i] = (shoulder - 1f) * 0.28f + plankLift - lateWood * 0.04f;
				}
			}
		}

		painter.Blend( planks, palette );
		painter.Blend( late, LateWood );
		painter.Blend( gap, Gap, roughness: 0.75f );
		painter.AddHeight( relief, 1f );
	}

	/// <summary>
	/// The ring between a cell's edge (pushed a pixel and a half out, so the outermost pixels are fully
	/// covered) and a block outline inside it, as one polygon: the cell square, a zero-width bridge to the
	/// outline, the outline, and back. Painter.Polygon fills by crossing count, so the bridge cancels
	/// itself and the outline becomes a hole.
	/// </summary>
	static Vector2[] JointRing( Vector2 min, float size, Vector2[] outline )
	{
		var points = new Vector2[outline.Length + 6];
		var lo = min - new Vector2( 1.5f, 1.5f );
		var hi = min + new Vector2( size + 1.5f, size + 1.5f );
		points[0] = lo; points[1] = new Vector2( hi.x, lo.y ); points[2] = hi; points[3] = new Vector2( lo.x, hi.y ); points[4] = lo;
		for ( var k = 0; k < outline.Length; k++ ) points[5 + k] = outline[k];
		points[^1] = outline[0];
		return points;
	}

	/// <summary>
	/// Dark walnut coffers, one bay per tile underside: half a beam along every edge (neighbouring
	/// undersides complete the beams, with a fine groove where they meet), a two-step moulding down into
	/// the recess, and a fielded panel. Beam grain runs along each beam and meets its neighbour in a
	/// mitre at the corners; panel grain runs one way or the other per bay, with its own figure.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		const int Beam = 24;
		var painter = new Painter( CeilingSize, Walnut, 0.45f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();

		var panelAlongX = new bool[4];
		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var tone = Next() * 2f - 1f;
			var wood = tone < 0f ? Mix( Walnut, WalnutDeep, -tone * 0.4f ) : Mix( Walnut, WalnutLight, tone * 0.4f );
			panelAlongX[c] = Next() < 0.5f;

			painter.Fill( (int)min.x, (int)min.y, cell, cell, Mix( wood, WalnutLight, 0.2f ), height: 1f );
			painter.Polygon( Square( min, Beam, cell ), Mix( wood, WalnutDeep, 0.25f ), height: 0.72f, bevel: 2.5f );
			painter.Polygon( Square( min, Beam + 9, cell ), Mix( wood, WalnutDeep, 0.15f ), height: 0.5f, bevel: 2.5f );
			painter.Polygon( Square( min, Beam + 16, cell ), Mix( wood, WalnutDeep, 0.35f ), height: 0.22f, bevel: 2f );

			// The fielded panel: raised, its edges bevelled wide, never quite centred or square.
			var inset = Beam + 30 + Next() * 4f;
			var panel = Square( min, inset, cell );
			for ( var k = 0; k < 4; k++ ) panel[k] += new Vector2( Next() - 0.5f, Next() - 0.5f ) * 2.5f;
			painter.Polygon( panel, wood, height: 0.48f, bevel: 13f );
		}

		PaintWalnutGrain( painter, panelAlongX, Beam );

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );

			// Where neighbouring undersides meet: the beam's centre groove.
			painter.Polygon( JointRing( min, cell, Square( min, 1.5f, cell ) ), WalnutGrain, alpha: 0.7f, height: 0.85f );

			// A shrinkage check along the panel's grain on one bay; a dark heartwood streak on another.
			if ( c == 0 )
			{
				var start = min + new Vector2( 90f + Next() * 60f, 80f + Next() * 30f );
				var crack = Wander( start, panelAlongX[c] ? 0f : MathF.PI * 0.5f, 9f, 6 + random.Next( 3 ), 0.08f, random );
				Polyline( painter, crack, 0.9f, WalnutGrain, 0.7f, 0.4f );
			}
			if ( c == 3 )
			{
				var at = min + new Vector2( 128f + (Next() - 0.5f) * 40f, 128f + (Next() - 0.5f) * 40f );
				var half = panelAlongX[c] ? new Vector2( 50f, 9f ) : new Vector2( 9f, 50f );
				painter.Polygon( Organic( at, half, 0.6f, Seed + 560, 0f, 24 ), WalnutDeep, alpha: 0.3f );
			}
		}

		painter.Mottle( 0.06f, 3, Seed + 520, 4 );
		painter.RoughnessNoise( 0.06f, 6, Seed + 521, 2 );
		painter.Cavity( 0.5f, 4 );
		painter.Grain( 0.012f, Seed + 523 );
		return painter.Bake( "arena_4_ceiling", Painter.Matte, 4f );
	}

	/// <summary>A square inset by <paramref name="inset"/> px from a cell's edges, corners clockwise in the image.</summary>
	static Vector2[] Square( Vector2 min, float inset, float size ) => new[]
	{
		min + new Vector2( inset, inset ), min + new Vector2( size - inset, inset ),
		min + new Vector2( size - inset, size - inset ), min + new Vector2( inset, size - inset ),
	};

	/// <summary>
	/// Walnut grain over the whole ceiling in one pass: in the beam and moulding band, along the
	/// nearest edge (the diagonal where two edges are equally near is the mitre); in the bay, along the
	/// panel's direction. Late-wood lines darken, a broad figure shifts the tone, and the odd pale
	/// sapwood streak shows on the beams.
	/// </summary>
	void PaintWalnutGrain( Painter painter, bool[] panelAlongX, int beam )
	{
		const int cell = CeilingSize / 2;
		var grain = painter.Noise( CeilingSize / 2, Seed + 530, 1 );
		var figure = painter.Noise( 24, Seed + 531, 2 );
		var dark = new float[CeilingSize * CeilingSize];
		var light = new float[CeilingSize * CeilingSize];
		var groove = new float[CeilingSize * CeilingSize];
		var band = beam + 16;

		for ( var y = 0; y < CeilingSize; y++ )
		{
			var ly = y % cell;
			for ( var x = 0; x < CeilingSize; x++ )
			{
				var lx = x % cell;
				var c = (y / cell) * 2 + x / cell;
				var toLeft = lx; var toRight = cell - 1 - lx; var toTop = ly; var toBottom = cell - 1 - ly;
				var nearestX = Math.Min( toLeft, toRight ); var nearestY = Math.Min( toTop, toBottom );

				int along, across, zone;
				if ( Math.Min( nearestX, nearestY ) < band )
				{
					// A beam along a vertical edge when that edge is nearer, along a horizontal one otherwise.
					if ( nearestX < nearestY ) { along = ly; across = nearestX; zone = toLeft < toRight ? 1 : 2; }
					else { along = lx; across = nearestY; zone = toTop < toBottom ? 3 : 4; }
				}
				else
				{
					along = panelAlongX[c] ? lx : ly; across = panelAlongX[c] ? ly : lx; zone = 5;
				}

				var offset = zone * 97 + c * 211;
				var s = Streak( grain, CeilingSize, across + offset, (along + offset) / 45f );
				var f = Streak( figure, CeilingSize, across + offset, along / 28f );
				var i = y * CeilingSize + x;
				dark[i] = (s > 0.6f ? MathF.Min( (s - 0.6f) * 6f, 1f ) * 0.4f : 0f) + (f > 0.6f ? (f - 0.6f) * 0.6f : 0f);
				light[i] = f < 0.32f ? (0.32f - f) * 0.7f : 0f;
				groove[i] = -dark[i] * 0.05f;
			}
		}

		painter.Blend( dark, WalnutGrain );
		painter.Blend( light, WalnutLight );
		painter.AddHeight( groove, 1f );
	}
}
