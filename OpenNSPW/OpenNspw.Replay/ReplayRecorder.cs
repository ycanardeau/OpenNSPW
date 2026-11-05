namespace OpenNspw.Replay;

// A file not opened yet: what is written is kept in memory until the stream to write it to is given (Keep), and then goes
// there after it. Disposed before, it drops what it kept.
internal sealed class PendingStream : Stream
{
	private MemoryStream? _kept = new();
	private Stream? _target;

	public bool IsKept => _target is not null;

	public override bool CanRead => false;

	public override bool CanSeek => false;

	public override bool CanWrite => true;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	// What was kept, then what is written after it, goes to `target`, which this stream disposes.
	public void Keep(Stream target)
	{
		_kept!.WriteTo(target);
		_kept = null;
		_target = target;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		(_target ?? _kept)!.Write(buffer, offset, count);
	}

	public override void Flush()
	{
		_target?.Flush();
	}

	public override int Read(byte[] buffer, int offset, int count) =>
		throw new NotSupportedException();

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_target?.Dispose();
		base.Dispose(disposing);
	}
}

// Records each battle of a game in a replay of its own, in the stream that `open` opens for its start, which the recorder
// disposes as the battle ends: as the next one starts, or as the recorder is disposed. The map editor's battles are not
// recorded. It changes nothing in the game.
//
// In a game that plays a replay (Replay), added after it, it records the battle as the replay plays it, with the frames'
// times as they were played, but opens the stream only once the player takes the battle over (ReplayPlayer.TakeOver),
// and keeps it in memory until then: the new replay is the battle from its start, as recorded, then what the player
// did. A replay never taken over leaves none.
public sealed class ReplayRecorder(Func<ReplayStart, Stream> open) : IGameExtension, IDisposable
{
	// The frames between two flushes of the file.
	private const int FlushInterval = 100;

	private readonly Func<ReplayStart, Stream> _open = open;
	private ReplayWriter? _writer;
	private PendingStream? _pending;
	private ReplayStart? _start;
	private uint _lastTime;
	private WorldPosition? _camera;
	private GameResult _result;
	private int _frames;

	// The frames between two hashes of the battle: 1 hashes every frame, to find the first that differs.
	public int HashInterval { get; init; } = 10;

	// The replay that the game plays, or null.
	public ReplayPlayer? Replay { get; init; }

	// The bytes of a file of the game, or null if there is none.
	private static byte[]? ReadFile(Nspw game, string path)
	{
		using var stream = game.Platform.OpenFile(
			path.Replace('\\', '/'),
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite
		);
		if (stream is null)
			return null;

		using var bytes = new MemoryStream();
		stream.CopyTo(bytes);
		return bytes.ToArray();
	}

	// The files that a network battle starting now reads: the saved battle it resumes (StartFromSave), and the map
	// editor's scenario (LoadScenarioFile3), which a saved battle may name in its place, read again once it has started.
	private static IEnumerable<StartFile> StartFiles(Nspw game)
	{
		if (game.IsSingleplayer)
			yield break;

		string[] paths =
		[
			game.ScenarioNumber switch
			{
				-1 => "Saved\\resume_1.dat",
				-2 => "Saved\\auto_save.dat",
				_ => "",
			},
			game.ReadNetworkBattleSettings().UserScenarioFileName,
		];
		foreach (var path in paths)
		{
			if (path != "" && ReadFile(game, path) is { } bytes)
				yield return new StartFile(path, bytes);
		}
	}

	// The battle recorded ends.
	private void EndBattle()
	{
		_writer?.Dispose();
		_writer = null;
		_pending = null;
	}

	public void BattleStarting(Nspw game)
	{
		EndBattle();
		_start = game.IsEditingMap
			? null
			: new ReplayStart(
				game.IsSingleplayer ? ReplayMode.Singleplayer
					: game.IsHost ? ReplayMode.NetworkHost
					: ReplayMode.NetworkGuest,
				game.ScenarioNumber,
				DateTimeOffset.UtcNow,
				ReplayFormat.Version,
				game.RandomState,
				0,
				HashInterval,
				game.IsSingleplayer ? null : game.ReadNetworkBattleSettings(),
				[.. StartFiles(game)]
			);
	}

	// The battle has started: the last seed of a single-player battle, and the map editor's scenario that a saved battle
	// names.
	public void BattleStarted(Nspw game)
	{
		if (_start is null)
			return;

		var start = _start with
		{
			BattleSeed = game.RandomState,
			Files = [.. _start.Files.UnionBy(StartFiles(game), file => file.Path)],
		};
		_start = start;
		_pending = Replay is null ? null : new PendingStream();
		_writer = new ReplayWriter(_pending ?? _open(start), start);
		_lastTime = game.Now;
		_camera = null;
		_result = GameResult.None;
		_frames = 0;
	}

	public short FrameStarting(Nspw game, short ticks)
	{
		if (_pending is { IsKept: false } && Replay!.IsTakenOver)
		{
			_pending.Keep(_open(_start!));
			_writer!.Flush();
		}

		_writer?.WriteFrame(
			Replay?.FrameMilliseconds ?? (int)(game.Now - _lastTime),
			ticks,
			game.GameSpeed,
			game.IsBattleHeld
		);
		_lastTime = game.Now;
		return ticks;
	}

	public void Drawing(Nspw game)
	{
		if (_writer is null || _camera is { } camera && camera.Equals(game.CameraPosition))
			return;

		_camera = game.CameraPosition;
		_writer.WriteCamera(game.CameraPosition);
	}

	public void PlayerActed(Nspw game, PlayerAction action)
	{
		_writer?.WriteAction(action);
	}

	public void FrameEnded(Nspw game)
	{
		if (_writer is null)
			return;

		_frames++;
		if (game.Result != _result)
		{
			_result = game.Result;
			_writer.WriteResult(game.Result);
		}

		if (_frames % HashInterval == 0)
			_writer.WriteHash(ReplayFormat.HashBattle(game));
		if (_frames % FlushInterval == 0)
			_writer.Flush();
	}

	public void MessageReceived(Nspw game, ReadOnlySpan<byte> message)
	{
		if (game.Mode == GameMode.Battle)
			_writer?.WriteReceived(message);
	}

	public void RivalLeft(Nspw game)
	{
		if (game.Mode == GameMode.Battle)
			_writer?.WriteRivalLeft();
	}

	// A note for the frame, such as what a computer player decided in it, to read as the replay plays. On the game's
	// thread.
	public void Note(string text)
	{
		_writer?.WriteNote(text);
	}

	public void Dispose()
	{
		EndBattle();
	}
}
