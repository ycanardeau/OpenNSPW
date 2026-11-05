using OpenNspw.DirectPlay;
using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// The garbage that a game makes as it plays, which the collector stops it to take: budgets, so that a change that makes
// much more fails here, before it is merged, rather than in a battle that stutters. What a thread allocates is counted
// on that thread (GC.GetAllocatedBytesForCurrentThread).
public sealed class AllocationTests
{
	// The most bytes that a frame of a battle may allocate on the game's thread: the original's debug lines (DBG_MODE),
	// whose numbers printf takes boxed, make most of what it makes now.
	private const long FrameBudget = 2_500;

	// What `run` allocates on this thread once it is warm: the least of several rounds, as the runtime's tiered
	// compilation may do its own work on the calling thread in a round, and allocate for it.
	internal static long SteadyBytes(Action run)
	{
		var least = long.MaxValue;
		for (var round = 0; round < 10 && least != 0; round++)
		{
			var start = GC.GetAllocatedBytesForCurrentThread();
			run();
			least = Math.Min(least, GC.GetAllocatedBytesForCurrentThread() - start);
		}

		return least;
	}

	// Counts what the game's thread allocates from the start of each frame of the battle to its presentation, after a
	// number of frames to warm up: not the message loop between frames, whose DirectPlay is the tests' own.
	private sealed class Meter(int warmUp) : IGameExtension
	{
		private readonly int _warmUp = warmUp;
		private long _start;

		public int Frames { get; private set; }

		public long Bytes { get; private set; }

		public short FrameStarting(Nspw game, short ticks)
		{
			_start = GC.GetAllocatedBytesForCurrentThread();
			return ticks;
		}

		public void Presented(Nspw game)
		{
			var bytes = GC.GetAllocatedBytesForCurrentThread() - _start;
			if (Frames++ >= _warmUp)
				Bytes += bytes;
		}
	}

	// A battle of 1.22's first scenario, drawn from 1.22's sprite sheet or from the app's sprite files, whose sheet looks
	// up a piece at each blit (SpriteSheetSurface), and at a steady speed (docs/SteadySpeed.md).
	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void ABattlesFrameMakesLittleGarbage(bool drawsFromSpriteFiles, bool isSteady)
	{
		const int warmUp = 100;
		const int frames = 300;
		var meter = new Meter(warmUp);
		using var run = new SingleplayerRun(
			1,
			game =>
			{
				if (isSteady)
					game.Extensions.Add(new Pacing.SteadySpeed());
				game.Extensions.Add(meter);
			},
			drawsFromSpriteFiles: drawsFromSpriteFiles,
			recordsOutput: false
		);

		run.Frames(warmUp + frames);

		var perFrame = meter.Bytes / (meter.Frames - warmUp);
		Assert.True(
			perFrame <= FrameBudget,
			$"A frame allocated {perFrame} bytes, more than the {FrameBudget} it may."
		);
	}

	// What a frame not shown allocates on the game's thread, from its start to the next frame's, which covers its text
	// written after its end, after a number of frames to warm up: at a steady speed of 300, which runs about 1,700 frames a
	// second, most of them not shown (docs/SteadySpeed.md).
	private sealed class FramesNotShown(int warmUp) : IGameExtension
	{
		private readonly int _warmUp = warmUp;
		private long _start;
		private bool _wasNotShown;
		private int _seen;

		public int Frames { get; private set; }

		public long Bytes { get; private set; }

		public short FrameStarting(Nspw game, short ticks)
		{
			var now = GC.GetAllocatedBytesForCurrentThread();
			if (_wasNotShown && _seen++ >= _warmUp)
			{
				Frames++;
				Bytes += now - _start;
			}

			_start = now;
			_wasNotShown = false;
			return ticks;
		}

		public void FrameEnded(Nspw game) => _wasNotShown = game.Renderer.DrawsNothing;
	}

	// The most bytes that a frame not shown may allocate: it draws nothing, but runs as any other, its unit panel's texts
	// formatted too.
	private const long NotShownBudget = 800;

	// A frame not shown writes and decodes no text, which would make garbage in each of the many: at speed 300 it once made
	// about 2.6 MB a second, and a full collection every few seconds.
	[Fact]
	public void AFrameNotShownMakesLittleGarbage()
	{
		var meter = new FramesNotShown(warmUp: 500);
		using var run = new SingleplayerRun(
			1,
			game =>
			{
				game.Extensions.Add(new Pacing.SteadySpeed());
				game.Extensions.Add(meter);
			},
			recordsOutput: false
		);
		run.Frames(3);
		run.Game.GameSpeed = 300;

		run.Frames(60);

		Assert.True(meter.Frames > 1_000);
		var perFrame = meter.Bytes / meter.Frames;
		Assert.True(
			perFrame <= NotShownBudget,
			$"A frame not shown allocated {perFrame} bytes, more than the {NotShownBudget} it may."
		);
	}

	// DirectPlay's DoWork, which the game's message loop calls without a pause, allocates nothing: it once made a garbage
	// collection about 170 times a second.
	[Fact]
	public void DirectPlaysDoWorkMakesNoGarbage()
	{
		var factory = new DirectPlay8Factory();
		factory.CoCreateInstance(dplay8.CLSID_DirectPlay8Peer, default, out var peer);
		factory.CoCreateInstance(dplay8.CLSID_DirectPlay8ThreadPool, default, out var threadPool);
		var pool = (IDirectPlay8ThreadPool)threadPool!;

		var bytes = SteadyBytes(() =>
		{
			for (var i = 0; i < 10_000; i++)
				pool.DoWork(0, 0);
		});

		((IDirectPlay8Peer)peer!).Close(0);
		Assert.Equal(0, bytes);
	}

	// The app rasterizes a text that the game writes again, as it writes most of them every frame, only once.
	[Fact]
	public void TheAppRasterizesATextOnce()
	{
		using var files = GameData.CreateEmpty();
		var platform = new Desktop.DesktopPlatform(
			new GameFileLayers([], files.Path, []),
			new Desktop.TextRasterizer()
		);
		var first = platform.RasterizeText("経過時間 12", 12, "ＭＳ ゴシック");
		TextBitmap? again = null;

		var bytes = SteadyBytes(() =>
			again = platform.RasterizeText("経過時間 12", 12, "ＭＳ ゴシック")
		);

		Assert.Same(first, again);
		Assert.Equal(0, bytes);
	}
}
