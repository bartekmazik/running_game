using System;

/// <summary>
/// A small canvas for painting surfaces in code: colour, a height channel that becomes a normal map,
/// and a roughness channel, so stone, wood and metal catch the light as different materials instead
/// of flat prints. Everything wraps at the edges, so any surface painted here tiles seamlessly.
///
/// The style it is built for is stylised realism: believable materials and light — grain, sheen,
/// dark crevices, worn edges — in natural colours, without photographic noise or pixel art.
///
/// Channels are plain float arrays and the whole-surface passes avoid calls in their inner loops:
/// every arena surface is painted at load, and locally the game is compiled without optimisations,
/// where each call and each vector temporary costs real time at a million pixels.
/// </summary>
public sealed class Painter
{
	/// <summary>Base material for non-metal surfaces; its shader reads colour, normal and roughness from our textures.</summary>
	public const string Matte = "materials/dev/dev_nonmetal_rough70.vmat";

	/// <summary>Base material for metal surfaces (metalness comes from the material, not from a texture).</summary>
	public const string Metal = "materials/dev/dev_metal_rough50.vmat";

	/// <summary>Finest noise detail: six octaves reach ~5 px cells on a 1024² texture at period 8, and each octave costs a full pass.</summary>
	public const int MaxOctaves = 6;

	/// <summary>
	/// Height difference against the blurred neighbourhood that counts as a full crevice or a full
	/// edge for <see cref="Cavity"/> and <see cref="EdgeWear"/>: half of the usual shape height of 1.
	/// </summary>
	const float ReliefDepth = 0.5f;

	/// <summary>How much rougher a fully occluded crevice gets at amount 1 — dust collects there.</summary>
	const float CavityRoughening = 0.3f;

	/// <summary>How far a fully worn edge moves toward white at amount 1 — chipped stone and rubbed paint read lighter.</summary>
	const float WornLift = 0.5f;

	/// <summary>How much smoother a fully worn edge gets at amount 1 — feet and hands polish what sticks out.</summary>
	const float WearPolish = 0.35f;

	public int Size { get; }

	readonly float[] _red;
	readonly float[] _green;
	readonly float[] _blue;
	readonly float[] _height;
	readonly float[] _roughness;

	/// <summary>
	/// Scratch fields for the whole-surface passes (noise for Mottle and friends, the two blur passes),
	/// made once per canvas and reused: every arena surface is painted in one go at load, and a fresh
	/// 4 MB array per pass piled up enough large-object garbage to stall the build for a collection.
	/// </summary>
	float[] _scratchNoise;
	float[] _scratchBlur;
	float[] _scratchBlurOut;

	/// <summary>A square canvas of <paramref name="size"/> pixels, filled with one colour at zero height and one roughness (0 = mirror, 1 = chalk).</summary>
	public Painter( int size, Color fill, float roughness = 0.75f )
	{
		Size = size;
		_red = new float[size * size];
		_green = new float[size * size];
		_blue = new float[size * size];
		_height = new float[size * size];
		_roughness = new float[size * size];
		Array.Fill( _red, fill.r );
		Array.Fill( _green, fill.g );
		Array.Fill( _blue, fill.b );
		Array.Fill( _roughness, roughness );
	}

	int Wrap( int v ) => ((v % Size) + Size) % Size;

	/// <summary>
	/// The one primitive: fills wherever the signed distance is negative, with a 1-px soft edge,
	/// wrapping across the texture edges. A height raises the shape toward it (the bevel rounds the
	/// rim from whatever height was already there), a roughness overrides the surface finish.
	/// </summary>
	public void Shape( Func<float, float, float> sdf, float minX, float minY, float maxX, float maxY, Color color, float alpha = 1f, float height = float.NaN, float bevel = 0f, float roughness = float.NaN )
	{
		var hasHeight = !float.IsNaN( height );
		var hasRoughness = !float.IsNaN( roughness );
		var inverseBevel = bevel > 0f ? 1f / bevel : 0f;
		var size = Size;

		for ( var y = (int)MathF.Floor( minY ) - 1; y <= (int)MathF.Ceiling( maxY ) + 1; y++ )
		{
			var row = Wrap( y ) * size;
			for ( var x = (int)MathF.Floor( minX ) - 1; x <= (int)MathF.Ceiling( maxX ) + 1; x++ )
			{
				var d = sdf( x + 0.5f, y + 0.5f );
				var coverage = 0.5f - d;
				if ( coverage <= 0f ) continue;
				if ( coverage > 1f ) coverage = 1f;

				var i = row + ((uint)x < (uint)size ? x : Wrap( x ));
				var blend = coverage * alpha;
				_red[i] += (color.r - _red[i]) * blend;
				_green[i] += (color.g - _green[i]) * blend;
				_blue[i] += (color.b - _blue[i]) * blend;

				if ( hasRoughness )
					_roughness[i] += (roughness - _roughness[i]) * blend;

				if ( !hasHeight ) continue;
				var shoulder = 1f;
				if ( bevel > 0f )
				{
					var t = -d * inverseBevel;
					shoulder = t <= 0f ? 0f : t >= 1f ? 1f : t * t * (3f - 2f * t);
				}
				var shaped = _height[i] + (height - _height[i]) * shoulder;
				_height[i] += (shaped - _height[i]) * coverage;
			}
		}
	}

	static float Smooth( float t ) => t * t * (3f - 2f * t);

	/// <summary>
	/// Sets a whole pixel rectangle at once — colour, and optionally height and roughness — with hard
	/// edges and no wrapping. For underlays (a block's base before its outline is painted) where the
	/// signed-distance path would cost a closure call per pixel for nothing.
	/// </summary>
	public void Fill( int x, int y, int w, int h, Color color, float height = float.NaN, float roughness = float.NaN )
	{
		var hasHeight = !float.IsNaN( height );
		var hasRoughness = !float.IsNaN( roughness );
		for ( var row = y; row < y + h; row++ )
		{
			var offset = row * Size;
			for ( var i = offset + x; i < offset + x + w; i++ )
			{
				_red[i] = color.r; _green[i] = color.g; _blue[i] = color.b;
				if ( hasHeight ) _height[i] = height;
				if ( hasRoughness ) _roughness[i] = roughness;
			}
		}
	}

	/// <summary>
	/// Rounded rectangle; the workhorse for bricks, planks and panels. <paramref name="angle"/> turns
	/// it about its centre in degrees, clockwise as seen in the image (image y grows downward), so a
	/// laid course can sit slightly crooked.
	/// </summary>
	public void Rect( float x, float y, float w, float h, Color color, float radius = 0f, float alpha = 1f, float height = float.NaN, float bevel = 0f, float roughness = float.NaN, float angle = 0f )
	{
		var cx = x + w * 0.5f; var cy = y + h * 0.5f;
		var hx = w * 0.5f - radius; var hy = h * 0.5f - radius;
		var cos = MathF.Cos( angle * (MathF.PI / 180f) );
		var sin = MathF.Sin( angle * (MathF.PI / 180f) );

		// Axis-aligned bounds of the turned rectangle.
		var ex = MathF.Abs( cos ) * w * 0.5f + MathF.Abs( sin ) * h * 0.5f;
		var ey = MathF.Abs( sin ) * w * 0.5f + MathF.Abs( cos ) * h * 0.5f;

		Shape( ( px, py ) =>
		{
			// Into the rectangle's own frame: rotate the offset back by the angle.
			var dx = px - cx; var dy = py - cy;
			var qx = dx * cos + dy * sin; if ( qx < 0f ) qx = -qx; qx -= hx;
			var qy = dy * cos - dx * sin; if ( qy < 0f ) qy = -qy; qy -= hy;
			var ox = qx > 0f ? qx : 0f; var oy = qy > 0f ? qy : 0f;
			var inside = qx > qy ? qx : qy;
			return MathF.Sqrt( ox * ox + oy * oy ) + (inside < 0f ? inside : 0f) - radius;
		}, cx - ex, cy - ey, cx + ex, cy + ey, color, alpha, height, bevel, roughness );
	}

	/// <summary>
	/// Any simple polygon, convex or concave, with an exact signed distance (nearest edge, inside by
	/// crossing count) within the bevel, so bevels follow every corner. Irregular flagstones, chipped
	/// tile corners, cracked plates and uneven planks are drawn with this; <see cref="Blob"/> makes
	/// the outlines. Points may run either way round.
	/// </summary>
	public void Polygon( Vector2[] points, Color color, float alpha = 1f, float height = float.NaN, float bevel = 0f, float roughness = float.NaN )
	{
		var count = points.Length;
		var xs = new float[count]; var ys = new float[count];
		var minX = float.MaxValue; var minY = float.MaxValue; var maxX = float.MinValue; var maxY = float.MinValue;
		for ( var k = 0; k < count; k++ )
		{
			xs[k] = points[k].x; ys[k] = points[k].y;
			minX = MathF.Min( minX, xs[k] ); maxX = MathF.Max( maxX, xs[k] );
			minY = MathF.Min( minY, ys[k] ); maxY = MathF.Max( maxY, ys[k] );
		}

		// Edge k runs from point k to point k + 1; its vector, 1 / length² and x/y extent are fixed.
		var edgeX = new float[count]; var edgeY = new float[count]; var inverseLengthSq = new float[count];
		var edgeMinX = new float[count]; var edgeMaxX = new float[count]; var edgeMinY = new float[count]; var edgeMaxY = new float[count];
		for ( var k = 0; k < count; k++ )
		{
			var next = k + 1 == count ? 0 : k + 1;
			edgeX[k] = xs[next] - xs[k]; edgeY[k] = ys[next] - ys[k];
			inverseLengthSq[k] = 1f / MathF.Max( edgeX[k] * edgeX[k] + edgeY[k] * edgeY[k], 0.0001f );
			edgeMinX[k] = MathF.Min( xs[k], xs[next] ); edgeMaxX[k] = MathF.Max( xs[k], xs[next] );
			edgeMinY[k] = MathF.Min( ys[k], ys[next] ); edgeMaxY[k] = MathF.Max( ys[k], ys[next] );
		}

		// Shape only needs the distance up to the bevel plus the 1-px soft edge: beyond that the pixel
		// is fully covered at full height, or not touched. So each row looks only at the edges within
		// that reach of it, and finds its crossings once instead of once per pixel. The pixel loop is
		// Shape's, inlined: a block outline covers a quarter of a 1024² atlas, and a closure call per
		// pixel was most of its cost in the unoptimised build.
		var reach = MathF.Max( bevel, 0f ) + 1f;
		var reachSq = reach * reach;
		var nearby = new int[count];
		var crossings = new float[count];
		var hasHeight = !float.IsNaN( height );
		var hasRoughness = !float.IsNaN( roughness );
		var inverseBevel = bevel > 0f ? 1f / bevel : 0f;
		var size = Size;
		var xStart = (int)MathF.Floor( minX ) - 1;
		var xEnd = (int)MathF.Ceiling( maxX ) + 1;

		// Per row, which pixels have any edge within reach, and their squared distance to the nearest:
		// each nearby edge is measured only across its own x reach, instead of every pixel testing
		// every nearby edge. Everywhere else the distance is simply "beyond reach" — most of a large
		// block's interior.
		var nearEdge = new bool[xEnd - xStart + 1];
		var rowNearest = new float[xEnd - xStart + 1];

		for ( var y = (int)MathF.Floor( minY ) - 1; y <= (int)MathF.Ceiling( maxY ) + 1; y++ )
		{
			var py = y + 0.5f;
			var row = Wrap( y ) * size;
			var nearbyCount = 0; var crossingCount = 0;
			for ( var k = 0; k < count; k++ )
			{
				if ( py >= edgeMinY[k] - reach && py <= edgeMaxY[k] + reach )
					nearby[nearbyCount++] = k;

				// Even-odd crossing test against a ray toward +x.
				if ( (ys[k] > py) != (ys[k] + edgeY[k] > py) )
					crossings[crossingCount++] = xs[k] + edgeX[k] * (py - ys[k]) / edgeY[k];
			}

			Array.Clear( nearEdge );
			Array.Fill( rowNearest, reachSq );
			for ( var n = 0; n < nearbyCount; n++ )
			{
				var k = nearby[n];
				// Pixels whose centre (x + 0.5) lies within [min - reach, max + reach] of the edge.
				var from = Math.Max( (int)MathF.Ceiling( edgeMinX[k] - reach - 0.5f ), xStart );
				var to = Math.Min( (int)MathF.Floor( edgeMaxX[k] + reach - 0.5f ), xEnd );
				var wy = py - ys[k];
				for ( var x = from; x <= to; x++ )
				{
					nearEdge[x - xStart] = true;
					var wx = x + 0.5f - xs[k];
					var t = (wx * edgeX[k] + wy * edgeY[k]) * inverseLengthSq[k];
					t = t < 0f ? 0f : t > 1f ? 1f : t;
					var dx = wx - edgeX[k] * t; var dy = wy - edgeY[k] * t;
					var distanceSq = dx * dx + dy * dy;
					if ( distanceSq < rowNearest[x - xStart] ) rowNearest[x - xStart] = distanceSq;
				}
			}

			for ( var x = xStart; x <= xEnd; x++ )
			{
				var px = x + 0.5f;
				var inside = false;
				for ( var c = 0; c < crossingCount; c++ )
					if ( px < crossings[c] ) inside = !inside;

				// A run with no edge within reach is wholly inside or wholly outside (a crossing lies on a
				// nearby edge, so it would have marked its neighbours): skip it, or set it in one tight loop.
				if ( !nearEdge[x - xStart] )
				{
					var runEnd = x;
					while ( runEnd < xEnd && !nearEdge[runEnd + 1 - xStart] ) runEnd++;
					if ( inside )
					{
						for ( var rx = x; rx <= runEnd; rx++ )
						{
							var j = row + ((uint)rx < (uint)size ? rx : Wrap( rx ));
							_red[j] += (color.r - _red[j]) * alpha;
							_green[j] += (color.g - _green[j]) * alpha;
							_blue[j] += (color.b - _blue[j]) * alpha;
							if ( hasRoughness ) _roughness[j] += (roughness - _roughness[j]) * alpha;
							if ( hasHeight ) _height[j] = height;
						}
					}
					x = runEnd;
					continue;
				}

				var nearestSq = rowNearest[x - xStart];
				var distance = nearestSq >= reachSq ? reach : MathF.Sqrt( nearestSq );
				var d = inside ? -distance : distance;
				var coverage = 0.5f - d;
				if ( coverage <= 0f ) continue;
				if ( coverage > 1f ) coverage = 1f;

				var i = row + ((uint)x < (uint)size ? x : Wrap( x ));
				var blend = coverage * alpha;
				_red[i] += (color.r - _red[i]) * blend;
				_green[i] += (color.g - _green[i]) * blend;
				_blue[i] += (color.b - _blue[i]) * blend;

				if ( hasRoughness )
					_roughness[i] += (roughness - _roughness[i]) * blend;

				if ( !hasHeight ) continue;
				var shoulder = 1f;
				if ( bevel > 0f )
				{
					var s = -d * inverseBevel;
					shoulder = s <= 0f ? 0f : s >= 1f ? 1f : s * s * (3f - 2f * s);
				}
				var shaped = _height[i] + (height - _height[i]) * shoulder;
				_height[i] += (shaped - _height[i]) * coverage;
			}
		}
	}

	/// <summary>
	/// An irregular outline for <see cref="Polygon"/>: <paramref name="corners"/> points around a
	/// rounded-rectangle-ish curve (a superellipse) inside <paramref name="halfSize"/>, each pushed
	/// inward (up to 12 % at jitter 1) and slid along the curve (up to 40 % of the spacing) by
	/// <paramref name="jitter"/> (0..1). The radius only ever shrinks, so a blob never leaves its box
	/// and neighbouring stones keep their joint. Seeded, so the same call always draws the same stone.
	/// </summary>
	public static Vector2[] Blob( Vector2 center, Vector2 halfSize, int corners, float jitter, int seed )
	{
		var random = new System.Random( seed );
		var points = new Vector2[corners];
		var step = MathF.PI * 2f / corners;

		for ( var k = 0; k < corners; k++ )
		{
			// Sliding less than half a step keeps the points in order, so the outline never folds.
			var angle = (k + ((float)random.NextDouble() - 0.5f) * 0.8f * jitter) * step;
			var cos = MathF.Cos( angle ); var sin = MathF.Sin( angle );

			// Superellipse of exponent 4: square shoulders with rounded corners.
			var x = MathF.Sign( cos ) * MathF.Sqrt( MathF.Abs( cos ) );
			var y = MathF.Sign( sin ) * MathF.Sqrt( MathF.Abs( sin ) );
			var shrink = 1f - (float)random.NextDouble() * 0.12f * jitter;
			points[k] = new Vector2( center.x + x * halfSize.x * shrink, center.y + y * halfSize.y * shrink );
		}

		return points;
	}

	/// <summary>Disc; rivets, dots and blobs.</summary>
	public void Circle( float cx, float cy, float r, Color color, float alpha = 1f, float height = float.NaN, float bevel = 0f, float roughness = float.NaN )
	{
		Shape( ( px, py ) =>
		{
			var dx = px - cx; var dy = py - cy;
			return MathF.Sqrt( dx * dx + dy * dy ) - r;
		}, cx - r, cy - r, cx + r, cy + r, color, alpha, height, bevel, roughness );
	}

	/// <summary>Round-capped line segment; mortar, cracks and stripes.</summary>
	public void Line( Vector2 a, Vector2 b, float width, Color color, float alpha = 1f, float height = float.NaN, float roughness = float.NaN )
	{
		var ax = a.x; var ay = a.y;
		var abx = b.x - a.x; var aby = b.y - a.y;
		var lengthSq = MathF.Max( abx * abx + aby * aby, 0.0001f );
		var halfWidth = width * 0.5f;
		Shape( ( px, py ) =>
		{
			var x = px - ax; var y = py - ay;
			var t = (x * abx + y * aby) / lengthSq;
			t = t < 0f ? 0f : t > 1f ? 1f : t;
			x -= abx * t; y -= aby * t;
			return MathF.Sqrt( x * x + y * y ) - halfWidth;
		}, MathF.Min( a.x, b.x ) - width, MathF.Min( a.y, b.y ) - width, MathF.Max( a.x, b.x ) + width, MathF.Max( a.y, b.y ) + width, color, alpha, height, roughness: roughness );
	}

	/// <summary>
	/// Tileable fractal value noise in 0..1 for every pixel. <paramref name="period"/> is the number of
	/// lattice cells across the texture for the coarsest octave (each further octave doubles it); it
	/// need not divide the size, because the lattice repeats after exactly one texture width either way.
	/// </summary>
	float[] Fbm( int period, int seed, int octaves ) => Fbm( new float[Size * Size], period, seed, octaves );

	/// <summary>Fractal noise into the canvas's own scratch field, for passes that use it once and drop it.</summary>
	float[] ScratchFbm( int period, int seed, int octaves )
	{
		if ( _scratchNoise is null ) _scratchNoise = new float[Size * Size];
		else Array.Clear( _scratchNoise );
		return Fbm( _scratchNoise, period, seed, octaves );
	}

	/// <summary>Adds the octaves into <paramref name="field"/>, which must start at zero.</summary>
	float[] Fbm( float[] field, int period, int seed, int octaves )
	{
		octaves = Math.Clamp( octaves, 1, MaxOctaves );

		// Amplitudes halve per octave and are pre-divided by their sum, so the field lands in 0..1.
		var amplitude = 0.5f / (1f - MathF.Pow( 0.5f, octaves ));
		for ( var o = 0; o < octaves; o++ )
		{
			AddNoiseOctave( field, period << o, seed + o * 17, amplitude );
			amplitude *= 0.5f;
		}

		return field;
	}

	/// <summary>
	/// One octave of smooth value noise, added to <paramref name="field"/> at <paramref name="amplitude"/>.
	/// Built a row at a time: the two lattice rows around it are hashed once per lattice row and blended
	/// once per pixel row, so each pixel costs two reads and a lerp.
	/// </summary>
	void AddNoiseOctave( float[] field, int period, int seed, float amplitude )
	{
		var size = Size;

		// Lattice column and smoothed fraction depend only on x, so they are computed once per octave.
		var column = new int[size];
		var fraction = new float[size];
		for ( var x = 0; x < size; x++ )
		{
			var g = (float)(x * period) / size;
			column[x] = (int)g;
			fraction[x] = Smooth( g - column[x] );
		}

		// period + 1 entries, so column + 1 never needs wrapping; the last entry is lattice 0 again.
		var upper = new float[period + 1];
		var lower = new float[period + 1];
		var blended = new float[period + 1];
		var cachedRow = -1;

		for ( var y = 0; y < size; y++ )
		{
			var g = (float)(y * period) / size;
			var row = (int)g;
			var ty = Smooth( g - row );

			if ( row != cachedRow )
			{
				for ( var lx = 0; lx <= period; lx++ )
				{
					upper[lx] = Lattice( lx, row, period, seed );
					lower[lx] = Lattice( lx, row + 1, period, seed );
				}
				cachedRow = row;
			}

			for ( var lx = 0; lx <= period; lx++ )
				blended[lx] = (upper[lx] + (lower[lx] - upper[lx]) * ty) * amplitude;

			var offset = y * size;
			for ( var x = 0; x < size; x++ )
			{
				var c = column[x];
				var left = blended[c];
				field[offset + x] += left + (blended[c + 1] - left) * fraction[x];
			}
		}
	}

	/// <summary>Deterministic hash of a wrapped lattice point to 0..1.</summary>
	static float Lattice( int lx, int ly, int period, int seed )
	{
		lx = ((lx % period) + period) % period; ly = ((ly % period) + period) % period;
		var h = (uint)(lx * 374761393 + ly * 668265263 + seed * 2246822519u);
		h = (h ^ (h >> 13)) * 1274126177u;
		return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
	}

	/// <summary>
	/// The tileable fractal noise field itself (0..1, one value per pixel, row-major), for masks a
	/// surface builds on its own — moss in the low spots, ice under thin snow, warped sand ripples —
	/// and then applies with <see cref="Blend"/> or <see cref="AddHeight"/>.
	/// </summary>
	public float[] Noise( int period, int seed, int octaves = 4 ) => Fbm( period, seed, octaves );

	/// <summary>
	/// Moves every pixel toward <paramref name="color"/> (and optionally a height and a roughness) by
	/// its own weight in 0..1 — the per-pixel counterpart of a shape's alpha, for patches that have no
	/// outline: fire-flashing on clay, damp sand, worn carpet, stains.
	/// </summary>
	public void Blend( float[] weight, Color color, float height = float.NaN, float roughness = float.NaN )
	{
		var hasHeight = !float.IsNaN( height );
		var hasRoughness = !float.IsNaN( roughness );
		for ( var i = 0; i < weight.Length; i++ )
		{
			var w = weight[i];
			if ( w <= 0f ) continue;
			if ( w > 1f ) w = 1f;
			_red[i] += (color.r - _red[i]) * w;
			_green[i] += (color.g - _green[i]) * w;
			_blue[i] += (color.b - _blue[i]) * w;
			if ( hasHeight ) _height[i] += (height - _height[i]) * w;
			if ( hasRoughness ) _roughness[i] += (roughness - _roughness[i]) * w;
		}
	}

	/// <summary>
	/// The per-pixel counterpart of <see cref="Blend(float[], Color, float, float)"/> for surfaces made
	/// of many pieces computed per pixel — planks, stones, column tops — each with a colour of its own:
	/// every pixel moves toward <paramref name="palette"/>[<paramref name="index"/>] by that entry's
	/// alpha, so an entry with alpha 0 (by convention entry 0) leaves its pixels alone. One pass and no
	/// weight array, where a Blend per colour would cost a pass and a full-size array each.
	/// </summary>
	public void Blend( byte[] index, Color[] palette )
	{
		for ( var i = 0; i < index.Length; i++ )
		{
			var color = palette[index[i]];
			var w = color.a;
			if ( w <= 0f ) continue;
			_red[i] += (color.r - _red[i]) * w;
			_green[i] += (color.g - _green[i]) * w;
			_blue[i] += (color.b - _blue[i]) * w;
		}
	}

	/// <summary>Adds <paramref name="amount"/> × <paramref name="delta"/> to the height channel, pixel for pixel — for relief computed outside the canvas (ripples, weave).</summary>
	public void AddHeight( float[] delta, float amount )
	{
		for ( var i = 0; i < delta.Length; i++ )
			_height[i] += delta[i] * amount;
	}

	/// <summary>Soft blotchy brightness variation so large surfaces do not read as one flat fill.</summary>
	public void Mottle( float amount, int period, int seed, int octaves = 4 )
	{
		var noise = ScratchFbm( period, seed, octaves );
		for ( var i = 0; i < noise.Length; i++ )
		{
			var k = 1f + (noise[i] - 0.5f) * 2f * amount;
			_red[i] *= k; _green[i] *= k; _blue[i] *= k;
		}
	}

	/// <summary>Fine per-pixel luminance noise; keeps flat colour from looking digital.</summary>
	public void Grain( float amount, int seed )
	{
		// An inline xorshift rather than System.Random: a call per pixel was a measurable share of
		// every surface's cost in the unoptimised build, and grain needs no better randomness.
		var state = (uint)seed * 2654435761u | 1u;
		var scale = 2f * amount / 16777216f;
		for ( var i = 0; i < _red.Length; i++ )
		{
			state ^= state << 13; state ^= state >> 17; state ^= state << 5;
			var k = 1f - amount + (state >> 8) * scale;
			_red[i] *= k; _green[i] *= k; _blue[i] *= k;
		}
	}

	/// <summary>Uneven surface relief (not colour), so the normal map breaks up perfectly flat faces.</summary>
	public void HeightNoise( float amount, int period, int seed, int octaves = 4 )
	{
		var noise = ScratchFbm( period, seed, octaves );
		for ( var i = 0; i < noise.Length; i++ )
			_height[i] += (noise[i] - 0.5f) * amount;
	}

	/// <summary>Patchy variation of the finish — smudges, polished tracks, dull spots — so the sheen is never uniform.</summary>
	public void RoughnessNoise( float amount, int period, int seed, int octaves = 4 )
	{
		var noise = ScratchFbm( period, seed, octaves );
		for ( var i = 0; i < noise.Length; i++ )
			_roughness[i] += (noise[i] - 0.5f) * 2f * amount;
	}

	/// <summary>
	/// Baked ambient occlusion: wherever the surface sits below its neighbourhood (joints, cracks,
	/// grooves) the colour darkens and the finish gets rougher, because light reaches crevices less
	/// and dust collects there. Call after all height is painted.
	/// </summary>
	public void Cavity( float amount, int radius )
	{
		var blurred = BlurHeight( radius );
		for ( var i = 0; i < blurred.Length; i++ )
		{
			var occlusion = (blurred[i] - _height[i]) * (1f / ReliefDepth);
			if ( occlusion <= 0f ) continue;
			if ( occlusion > 1f ) occlusion = 1f;

			var k = 1f - amount * occlusion;
			_red[i] *= k; _green[i] *= k; _blue[i] *= k;
			_roughness[i] += amount * occlusion * CavityRoughening;
		}
	}

	/// <summary>
	/// Worn edges: wherever the surface stands above its neighbourhood (rims, corners, bevels) the
	/// colour lightens and the finish gets smoother, in patches picked by noise so the wear looks
	/// earned rather than outlined. Call after all height is painted.
	/// </summary>
	public void EdgeWear( float amount, int radius, int seed )
	{
		var blurred = BlurHeight( radius );

		// Patches roughly eight wear radii across, whatever the texture size; three octaves are
		// plenty for a mask that only shows along edges.
		var patches = ScratchFbm( Math.Max( 2, Size / (radius * 8) ), seed, 3 );

		for ( var i = 0; i < blurred.Length; i++ )
		{
			var edge = (_height[i] - blurred[i]) * (1f / ReliefDepth);
			if ( edge <= 0f ) continue;
			if ( edge > 1f ) edge = 1f;

			var patch = (patches[i] - 0.48f) * 5f;
			if ( patch <= 0f ) continue;
			if ( patch > 1f ) patch = 1f;

			var wear = amount * edge * patch * patch * (3f - 2f * patch);
			var lift = wear * WornLift;
			_red[i] += (1f - _red[i]) * lift;
			_green[i] += (1f - _green[i]) * lift;
			_blue[i] += (1f - _blue[i]) * lift;
			_roughness[i] -= wear * WearPolish;
		}
	}

	/// <summary>
	/// Separable box blur of the height field, wrapping at the edges so the result stays seamless.
	/// Running sums keep it linear in pixels whatever the radius; the vertical pass walks rows so it
	/// reads memory in order. The result lives in a scratch field: use it before the next blur.
	/// </summary>
	float[] BlurHeight( int radius )
	{
		var size = Size;
		var horizontal = _scratchBlur ??= new float[size * size];
		var output = _scratchBlurOut ??= new float[size * size];
		var weight = 1f / (2 * radius + 1);

		// Which pixel enters and which leaves the window when it steps past position p.
		var entering = new int[size];
		var leaving = new int[size];
		for ( var p = 0; p < size; p++ )
		{
			entering[p] = Wrap( p + radius + 1 );
			leaving[p] = Wrap( p - radius );
		}

		for ( var y = 0; y < size; y++ )
		{
			var offset = y * size;
			var sum = 0f;
			for ( var k = -radius; k <= radius; k++ )
				sum += _height[offset + Wrap( k )];

			for ( var x = 0; x < size; x++ )
			{
				horizontal[offset + x] = sum * weight;
				sum += _height[offset + entering[x]] - _height[offset + leaving[x]];
			}
		}

		var sums = new float[size];
		for ( var k = -radius; k <= radius; k++ )
		{
			var offset = Wrap( k ) * size;
			for ( var x = 0; x < size; x++ )
				sums[x] += horizontal[offset + x];
		}

		for ( var y = 0; y < size; y++ )
		{
			var offset = y * size;
			var enter = entering[y] * size;
			var leave = leaving[y] * size;
			for ( var x = 0; x < size; x++ )
			{
				output[offset + x] = sums[x] * weight;
				sums[x] += horizontal[enter + x] - horizontal[leave + x];
			}
		}

		return output;
	}

	/// <summary>
	/// The painterly two-tone: slopes facing the upper-left get lighter, the others darker, so the
	/// relief reads even where the scene light is flat. Done once, after all height is painted.
	/// </summary>
	public void ShadeFromHeight( float light, float shadow )
	{
		var size = Size;
		for ( var y = 0; y < size; y++ )
		{
			var row = y * size;
			var above = Wrap( y - 1 ) * size;
			var below = Wrap( y + 1 ) * size;

			for ( var x = 0; x < size; x++ )
			{
				var left = x == 0 ? size - 1 : x - 1;
				var right = x == size - 1 ? 0 : x + 1;
				var slope = (_height[row + right] - _height[row + left]) + (_height[below + x] - _height[above + x]);
				var k = slope > 0f ? 1f + MathF.Min( slope, 1f ) * light : 1f + MathF.Max( slope, -1f ) * shadow;

				var i = row + x;
				_red[i] *= k; _green[i] *= k; _blue[i] *= k;
			}
		}
	}

	/// <summary>
	/// Bakes colour, height and roughness into the bytes of a colour texture and a normal texture
	/// (roughness travels in the normal map's blue channel, where the shader reads it per pixel).
	/// Touches nothing but this canvas, so it runs on a worker thread: the arena paints its surfaces
	/// side by side and uploads them afterwards on the main thread (<see cref="PaintedSurface.Upload"/>).
	/// </summary>
	public PaintedSurface Bake( string name, string baseMaterial, float normalStrength )
	{
		var size = Size;
		var color = new byte[size * size * 4];
		var normal = new byte[size * size * 4];

		for ( var y = 0; y < size; y++ )
		{
			var row = y * size;
			var above = Wrap( y - 1 ) * size;
			var below = Wrap( y + 1 ) * size;

			for ( var x = 0; x < size; x++ )
			{
				var i = row + x;
				var o = i * 4;
				var r = _red[i] * 255f; var g = _green[i] * 255f; var b = _blue[i] * 255f;
				color[o + 0] = (byte)(r <= 0f ? 0f : r >= 255f ? 255f : r);
				color[o + 1] = (byte)(g <= 0f ? 0f : g >= 255f ? 255f : g);
				color[o + 2] = (byte)(b <= 0f ? 0f : b >= 255f ? 255f : b);
				color[o + 3] = 255;

				// The shader decodes normals as hemi-octahedral R/G (compiled dev normals are
				// HemiOctIsoRoughness_RG_B); plain RGB normals come out rotated 45 degrees.
				// n = ( -dx, dy, 1 ) normalised, then divided by |x| + |y| + z — the normalisation
				// cancels in that ratio, so it is skipped.
				var left = x == 0 ? size - 1 : x - 1;
				var right = x == size - 1 ? 0 : x + 1;
				var nx = -(_height[row + right] - _height[row + left]) * normalStrength;
				var ny = (_height[below + x] - _height[above + x]) * normalStrength;
				var inverseSum = 1f / ((nx < 0f ? -nx : nx) + (ny < 0f ? -ny : ny) + 1f);
				var ox = nx * inverseSum; var oy = ny * inverseSum;
				normal[o + 0] = (byte)(((ox + oy) * 0.5f + 0.5f) * 255f);
				normal[o + 1] = (byte)(((ox - oy) * 0.5f + 0.5f) * 255f);
				var rough = _roughness[i] * 255f;
				normal[o + 2] = (byte)(rough <= 0f ? 0f : rough >= 255f ? 255f : rough);
				normal[o + 3] = 255;
			}
		}

		return new PaintedSurface( name, baseMaterial, size, color, normal );
	}
}

/// <summary>
/// One painted surface as plain bytes — what a <see cref="Painter"/> hands over from a worker thread.
/// Textures and materials are engine resources and are only made on the main thread, by <see cref="Upload"/>.
/// </summary>
public sealed class PaintedSurface
{
	public string Name { get; }
	public string BaseMaterial { get; }
	public int Size { get; }

	readonly byte[] _color;
	readonly byte[] _normal;

	public PaintedSurface( string name, string baseMaterial, int size, byte[] color, byte[] normal )
	{
		Name = name;
		BaseMaterial = baseMaterial;
		Size = size;
		_color = color;
		_normal = normal;
	}

	/// <summary>
	/// The surface's two textures on a copy of its base material; tint amount is 1 so
	/// ModelRenderer.Tint carries each renderer's tint in full. Main thread only.
	/// </summary>
	public Material Upload()
	{
		var colorTexture = Texture.Create( Size, Size, ImageFormat.RGBA8888 ).WithName( Name + "_color" ).WithData( _color ).WithMips().Finish();
		var normalTexture = Texture.Create( Size, Size, ImageFormat.RGBA8888 ).WithName( Name + "_normal" ).WithData( _normal ).WithMips().Finish();

		var material = Material.Load( BaseMaterial ).CreateCopy( Name );
		material.Set( "Color", colorTexture );
		material.Set( "Normal", normalTexture );
		material.Set( "g_flModelTintAmount", 1f );
		return material;
	}
}
