using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.all_head;

namespace OpenNspw.Tests;

// The traces of 1.22 (docs/Verification.md): their layout, and a trace of the port, replayed.
public sealed class Trace122Tests
{
	private const int Frames = 60;

	// The game follows or records a trace with the cursor in the middle of the battle area, where it neither scrolls the
	// view nor clicks anything: the replay gives the game 1.22's view at each drawing, but at (0, 0), its corner, the
	// view would scroll on the drawn tick before it.
	private static void SetUp(Nspw game, ISingleplayerTrace trace)
	{
		game.Trace = trace;
		game.SetCursorPos(CMBT_WIDTH / 2, CMBT_HEIGHT / 2);
	}

	private static byte[] RecordPort(int scenario, int frames)
	{
		var stream = new MemoryStream();
		var recorder = new Trace122Recorder(stream);
		using (var run = new SingleplayerRun(scenario, game => SetUp(game, recorder)))
		{
			run.Frames(frames);
		}
		recorder.Dispose();
		return stream.ToArray();
	}

	private static Trace122Replay Replay(byte[] trace, int frames, Action<Nspw>? change = null)
	{
		var replay = new Trace122Replay(new MemoryStream(trace));
		using var run = new SingleplayerRun(replay.Scenario, game => SetUp(game, replay));
		for (var frame = 0; frame < frames && replay.Divergence is null; frame++)
		{
			if (frame == frames / 2)
				change?.Invoke(run.Game);
			run.Frames(1);
		}
		return replay;
	}

	// The traces recorded from 1.22 on Windows, in Traces122 (docs/Verification.md): the port follows each to its end
	// without a difference. With none there, nothing is checked.
	[Fact]
	public void ThePortFollowsTheTracesOf122()
	{
		var directory = Path.Combine(AppContext.BaseDirectory, "Traces122");
		var divergences = new List<string>();
		foreach (
			var file in Directory.Exists(directory)
				? Directory.GetFiles(directory, "*.n122").Order().ToArray()
				: []
		)
		{
			using var replay = new Trace122Replay(File.OpenRead(file));
			using var run = new SingleplayerRun(replay.Scenario, game => SetUp(game, replay));
			while (!replay.HasEnded && replay.Divergence is null)
				run.Frames(1);
			if (replay.Divergence is { } divergence)
				divergences.Add($"{Path.GetFileName(file)}: {divergence}");
		}

		// Each in full: Assert.Empty cuts the items short, and the difference is at their end.
		Assert.True(divergences.Count == 0, string.Join(Environment.NewLine, divergences));
	}

	// The fields are where 1.22 has them (docs/Singleplayer.md).
	[Theory]
	[InlineData("Position.X", 0x08)]
	[InlineData("Direction.Degrees", 0x70)]
	[InlineData("PathX[0]", 0xC0)]
	[InlineData("PathY[0]", 0x2C0)]
	[InlineData("hp[0]", 0x4F8)]
	[InlineData("arm[0]", 0x518)]
	[InlineData("gas[0]", 0x538)]
	[InlineData("Random250[0]", 0x560)]
	public void UnitFieldsAreAt122sOffsets(string name, int offset)
	{
		Assert.Equal(offset, Trace122Layout.Unit.Single(field => field.Name == name).Offset);
	}

	[Fact]
	public void FireFieldsAreAt122sOffsets()
	{
		Assert.Equal(0x30, Trace122Layout.Fire.Single(field => field.Name == "Position.X").Offset);
		Assert.Equal(
			0x68,
			Trace122Layout.Fire.Single(field => field.Name == "Destination.Y").Offset
		);
	}

	// A trace of the port replays without a difference: the seeds, the ticks of each frame and the comparison work.
	[Fact]
	public void TheTraceOfThePortReplays()
	{
		var trace = RecordPort(3, Frames);
		using var replay = Replay(trace, Frames);

		Assert.Null(replay.Divergence);
		Assert.Equal(Frames, replay.Frames);
	}

	// The view moved, as the player's input moves it, is put back to the trace's at the next drawing.
	[Fact]
	public void TheReplayFollowsTheView()
	{
		var trace = RecordPort(3, Frames);
		var views = new List<WorldPosition>();
		using (var reader = new Trace122Reader(new MemoryStream(trace)))
		{
			for (var point = reader.Next(); point.Kind != TracePoint.End; point = reader.Next())
				if (point.Kind == TracePoint.Drawing)
					views.Add(point.Camera);
		}

		using var replay = new Trace122Replay(new MemoryStream(trace));
		using var run = new SingleplayerRun(replay.Scenario, game => SetUp(game, replay));
		run.Frames(Frames / 2);
		run.Game.CameraPosition = new WorldPosition(World.Left, World.Top);
		run.Frames(Frames / 2);

		Assert.Null(replay.Divergence);
		Assert.Equal(views[^1], run.Game.CameraPosition);
	}

	// The cursor changes only what is drawn and the view, which the replay gives back: at (0, 0), the corner, it scrolls
	// the view every frame, and the replay still follows.
	[Fact]
	public void TheCursorDoesNotChangeTheReplay()
	{
		var trace = RecordPort(3, Frames);
		using var replay = new Trace122Replay(new MemoryStream(trace));
		using var run = new SingleplayerRun(replay.Scenario, game => game.Trace = replay);
		run.Frames(Frames);

		Assert.Null(replay.Divergence);
	}

	// The player's orders, given while the trace is recorded, are given back by the replay, which takes no input: a ship
	// of the player's sent away. Selecting it is no order: only its move is recorded, with the selection.
	[Fact]
	public void TheReplayGivesThePlayersOrders()
	{
		var stream = new MemoryStream();
		var recorder = new Trace122Recorder(stream);
		int ship;
		WorldPosition destination;
		using (var run = new SingleplayerRun(4, game => SetUp(game, recorder)))
		{
			var game = run.Game;
			run.Frames(2);
			ship = Enumerable
				.Range(1, game.MaxUnitId)
				.First(m =>
					game.Units[m].IsUsed
					&& game.Units[m].Side == game.LocalSide
					&& game.Units[m].Category == UnitCategory.Ship
					&& game.Units[m].Kind <= UnitKind.Destroyer
				);
			destination = new WorldPosition(
				game.Units[ship].Position.X + 400,
				game.Units[ship].Position.Y
			);
			game.Commands.ClickUnit(ship);
			run.Frames(2);
			game.Commands.SetDestination(destination);
			run.Frames(Frames);
		}
		recorder.Dispose();
		var trace = stream.ToArray();

		using var reader = new Trace122Reader(new MemoryStream(trace));
		var orders = new List<Trace122Orders>();
		for (var point = reader.Next(); point.Kind != TracePoint.End; point = reader.Next())
			if (point.Orders is { } given)
				orders.Add(given);
		var order = Assert.Single(orders);
		Assert.Equal(ship, order.SelectedUnit);
		Assert.Equal(ship, order.Move.Unit);
		Assert.Equal(destination, order.Move.Destination);

		using var replay = Replay(trace, Frames + 4);
		Assert.Null(replay.Divergence);
	}

	// A change to the game shows at the next tick, by the unit and the field.
	[Fact]
	public void TheReplayFindsAChange()
	{
		var trace = RecordPort(3, Frames);
		using var replay = Replay(trace, Frames, game => game.Units[1].HP--);

		Assert.NotNull(replay.Divergence);
		Assert.Contains("unit 1: hp[0]", replay.Divergence);
	}
}
