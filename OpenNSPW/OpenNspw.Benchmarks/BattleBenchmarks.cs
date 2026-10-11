using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace OpenNspw.Benchmarks;

// The game's own work in a battle of the first scenario, in which both sides send their ships and planes at each other
// (SoloGame.SendForcesAtEachOther). Each iteration starts from the same tick of the battle, where the air battle
// begins, and runs the same ticks or frames, so that versions of the game that behave the same do the same work. The
// state that an iteration ends with is printed, and must be the same in every iteration.
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 5, iterationCount: 20, invocationCount: 1)]
public class BattleBenchmarks
{
	private const int StartTick = 2000;

	// 50 turns (cnct_loop).
	private const int Ticks = 1500;

	// 10 turns at the normal speed.
	private const int Frames = 300;

	private const int Draws = 100;

	private SoloGame _game = null!;
	private GameSnapshot _start = null!;
	private string? _endHash;

	[GlobalSetup]
	public void Setup()
	{
		_game = SoloGame.Start();
		_game.SendForcesAtEachOther();
		while (_game.Game.Tick < StartTick)
		{
			_game.Tick();
		}

		_start = GameSnapshot.Take(_game.Game);
	}

	[IterationSetup]
	public void Restore()
	{
		_start.Restore(_game.Game);
	}

	[IterationCleanup]
	public void CheckState()
	{
		var hash = _game.StateHash();
		if (_endHash is null)
		{
			_endHash = hash;
			Console.WriteLine($"// State after an iteration: {hash}");
		}
		else if (hash != _endHash)
		{
			throw new InvalidOperationException($"The iteration ended with the state {hash}, not {_endHash}: it did not do the same work.");
		}
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		_game.Dispose();
	}

	// The simulation: UpdateBattle and CheckResult, as UpdateFrame runs them each tick.
	[Benchmark(OperationsPerInvoke = Ticks)]
	public void Tick()
	{
		for (var i = 0; i < Ticks; i++)
		{
			_game.Tick();
		}
	}

	// A whole frame at the normal speed (UpdateFrame): input, a tick, drawing the battle and showing the frame.
	[Benchmark(OperationsPerInvoke = Frames)]
	public void Frame()
	{
		for (var i = 0; i < Frames; i++)
		{
			_game.Frame();
		}
	}

	// Drawing the battle (UpdateUnitInfo, DrawBattleArea and DrawMinimap), the same frame each time.
	[Benchmark(OperationsPerInvoke = Draws)]
	public void Draw()
	{
		for (var i = 0; i < Draws; i++)
		{
			_game.Draw();
		}
	}
}
