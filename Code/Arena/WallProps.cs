using System;
using System.Collections.Generic;

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

	/// <summary>
	/// Builds <paramref name="build"/> from a stream of its own, seeded by one draw from the wall's: however
	/// many draws it takes, the props after it come out the same, so a plant's detail can be retuned
	/// without reshuffling the rest of the wall.
	/// </summary>
	protected void Apart( Action build )
	{
		var wall = _random;
		_random = new Random( wall.Next() );
		build();
		_random = wall;
	}

	public float Next() => (float)_random.NextDouble();

	/// <summary>Shuffles <paramref name="list"/> in place from the wall's stream (Fisher–Yates).</summary>
	public void Shuffle<T>( List<T> list )
	{
		for ( var k = list.Count - 1; k > 0; k-- )
		{
			var j = (int)(Next() * (k + 1));
			(list[k], list[j]) = (list[j], list[k]);
		}
	}

	/// <summary>
	/// Deals one of <paramref name="deck"/> to each of <paramref name="count"/> bays in wall order, from the
	/// wall's stream: the deck shuffled, and shuffled afresh whenever it runs out rather than dealt round
	/// again in the same order, and no card dealt next to itself — so neighbouring bays never share a set,
	/// on a wall of any length. <paramref name="allowed"/> can keep a card out of a bay (by its place in the deal).
	/// </summary>
	public List<T> Deal<T>( List<T> deck, int count, Func<int, T, bool> allowed = null )
	{
		var hand = new List<T>();
		var pile = new List<T>();
		for ( var k = 0; k < count; k++ )
		{
			var slot = k;
			bool Fits( T card ) => (slot == 0 || !EqualityComparer<T>.Default.Equals( card, hand[slot - 1] )) && (allowed is null || allowed( slot, card ));
			var i = pile.FindIndex( Fits );
			if ( i < 0 )
			{
				var fresh = new List<T>( deck );
				Shuffle( fresh );
				pile.AddRange( fresh );
				i = pile.FindIndex( Fits );
			}
			hand.Add( pile[i] );
			pile.RemoveAt( i );
		}
		return hand;
	}

	/// <summary>
	/// Keeps <paramref name="card"/> to one bay of <paramref name="hand"/>: any later one is swapped for the first of
	/// <paramref name="spares"/> that matches neither neighbour (or the first spare, where every one would) — for the
	/// pieces a wall should show once, which a long wall's second pass through the deck would deal again.
	/// </summary>
	public void Once<T>( List<T> hand, T card, List<T> spares )
	{
		var seen = false;
		for ( var k = 0; k < hand.Count; k++ )
		{
			if ( !EqualityComparer<T>.Default.Equals( hand[k], card ) ) continue;
			if ( !seen ) { seen = true; continue; }
			var slot = k;
			bool Fits( T spare ) => (slot == 0 || !EqualityComparer<T>.Default.Equals( spare, hand[slot - 1] )) && (slot + 1 >= hand.Count || !EqualityComparer<T>.Default.Equals( spare, hand[slot + 1] ));
			var i = spares.FindIndex( Fits );
			hand[k] = spares[Math.Max( i, 0 )];
		}
	}

	protected float Range( float a, float b ) => a + (b - a) * Next();
	protected int Pick( int count ) => _random.Next( count );

	/// <summary>The stretches of <paramref name="from"/>..<paramref name="to"/> that nothing in <paramref name="taken"/> covers, kept a hair clear of what is there.</summary>
	protected static List<(float From, float To)> Gaps( List<(float From, float To)> taken, float from, float to )
	{
		var sorted = new List<(float From, float To)>( taken );
		sorted.Sort( ( a, b ) => a.From.CompareTo( b.From ) );
		var gaps = new List<(float From, float To)>();
		var at = from;
		foreach ( var (start, end) in sorted )
		{
			if ( start - 2f > at ) gaps.Add( (at, MathF.Min( start - 2f, to )) );
			at = MathF.Max( at, end + 2f );
		}
		if ( to > at ) gaps.Add( (at, to) );
		return gaps.FindAll( g => g.To > g.From );
	}

	/// <summary>A closed circle of <paramref name="segments"/> about <paramref name="centre"/> in the plane of <paramref name="a"/> and <paramref name="b"/>.</summary>
	protected static Vector3[] Loop( Vector3 centre, Vector3 a, Vector3 b, float radius, int segments )
	{
		var path = new Vector3[segments + 1];
		for ( var i = 0; i <= segments; i++ )
		{
			var t = MathF.Tau * i / segments;
			path[i] = centre + (a * MathF.Cos( t ) + b * MathF.Sin( t )) * radius;
		}
		return path;
	}

	/// <summary>
	/// Lays <paramref name="items"/> side by side in <paramref name="from"/>..<paramref name="to"/>, each its width with
	/// <paramref name="air"/> between, the row slid somewhere along the stretch and as often run the other way; what
	/// does not fit is left off the end. Each item builds itself at the centre it is given.
	/// </summary>
	protected void Row( float from, float to, float air, params (float Width, Action<float> Place)[] items )
	{
		var list = new List<(float Width, Action<float> Place)>( items );
		float Total()
		{
			var sum = 0f;
			foreach ( var item in list ) sum += item.Width + air;
			return sum - air;
		}
		while ( list.Count > 0 && Total() > to - from ) list.RemoveAt( list.Count - 1 );
		if ( list.Count == 0 ) return;
		if ( Next() < 0.5f ) list.Reverse();
		var at = from + Range( 0f, to - from - Total() );
		foreach ( var (width, place) in list )
		{
			place( at + width * 0.5f );
			at += width + air;
		}
	}

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
