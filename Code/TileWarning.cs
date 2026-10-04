/// <summary>
/// The visible half of a collapse. Every machine watches every player's footprint itself and
/// heats up the tiles under them, so a doomed tile glows and sags before the host's batched
/// collapse arrives and removes it.
///
/// Costs no network traffic at all: player positions and IsGrounded already replicate, and the
/// footprint arithmetic is the same one the owner uses to report steps. The host stays the only
/// authority on what actually collapses — this never disables a tile, it only paints one.
/// </summary>
public sealed class TileWarning : Component
{
	ArenaGenerator _arena;
	RoundManager _round;

	/// <summary>Packed tile index → when it started warning.</summary>
	readonly Dictionary<int, float> _warned = new();

	readonly List<int> _finished = new();

	readonly int[] _footprint = new int[4];

	protected override void OnAwake()
	{
		_arena = GetComponent<ArenaGenerator>();
		_round = GetComponent<RoundManager>();
	}

	protected override void OnUpdate()
	{
		// Steps only doom tiles while the round is live (TileDecay.Armed), so the paint follows
		// the same rule. ArenaGenerator.ResetAll restores looks on the way into a new round.
		if ( _round.State != RoundManager.RoundState.Playing )
		{
			_warned.Clear();
			return;
		}

		foreach ( var player in Scene.GetAll<TntPlayer>() )
		{
			if ( !player.CanMarkTiles ) continue;

			var count = _arena.GetFootprintTiles( player.WorldPosition, TntPlayer.FootprintRadius, _footprint );

			for ( var i = 0; i < count; i++ )
			{
				var packed = _footprint[i];
				if ( _warned.ContainsKey( packed ) ) continue;

				TileDecay.UnpackIndex( packed, out var floor, out var x, out var y );

				var tile = _arena.GetTile( floor, x, y );
				if ( tile is null || !tile.Enabled ) continue;

				_warned[packed] = Time.Now;
			}
		}

		foreach ( var (packed, started) in _warned )
		{
			TileDecay.UnpackIndex( packed, out var floor, out var x, out var y );

			var tile = _arena.GetTile( floor, x, y );

			// Collapsed by the host's broadcast: put the resting look back while it's hidden, so
			// the next round re-enables a clean tile.
			if ( !tile.Enabled )
			{
				_arena.RestoreTile( floor, x, y );
				_finished.Add( packed );
				continue;
			}

			var t = ((Time.Now - started) / Arena.DecayDelay).Clamp( 0f, 1f );
			_arena.SetTileWarning( floor, x, y, t );
		}

		foreach ( var packed in _finished )
		{
			_warned.Remove( packed );
		}

		_finished.Clear();
	}
}
