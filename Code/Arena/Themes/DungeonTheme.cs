using System;
using System.Collections.Generic;

/// <summary>
/// Floor 7 (index 6): a dungeon. Underfoot, granite flagstones — two or three to a block, cut
/// irregular and laid in damp mortar, each stone its own tone and lean (slate, lilac, warm grey),
/// flecked with crystals, its edges worn round and chipped, moss in the joints. Overhead (the
/// undersides of the workshop floor), rough rubble stone, pillowed and pitted, set in deep lime
/// mortar, with damp and soot.
/// </summary>
public sealed class DungeonTheme : ArenaTheme
{
	public override string Name => "Dungeon";
	public override int Floor => 6;
	public override Color LightColor => new( 1.40f, 0.82f, 0.48f );

	static Color Granite => Palette.Hex( 0x8D8798 );
	static Color GraniteLight => Palette.Hex( 0xA49FAE );
	static Color GraniteShade => Palette.Hex( 0x6F6A7A );

	/// <summary>Stones that lean warm grey and stones that lean toward the floor's slate lilac — both close to grey.</summary>
	static Color GraniteWarm => Palette.Hex( 0x958C89 );
	static Color GraniteLilac => Palette.Hex( 0x8C849B );

	/// <summary>Feldspar and quartz (pale) and mica (dark) in the granite.</summary>
	static Color Feldspar => Palette.Hex( 0xBDB7BE );
	static Color Mica => Palette.Hex( 0x45414B );

	/// <summary>
	/// Lime mortar in the walls' joints and the trim's: warm and sandy against the cool granite, about
	/// as light — a filled joint reads as a band of another material, not as a dark crack.
	/// </summary>
	static Color Mortar => Palette.Hex( 0x8A847E );

	/// <summary>
	/// The joints between the floor's flags: dark, neutral grime packed into the lime. The torches face
	/// the chamfers that carry the tile joints, and a light joint lit warm drew every tile edge as a
	/// salmon line; a dark one stays a dark line under any light (6b review).
	/// </summary>
	static Color FloorJoint => Palette.Hex( 0x4A464C );

	static Color Moss => Palette.Hex( 0x7FA06A );

	/// <summary>Salts left at the tide line of rising damp: chalky off-white.</summary>
	static Color Salt => Palette.Hex( 0xCFCBC4 );

	/// <summary>Damp patches on the stone.</summary>
	static Color Damp => Palette.Hex( 0x6D6874 );

	static Color Rubble => Palette.Hex( 0x5E5866 );
	static Color RubbleLight => Palette.Hex( 0x77707C );
	static Color RubbleDark => Palette.Hex( 0x46414C );
	static Color RubbleWarm => Palette.Hex( 0x665C5C );
	static Color Lime => Palette.Hex( 0x75707A );
	static Color Soot => Palette.Hex( 0x2E2B33 );

	/// <summary>
	/// The walls' stone, greyer and warmer than the floor's flags: under the torches a lilac granite
	/// wall read pink. A neutral grey, a warm grey and a slate, with lilac only as a lean in a few.
	/// </summary>
	static Color WallStoneGrey => Palette.Hex( 0x7E7A77 );
	static Color WallStoneWarm => Palette.Hex( 0x85796E );
	static Color WallStoneSlate => Palette.Hex( 0x67676F );

	/// <summary>Iron leaching out of the odd stone: a dull ochre-brown.</summary>
	static Color IronStain => Palette.Hex( 0x8A6A4E );

	/// <summary>
	/// The structural front layer, and six torches' worth of low, warm light at torch height along the
	/// walls (the torches themselves, arches and the rest come in Task 10).
	/// </summary>
	public override void Dress( FloorContext room )
	{
		BuildFrontLayer( room, new FrontStyle { Post = 34f, Pier = 80f, Corner = 36f, Cornice = 28f, Skirting = 12f, SkirtingDepth = 6f } );
		AddLamps( room, 6, 125f, -12f, LightColor * 4f, Seed + 900 );
	}

	/// <summary>
	/// Coursed ashlar, old and damp: courses of different heights, blocks of different lengths and
	/// now and then two small stones stacked in one slot, every block cut a little out of true (its
	/// sides wander, its corners differ), pillowed, dressed with diagonal tooling that runs its own way
	/// on each stone, chipped along its arrises. Grey granite leaning slate, lilac or warm, a few
	/// stones iron-stained. Lime mortar sits deep and crumbled in the joints. Damp rises from the floor
	/// with a tide line of salts above it and moss in the low joints; soot hangs under the cornice.
	/// </summary>
	protected override PaintedSurface PaintWall( int variant )
	{
		const float Ppu = WallSize / WallRepeat;
		var joints = Mix( Mortar, Soot, 0.3f );
		var painter = new Painter( WallSize, joints, 0.95f );
		var random = new Random( Seed + 600 );
		float Next() => (float)random.NextDouble();
		var tones = new[] { WallStoneGrey, WallStoneWarm, WallStoneSlate, Mix( WallStoneGrey, GraniteLilac, 0.35f ), Mix( WallStoneWarm, Rubble, 0.4f ) };

		// The mortar bed: sunk, rough, crumbled.
		var bed = painter.Noise( 48, Seed + 601, 3 );
		for ( var i = 0; i < bed.Length; i++ ) bed[i] = 0.18f + (bed[i] - 0.5f) * 0.25f;
		painter.AddHeight( bed, 1f );

		var stones = new List<(Vector2 Min, Vector2 Size)>();
		var y = 0f;
		while ( y < WallSize - 1f )
		{
			var course = (24f + Next() * 14f) * Ppu;
			if ( WallSize - (y + course) < 22f * Ppu ) course = WallSize - y;
			var x = Next() * WallSize;
			var end = x + WallSize;
			while ( x < end - 1f )
			{
				var length = (30f + Next() * 52f) * Ppu;
				if ( end - (x + length) < 26f * Ppu ) length = end - x;

				// Now and then a slot filled by two smaller stones, one over the other.
				if ( Next() < 0.1f && length < 55f * Ppu )
				{
					var split = course * (0.4f + Next() * 0.2f);
					stones.Add( (new Vector2( x, y ), new Vector2( length, split )) );
					stones.Add( (new Vector2( x, y + split ), new Vector2( length, course - split )) );
				}
				else stones.Add( (new Vector2( x, y ), new Vector2( length, course )) );
				x += length;
			}
			y += course;
		}

		var tooling = new float[WallSize * WallSize];
		foreach ( var (min, size) in stones )
		{
			var joint = 8f + Next() * 6f;
			var outline = WallStone( min, size, joint, random.Next() );
			var tone = Mix( tones[random.Next( tones.Length )], tones[random.Next( tones.Length )], Next() );
			tone = Mix( tone, Next() < 0.5f ? Feldspar : Mix( Rubble, Soot, 0.3f ), Next() * 0.3f );
			painter.Polygon( outline, tone, height: 0.85f + Next() * 0.3f, bevel: 14f + Next() * 10f, roughness: 0.86f + Next() * 0.08f );

			// Tooling: fine parallel strokes across the face at the mason's angle for this stone.
			var angle = (Next() < 0.5f ? -1f : 1f) * (0.5f + Next() * 0.5f);
			var direction = new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) );
			var across = new Vector2( -direction.y, direction.x );
			var centre = min + size * 0.5f;
			var reach = (size.x + size.y) * 0.5f;
			for ( var s = -reach; s < reach; s += 4f + Next() * 4f )
			{
				var from = centre + across * s - direction * reach;
				var to = centre + across * s + direction * reach;
				ClipLine( tooling, from, to, min + new Vector2( joint + 6f, joint + 6f ), size - new Vector2( joint * 2f + 12f, joint * 2f + 12f ), 0.25f + Next() * 0.2f );
			}

			// Chips off the arrises: a bite or two from some edges, sunk to the joint.
			var chips = random.Next( 3 ) - 1;
			for ( var c = 0; c < chips; c++ )
			{
				var horizontal = Next() < 0.5f;
				var low = Next() < 0.5f;
				var at = horizontal
					? new Vector2( min.x + joint + Next() * (size.x - joint * 2f), low ? min.y + size.y - joint * 0.5f : min.y + joint * 0.5f )
					: new Vector2( low ? min.x + size.x - joint * 0.5f : min.x + joint * 0.5f, min.y + joint + Next() * (size.y - joint * 2f) );
				var along = horizontal ? new Vector2( 1f, 0f ) : new Vector2( 0f, 1f );
				var into = horizontal ? new Vector2( 0f, low ? -1f : 1f ) : new Vector2( low ? -1f : 1f, 0f );
				painter.Polygon( NickOutline( at, along, into, 6f + Next() * 10f, 4f + Next() * 5f, random.Next() ), Mix( tone, Soot, 0.25f ), height: 0.55f, bevel: 4f, roughness: 0.95f );
			}

			// An iron stain on the odd stone, soaked into its face.
			if ( Next() < 0.08f )
				painter.Polygon( Organic( centre + new Vector2( (Next() - 0.5f) * size.x * 0.4f, (Next() - 0.5f) * size.y * 0.3f ), size * new Vector2( 0.3f, 0.35f ), 0.9f, random.Next(), Next() * MathF.Tau ), IronStain, alpha: 0.18f );
		}

		painter.AddHeight( Scaled( tooling, -0.06f ), 1f );
		painter.Blend( Scaled( tooling, 0.25f ), WallStoneSlate );

		// Weathered faces: blotches of darker and paler stone, and pits where softer grains washed out.
		var blotch = painter.Noise( 24, Seed + 611, 3 );
		var pits = painter.Noise( 192, Seed + 612, 2 );
		var darker = new float[WallSize * WallSize];
		var paler = new float[WallSize * WallSize];
		var pitted = new float[WallSize * WallSize];
		for ( var i = 0; i < blotch.Length; i++ )
		{
			var b = (blotch[i] - 0.5f) * 2f;
			darker[i] = b < 0f ? -b * 0.22f : 0f;
			paler[i] = b > 0f ? b * 0.14f : 0f;
			pitted[i] = pits[i] < 0.3f ? (0.3f - pits[i]) * 3f : 0f;
		}
		painter.Blend( darker, Mix( Rubble, Soot, 0.4f ) );
		painter.Blend( paler, Feldspar );
		painter.Blend( Scaled( pitted, 0.4f ), Mix( Rubble, Soot, 0.5f ) );
		painter.AddHeight( Scaled( pitted, -0.12f ), 1f );

		// Rising damp: dark stone at the foot, a pale tide line of salts at its edge, moss in the low joints.
		var damp = painter.Noise( 6, Seed + 602, 3 );
		var mossNoise = painter.Noise( 24, Seed + 603, 3 );
		var soot = painter.Noise( 5, Seed + 604, 3 );
		var wet = new float[WallSize * WallSize];
		var salt = new float[WallSize * WallSize];
		var moss = new float[WallSize * WallSize];
		var smoke = new float[WallSize * WallSize];
		var height = new float[WallSize * WallSize];
		painter.CopyHeight( height );
		for ( var py = 0; py < WallSize; py++ )
		{
			var v = WallRepeat - py / Ppu;
			for ( var px = 0; px < WallSize; px++ )
			{
				var i = py * WallSize + px;
				var line = 34f + (damp[i] - 0.5f) * 40f;
				wet[i] = v < line ? MathF.Min( (line - v) / 12f, 1f ) * 0.45f : 0f;
				var edge = MathF.Abs( v - line - 2f );
				salt[i] = edge < 4f ? (1f - edge / 4f) * 0.3f * damp[i] : 0f;
				var low = v < 50f ? (50f - v) / 50f : 0f;
				moss[i] = height[i] < 0.5f && mossNoise[i] > 0.55f ? MathF.Min( (mossNoise[i] - 0.55f) * 5f, 1f ) * low * 0.6f : 0f;
				var high = v > 150f ? (v - 150f) / 58f : 0f;
				smoke[i] = high * (0.25f + soot[i] * 0.35f);
			}
		}
		painter.Blend( wet, Damp, roughness: 0.6f );
		painter.Blend( salt, Salt, roughness: 0.95f );
		painter.Blend( moss, Mix( Moss, Granite, 0.35f ), roughness: 0.95f );
		painter.Blend( smoke, Soot );

		painter.HeightNoise( 0.35f, 16, Seed + 605, 3 );
		painter.HeightNoise( 0.12f, 96, Seed + 606, 2 );
		painter.Mottle( 0.07f, 8, Seed + 607, 4 );
		painter.RoughnessNoise( 0.05f, 12, Seed + 608, 2 );
		painter.Cavity( 0.6f, 6 );
		painter.EdgeWear( 0.2f, 4, Seed + 609 );
		painter.Grain( 0.015f, Seed + 610 );
		return painter.Bake( "arena_6_wall", Painter.Matte, 6f );
	}

	/// <summary>
	/// A dressed stone in its slot: a rounded rectangle inset by half the joint on every side, each
	/// side pushed in or out a little along its length, so no two stones are quite square.
	/// </summary>
	static Vector2[] WallStone( Vector2 min, Vector2 size, float joint, int seed )
	{
		var random = new Random( seed );
		float Next() => (float)random.NextDouble();
		float Inset() => joint * (0.4f + Next() * 0.3f);
		var left = min.x + Inset(); var right = min.x + size.x - Inset();
		var top = min.y + Inset(); var bottom = min.y + size.y - Inset();
		var corners = new[] { new Vector2( left, top ), new Vector2( right, top ), new Vector2( right, bottom ), new Vector2( left, bottom ) };
		var cut = new float[4];
		for ( var k = 0; k < 4; k++ ) cut[k] = 3f + Next() * 7f;

		var points = new List<Vector2>();
		for ( var k = 0; k < 4; k++ )
		{
			// Each side runs from just past its corner's cut to just before the next one's, bowed a
			// little and wandering a pixel or so, so the stone is never a true rectangle.
			var a = corners[k]; var b = corners[(k + 1) % 4];
			var direction = (b - a).Normal;
			var from = a + direction * cut[k];
			var to = b - direction * cut[(k + 1) % 4];
			var normal = new Vector2( direction.y, -direction.x );
			var bulge = (Next() - 0.5f) * 3f;
			var steps = Math.Max( 2, (int)((to - from).Length / 24f) );
			for ( var s = 0; s <= steps; s++ )
			{
				var t = s / (float)steps;
				var push = MathF.Sin( t * MathF.PI ) * bulge + (s == 0 || s == steps ? 0f : (Next() - 0.5f) * 1.5f);
				points.Add( from + (to - from) * t + normal * push );
			}
		}
		return points.ToArray();
	}

	/// <summary>A line clipped to a rectangle, written into a weight field (seamless), for tooling strokes.</summary>
	static void ClipLine( float[] field, Vector2 from, Vector2 to, Vector2 min, Vector2 size, float weight )
	{
		if ( size.x <= 0f || size.y <= 0f ) return;
		var length = (to - from).Length;
		var steps = (int)length;
		for ( var s = 0; s <= steps; s++ )
		{
			var p = from + (to - from) * (s / (float)steps);
			if ( p.x < min.x || p.y < min.y || p.x > min.x + size.x || p.y > min.y + size.y ) continue;
			var x = ((int)p.x % WallSize + WallSize) % WallSize;
			var y = ((int)p.y % WallSize + WallSize) % WallSize;
			field[y * WallSize + x] = MathF.Max( field[y * WallSize + x], weight );
		}
	}

	/// <summary>A copy of a field scaled by <paramref name="amount"/>.</summary>
	static float[] Scaled( float[] field, float amount )
	{
		var copy = new float[field.Length];
		for ( var i = 0; i < field.Length; i++ ) copy[i] = field[i] * amount;
		return copy;
	}

	/// <summary>
	/// Dressed granite for the front layer: every moulding is laid in blocks, with a mortar joint across
	/// the run every 15 to 35 units, each block its own tone; the faces have a smooth drafted margin
	/// round a rougher, pitched middle, as quoins and lintels are cut. The cornice is a moulded corbel
	/// course, the skirting a plinth course, the slab edge a kerb.
	/// </summary>
	protected override PaintedSurface PaintTrim()
	{
		var body = Mix( WallStoneGrey, Feldspar, 0.25f );
		return PaintTrimAtlas( "arena_6_trim", body, Rubble, 0.85f, 0f, Seed + 700, painter =>
		{
			var random = new Random( Seed + 710 );
			float Next() => (float)random.NextDouble();
			var pitch = painter.Noise( 64, Seed + 711, 3 );
			var relief = new float[TrimSize * TrimSize];
			foreach ( var band in new[] { TrimBand.Cornice, TrimBand.Skirting, TrimBand.Pilaster, TrimBand.SlabEdge, TrimBand.Stock } )
			{
				var (top, height) = TrimRows( band );

				// Block lengths across the band's run, laid out to fill the repeat exactly.
				var x = 0f;
				while ( x < TrimSize - 1f )
				{
					var length = (15f + Next() * 20f) * 4f;
					if ( TrimSize - (x + length) < 50f ) length = TrimSize - x;
					var tone = Mix( body, Next() < 0.5f ? Feldspar : WallStoneSlate, Next() * 0.35f );
					tone = Mix( tone, Next() < 0.5f ? WallStoneWarm : GraniteLilac, Next() * 0.3f );
					painter.Rect( x + 2f, top, length - 4f, height, tone, alpha: 0.8f );

					// The drafted margin stays smooth; the middle is pitched rough.
					for ( var py = top + 6; py < top + height - 6; py++ )
					for ( var px = (int)x + 8; px < (int)(x + length) - 8; px++ )
						relief[py * TrimSize + px] = (pitch[py * TrimSize + px] - 0.5f) * 0.12f;

					painter.Rect( x - 2f, top, 4f, height, Mix( Mortar, Soot, 0.25f ), height: 0.35f, roughness: 0.95f );
					x += length;
				}
			}
			painter.AddHeight( relief, 1f );
		} );
	}

	protected override PaintedSurface PaintFloor()
	{
		var painter = new Painter( FloorSize, Mortar, 0.5f );
		var random = new Random( Seed );

		PaintFlagstones( painter, random );

		// Damp patches, drawn over stone and mortar alike.
		painter.Blend( Ramp( painter.Noise( 3, Seed + 30, 2 ), 0.56f, 0.82f, 0.4f ), Damp, roughness: 0.55f );

		painter.HeightNoise( 0.5f, 8, Seed + 31, 2 );
		painter.Mottle( 0.07f, 4, Seed + 32, 3 );
		painter.RoughnessNoise( 0.08f, 6, Seed + 33, 2 );
		painter.Cavity( 0.5f, 5 );
		painter.EdgeWear( 0.2f, 4, Seed + 34 );
		painter.Grain( 0.015f, Seed + 35 );

		// The chamfers flat, dull and a shade darker. The forge's lava light shines up at the ceiling
		// under this floor from below the slab, and nothing casts shadows: a chamfer tilted further
		// still by a painted worn arris faced that light and drew every tile edge as a salmon line
		// (6b review). On the chamfer's own 45° the light's cone cannot reach it (ForgeTheme.BounceCone).
		const int Cell = FloorSize / 2;
		var rim = new float[FloorSize * FloorSize];
		for ( var y = 0; y < FloorSize; y++ )
		for ( var x = 0; x < FloorSize; x++ )
		{
			var toEdge = MathF.Min( MathF.Min( x % Cell, Cell - 1 - x % Cell ), MathF.Min( y % Cell, Cell - 1 - y % Cell ) ) + 0.5f;
			rim[y * FloorSize + x] = Step( Rim + 3f, Rim - 1f, toEdge );
		}
		painter.Level( rim, 1f );
		painter.Roughen( rim, 0.95f );
		painter.Blend( Scaled( rim, 0.2f ), Soot );
		return painter.Bake( "arena_6_floor", Painter.Matte, 7f );
	}

	/// <summary>
	/// The flagstones, per pixel. Each block is laid as two or three stones around seed points (a
	/// Voronoi cell per stone), read through a noise warp so the cut lines bend a little. The distance
	/// to the nearest split (exact, from the bisectors) or to the block's foot gives each stone a worn,
	/// rounded arris and the mortar joint its width: 4–7 px between stones, about the block joint's,
	/// varying slowly along its length. The block's foot stays clean — a straight cut, nicked only in a
	/// few places by larger, slowly varying chips — because a fine modulation there foreshortens into
	/// stitching along the chamfer. Stones sit clearly proud or sunk of each other, and each has its
	/// own colour from a small palette (one blend pass for all of them).
	/// </summary>
	void PaintFlagstones( Painter painter, Random random )
	{
		const int cell = FloorSize / 2;
		const int mask = FloorSize * FloorSize - 1;
		float Next() => (float)random.NextDouble();

		// One warp field read at two offsets serves both axes; the offset is far enough apart that the
		// two reads are unrelated.
		var warp = painter.Noise( 14, Seed + 20, 2 );
		var width = painter.Noise( 10, Seed + 22, 1 );
		var nicks = painter.Noise( 6, Seed + 25, 1 );
		var crystals = painter.Noise( FloorSize / 2, Seed + 23, 1 );
		var mossNoise = painter.Noise( 20, Seed + 24, 2 );

		// Outputs reuse the noise fields where a pixel's noise is read only at that pixel: crystals
		// become the pale-fleck weight, width the joint weight, mossNoise the moss weight.
		var relief = new float[FloorSize * FloorSize];
		var owners = new byte[FloorSize * FloorSize];
		var dark = new float[FloorSize * FloorSize];
		var pale = crystals; var joint = width; var moss = mossNoise;
		var palette = new Color[13];

		// Per block: the stone seeds, and for every ordered pair the bisector as a unit normal and an
		// offset, so the per-pixel distance to a split is one dot product.
		var seedX = new float[3]; var seedY = new float[3]; var lift = new float[3];
		var normalX = new float[9]; var normalY = new float[9]; var offset = new float[9];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, FloorSize );
			var count = c == 0 || c == 3 ? 2 : 3;
			var turn = Next() * MathF.Tau;
			var centreX = cell * 0.5f + (Next() - 0.5f) * 60f;
			var centreY = cell * 0.5f + (Next() - 0.5f) * 60f;
			for ( var k = 0; k < count; k++ )
			{
				var angle = turn + k * MathF.Tau / count + (Next() - 0.5f) * 0.5f;
				var radius = cell * 0.26f * (0.85f + Next() * 0.3f);
				seedX[k] = centreX + MathF.Cos( angle ) * radius;
				seedY[k] = centreY + MathF.Sin( angle ) * radius;
				lift[k] = (Next() - 0.5f) * 0.3f;

				var tone = Next() * 2f - 1f;
				var lean = Next() * 2f - 1f;
				var color = tone > 0f ? Mix( Granite, GraniteLight, tone * 0.45f ) : Mix( Granite, GraniteShade, -tone * 0.5f );
				palette[1 + c * 3 + k] = lean > 0f ? Mix( color, GraniteLilac, lean * 0.5f ) : Mix( color, GraniteWarm, -lean * 0.5f );
			}
			for ( var a = 0; a < count; a++ )
			for ( var b = 0; b < count; b++ )
			{
				if ( a == b ) continue;
				var nx = seedX[b] - seedX[a]; var ny = seedY[b] - seedY[a];
				var length = MathF.Sqrt( nx * nx + ny * ny );
				nx /= length; ny /= length;
				normalX[a * 3 + b] = nx; normalY[a * 3 + b] = ny;
				offset[a * 3 + b] = (seedX[a] + seedX[b]) * 0.5f * nx + (seedY[a] + seedY[b]) * 0.5f * ny;
			}
			var footInset = 6f + Next() * 2f;

			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * FloorSize + (int)min.x;
				for ( var x = 0; x < cell; x++ )
				{
					var i = row + x;
					var px = x + 0.5f + (warp[i] - 0.5f) * 18f;
					var py = y + 0.5f + (warp[(i + 300 * FloorSize + 300) & mask] - 0.5f) * 18f;

					// Nearest seed, then the distance to its region's edge: the nearest bisector.
					var owner = 0; var best = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						var dx = px - seedX[k]; var dy = py - seedY[k];
						var dd = dx * dx + dy * dy;
						if ( dd < best ) { best = dd; owner = k; }
					}
					var split = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						if ( k == owner ) continue;
						var pair = owner * 3 + k;
						var e = offset[pair] - px * normalX[pair] - py * normalY[pair];
						if ( e < split ) split = e;
					}

					// Mortar between stones 4–7 px either side of the split; the block's own edge (the
					// chamfer's foot) is unwarped so neighbouring tiles still meet cleanly.
					var halfJoint = 4f + width[i] * 3f;
					var nick = nicks[i] > 0.68f ? (nicks[i] - 0.68f) * 45f : 0f;
					var mossy = mossNoise[i];
					var crystal = crystals[i];
					joint[i] = 0f; moss[i] = 0f; pale[i] = 0f;
					var toEdge = MathF.Min( MathF.Min( x + 0.5f, cell - x - 0.5f ), MathF.Min( y + 0.5f, cell - y - 0.5f ) );
					var foot = toEdge - footInset - nick;
					var edge = MathF.Min( split - halfJoint, foot );

					if ( edge <= 0f )
					{
						joint[i] = 0.9f;
						relief[i] = 0.3f;
						if ( crystal > 0.6f ) pale[i] = MathF.Min( (crystal - 0.6f) * 4f, 1f ) * 0.12f;
						var m = (mossy - 0.55f) * 4f;
						moss[i] = m > 0f ? MathF.Min( m, 1f ) * 0.4f : 0f;
						continue;
					}

					// A worn, rounded arris about twelve pixels wide, then the stone's own level.
					var t = edge >= 12f ? 1f : edge / 12f;
					var shoulder = t * t * (3f - 2f * t);
					relief[i] = 0.3f + shoulder * (0.7f + lift[owner]);
					owners[i] = (byte)(1 + c * 3 + owner);
					if ( edge < 3f )
					{
						var m = (mossy - 0.6f) * 4f;
						moss[i] = m > 0f ? MathF.Min( m, 1f ) * (1f - edge / 3f) * 0.4f : 0f;
					}

					// Crystals: sparse pale feldspar and dark mica, low contrast — granite, not asphalt. They
					// fade out over the chamfer: seen nearly face-on there, specks along every tile edge
					// would line up into a dotted seam.
					var fade = MathF.Min( MathF.Max( (toEdge - 10f) / 24f, 0f ), 1f );
					if ( crystal > 0.7f ) pale[i] = MathF.Min( (crystal - 0.7f) * 6f, 1f ) * 0.22f * fade;
					else if ( crystal < 0.29f ) dark[i] = MathF.Min( (0.29f - crystal) * 6f, 1f ) * 0.28f * fade;
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( owners, palette );
		painter.Blend( pale, Feldspar );
		painter.Blend( dark, Mica );
		painter.Blend( joint, FloorJoint, roughness: 0.85f );
		painter.Blend( moss, Mix( Mix( Moss, Granite, 0.45f ), FloorJoint, 0.25f ), roughness: 0.9f );
	}

	/// <summary>
	/// Rubble masonry seen from below: five or six rough stones per underside around jittered points,
	/// each pillowed (rounded toward the room), pitted, its own tone, set in deep lime mortar whose
	/// width wanders; soot drifts across the lot.
	/// </summary>
	protected override PaintedSurface PaintCeiling()
	{
		const int cell = CeilingSize / 2;
		const int Stones = 6;
		var painter = new Painter( CeilingSize, Rubble, 0.9f );
		var random = new Random( Seed + 500 );
		float Next() => (float)random.NextDouble();
		var warpX = painter.Noise( 10, Seed + 510, 2 );
		var warpY = painter.Noise( 10, Seed + 511, 2 );
		var width = painter.Noise( 40, Seed + 512, 1 );
		var relief = new float[CeilingSize * CeilingSize];
		var owners = new byte[CeilingSize * CeilingSize];
		var mortar = width;
		var palette = new Color[1 + 4 * Stones];
		var seedX = new float[Stones]; var seedY = new float[Stones];
		var normalX = new float[Stones * Stones]; var normalY = new float[Stones * Stones]; var offset = new float[Stones * Stones];

		for ( var c = 0; c < 4; c++ )
		{
			var min = CellOrigin( c, CeilingSize );
			var count = 5 + random.Next( 2 );
			for ( var k = 0; k < count; k++ )
			{
				// Jittered points on a loose 3 x 2 grid, so stones are neither tiny nor huge.
				seedX[k] = (k % 3 + 0.2f + Next() * 0.6f) * cell / 3f;
				seedY[k] = (k / 3 + 0.2f + Next() * 0.6f) * cell / 2f;
				var tone = Next() * 2f - 1f;
				var color = tone > 0f ? Mix( Rubble, RubbleLight, tone * 0.6f ) : Mix( Rubble, RubbleDark, -tone * 0.7f );
				palette[1 + c * Stones + k] = Mix( color, RubbleWarm, Next() * 0.6f );
			}
			for ( var a = 0; a < count; a++ )
			for ( var b = 0; b < count; b++ )
			{
				if ( a == b ) continue;
				var nx = seedX[b] - seedX[a]; var ny = seedY[b] - seedY[a];
				var length = MathF.Sqrt( nx * nx + ny * ny );
				nx /= length; ny /= length;
				normalX[a * Stones + b] = nx; normalY[a * Stones + b] = ny;
				offset[a * Stones + b] = (seedX[a] + seedX[b]) * 0.5f * nx + (seedY[a] + seedY[b]) * 0.5f * ny;
			}

			for ( var y = 0; y < cell; y++ )
			{
				var row = ((int)min.y + y) * CeilingSize + (int)min.x;
				for ( var x = 0; x < cell; x++ )
				{
					var i = row + x;
					var px = x + 0.5f + (warpX[i] - 0.5f) * 22f;
					var py = y + 0.5f + (warpY[i] - 0.5f) * 22f;
					var owner = 0; var best = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						var dx = px - seedX[k]; var dy = py - seedY[k];
						var dd = dx * dx + dy * dy;
						if ( dd < best ) { best = dd; owner = k; }
					}
					var split = float.MaxValue;
					for ( var k = 0; k < count; k++ )
					{
						if ( k == owner ) continue;
						var pair = owner * Stones + k;
						var e = offset[pair] - px * normalX[pair] - py * normalY[pair];
						if ( e < split ) split = e;
					}

					var foot = MathF.Min( MathF.Min( x + 0.5f, cell - x - 0.5f ), MathF.Min( y + 0.5f, cell - y - 0.5f ) ) - 3f;
					var edge = MathF.Min( split - 3f - width[i] * 6f, foot );
					mortar[i] = 0f;
					if ( edge <= 0f )
					{
						mortar[i] = 0.8f;
						relief[i] = 0.1f;
						continue;
					}

					// Pillowed: the stone bulges toward the room, steepest at its edge.
					var t = MathF.Min( edge / 26f, 1f );
					relief[i] = 0.3f + MathF.Sqrt( t ) * 0.7f;
					owners[i] = (byte)(1 + c * Stones + owner);
				}
			}
		}

		painter.AddHeight( relief, 1f );
		painter.Blend( owners, palette );
		painter.Blend( mortar, Lime, roughness: 0.95f );
		painter.Blend( Ramp( painter.Noise( 3, Seed + 520, 3 ), 0.5f, 0.85f, 0.55f ), Soot );
		painter.HeightNoise( 0.8f, 12, Seed + 521, 3 );
		painter.HeightNoise( 0.25f, 64, Seed + 522, 1 );
		painter.Mottle( 0.12f, 8, Seed + 523, 4 );
		painter.Cavity( 0.6f, 4 );
		painter.Grain( 0.015f, Seed + 524 );
		return painter.Bake( "arena_6_ceiling", Painter.Matte, 5f );
	}
}
