using System;

/// <summary>
/// One short-lived visual piece: a dev model that grows or shrinks, fades, drifts and then removes
/// itself. Every effect in the game is built from these — particle systems are out of scope
/// (CLAUDE.md), and an emissive dev box or sphere under the camera's bloom reads as a cartoon puff.
/// Local presentation only; never networked.
/// </summary>
public sealed class FxPiece : Component
{
	public float Life { get; set; } = 0.5f;

	/// <summary>Seconds before the piece appears — lets a line of pieces ripple outwards.</summary>
	public float Delay { get; set; }

	public Vector3 StartScale { get; set; } = Vector3.One;
	public Vector3 EndScale { get; set; } = Vector3.One;

	public Color StartTint { get; set; } = Color.White;
	public Color EndTint { get; set; } = Color.Black;

	public Vector3 Velocity { get; set; }
	public float Gravity { get; set; }

	/// <summary>Degrees per second about Z.</summary>
	public float Spin { get; set; }

	/// <summary>Scale with an overshoot ("pop") rather than a straight lerp.</summary>
	public bool Pop { get; set; }

	ModelRenderer _renderer;
	TimeSince _age;

	protected override void OnStart()
	{
		_renderer = GetComponent<ModelRenderer>();
		_age = 0f;
		_renderer.Enabled = Delay <= 0f;
		Apply( 0f );
	}

	protected override void OnUpdate()
	{
		var age = _age - Delay;
		if ( age < 0f ) return;

		if ( !_renderer.Enabled ) _renderer.Enabled = true;

		var t = age / Life;
		if ( t >= 1f )
		{
			GameObject.Destroy();
			return;
		}

		Velocity += Vector3.Down * Gravity * Time.Delta;
		WorldPosition += Velocity * Time.Delta;

		if ( Spin != 0f )
			WorldRotation *= Rotation.FromYaw( Spin * Time.Delta );

		Apply( t );
	}

	void Apply( float t )
	{
		var eased = Pop ? PopCurve( t ) : 1f - (1f - t) * (1f - t);
		LocalScale = Vector3.Lerp( StartScale, EndScale, eased );
		_renderer.Tint = Color.Lerp( StartTint, EndTint, t * t );
	}

	/// <summary>0 → 1.15 by a third of the way → settles at 1. A toy "boing".</summary>
	static float PopCurve( float t )
	{
		if ( t < 0.33f ) return t / 0.33f * 1.15f;
		return 1.15f - (t - 0.33f) / 0.67f * 0.15f;
	}
}

/// <summary>
/// A piece that circles a point — vortex debris, dizzy stars, a bubble shield's motes.
/// Follows an optional target so it can ride along with a player.
/// </summary>
public sealed class FxOrbit : Component
{
	public GameObject Target { get; set; }
	public Vector3 Centre { get; set; }
	public Vector3 Offset { get; set; }
	public float Radius { get; set; } = 40f;
	public float Speed { get; set; } = 360f;
	public float Phase { get; set; }
	public float Bob { get; set; }
	public float Life { get; set; } = 2f;

	/// <summary>Shrink the radius to nothing over the life — debris sucked into a vortex.</summary>
	public bool Collapse { get; set; }

	TimeSince _age;

	protected override void OnStart() => _age = 0f;

	protected override void OnUpdate()
	{
		if ( _age > Life || (Target is not null && !Target.IsValid()) )
		{
			GameObject.Destroy();
			return;
		}

		var t = _age / Life;
		var centre = (Target.IsValid() ? Target.WorldPosition : Centre) + Offset;
		var angle = Phase + Speed * _age;
		var radius = Collapse ? Radius * (1f - t) : Radius;
		var bob = Bob * MathF.Sin( (Phase + _age * 360f) * MathF.PI / 180f );

		WorldPosition = centre + Rotation.FromYaw( angle ).Forward * radius + Vector3.Up * bob;
		WorldRotation = Rotation.FromYaw( angle * 2f );
	}
}

/// <summary>
/// Visual vocabulary for abilities, built from dev models. Static helpers that spawn local,
/// unsaved, unnetworked GameObjects; each cleans itself up via <see cref="FxPiece"/> or
/// <see cref="FxOrbit"/>.
/// </summary>
public static class AbilityFx
{
	/// <summary>
	/// Emissive white, for the few things that should bloom (a hit flash). Anything coloured uses
	/// the matte surface instead: under bloom and the tonemapper an emissive pastel blows out to
	/// white, and the whole point of a pastel puff is its colour.
	/// </summary>
	public const string GlowMaterialPath = "materials/dev/primary_white_emissive.vmat";

	public const string MatteMaterialPath = ArenaGenerator.SurfaceMaterialPath;

	static Material _matte;

	/// <summary>
	/// The matte dev surface with full model tint — out of the box it applies only part of the tint,
	/// which turns pastels grey (the same fix Painter makes for the arena's surfaces). Built once; the
	/// source material never changes, so a static is safe here.
	/// </summary>
	public static Material Matte
	{
		get
		{
			if ( _matte is not null ) return _matte;

			_matte = Material.Load( MatteMaterialPath ).CreateCopy( "tnt_fx_matte" );
			_matte.Set( "g_flModelTintAmount", 1f );
			return _matte;
		}
	}

	const string BoxPath = "models/dev/box.vmdl";
	const string SpherePath = "models/dev/sphere.vmdl";

	/// <summary>Pastel effect colours. ≤ 1 per channel: Tint is 8-bit and wraps.</summary>
	public static readonly Color Mint = new( 0.46f, 0.92f, 0.70f );
	public static readonly Color Coral = new( 1.00f, 0.46f, 0.48f );
	public static readonly Color Butter = new( 1.00f, 0.86f, 0.40f );
	public static readonly Color Sky = new( 0.52f, 0.80f, 1.00f );
	public static readonly Color Lilac = new( 0.80f, 0.60f, 1.00f );
	public static readonly Color Ice = new( 0.72f, 0.94f, 1.00f );
	public static readonly Color Glue = new( 0.62f, 0.90f, 0.34f );
	public static readonly Color Ink = new( 0.20f, 0.18f, 0.30f );

	/// <summary>Model size in units, so callers think in world sizes rather than model scales.</summary>
	static float ModelUnits( Model model ) => MathF.Max( model.Bounds.Size.x, 1f );

	static GameObject Make( string modelPath, Vector3 position, out ModelRenderer renderer, string name = "Fx", bool glow = false )
	{
		var go = new GameObject( true, name );
		go.Flags |= GameObjectFlags.NotSaved;
		go.NetworkMode = NetworkMode.Never;
		go.WorldPosition = position;

		renderer = go.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( modelPath );
		renderer.MaterialOverride = glow ? Material.Load( GlowMaterialPath ) : Matte;
		renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
		return go;
	}

	public static FxPiece Piece( bool sphere, Vector3 position, Vector3 startSize, Vector3 endSize, Color start, Color end, float life, bool glow = false )
	{
		var go = Make( sphere ? SpherePath : BoxPath, position, out var renderer, "Fx", glow );
		var units = ModelUnits( renderer.Model );

		var piece = go.AddComponent<FxPiece>();
		piece.Life = life;
		piece.StartScale = startSize / units;
		piece.EndScale = endSize / units;
		piece.StartTint = start;
		piece.EndTint = end;
		return piece;
	}

	/// <summary>
	/// A ring of cubes racing outwards to <paramref name="radius"/> — the reach of an area effect.
	/// A solid disc read as a coloured floor, not a wave; a broken ring reads as motion.
	/// </summary>
	public static void Ring( Vector3 centre, float radius, Color color, float life = 0.45f )
	{
		var count = Math.Clamp( (int)(radius / 10f), 10, 36 );

		for ( var i = 0; i < count; i++ )
		{
			var dir = Rotation.FromYaw( i * 360f / count ).Forward;
			var piece = Piece( false, centre + Vector3.Up * 10f + dir * 16f, new Vector3( 12f, 12f, 8f ), new Vector3( 4f ), color, color.Darken( 0.5f ), life );
			piece.Velocity = dir * (radius - 16f) / life;
			piece.Spin = 240f;
		}
	}

	/// <summary>Cubes flung out and up from a point — a cast, a pop, a hit.</summary>
	public static void Burst( Vector3 position, Color color, int count = 8, float speed = 220f, float size = 9f )
	{
		for ( var i = 0; i < count; i++ )
		{
			var yaw = i * 360f / count + Game.Random.Next( 0, 30 );
			var dir = Rotation.FromYaw( yaw ).Forward;

			var piece = Piece( false, position, new Vector3( size ), new Vector3( 1f ), color, color.Darken( 0.6f ), 0.55f );
			piece.Velocity = dir * speed * (0.7f + Game.Random.NextSingle() * 0.6f) + Vector3.Up * (speed * 0.9f);
			piece.Gravity = 900f;
			piece.Spin = 360f;
		}
	}

	/// <summary>A row of popping cubes from A to B — Trench, Bridge, Hook, Blink.</summary>
	public static void Line( Vector3 from, Vector3 to, Color color, float spacing = 28f, float size = 12f )
	{
		var delta = to - from;
		var steps = Math.Max( 1, (int)(delta.Length / spacing) );

		for ( var i = 0; i <= steps; i++ )
		{
			var piece = Piece( false, from + delta * (i / (float)steps), new Vector3( 0.5f ), new Vector3( size ), color, color.Darken( 0.7f ), 0.5f );
			piece.Delay = i * 0.025f;
			piece.Pop = true;
		}
	}

	/// <summary>A column of light shooting up — Super Jump, Updraft, Catapult.</summary>
	public static void Column( Vector3 position, Color color, float height = 220f )
	{
		var piece = Piece( false, position + Vector3.Up * (height * 0.5f), new Vector3( 30f, 30f, 10f ), new Vector3( 6f, 6f, height ), color, color.Darken( 0.4f ), 0.5f );
		piece.Spin = 180f;
	}

	/// <summary>A quick flash on a body — something hit them.</summary>
	public static void Flash( Vector3 position, Color color, float size = 60f )
	{
		var piece = Piece( true, position, new Vector3( size * 0.3f ), new Vector3( size ), color, Color.Black, 0.3f, true );
		piece.Pop = true;
	}

	/// <summary>Debris that circles a point and is sucked in — Vortex.</summary>
	public static void Whirl( Vector3 centre, float radius, Color color, float life )
	{
		for ( var i = 0; i < 12; i++ )
		{
			var go = Make( BoxPath, centre, out var renderer );
			go.LocalScale = new Vector3( 10f / ModelUnits( renderer.Model ) );
			renderer.Tint = i % 2 == 0 ? color : color.Darken( 0.25f );

			var orbit = go.AddComponent<FxOrbit>();
			orbit.Centre = centre;
			orbit.Offset = Vector3.Up * (12f + i * 6f);
			orbit.Radius = radius * (0.6f + (i % 3) * 0.2f);
			orbit.Speed = 300f + i * 15f;
			orbit.Phase = i * 30f;
			orbit.Life = life;
			orbit.Collapse = true;
		}
	}

	/// <summary>
	/// Small stars circling above a head — stunned, or dazed after a knockdown. The classic cartoon
	/// read for "this one is out of it", from three emissive cubes.
	/// </summary>
	public static void Dizzy( GameObject target, float life, Color color )
	{
		for ( var i = 0; i < 3; i++ )
		{
			var go = Make( BoxPath, target.WorldPosition, out var renderer, "Dizzy" );
			go.LocalScale = new Vector3( 7f / ModelUnits( renderer.Model ) );
			renderer.Tint = color;

			var orbit = go.AddComponent<FxOrbit>();
			orbit.Target = target;
			orbit.Offset = Vector3.Up * 84f;
			orbit.Radius = 16f;
			orbit.Speed = 420f;
			orbit.Phase = i * 120f;
			orbit.Bob = 3f;
			orbit.Life = life;
		}
	}

	/// <summary>
	/// Motes circling a body for as long as a status lasts — Bubble Shield. Returned so the caller
	/// can pop them early when the shield is used up.
	/// </summary>
	public static List<GameObject> Bubble( GameObject target, float life, Color color )
	{
		var motes = new List<GameObject>();

		for ( var i = 0; i < 8; i++ )
		{
			var go = Make( SpherePath, target.WorldPosition, out var renderer, "Bubble" );
			go.LocalScale = new Vector3( 9f / ModelUnits( renderer.Model ) );
			renderer.Tint = color;

			var orbit = go.AddComponent<FxOrbit>();
			orbit.Target = target;
			orbit.Offset = Vector3.Up * (20f + (i % 4) * 14f);
			orbit.Radius = 34f;
			orbit.Speed = i % 2 == 0 ? 200f : -200f;
			orbit.Phase = i * 45f;
			orbit.Bob = 6f;
			orbit.Life = life;

			motes.Add( go );
		}

		return motes;
	}

	/// <summary>The lobbed TNT block: arcs to its target, then blinks until it goes off.</summary>
	public static void Bomb( Vector3 from, Vector3 to, float fuse )
	{
		var go = Make( BoxPath, from, out var renderer, "TNT" );
		go.LocalScale = new Vector3( 22f / ModelUnits( renderer.Model ) );
		renderer.Tint = Coral;

		var bomb = go.AddComponent<FxBomb>();
		bomb.From = from;
		bomb.To = to;
		bomb.Fuse = fuse;
	}

	/// <summary>A planted mine: a squat disc with a blinking dome. Returned so the owner can remove it.</summary>
	public static GameObject Mine( Vector3 position )
	{
		var go = Make( SpherePath, position + Vector3.Up * 4f, out var renderer, "Mine" );
		go.LocalScale = new Vector3( 26f, 26f, 8f ) / ModelUnits( renderer.Model );
		renderer.Tint = Ink;

		var dome = Make( SpherePath, position + Vector3.Up * 9f, out var domeRenderer, "Mine Light" );
		dome.SetParent( go, true );
		dome.LocalScale = new Vector3( 0.35f, 0.35f, 1f );
		domeRenderer.Tint = Coral;

		go.AddComponent<FxBlink>().Target = domeRenderer;
		return go;
	}
}

/// <summary>Blinks a renderer between its colour and dark — mines, a lit fuse.</summary>
public sealed class FxBlink : Component
{
	public ModelRenderer Target { get; set; }
	public float Rate { get; set; } = 3f;

	Color _on;

	protected override void OnStart() => _on = Target.Tint;

	protected override void OnUpdate()
	{
		var lit = MathF.Sin( Time.Now * Rate * MathF.PI * 2f ) > 0f;
		Target.Tint = lit ? _on : _on.Darken( 0.7f );
	}
}

/// <summary>Flight and fuse of a thrown TNT block. Visual only — the host times the real blast.</summary>
public sealed class FxBomb : Component
{
	public Vector3 From { get; set; }
	public Vector3 To { get; set; }
	public float Fuse { get; set; } = 1.2f;

	const float FlightTime = 0.45f;

	ModelRenderer _renderer;
	TimeSince _age;

	protected override void OnStart()
	{
		_renderer = GetComponent<ModelRenderer>();
		_age = 0f;
	}

	protected override void OnUpdate()
	{
		if ( _age >= Fuse )
		{
			AbilityFx.Burst( WorldPosition, AbilityFx.Coral, 12, 300f, 12f );
			AbilityFx.Ring( To, 110f, AbilityFx.Butter );
			Sfx.At( Sfx.Blast, WorldPosition, 0.9f, 1.15f );
			GameObject.Destroy();
			return;
		}

		var t = (_age / FlightTime).Clamp( 0f, 1f );
		var arc = Vector3.Up * (4f * 90f * t * (1f - t));
		WorldPosition = Vector3.Lerp( From, To, t ) + arc;
		WorldRotation = Rotation.FromYaw( _age * 540f );

		// Fuse blinks faster as it burns down.
		var rate = 3f + 10f * (_age / Fuse);
		var lit = MathF.Sin( _age * rate * MathF.PI * 2f ) > 0f;
		_renderer.Tint = lit ? AbilityFx.Coral : Color.White;
	}
}
