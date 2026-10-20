using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// The traces of 1.22 (docs/Verification.md): their layout, and a trace of the port, replayed.
public sealed class Trace122Tests
{
	private const int Frames = 60;

	private static byte[] RecordPort(int scenario, int frames)
	{
		var stream = new MemoryStream();
		var recorder = new Trace122Recorder(stream);
		using (var run = new SingleplayerRun(scenario, game => game.Trace = recorder))
		{
			run.Frames(frames);
		}
		recorder.Dispose();
		return stream.ToArray();
	}

	private static Trace122Replay Replay(byte[] trace, int frames, Action<Nspw>? change = null)
	{
		var replay = new Trace122Replay(new MemoryStream(trace));
		using var run = new SingleplayerRun(replay.Scenario, game => game.Trace = replay);
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
		foreach (var file in Directory.Exists(directory) ? Directory.GetFiles(directory, "*.n122").Order().ToArray() : [])
		{
			using var replay = new Trace122Replay(File.OpenRead(file));
			using var run = new SingleplayerRun(replay.Scenario, game => game.Trace = replay);
			while (!replay.HasEnded && replay.Divergence is null)
				run.Frames(1);
			if (replay.Divergence is { } divergence)
				divergences.Add($"{Path.GetFileName(file)}: {divergence}");
		}

		Assert.Empty(divergences);
	}

	// The fields are where 1.22 has them (docs/Singleplayer.md).
	[Theory]
	[InlineData("Position.X", 0x08)]
	[InlineData("Direction", 0x70)]
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
		Assert.Equal(0x68, Trace122Layout.Fire.Single(field => field.Name == "Destination.Y").Offset);
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

	// A change to the game shows at the next tick, by the unit and the field.
	[Fact]
	public void TheReplayFindsAChange()
	{
		var trace = RecordPort(3, Frames);
		using var replay = Replay(trace, Frames, game => game.Units[1].Hp--);

		Assert.NotNull(replay.Divergence);
		Assert.Contains("unit 1: hp[0]", replay.Divergence);
	}
}
