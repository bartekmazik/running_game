using System;
using System.Collections.Generic;

/// <summary>How a primitive's faces land on a material: tiled at a world scale, or fitted into an atlas cell.</summary>
public readonly struct Skin
{
	public readonly Material Material;

	/// <summary>World units per texture repeat. Zero means "fit": each face spans <see cref="Region"/> once.</summary>
	public readonly float Repeat;

	public readonly Rect Region;

	public Skin( Material material, float repeat )
	{
		Material = material;
		Repeat = repeat;
		Region = new Rect( 0f, 0f, 1f, 1f );
	}

	public Skin( Material material, Rect region )
	{
		Material = material;
		Repeat = 0f;
		Region = region;
	}
}

/// <summary>
/// Builds procedural geometry into one batch per material, then into a single Model — a whole room's
/// walls and props become a handful of draw calls. Flat-shaded on purpose: faceted props suit the
/// flat-colour look and keep the normals honest on bevels.
/// </summary>
public sealed class MeshKit
{
	/// <summary>Spike finding 1: the engine front-faces anticlockwise (seen from the front), so no flip.</summary>
	const bool FlipWinding = false;

	sealed class Batch
	{
		public readonly List<Vertex> Vertices = new();
		public readonly List<int> Indices = new();
	}

	readonly Dictionary<Material, Batch> _batches = new();
	BBox _bounds;
	bool _hasBounds;

	public bool IsEmpty => _batches.Count == 0;

	Batch For( Material material )
	{
		if ( !_batches.TryGetValue( material, out var batch ) )
			_batches[material] = batch = new Batch();
		return batch;
	}

	void Grow( Vector3 p )
	{
		_bounds = _hasBounds ? _bounds.AddPoint( p ) : new BBox( p, p );
		_hasBounds = true;
	}

	/// <summary>
	/// Corners a→b→c→d run anticlockwise seen from the front. UV: U runs along a→b, V grows downward (texture top = face up).
	/// On horizontal faces that puts texture-top toward world +Y and texture-left toward world -X; wall faces put texture-top up.
	/// </summary>
	public void Quad( Skin skin, Vector3 a, Vector3 b, Vector3 c, Vector3 d )
	{
		var uAxis = (b - a).Normal;
		var normal = Vector3.Cross( b - a, d - a ).Normal;
		var vAxis = Vector3.Cross( normal, uAxis ); // up the face

		Vector4 Uv( Vector3 p, float fu, float fv )
		{
			if ( skin.Repeat > 0f )
				return new Vector4( Vector3.Dot( p, uAxis ) / skin.Repeat, -Vector3.Dot( p, vAxis ) / skin.Repeat, 0f, 0f );

			var r = skin.Region;
			return new Vector4( r.Left + fu * r.Width, r.Top + (1f - fv) * r.Height, 0f, 0f );
		}

		var batch = For( skin.Material );
		var start = batch.Vertices.Count;
		batch.Vertices.Add( new Vertex( a, normal, uAxis, Uv( a, 0f, 0f ) ) );
		batch.Vertices.Add( new Vertex( b, normal, uAxis, Uv( b, 1f, 0f ) ) );
		batch.Vertices.Add( new Vertex( c, normal, uAxis, Uv( c, 1f, 1f ) ) );
		batch.Vertices.Add( new Vertex( d, normal, uAxis, Uv( d, 0f, 1f ) ) );
		AddTriangle( batch, start, start + 1, start + 2 );
		AddTriangle( batch, start, start + 2, start + 3 );

		Grow( a ); Grow( b ); Grow( c ); Grow( d );
	}

	public void Tri( Skin skin, Vector3 a, Vector3 b, Vector3 c )
	{
		var uAxis = (b - a).Normal;
		var normal = Vector3.Cross( b - a, c - a ).Normal;
		var vAxis = Vector3.Cross( normal, uAxis );

		Vector4 Uv( Vector3 p, float fu, float fv )
		{
			if ( skin.Repeat > 0f )
				return new Vector4( Vector3.Dot( p, uAxis ) / skin.Repeat, -Vector3.Dot( p, vAxis ) / skin.Repeat, 0f, 0f );
			var r = skin.Region;
			return new Vector4( r.Left + fu * r.Width, r.Top + (1f - fv) * r.Height, 0f, 0f );
		}

		var batch = For( skin.Material );
		var start = batch.Vertices.Count;
		batch.Vertices.Add( new Vertex( a, normal, uAxis, Uv( a, 0f, 0f ) ) );
		batch.Vertices.Add( new Vertex( b, normal, uAxis, Uv( b, 1f, 0f ) ) );
		batch.Vertices.Add( new Vertex( c, normal, uAxis, Uv( c, 0.5f, 1f ) ) );
		AddTriangle( batch, start, start + 1, start + 2 );

		Grow( a ); Grow( b ); Grow( c );
	}

	static void AddTriangle( Batch batch, int a, int b, int c )
	{
		batch.Indices.Add( a );
		batch.Indices.Add( FlipWinding ? c : b );
		batch.Indices.Add( FlipWinding ? b : c );
	}

	/// <summary>A quad whose winding is fixed to face <paramref name="outward"/>, whatever order the corners came in.</summary>
	void QuadFacing( Skin skin, Vector3 outward, Vector3 a, Vector3 b, Vector3 c, Vector3 d )
	{
		if ( Vector3.Dot( Vector3.Cross( b - a, d - a ), outward ) >= 0f ) Quad( skin, a, b, c, d );
		else Quad( skin, d, c, b, a );
	}

	void TriFacing( Skin skin, Vector3 outward, Vector3 a, Vector3 b, Vector3 c )
	{
		if ( Vector3.Dot( Vector3.Cross( b - a, c - a ), outward ) >= 0f ) Tri( skin, a, b, c );
		else Tri( skin, a, c, b );
	}

	public void Box( Skin skin, Vector3 center, Vector3 size ) => Box( skin, center, size, Rotation.Identity );

	public void Box( Skin skin, Vector3 center, Vector3 size, Rotation rotation ) => BevelBox( skin, center, size, 0f, rotation );

	public void BevelBox( Skin skin, Vector3 center, Vector3 size, float bevel ) => BevelBox( skin, center, size, bevel, Rotation.Identity );

	/// <summary>
	/// A box with every edge chamfered by <paramref name="bevel"/>: six inset faces, twelve edge strips,
	/// eight corner triangles. The chamfers catch light, which is what makes a generated block read
	/// as an object rather than a grey cut-out. Zero bevel gives a plain six-face box.
	/// </summary>
	public void BevelBox( Skin skin, Vector3 center, Vector3 size, float bevel, Rotation rotation )
	{
		var h = size * 0.5f;
		bevel = MathF.Min( bevel, MathF.Min( h.x, MathF.Min( h.y, h.z ) ) * 0.9f );
		var i = new Vector3( h.x - bevel, h.y - bevel, h.z - bevel );

		Vector3 P( float x, float y, float z ) => center + rotation * new Vector3( x, y, z );
		Vector3 Out( float x, float y, float z ) => rotation * new Vector3( x, y, z );

		for ( var s = -1; s <= 1; s += 2 )
		{
			QuadFacing( skin, Out( s, 0, 0 ), P( s * h.x, -i.y, -i.z ), P( s * h.x, i.y, -i.z ), P( s * h.x, i.y, i.z ), P( s * h.x, -i.y, i.z ) );
			QuadFacing( skin, Out( 0, s, 0 ), P( -i.x, s * h.y, -i.z ), P( i.x, s * h.y, -i.z ), P( i.x, s * h.y, i.z ), P( -i.x, s * h.y, i.z ) );
			QuadFacing( skin, Out( 0, 0, s ), P( -i.x, -i.y, s * h.z ), P( i.x, -i.y, s * h.z ), P( i.x, i.y, s * h.z ), P( -i.x, i.y, s * h.z ) );
		}

		if ( bevel <= 0f ) return;

		for ( var sa = -1; sa <= 1; sa += 2 )
		for ( var sb = -1; sb <= 1; sb += 2 )
		{
			// Edges along Z (between the X and Y faces), along Y (X and Z), along X (Y and Z).
			QuadFacing( skin, Out( sa, sb, 0 ),
				P( sa * h.x, sb * i.y, -i.z ), P( sa * h.x, sb * i.y, i.z ), P( sa * i.x, sb * h.y, i.z ), P( sa * i.x, sb * h.y, -i.z ) );
			QuadFacing( skin, Out( sa, 0, sb ),
				P( sa * h.x, -i.y, sb * i.z ), P( sa * h.x, i.y, sb * i.z ), P( sa * i.x, i.y, sb * h.z ), P( sa * i.x, -i.y, sb * h.z ) );
			QuadFacing( skin, Out( 0, sa, sb ),
				P( -i.x, sa * h.y, sb * i.z ), P( i.x, sa * h.y, sb * i.z ), P( i.x, sa * i.y, sb * h.z ), P( -i.x, sa * i.y, sb * h.z ) );

			for ( var sc = -1; sc <= 1; sc += 2 )
				TriFacing( skin, Out( sa, sb, sc ), P( sa * h.x, sb * i.y, sc * i.z ), P( sa * i.x, sb * h.y, sc * i.z ), P( sa * i.x, sb * i.y, sc * h.z ) );
		}
	}

	public void Lathe( Skin skin, Vector3 baseCenter, Vector2[] profile, int sides ) => Lathe( skin, baseCenter, profile, sides, Rotation.Identity );

	/// <summary>
	/// Spins a (radius, height) profile around the local Z axis — pots, lamps, pillars, bottles, cones.
	/// Faceted: each segment is its own quad, so a 10-sided pot reads as a crafted object.
	/// </summary>
	public void Lathe( Skin skin, Vector3 baseCenter, Vector2[] profile, int sides, Rotation rotation, bool capBottom = true, bool capTop = true )
	{
		Vector3 Ring( int step, Vector2 rz )
		{
			var angle = step * MathF.Tau / sides;
			return baseCenter + rotation * new Vector3( MathF.Cos( angle ) * rz.x, MathF.Sin( angle ) * rz.x, rz.y );
		}

		for ( var p = 0; p < profile.Length - 1; p++ )
		for ( var s = 0; s < sides; s++ )
		{
			var a = Ring( s, profile[p] );
			var b = Ring( s + 1, profile[p] );
			var c = Ring( s + 1, profile[p + 1] );
			var d = Ring( s, profile[p + 1] );
			var mid = (a + b + c + d) * 0.25f;
			var axisPoint = baseCenter + rotation * new Vector3( 0f, 0f, (profile[p].y + profile[p + 1].y) * 0.5f );
			var outward = mid - axisPoint;

			var bottomApex = profile[p].x <= 0.0001f;
			var topApex = profile[p + 1].x <= 0.0001f;

			if ( bottomApex && topApex ) continue;
			if ( topApex ) TriFacing( skin, outward, a, b, c );        // c == d: cone tip above
			else if ( bottomApex ) TriFacing( skin, outward, a, c, d ); // a == b: cone tip below
			else QuadFacing( skin, outward, a, b, c, d );
		}

		var up = rotation * Vector3.Up;
		if ( capBottom && profile[0].x > 0.0001f )
		{
			var centre = baseCenter + rotation * new Vector3( 0f, 0f, profile[0].y );
			for ( var s = 0; s < sides; s++ )
				TriFacing( skin, -up, centre, Ring( s, profile[0] ), Ring( s + 1, profile[0] ) );
		}

		var last = profile[^1];
		if ( capTop && last.x > 0.0001f )
		{
			var centre = baseCenter + rotation * new Vector3( 0f, 0f, last.y );
			for ( var s = 0; s < sides; s++ )
				TriFacing( skin, up, centre, Ring( s, last ), Ring( s + 1, last ) );
		}
	}

	public void Sphere( Skin skin, Vector3 center, float radius, int rings, int sides )
	{
		var profile = new Vector2[rings + 1];
		for ( var r = 0; r <= rings; r++ )
		{
			var a = MathF.PI * r / rings - MathF.PI * 0.5f;
			profile[r] = new Vector2( MathF.Cos( a ) * radius, MathF.Sin( a ) * radius + radius );
		}
		Lathe( skin, center - Vector3.Up * radius, profile, sides, Rotation.Identity, false, false );
	}

	public Model Build( string name )
	{
		var builder = Model.Builder.WithName( name );

		foreach ( var (material, batch) in _batches )
		{
			var mesh = new Mesh( material );
			mesh.CreateVertexBuffer( batch.Vertices.Count, batch.Vertices );
			mesh.CreateIndexBuffer( batch.Indices.Count, batch.Indices );
			mesh.Bounds = _bounds;
			builder.AddMesh( mesh );
		}

		return builder.Create();
	}
}
