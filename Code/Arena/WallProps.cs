using System;

/// <summary>
/// What every theme's prop builder stands on: wall space and the rules for what may stand in it.
///
/// A point is (u along the wall, depth into it, v up), as <see cref="FloorContext.OnWall"/> takes them,
/// and a direction likewise; a rotation built in wall space (x along, y into the wall, z up) is turned
/// into the world by <see cref="Turn"/>. Below <see cref="Clear"/> nothing may reach in front of
/// <see cref="Front"/> — just inside the collider plane, the edge of the play volume — and above it
/// a prop may lean out <see cref="Overhang"/> over the room. Each wall draws from a stream of its own
/// (<see cref="Begin"/>), so no two walls are dressed alike, and every machine builds the same room.
/// Everything lands in the floor's <see cref="MeshKit"/>.
/// </summary>
public abstract class WallProps
{
	/// <summary>Below this height props stay inside the wall's thickness; above it they may reach <see cref="Overhang"/> out over the room.</summary>
	public const float Clear = 140f;

	/// <summary>How far a prop above <see cref="Clear"/> may reach in front of the collider plane (the limit is 24; this keeps a margin).</summary>
	public const float Overhang = 22f;

	/// <summary>The nearest depth a prop below <see cref="Clear"/> may reach: a hair behind the collider plane.</summary>
	public const float Front = 2f;

	/// <summary>The wall face behind the bays, less a hair: what stands against the wall stands here.</summary>
	public const float Face = FloorContext.Recess - 1.5f;

	/// <summary>The floor of the bays: the slab edge's top, a step below the tiles.</summary>
	public const float Ledge = FloorContext.LedgeTop;

	protected readonly FloorContext Room;
	protected readonly MeshKit Kit;
	protected int Side;
	protected Rotation Run;
	Random _random;

	protected WallProps( FloorContext room )
	{
		Room = room;
		Kit = room.Kit;
	}

	/// <summary>Works on wall <paramref name="side"/> from here on, drawing from a stream of its own.</summary>
	public void Begin( int side, int seed )
	{
		OnSide( side );
		_random = new Random( seed );
	}

	/// <summary>Moves to wall <paramref name="side"/>, carrying on the same stream.</summary>
	protected void OnSide( int side )
	{
		Side = side;
		Run = Rotation.LookAt( FloorContext.Along( side ), Vector3.Up );
	}

	public float Next() => (float)_random.NextDouble();
	protected float Range( float a, float b ) => a + (b - a) * Next();
	protected int Pick( int count ) => _random.Next( count );

	protected Vector3 At( Vector3 w ) => Room.OnWall( Side, w.x, w.z, w.y );
	protected Vector3[] At( Vector3[] path ) => Array.ConvertAll( path, p => At( p ) );

	/// <summary>A wall-space rotation as the world sees it: wall space's x, y, z are along, into and up.</summary>
	protected Rotation Turn( Rotation local ) => Run * local;

	/// <summary>
	/// A wall-space rotation whose Forward is <paramref name="direction"/>, rolled <paramref name="roll"/>
	/// degrees about it, its Up as near <paramref name="up"/> (default world up) as it allows.
	/// </summary>
	protected static Rotation Aim( Vector3 direction, float roll, Vector3 up = default )
	{
		if ( up == Vector3.Zero ) up = Vector3.Up;
		if ( MathF.Abs( Vector3.Dot( direction, up ) ) > 0.97f ) up = new Vector3( up.z, up.x, up.y );
		return Rotation.LookAt( direction, up ) * Rotation.FromRoll( roll );
	}

	/// <summary>A level direction <paramref name="yaw"/> radians round from along the wall.</summary>
	protected static Vector3 Flat( float yaw ) => new( MathF.Cos( yaw ), MathF.Sin( yaw ), 0f );

	/// <summary>The nearest depth a prop may reach at height <paramref name="v"/>.</summary>
	protected static float Nearest( float v ) => v < Clear ? Front : -Overhang;

	/// <summary>A point <paramref name="t"/> of the way along a path, by its points.</summary>
	protected static Vector3 Along( Vector3[] path, float t )
	{
		var f = Math.Clamp( t, 0f, 1f ) * (path.Length - 1);
		var i = Math.Min( (int)f, path.Length - 2 );
		return path[i] + (path[i + 1] - path[i]) * (f - i);
	}

	/// <summary>
	/// Keeps a path in bounds, in place: points that would leave them are pulled back in front of the wall
	/// face and behind <see cref="Nearest"/>, by <paramref name="radius"/> more for a tube that thick.
	/// </summary>
	protected static void Inside( Vector3[] path, float radius = 0f )
	{
		for ( var i = 0; i < path.Length; i++ )
			path[i] = path[i].WithY( Math.Clamp( path[i].y, Nearest( path[i].z ) + 0.5f + radius, Face ) );
	}
}
