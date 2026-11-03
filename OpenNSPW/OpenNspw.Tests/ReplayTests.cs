using OpenNspw.Replay;
using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.Tests.Lockstep.BattleScripts;

namespace OpenNspw.Tests;

// Replays (docs/Replays.md): a battle recorded, and played again without input, as fast as the game can.
public sealed class ReplayTests
{
	private const int Frames = 60;

	private static byte[] Record(int scenario, Action<SingleplayerRun> play, int hashInterval = 1)
	{
		var stream = new MemoryStream();
		var recorder = new ReplayRecorder(_ => stream) { HashInterval = hashInterval };
		using (var run = new SingleplayerRun(scenario, game => game.Extensions.Add(recorder)))
		{
			play(run);
		}
		recorder.Dispose();
		return stream.ToArray();
	}

	// Plays a replay for `frames` frames, changing the game halfway if `change` says so. The game starts at another date
	// than the recorded one, which would seed it otherwise.
	private static ReplayPlayer Play(byte[] replay, int frames, Action<Nspw>? change = null)
	{
		var player = new ReplayPlayer(new MemoryStream(replay)) { Speed = double.PositiveInfinity };
		using var run = new SingleplayerRun(
			player.Start.Scenario,
			game => game.Extensions.Add(player),
			startDate: 1_234_567_890
		);
		for (var frame = 0; frame < frames; frame++)
		{
			if (frame == frames / 2)
				change?.Invoke(run.Game);
			run.Frames(1);
		}
		return player;
	}

	[Fact]
	public void TheReplayPlaysTheBattleAgain()
	{
		var replay = Record(3, run => run.Frames(Frames));

		using var player = Play(replay, Frames);

		Assert.Equal(ReplayMode.Singleplayer, player.Start.Mode);
		Assert.Equal(3, player.Start.Scenario);
		Assert.Null(player.Divergence);
		Assert.True(player.Frame >= Frames - 1);
	}

	// A change to the game shows at the frame's hash.
	[Fact]
	public void TheReplayFindsAChange()
	{
		var replay = Record(3, run => run.Frames(Frames));

		using var player = Play(replay, Frames, game => game.Units[1].HP--);

		Assert.NotNull(player.Divergence);
		Assert.Contains("not the one recorded", player.Divergence);
	}

	// The player's ship, of scenario 4, that the tests order.
	private static int PlayersShip(Nspw game) =>
		Enumerable
			.Range(1, game.MaxUnitId)
			.First(m =>
				game.Units[m].IsUsed
				&& game.Units[m].Side == game.LocalSide
				&& game.Units[m].Category == UnitCategory.Ship
				&& game.Units[m].Kind <= UnitKind.Destroyer
			);

	private static List<PlayerAction> ActionsOf(byte[] replay)
	{
		using var reader = new ReplayReader(new MemoryStream(replay));
		var actions = new List<PlayerAction>();
		for (var item = reader.Next(); item.Kind != ReplayRecord.End; item = reader.Next())
			if (item.Kind == ReplayRecord.Action)
				actions.Add(item.Action!);
		return actions;
	}

	// The player's orders, given with the commands of the host's UI as a computer player gives them, are recorded where
	// the game takes them, and given back by the replay, which takes no input: a ship selected and sent away, then
	// deselected and selected again by right clicks.
	[Fact]
	public void TheReplayGivesThePlayersActions()
	{
		int ship = 0;
		var destination = default(WorldPosition);
		var replay = Record(
			4,
			run =>
			{
				var game = run.Game;
				run.Frames(2);
				ship = PlayersShip(game);
				destination = new WorldPosition(
					game.Units[ship].Position.X + 400,
					game.Units[ship].Position.Y
				);
				game.Commands.ClickUnit(ship);
				run.Frames(2);
				game.Commands.SetDestination(destination);
				run.Frames(10);
				game.Commands.RightClick();
				run.Frames(2);
				game.Commands.RightClick();
				run.Frames(Frames);
			}
		);

		Assert.Equal(
			[
				new PlayerAction.ClickUnit(ship),
				new PlayerAction.SetDestination(destination),
				new PlayerAction.CancelSelection(ship),
				new PlayerAction.RestoreSelection(),
			],
			ActionsOf(replay)
		);

		using var player = Play(replay, Frames + 16);
		Assert.Null(player.Divergence);
	}

	// The game's speed that the player chose, as the game showed it, is shown again as the replay plays: 4 after 1.
	[Fact]
	public void TheReplayShowsTheSpeedPlayedAt()
	{
		var replay = Record(
			3,
			run =>
			{
				run.Frames(5);
				run.Game.GameSpeed = 4;
				run.Frames(10);
			}
		);

		var player = new ReplayPlayer(new MemoryStream(replay)) { Speed = double.PositiveInfinity };
		using var run = new SingleplayerRun(3, game => game.Extensions.Add(player));
		run.Frames(3);
		var before = run.Game.GameSpeed;
		run.Frames(10);

		Assert.Equal(1, before);
		Assert.Equal(4, run.Game.GameSpeed);
		Assert.Null(player.Divergence);
		player.Dispose();
	}

	// A replay cut short, as when the game stops without ending it, plays up to where it was cut.
	[Fact]
	public void ACutReplayPlaysToItsCut()
	{
		var replay = Record(3, run => run.Frames(Frames));

		using var player = Play(replay[..(replay.Length / 2)], Frames);

		Assert.True(player.HasEnded);
		Assert.Null(player.Divergence);
	}

	// A replay of another version of the format is told to be one, and refused, rather than read wrong.
	[Fact]
	public void AReplayOfAnotherVersionIsRefused()
	{
		var replay = Record(3, run => run.Frames(2));
		var bytes = new MemoryStream();
		using (
			var gzip = new System.IO.Compression.GZipStream(
				new MemoryStream(replay),
				System.IO.Compression.CompressionMode.Decompress
			)
		)
			gzip.CopyTo(bytes);
		var raw = bytes.ToArray();
		raw[ReplayFormat.MagicOfAnyVersion.Length] = (byte)'1';
		var older = new MemoryStream();
		using (
			var gzip = new System.IO.Compression.GZipStream(
				older,
				System.IO.Compression.CompressionLevel.Fastest,
				leaveOpen: true
			)
		)
			gzip.Write(raw);

		older.Position = 0;
		Assert.True(ReplayFormat.IsReplay(older));
		var exception = Assert.Throws<InvalidDataException>(() => new ReplayReader(older));
		Assert.Contains("another version", exception.Message);
	}

	// A battle of ミッドウェイ攻略１ between two games, each recorded: the host has a transport land, with the mouse, and
	// the guest sends a ship away, with the commands, so that each game's replay has the rival's orders as well as its own.
	private static (byte[] Host, byte[] Guest) RecordNetwork(int frames)
	{
		var host = new MemoryStream();
		var guest = new MemoryStream();
		var hostRecorder = new ReplayRecorder(_ => host) { HashInterval = 1 };
		var guestRecorder = new ReplayRecorder(_ => guest) { HashInterval = 1 };
		using (var run = new LockstepRun())
		{
			run.Host.Game.Extensions.Add(hostRecorder);
			run.Guest.Game.Extensions.Add(guestRecorder);
			StartBattle(run, 5);
			var hostGame = run.Host.Game;
			var transport = Enumerable
				.Range(1, hostGame.MaxUnitId)
				.First(m =>
					hostGame.Units[m].IsUsed
					&& hostGame.Units[m].Kind == UnitKind.Transport
					&& hostGame.Units[m].Side == hostGame.LocalSide
					&& LandTransport(run, run.Host, new MouseInput(run, run.Host), m)
				);
			var guestGame = run.Guest.Game;
			var ship = PlayersShip(guestGame);
			var input = new CommandInput(run, run.Guest);
			Select(input, guestGame, ship);
			run.Until(() => guestGame.CanOrder);
			input.SetDestination(
				new WorldPosition(
					guestGame.Units[ship].Position.X + 400,
					guestGame.Units[ship].Position.Y
				)
			);
			run.Frames(run.Host, frames);
			Assert.False(hostGame.HasSavedDesync, "The host detected a desync.");
		}
		hostRecorder.Dispose();
		guestRecorder.Dispose();
		return (host.ToArray(), guest.ToArray());
	}

	// Plays a network battle's replay in a game of its own, with no rival, to its end.
	private static ReplayPlayer PlayAlone(byte[] replay)
	{
		var player = new ReplayPlayer(new MemoryStream(replay)) { Speed = double.PositiveInfinity };
		using var data = GameData.CreateCopy();
		var scheduler = new LockstepScheduler();
		var game = new LockstepGame(scheduler, new LoopbackNetwork(), data.Path, "Carol");
		game.Game.Extensions.Add(player);
		game.Start();
		try
		{
			for (var steps = 0; !player.HasEnded && !game.Ended; steps++)
			{
				Assert.True(
					steps < 100_000,
					$"The replay stopped at frame {player.Frame}: mode {game.Game.Mode}, dialog {game.Game.MainDialog?.Id}, active {game.Game.IsAppActive}, frames {game.FrameCount}, players {game.Game.ActivePlayerCount}, peer {game.Game.DirectPlayPeer?.GetType().Name}"
				);
				game.Step();
			}

			Assert.False(game.Game.HasSavedDesync, "The replay found a desync.");
			Assert.Empty(game.MessageBoxes);
		}
		finally
		{
			scheduler.Stop();
			game.Stop();
		}

		return player;
	}

	private static int FramesOf(byte[] replay)
	{
		using var reader = new ReplayReader(new MemoryStream(replay));
		var frames = 0;
		for (var item = reader.Next(); item.Kind != ReplayRecord.End; item = reader.Next())
			if (item.Kind == ReplayRecord.Frame)
				frames++;
		return frames;
	}

	// Each player's replay of a network battle plays alone, the rival's messages given as they came, with the same battle
	// every frame.
	[Fact]
	public void ANetworkBattlesReplaysPlayAlone()
	{
		var (host, guest) = RecordNetwork(Frames * 4);

		foreach (
			var (replay, mode) in new[]
			{
				(host, ReplayMode.NetworkHost),
				(guest, ReplayMode.NetworkGuest),
			}
		)
		{
			using var player = PlayAlone(replay);
			Assert.Equal(mode, player.Start.Mode);
			Assert.Contains(ActionsOf(replay), action => action is PlayerAction.SetDestination);
			Assert.Null(player.Divergence);
			Assert.Equal(FramesOf(replay), player.Frame);
		}
	}
}
