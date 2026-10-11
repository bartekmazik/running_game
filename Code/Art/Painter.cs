using System;

/// <summary>
/// A small canvas for painting surfaces in code: colour plus a height channel that becomes a normal
/// map, so bricks and planks catch the light instead of being flat prints. Everything wraps at the
/// edges, so any surface painted here tiles seamlessly.
///
/// The style it is built for is the user's references: flat colour shapes, a soft two-tone shade,
/// a little grain — no photographic noise and no pixel art.
/// </summary>
public sealed class Painter
{
	public const string Matte = "materials/dev/dev_nonmetal_rough70.vmat";
	public const string Metal = "materials/dev/dev_metal_rough50.vmat";

	public int Size { get; }

	readonly Vector3[] _color;
	readonly float[] _height;

	public Painter( int size, Color fill )
	{
		Size = size;
		_color = new Vector3[size * size];
		_height = new float[size * size];
		Array.Fill( _color, new Vector3( fill.r, fill.g, fill.b ) );
	}

	int Wrap( int v ) => ((v % Size) + Size) % Size;
	int Index( int x, int y ) => Wrap( y ) * Size + Wrap( x );

	/// <summary>The one primitive: fills wherever the signed distance is negative, with a 1-px soft edge, wrapping across the texture edges.</summary>
	public void Shape( Func<float, float, float> sdf, float minX, float minY, float maxX, float maxY, Color color, float alpha = 1f, float height = float.NaN, float bevel = 0f )
	{
		var target = new Vector3( color.r, color.g, color.b );

		for ( var y = (int)MathF.Floor( minY ) - 1; y <= (int)MathF.Ceiling( maxY ) + 1; y++ )
		for ( var x = (int)MathF.Floor( minX ) - 1; x <= (int)MathF.Ceiling( maxX ) + 1; x++ )
		{
			var d = sdf( x + 0.5f, y + 0.5f );
			var coverage = Math.Clamp( 0.5f - d, 0f, 1f );
			if ( coverage <= 0f ) continue;

			var i = Index( x, y );
			_color[i] = Vector3.Lerp( _color[i], target, coverage * alpha );

			if ( float.IsNaN( height ) ) continue;
			var shoulder = bevel > 0f ? Smooth( Math.Clamp( -d / bevel, 0f, 1f ) ) : 1f;
			_height[i] = MathX.Lerp( _height[i], height * shoulder, coverage );
		}
	}

	static float Smooth( float t ) => t * t * (3f - 2f * t);

	/// <summary>Rounded rectangle; the workhorse for bricks, planks and panels.</summary>
	public void Rect( float x, float y, float w, float h, Color color, float radius = 0f, float alpha = 1f, float height = float.NaN, float bevel = 0f )
	{
		var cx = x + w * 0.5f; var cy = y + h * 0.5f;
		var hx = w * 0.5f - radius; var hy = h * 0.5f - radius;
		Shape( ( px, py ) =>
		{
			var qx = MathF.Abs( px - cx ) - hx; var qy = MathF.Abs( py - cy ) - hy;
			var outside = new Vector2( MathF.Max( qx, 0f ), MathF.Max( qy, 0f ) ).Length;
			return outside + MathF.Min( MathF.Max( qx, qy ), 0f ) - radius;
		}, x, y, x + w, y + h, color, alpha, height, bevel );
	}

	/// <summary>Disc; rivets, dots and blobs.</summary>
	public void Circle( float cx, float cy, float r, Color color, float alpha = 1f, float height = float.NaN, float bevel = 0f )
	{
		Shape( ( px, py ) => new Vector2( px - cx, py - cy ).Length - r, cx - r, cy - r, cx + r, cy + r, color, alpha, height, bevel );
	}

	/// <summary>Round-capped line segment; mortar, cracks and stripes.</summary>
	public void Line( Vector2 a, Vector2 b, float width, Color color, float alpha = 1f, float height = float.NaN )
	{
		var ab = b - a;
		var lengthSq = MathF.Max( ab.LengthSquared, 0.0001f );
		Shape( ( px, py ) =>
		{
			var p = new Vector2( px, py ) - a;
			var t = Math.Clamp( Vector2.Dot( p, ab ) / lengthSq, 0f, 1f );
			return (p - ab * t).Length - width * 0.5f;
		}, MathF.Min( a.x, b.x ) - width, MathF.Min( a.y, b.y ) - width, MathF.Max( a.x, b.x ) + width, MathF.Max( a.y, b.y ) + width, color, alpha, height );
	}

	/// <summary>Tileable value noise in 0..1 at a lattice period that divides the texture size.</summary>
	float Noise( float x, float y, int period, int seed )
	{
		var cell = (float)Size / period;
		var gx = x / cell; var gy = y / cell;
		var x0 = (int)MathF.Floor( gx ); var y0 = (int)MathF.Floor( gy );
		var tx = Smooth( gx - x0 ); var ty = Smooth( gy - y0 );

		float Lattice( int lx, int ly )
		{
			lx = ((lx % period) + period) % period; ly = ((ly % period) + period) % period;
			var h = (uint)(lx * 374761393 + ly * 668265263 + seed * 2246822519u);
			h = (h ^ (h >> 13)) * 1274126177u;
			return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
		}

		var a = MathX.Lerp( Lattice( x0, y0 ), Lattice( x0 + 1, y0 ), tx );
		var b = MathX.Lerp( Lattice( x0, y0 + 1 ), Lattice( x0 + 1, y0 + 1 ), tx );
		return MathX.Lerp( a, b, ty );
	}

	float Fbm( float x, float y, int period, int seed )
	{
		var sum = 0f; var amp = 0.5f;
		for ( var o = 0; o < 4; o++ )
		{
			sum += Noise( x, y, period << o, seed + o * 17 ) * amp;
			amp *= 0.5f;
		}
		return sum / 0.9375f;
	}

	/// <summary>Soft blotchy brightness variation so large surfaces do not read as one flat fill.</summary>
	public void Mottle( float amount, int period, int seed )
	{
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
		{
			var i = y * Size + x;
			_color[i] *= 1f + (Fbm( x, y, period, seed ) - 0.5f) * 2f * amount;
		}
	}

	/// <summary>Fine per-pixel luminance noise; keeps flat colour from looking digital.</summary>
	public void Grain( float amount, int seed )
	{
		var random = new System.Random( seed );
		for ( var i = 0; i < _color.Length; i++ )
			_color[i] *= 1f + ((float)random.NextDouble() - 0.5f) * 2f * amount;
	}

	/// <summary>Uneven surface relief (not colour), so the normal map breaks up perfectly flat faces.</summary>
	public void HeightNoise( float amount, int period, int seed )
	{
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
			_height[y * Size + x] += (Fbm( x, y, period, seed ) - 0.5f) * amount;
	}

	/// <summary>
	/// The painterly two-tone: slopes facing the upper-left get lighter, the others darker, so the
	/// relief reads even where the scene light is flat. Done once, after all height is painted.
	/// </summary>
	public void ShadeFromHeight( float light, float shadow )
	{
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
		{
			var slope = (_height[Index( x - 1, y )] - _height[Index( x + 1, y )]) + (_height[Index( x, y - 1 )] - _height[Index( x, y + 1 )]);
			var i = y * Size + x;
			_color[i] *= slope > 0f ? 1f + MathF.Min( slope, 1f ) * light : 1f + MathF.Max( slope, -1f ) * shadow;
		}
	}

	/// <summary>Bakes colour and height into textures on a copy of the base material; tint amount is 1 so ModelRenderer.Tint carries each floor's hue.</summary>
	public Material ToMaterial( string name, string baseMaterial, float normalStrength, float roughness = 0.75f )
	{
		var color = new byte[Size * Size * 4];
		var normal = new byte[Size * Size * 4];
		var roughnessByte = (byte)(Math.Clamp( roughness, 0f, 1f ) * 255f);

		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
		{
			var i = y * Size + x;
			var c = _color[i];
			color[i * 4 + 0] = (byte)(Math.Clamp( c.x, 0f, 1f ) * 255f);
			color[i * 4 + 1] = (byte)(Math.Clamp( c.y, 0f, 1f ) * 255f);
			color[i * 4 + 2] = (byte)(Math.Clamp( c.z, 0f, 1f ) * 255f);
			color[i * 4 + 3] = 255;

			var dx = (_height[Index( x + 1, y )] - _height[Index( x - 1, y )]) * normalStrength;
			var dy = (_height[Index( x, y + 1 )] - _height[Index( x, y - 1 )]) * normalStrength;
			EncodeNormal( new Vector3( -dx, dy, 1f ).Normal, roughnessByte, normal, i * 4 );
		}

		var colorTexture = Texture.Create( Size, Size, ImageFormat.RGBA8888 ).WithName( name + "_color" ).WithData( color ).WithMips().Finish();
		var normalTexture = Texture.Create( Size, Size, ImageFormat.RGBA8888 ).WithName( name + "_normal" ).WithData( normal ).WithMips().Finish();

		var material = Material.Load( baseMaterial ).CreateCopy( name );
		material.Set( "Color", colorTexture );
		material.Set( "Normal", normalTexture );
		material.Set( "g_flModelTintAmount", 1f );
		return material;
	}

	/// <summary>
	/// The engine's shader decodes the normal map as hemi-octahedral in R/G (compiled dev normals are
	/// HemiOctIsoRoughness_RG_B), with roughness in B; plain RGB normals come out rotated 45 degrees.
	/// </summary>
	static void EncodeNormal( Vector3 n, byte roughness, byte[] target, int offset )
	{
		var sum = MathF.Abs( n.x ) + MathF.Abs( n.y ) + n.z;
		var ox = n.x / sum; var oy = n.y / sum;
		target[offset + 0] = (byte)(((ox + oy) * 0.5f + 0.5f) * 255f);
		target[offset + 1] = (byte)(((ox - oy) * 0.5f + 0.5f) * 255f);
		target[offset + 2] = roughness;
		target[offset + 3] = 255;
	}
}
