using OpenNspw.Replay;
using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.all_head;
using static OpenNspw.Tests.Lockstep.BattleScripts;

namespace OpenNspw.Tests;

// Replays (docs/Replays.md): a battle recorded, and played again without input, as fast as the game can.
public sealed class ReplayTests
{
	private const int Frames = 60;

	// Records a battle of `scenario`, with `options`, played by `play`.
	private static byte[] Record(
		int scenario,
		Action<SingleplayerRun> play,
		int hashInterval = 1,
		BattleOptions? options = null
	)
	{
		var stream = new MemoryStream();
		var recorder = new ReplayRecorder(_ => stream) { HashInterval = hashInterval };
		using (
			var run = new SingleplayerRun(
				scenario,
				game =>
				{
					game.Options = options ?? BattleOptions.None;
					game.Extensions.Add(recorder);
				}
			)
		)
		{
			play(run);
		}
		recorder.Dispose();
		return stream.ToArray();
	}

	// Plays a replay for `frames` of its frames, or to its end, changing the game halfway if `change` says so. The game
	// starts at another date than the recorded one, which would seed it otherwise, and with the player's `options`.
	private static ReplayPlayer Play(
		byte[] replay,
		int frames,
		Action<Nspw>? change = null,
		BattleOptions? options = null
	)
	{
		var player = new ReplayPlayer(new MemoryStream(replay)) { Speed = double.PositiveInfinity };
		var played = new ReplayFrames(player, frames / 2, change);
		using var run = new SingleplayerRun(
			player.Start.Scenario,
			game =>
			{
				game.Options = options ?? BattleOptions.None;
				game.Extensions.Add(player);
				game.Extensions.Add(played);
			},
			startDate: 1_234_567_890
		);
		played.PlayTo(run, frames);
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

	// A replay plays with the options it was recorded with, whatever the player's are: scenario 7 draws random numbers for
	// its effects from its first frames, from rand without SharedEffectRandom, which the battle draws from too.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void TheReplayPlaysWithItsOptions(bool sharedEffectRandom)
	{
		var options = new BattleOptions(SharedEffectRandom: sharedEffectRandom);
		var replay = Record(7, run => run.Frames(Frames), options: options);

		using var player = Play(
			replay,
			Frames,
			options: options with
			{
				SharedEffectRandom = !sharedEffectRandom,
			}
		);

		Assert.Equal(options, player.Start.Options);
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

	// The cursor does not move the replay's camera: recorded with the cursor amid the battlefield, where the view stays, and
	// played with it at (0, 0), the corner, where it would scroll the view every frame.
	[Fact]
	public void TheCursorDoesNotMoveTheCamera()
	{
		var camera = default(WorldPosition);
		var replay = Record(
			3,
			run =>
			{
				run.Game.SetCursorPos(CMBT_WIDTH / 2, CMBT_HEIGHT / 2);
				run.Frames(Frames);
				camera = run.Game.CameraPosition;
			}
		);

		var player = new ReplayPlayer(new MemoryStream(replay)) { Speed = double.PositiveInfinity };
		var played = new ReplayFrames(player);
		using var run = new SingleplayerRun(
			3,
			game =>
			{
				game.Extensions.Add(player);
				game.Extensions.Add(played);
			}
		);
		run.Game.SetCursorPos(0, 0);
		played.PlayTo(run);

		Assert.Equal(camera, played.ByFrame[played.ByFrame.Keys.Max()].Camera);
		player.Dispose();
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

	// Reads the player's orders of each frame where the game takes them (ActionSite.Orders), or gives them there whole.
	private sealed class FrameOrders(bool gives, Dictionary<int, PlayerOrders> byFrame)
		: IGameExtension
	{
		private readonly bool _gives = gives;
		private int _frame;

		public Dictionary<int, PlayerOrders> ByFrame { get; } = byFrame;

		public void InputSite(Nspw game, ActionSite site, bool counts)
		{
			if (site != ActionSite.Orders)
				return;

			if (!_gives && game.ReadPlayerOrders() is { } orders)
				ByFrame[_frame] = orders;
			else if (_gives && ByFrame.TryGetValue(_frame, out var given))
				game.Do(new PlayerAction.GiveOrders(given));
		}

		public void FrameEnded(Nspw game) => _frame++;
	}

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
				run.Player.Commands.ClickUnit(ship);
				run.Frames(2);
				run.Player.Commands.SetDestination(destination);
				run.Frames(10);
				run.Player.Commands.RightClick();
				run.Frames(2);
				run.Player.Commands.RightClick();
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

	// A frame's orders given whole, as a trace of 1.22 gives its player's, are recorded as one action, and given back by
	// the replay: the orders that a ship selected and sent away left, read in one game and given in another.
	[Fact]
	public void TheReplayGivesTheOrdersGivenWhole()
	{
		var read = new FrameOrders(gives: false, []);
		using (var run = new SingleplayerRun(4, game => game.Extensions.Add(read)))
		{
			var game = run.Game;
			run.Frames(2);
			var ship = PlayersShip(game);
			run.Player.Commands.ClickUnit(ship);
			run.Frames(2);
			run.Player.Commands.SetDestination(
				new WorldPosition(game.Units[ship].Position.X + 400, game.Units[ship].Position.Y)
			);
			run.Frames(2);
		}
		Assert.NotEmpty(read.ByFrame);

		var replay = Record(
			4,
			run =>
			{
				run.Game.Extensions.Add(new FrameOrders(gives: true, read.ByFrame));
				run.Frames(Frames);
			}
		);

		var actions = ActionsOf(replay);
		Assert.Equal(read.ByFrame.Count, actions.Count);
		Assert.All(actions, action => Assert.IsType<PlayerAction.GiveOrders>(action));
		Assert.Equal(
			read.ByFrame.Values.Select(orders => orders.Selections),
			actions.Select(action => ((PlayerAction.GiveOrders)action).Orders.Selections)
		);

		using var player = Play(replay, Frames);
		Assert.Null(player.Divergence);
	}

	// A replay taken over halfway is recorded again, in a replay of its own: the battle from its start, as recorded, then
	// what the player did, a ship sent away, which plays again as the new replay was played. Watched without being taken
	// over, it leaves none.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ATakenOverReplayIsRecordedAgain(bool takesOver)
	{
		var replay = Record(4, run => run.Frames(Frames));
		var player = new ReplayPlayer(new MemoryStream(replay)) { Speed = double.PositiveInfinity };
		var opened = 0;
		var stream = new MemoryStream();
		var recorder = new ReplayRecorder(_ =>
		{
			opened++;
			return stream;
		})
		{
			HashInterval = 1,
			Replay = player,
		};
		var played = new ReplayFrames(
			player,
			Frames / 2,
			takesOver ? _ => player.TakeOver() : null
		);
		int ship;
		WorldPosition destination;
		using (
			var run = new SingleplayerRun(
				4,
				game =>
				{
					game.Extensions.Add(player);
					game.Extensions.Add(recorder);
					game.Extensions.Add(played);
				},
				startDate: 1_234_567_890
			)
		)
		{
			var game = run.Game;
			played.PlayTo(run, Frames / 2);
			run.Frames(2);
			ship = PlayersShip(game);
			destination = new WorldPosition(
				game.Units[ship].Position.X + 400,
				game.Units[ship].Position.Y
			);
			run.Player.Commands.ClickUnit(ship);
			run.Frames(2);
			run.Player.Commands.SetDestination(destination);
			run.Frames(Frames);
		}
		recorder.Dispose();
		player.Dispose();

		if (!takesOver)
		{
			Assert.Equal(0, opened);
			return;
		}

		Assert.Equal(1, opened);
		var again = stream.ToArray();
		Assert.Equal(
			[new PlayerAction.ClickUnit(ship), new PlayerAction.SetDestination(destination)],
			ActionsOf(again)
		);
		using var replayed = Play(again, Frames / 2 + Frames + 8);
		Assert.True(replayed.HasEnded);
		Assert.Null(replayed.Divergence);
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
		var played = new ReplayFrames(player);
		using var run = new SingleplayerRun(
			3,
			game =>
			{
				game.Extensions.Add(player);
				game.Extensions.Add(played);
			}
		);
		played.PlayTo(run);

		Assert.Equal(1, played.ByFrame[3].Speed);
		Assert.Equal(4, played.ByFrame[13].Speed);
		Assert.Null(player.Divergence);
		player.Dispose();
	}

	// The frames played and shown, and the frame of the replay last shown, as each frame ends.
	private sealed class ShownFrames(ReplayPlayer player) : IGameExtension
	{
		private readonly ReplayPlayer _player = player;

		public int Played { get; private set; }

		public int Shown { get; private set; }

		public int LastShown { get; private set; }

		public void FrameEnded(Nspw game)
		{
			Played++;
			if (game.Renderer.DrawsNothing)
				return;

			Shown++;
			LastShown = _player.Frame;
		}
	}

	// A seek draws a frame every 100 milliseconds of the game's clock, and the one it ends with, and plays the same.
	[Fact]
	public void ASeekShowsFewFramesAndPlaysTheSame()
	{
		var replay = Record(3, run => run.Frames(Frames));
		var player = new ReplayPlayer(new MemoryStream(replay)) { IsPaused = true };
		player.SeekTo(Frames - 1);
		var shown = new ShownFrames(player);
		using var run = new SingleplayerRun(
			3,
			game =>
			{
				game.Extensions.Add(player);
				game.Extensions.Add(shown);
			},
			startDate: 1_234_567_890
		);

		while (player.Frame < Frames - 1)
			run.Frames(1);

		Assert.Null(player.Divergence);
		Assert.True(shown.Shown < shown.Played);
		Assert.Equal(Frames - 1, shown.LastShown);
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

	// A replay of version 2, whose frames have no speed, shows the speed that their ticks tell: speed + 1 ticks a frame,
	// and 20 while Space is held, which leaves the speed as it was.
	[Fact]
	public void AReplayOfVersion2TellsTheSpeedByTheTicks()
	{
		var bytes = new MemoryStream();
		using (
			var gzip = new System.IO.Compression.GZipStream(
				bytes,
				System.IO.Compression.CompressionLevel.Fastest,
				leaveOpen: true
			)
		)
		using (var writer = new BinaryWriter(gzip))
		{
			writer.Write(ReplayFormat.MagicOfVersion2);
			writer.Write((byte)ReplayMode.Singleplayer);
			writer.Write((short)3);
			writer.Write(0L);
			writer.Write("");
			writer.Write(0u);
			writer.Write(0u);
			writer.Write7BitEncodedInt(10);
			writer.Write(false);
			writer.Write7BitEncodedInt(0);
			foreach (var ticks in new[] { 2, 5, 20, 0, 3 })
			{
				writer.Write((byte)ReplayRecord.Frame);
				writer.Write7BitEncodedInt(88);
				writer.Write7BitEncodedInt(ticks);
			}
			writer.Write((byte)ReplayRecord.End);
		}

		bytes.Position = 0;
		using var reader = new ReplayReader(bytes);
		var speeds = new List<short>();
		for (var item = reader.Next(); item.Kind != ReplayRecord.End; item = reader.Next())
			speeds.Add(item.Speed);

		Assert.Equal(new short[] { 1, 4, 4, 0, 2 }, speeds);
		Assert.Equal(BattleOptions.None, reader.Start.Options);
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
