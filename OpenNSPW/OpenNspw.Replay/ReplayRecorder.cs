namespace OpenNspw.Replay;

// Records each battle of a game in a replay of its own, in the stream that `open` opens for its start, which the recorder
// disposes as the battle ends: as the next one starts, or as the recorder is disposed. The map editor's battles are not
// recorded. It changes nothing in the game.
public sealed class ReplayRecorder(Func<ReplayStart, Stream> open) : IGameExtension, IDisposable
{
	// The frames between two flushes of the file.
	private const int FlushInterval = 100;

	private readonly Func<ReplayStart, Stream> _open = open;
	private ReplayWriter? _writer;
	private ReplayStart? _start;
	private uint _lastTime;
	private WorldPosition? _camera;
	private GameResult _result;
	private int _frames;

	// The frames between two hashes of the battle: 1 hashes every frame, to find the first that differs.
	public int HashInterval { get; init; } = 10;

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
		_writer = new ReplayWriter(_open(start), start);
		_lastTime = game.Now;
		_camera = null;
		_result = GameResult.None;
		_frames = 0;
	}

	public short FrameStarting(Nspw game, short ticks)
	{
		_writer?.WriteFrame((int)(game.Now - _lastTime), ticks);
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
