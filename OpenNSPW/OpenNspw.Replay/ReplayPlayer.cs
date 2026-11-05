namespace OpenNspw.Replay;

// Plays a replay: starts the battle as it started, and gives it, frame by frame, the ticks, the view, what the player did
// and, in a network battle, what the rival sent, at the pace it was played (Speed, IsPaused) or as fast as it can up to a
// frame (SeekTo). It checks the battle's hashes, and keeps the first that differs (Divergence), but plays on. At its end,
// or once taken over (TakeOver), the game is the player's.
//
// The host gives a network replay's game the files it started from (StartFileSource), and a directory of its own to
// write in.
public sealed class ReplayPlayer : IGameExtension, IDisposable
{
	// The most milliseconds of play time that one wait for a frame may count, and that the play time may be ahead of the
	// frames, so that a frame that waited long, such as after a pause, or that the game took long to draw, does not hurry
	// the ones after it.
	private const int MaxStep = 250;

	// The milliseconds of the game's clock between two frames shown while the replay plays faster than the game draws them
	// (ShowsFrame): often enough to follow it.
	private const int ShownInterval = 100;

	// How late a frame may start, in milliseconds of play, and still be shown at its pace: later, the game does not keep
	// up.
	private const int LateBy = 20;

	// What Take returns when nothing matches: never written to.
	private static readonly List<ReplayItem> NoItems = [];

	private readonly ReplayReader _reader;

	// The records of the frame being played, and then of what the game took after it, that the game has not been given.
	private readonly List<ReplayItem> _frame = [];

	private readonly Lock _lock = new();
	private readonly List<(int Frame, string Text)> _notes = [];
	private ReplayItem _next;
	private uint? _lastNow;
	private double _playTime;
	private long _nextTime;
	private int? _seekTarget;
	private bool _steps;
	private bool _isTakenOver;

	// Whether the frame that starts is late (LateBy), and when a frame was last shown while the replay played faster than
	// the game drew them.
	private bool _isLate;
	private uint? _lastShown;

	// The camera as the player had it, which the game is given at each frame: the cursor still moves while the replay
	// plays, and at the battlefield's edge would scroll the camera, which decides what is drawn, and so the battle.
	private WorldPosition? _camera;

	public ReplayStart Start => _reader.Start;

	// The frames played so far, and how long they were played, in milliseconds.
	public int Frame { get; private set; }

	public long Milliseconds { get; private set; }

	// How long the frame that the game runs now was played, in milliseconds, or null if the replay did not give it.
	public int? FrameMilliseconds { get; private set; }

	// Whether the replay has no frame left.
	public bool HasEnded { get; private set; }

	// The first frame whose battle is not the one recorded, as its hash or its result says, or null.
	public string? Divergence { get; private set; }

	// How fast it plays, 1 as it was played; PositiveInfinity as fast as it can.
	public double Speed { get; set; } = 1;

	public bool IsPaused { get; set; }

	// Whether it plays as fast as it can, up to a frame: the host mutes it.
	public bool IsSeeking => _seekTarget is { } target && Frame < target;

	// Whether the player has taken the battle over (TakeOver).
	public bool IsTakenOver => _isTakenOver;

	public bool HoldsInput => !HasEnded && !_isTakenOver;

	// A network battle's replay starts it (GameStarted).
	public bool StartsBattle => Start.Network is not null;

	// What the game takes before the first frame: the rival's messages of a network battle that came before it.
	public ReplayPlayer(Stream stream)
	{
		_reader = new ReplayReader(stream);
		for (
			_next = _reader.Next();
			_next.Kind is not (ReplayRecord.Frame or ReplayRecord.End);
			_next = _reader.Next()
		)
			_frame.Add(_next);
		_nextTime = _next.Time;
	}

	// The notes of the frames played so far.
	public IReadOnlyList<(int Frame, string Text)> Notes()
	{
		lock (_lock)
		{
			return [.. _notes];
		}
	}

	// Plays as fast as it can until `frame` frames are played, then at its pace, paused if it was.
	public void SeekTo(int frame)
	{
		_seekTarget = frame;
	}

	// Plays one frame, while paused.
	public void Step()
	{
		_steps = true;
	}

	// Gives the game to the player from here.
	public void TakeOver()
	{
		_isTakenOver = true;
	}

	// The records of this frame that `matches`, with `state`, in order, which are taken out: in place, and none without
	// garbage, as most points of most frames have none. The list returned is only read.
	private List<ReplayItem> Take<T>(T state, Func<ReplayItem, T, bool> matches)
	{
		List<ReplayItem>? taken = null;
		var kept = 0;
		for (var i = 0; i < _frame.Count; i++)
		{
			var item = _frame[i];
			if (matches(item, state))
				(taken ??= []).Add(item);
			else
				_frame[kept++] = item;
		}

		_frame.RemoveRange(kept, _frame.Count - kept);
		return taken ?? NoItems;
	}

	// Gives the game a message of the rival, as DirectPlay gives it one (DoWork), or the rival leaving.
	private static unsafe void Receive(Nspw game, ReplayItem item)
	{
		if (item.Kind == ReplayRecord.RivalLeft)
		{
			game.DirectPlayMessageHandler(
				null,
				dplay8.DPN_MSGID_DESTROY_PLAYER,
				new DPNMSG_DESTROY_PLAYER { dpnidPlayer = game.RivalPlayerId }
			);
			return;
		}

		fixed (byte* data = item.Message)
		{
			game.DirectPlayMessageHandler(
				null,
				dplay8.DPN_MSGID_RECEIVE,
				new DPNMSG_RECEIVE
				{
					dpnidSender = game.RivalPlayerId,
					pReceiveData = data,
					dwReceiveDataSize = (uint)item.Message!.Length,
				}
			);
		}
	}

	// The rival's messages that the game took after the frame, as its DirectPlay does its work (ReplayDirectPlay.DoWork).
	internal void Receive(Nspw game)
	{
		if (!HoldsInput)
			return;

		foreach (
			var item in Take(
				0,
				static (item, _) => item.Kind is ReplayRecord.Received or ReplayRecord.RivalLeft
			)
		)
			Receive(game, item);
	}

	public void GameStarted(Nspw game)
	{
		if (Start.Network is { } network)
		{
			var directPlay = new ReplayDirectPlay(this, game);
			game.StartNetworkBattle(network, directPlay, directPlay);
		}
	}

	// A single-player battle starts with the options it was recorded with, not the player's.
	public void BattleStarting(Nspw game)
	{
		if (Start.Mode != ReplayMode.Singleplayer)
			return;

		game.srand(Start.RandomState);
		game.Options = Start.Options;
	}

	public void BattleStarted(Nspw game)
	{
		if (Start.Mode == ReplayMode.Singleplayer)
			game.srand(Start.BattleSeed);
	}

	public bool? StartsFrame(Nspw game)
	{
		if (!HoldsInput)
			return null;

		var elapsed = _lastNow is { } last ? Math.Min(game.Now - last, MaxStep) : 0;
		_lastNow = game.Now;
		// The play time stays with the frames while they go as fast as they can, so that it does not lag behind once they no
		// longer do, which would hold the next ones for as long as the seek skipped.
		if (IsSeeking || double.IsPositiveInfinity(Speed))
		{
			_playTime = _nextTime;
			return true;
		}

		if (IsPaused)
		{
			var steps = _steps;
			_steps = false;
			_playTime = _nextTime;
			return steps;
		}

		// Nor does it run ahead of the frames, by MaxStep at most, when the game takes longer to draw them than their pace,
		// as at 8×: it would hurry the frames after a slower speed is chosen, for as long as it ran ahead.
		_playTime = Math.Min(_playTime + elapsed * Speed, _nextTime + MaxStep);
		var starts = _playTime >= _nextTime;
		_isLate = starts && _playTime - _nextTime > LateBy;
		return starts;
	}

	// While it plays faster than the game draws the frames, as it seeks, at PositiveInfinity or with its frames late, a
	// frame every ShownInterval, and the one a seek ends with; the others draw nothing, and play the same
	// (IGameExtension.ShowsFrame). Every frame otherwise.
	public bool? ShowsFrame(Nspw game)
	{
		var isFast = IsSeeking || double.IsPositiveInfinity(Speed) || _isLate;
		if (!HoldsInput || !isFast || _seekTarget is { } target && Frame + 1 == target)
		{
			_lastShown = null;
			return null;
		}

		if (_lastShown is { } last && game.Now - last < ShownInterval)
			return false;

		_lastShown = game.Now;
		return true;
	}

	// Whether the frame that starts holds the battle, as it did.
	public bool? HoldsBattle(Nspw game) => HoldsInput ? _next.IsHeld : null;

	private void Diverge(string what)
	{
		Divergence ??= $"Frame {Frame}: {what}";
	}

	// What the game took after the last frame and has not been given yet: the player's answer to the system menu and the
	// debug keys, which the game takes between frames, an action whose site the battle did not reach, and the rival's
	// messages, if its DirectPlay has not given them.
	private void GiveTheRest(Nspw game)
	{
		foreach (var item in Take(0, static (_, _) => true))
		{
			if (item.Kind == ReplayRecord.Action)
				game.Do(item.Action!);
			else if (item.Kind is ReplayRecord.Received or ReplayRecord.RivalLeft)
				Receive(game, item);
		}
	}

	public short FrameStarting(Nspw game, short ticks)
	{
		FrameMilliseconds = null;
		if (!HoldsInput)
			return ticks;

		GiveTheRest(game);
		if (_next.Kind != ReplayRecord.Frame)
		{
			HasEnded = true;
			return ticks;
		}

		Frame++;
		Milliseconds += _next.Time;
		FrameMilliseconds = _next.Time;
		ticks = (short)_next.Value;
		// The game's speed as the player had it, which the game shows.
		game.GameSpeed = _next.Speed;
		for (
			_next = _reader.Next();
			_next.Kind is not (ReplayRecord.Frame or ReplayRecord.End);
			_next = _reader.Next()
		)
			_frame.Add(_next);
		_nextTime += _next.Time;
		return ticks;
	}

	public void Drawing(Nspw game)
	{
		if (!HoldsInput)
			return;

		foreach (var item in Take(0, static (item, _) => item.Kind == ReplayRecord.Camera))
			_camera = item.Camera;
		if (_camera is { } camera)
			game.CameraPosition = camera;
	}

	// The player's actions at this site in this frame.
	public void InputSite(Nspw game, ActionSite site, bool counts)
	{
		if (!HoldsInput)
			return;

		foreach (
			var item in Take(
				site,
				static (item, site) => item.Kind == ReplayRecord.Action && item.Action!.Site == site
			)
		)
			game.Do(item.Action!);
	}

	public void FrameEnded(Nspw game)
	{
		if (!HoldsInput)
			return;

		foreach (
			var item in Take(
				0,
				static (item, _) =>
					item.Kind is ReplayRecord.Note or ReplayRecord.Result or ReplayRecord.Hash
			)
		)
		{
			switch (item.Kind)
			{
				case ReplayRecord.Note:
					lock (_lock)
					{
						_notes.Add((Frame, item.Text!));
					}
					break;
				case ReplayRecord.Result when (GameResult)item.Value != game.Result:
					Diverge($"the result is {game.Result}, not {(GameResult)item.Value}.");
					break;
				case ReplayRecord.Hash when item.Hash != ReplayFormat.HashBattle(game):
					Diverge("the battle is not the one recorded.");
					break;
			}
		}
	}

	public void Dispose()
	{
		_reader.Dispose();
	}
}

// The DirectPlay of a network battle that a replay plays: it sends nothing, and gives the game the rival's messages as
// it does its work after each frame (DoWork), where DirectPlay gave them.
internal sealed class ReplayDirectPlay(ReplayPlayer player, Nspw game)
	: IDirectPlay8Peer,
		IDirectPlay8ThreadPool
{
	private readonly ReplayPlayer _player = player;
	private readonly Nspw _game = game;

	public uint Release() => 0;

	public int Initialize(object? pvUserContext, PFNDPNMESSAGEHANDLER pfn, uint dwFlags) =>
		winerror.S_OK;

	public int SetThreadCount(uint dwProcessorNum, uint dwNumThreads, uint dwFlags) =>
		winerror.S_OK;

	public int DoWork(uint dwAllowedTimeSlice, uint dwFlags)
	{
		_player.Receive(_game);
		return winerror.S_OK;
	}

	public int EnumServiceProviders(
		Guid? pguidServiceProvider,
		Guid? pguidApplication,
		DPN_SERVICE_PROVIDER_INFO[]? pSPInfoBuffer,
		ref uint pcbEnumData,
		ref uint pcReturned,
		uint dwFlags
	)
	{
		pcReturned = 0;
		return winerror.S_OK;
	}

	public int GetSPCaps(Guid? pguidSP, DPN_SP_CAPS pdpspCaps, uint dwFlags) => winerror.S_OK;

	public int SetPeerInfo(
		DPN_PLAYER_INFO pdpnPlayerInfo,
		object? pvAsyncContext,
		object? phAsyncHandle,
		uint dwFlags
	) => winerror.S_OK;

	public int GetPeerInfo(
		uint dpnid,
		DPN_PLAYER_INFO? pdpnPlayerInfo,
		ref uint pdwSize,
		uint dwFlags
	) => winerror.S_OK;

	public int Host(
		DPN_APPLICATION_DESC pdnAppDesc,
		IDirectPlay8Address?[] prgpDeviceInfo,
		uint cDeviceInfo,
		object? pdnSecurity,
		object? pdnCredentials,
		object? pvPlayerContext,
		uint dwFlags
	) => winerror.S_OK;

	public int Connect(
		DPN_APPLICATION_DESC pdnAppDesc,
		IDirectPlay8Address? pHostAddr,
		IDirectPlay8Address? pDeviceInfo,
		object? pdnSecurity,
		object? pdnCredentials,
		byte[]? pvUserConnectData,
		uint dwUserConnectDataSize,
		object? pvPlayerContext,
		object? pvAsyncContext,
		ref uint phAsyncHandle,
		uint dwFlags
	) => winerror.S_OK;

	public int SendTo(
		uint dpnid,
		ref DPN_BUFFER_DESC prgBufferDesc,
		uint cBufferDesc,
		uint dwTimeOut,
		object? pvAsyncContext,
		ref uint phAsyncHandle,
		uint dwFlags
	) => winerror.S_OK;

	public int Close(uint dwFlags) => winerror.S_OK;
}
