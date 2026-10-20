using OpenNspw;
using OpenNspw.Recorder122;

// Records traces of NSPW 1.22 on Windows (docs/Verification.md): runs NSPW_122.exe under the debugger, and records each
// battle played in it as a trace (Trace122), <scenario>-<date>-<time>.n122, in the output directory.
//
// OpenNspw.Recorder122 <NSPW_122.exe> [output directory]

// The functions of 1.22 where the trace's points are read, at their first instruction, but Seeded, after game_init's
// last srand.
const uint GameInit = 0x429C50;
const uint GameInitSeeded = 0x42A16E;
const uint Frame = 0x434570;
const uint CharaCont = 0x401000;
const uint Decision = 0x41C3C0;
const uint Orders = 0x430390;

// Its globals that the trace records.
const uint RandomState = 0x442250; // rand's holdrand
const uint Mode = 0x463B2A; // 10 in a battle
const uint Speed = 0x489FA8;
const uint CcCount = 0x489EE4;
const uint DrawnTick = 0x48A1EC;
const uint Scenario = 0x48A1E8; // sinario
const uint Units = 0x4916A0;
const uint Fires = 0x48A600;
const short BattleMode = 10;

// rand (0x436056): mov eax, [0x442250]; imul eax, eax, 0x343FD; the check that the program is 1.22.
byte[] randCode = [0xA1, 0x50, 0x22, 0x44, 0x00, 0x69, 0xC0, 0xFD, 0x43, 0x03, 0x00];

if (args.Length is < 1 or > 2)
{
	Console.Error.WriteLine("Usage: OpenNspw.Recorder122 <NSPW_122.exe> [output directory]");
	return 2;
}

var executable = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args.Length == 2 ? args[1] : ".");
Directory.CreateDirectory(output);

var state = new byte[Trace122.StateSize];
var gate = new object();
Trace122Writer? writer = null;
string? path = null;
var frames = 0;
var isChecked = false;

void Close()
{
	lock (gate)
	{
		if (writer is null)
			return;
		writer.Dispose();
		writer = null;
		Console.WriteLine($"Recorded {frames} frames in {path}.");
	}
}

void Hit(Debugger debugger, uint address)
{
	if (!isChecked)
	{
		if (!debugger.Read(0x436056, randCode.Length).AsSpan().SequenceEqual(randCode))
			throw new InvalidOperationException("This is not NSPW 1.22: its rand is not at 0x436056.");
		isChecked = true;
	}

	lock (gate)
	{
		var random = debugger.ReadUInt32(RandomState);
		switch (address)
		{
			case GameInit:
			{
				Close();
				var scenario = debugger.ReadInt16(Scenario);
				path = Path.Combine(output, $"{scenario}-{DateTime.Now:yyyyMMdd-HHmmss}.n122");
				writer = new Trace122Writer(File.Create(path), scenario);
				frames = 0;
				writer.WriteRandom(TracePoint.Start, random);
				Console.WriteLine($"Scenario {scenario} started; recording to {path}.");
				break;
			}
			case GameInitSeeded:
				writer?.WriteRandom(TracePoint.Seeded, random);
				break;
			case Frame:
				if (writer is null || debugger.ReadInt16(Mode) != BattleMode)
					break;
				writer.WriteFrame(debugger.ReadInt16(Speed));
				frames++;
				if (frames % 1000 == 0)
					Console.WriteLine($"{frames} frames.");
				break;
			case CharaCont:
				if (writer is null)
					break;
				debugger.Read(Units, Trace122.UnitCount * Trace122.UnitSize).CopyTo(state, 0);
				debugger.Read(Fires, Trace122.FireCount * Trace122.FireSize).CopyTo(state, Trace122.UnitCount * Trace122.UnitSize);
				writer.WriteTick(debugger.ReadInt32(CcCount), debugger.ReadInt32(DrawnTick) != 0, random, state);
				break;
			case Decision:
				writer?.WriteRandom(TracePoint.Decision, random);
				break;
			case Orders:
				writer?.WriteRandom(TracePoint.Orders, random);
				break;
		}
	}
}

Console.CancelKeyPress += (_, _) => Close();

using (var debugger = new Debugger(executable, [GameInit, GameInitSeeded, Frame, CharaCont, Decision, Orders], Hit))
{
	Console.WriteLine($"Running {executable}. Play a battle without touching the mouse or the keyboard, then close the game.");
	debugger.Run();
}

Close();
return 0;
