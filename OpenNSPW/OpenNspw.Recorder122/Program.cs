using OpenNspw;
using OpenNspw.Recorder122;

// Records traces of NSPW 1.22 on Windows (docs/Verification.md): runs NSPW_122.exe under the debugger, and records each
// battle played in it as a trace (Trace122), <scenario>-<date>-<time>.n122, in the output directory.
//
// OpenNspw.Recorder122 <NSPW_122.exe> [output directory] [--scenario <n> | --all] [--speed <n>] [--frames <n>]
//     [--orders <n>] [--seed <n>]
//
// With neither --scenario nor --all, the player chooses the battles in 1.22. With them, the recorder plays without
// anyone: it starts each scenario as a click on it does, at the speed given, gives the player's side an order every
// --orders frames (20 by default, 0 for none; Commander), from the recorder's random numbers of --seed, and ends 1.22
// after the frames given, or once the battle has a result, then goes on to the next scenario.

// The functions of 1.22 where the trace's points are read, at their first instruction, but Seeded, after game_init's
// last srand. Drawing is draw_cmbt_area, which only the battle's frame calls (0x434630).
const uint GameInit = 0x429C50;
const uint GameInitSeeded = 0x42A16E;
const uint Frame = 0x434570;
const uint CharaCont = 0x401000;
const uint Decision = 0x41C3C0;
const uint Orders = 0x430390;
const uint DrawCmbtArea = 0x40DB50;

// Where the input (0x4312A0) applies the combat menu's item clicked to the selected unit, which it has just stored in
// cmbt_menu_slctd (0x431C59).
const uint MenuItemApplied = 0x431C60;

// The uninitialized locals that cpu_strtgy reads, which the trace records as Windows' stack holds them, by the instruction
// that reads them and the local's offset from esp there: scenario 208's count of planes at fleet 2's base.
(uint Instruction, uint Offset)[] stackLocals = [(0x4050C7, 0x30)];

// Where the recorder plays in place of the player: the title screen's test of a click (0x40AD39), after which it goes
// to the scenarios; and the scenario screen's test for a battle to start (0x40C613), after its rows and its click have
// set sinario and the mode, before game_init.
const uint TitleClickTest = 0x40AD39;
const uint BattleStartTest = 0x40C613;

// Its globals that the trace records.
const uint RandomState = 0x442250; // rand's holdrand
const uint Mode = 0x463B2A; // 10 in a battle
const uint Speed = 0x489FA8;
const uint CcCount = 0x489EE4;
const uint DrawnTick = 0x48A1EC;
const uint Scenario = 0x48A1E8; // sinario
const uint Units = 0x4916A0;
const uint Fires = 0x48A600;
const uint Camera = 0x489AB0; // cmbt_x, cmbt_y: doubles
const short BattleMode = 10;

// The player's orders (Trace122Orders): new_pp[1] (used, x at 8, y at 16, cls at 24), new_slct[1] (sw, the_slct_unit at
// 4, m at 8, gr_x at 16, gr_y at 24), cmbt_menu_slctd, the_slct_unit, slct_unit_no and slct_unit[1].
const uint PlayerMoveOrder = 0x463AE0;
const uint PlayerSelectOrder = 0x489A60;
const uint MenuItem = 0x489AC0;
const uint SelectedUnit = 0x463AA8;
const uint SelectionCount = 0x463B28;
const uint Selection = 0x489CE0;

// The globals the recorder writes to play: the title's button, 3 once clicked, and game_end, set with a result.
const uint TitleButton = 0x463B30;
const uint Result = 0x483B44;
const short Clicked = 3;

// rand (0x436056): mov eax, [0x442250]; imul eax, eax, 0x343FD; the check that the program is 1.22.
byte[] randCode = [0xA1, 0x50, 0x22, 0x44, 0x00, 0x69, 0xC0, 0xFD, 0x43, 0x03, 0x00];

static string? Option(string[] args, string name)
{
	var index = Array.IndexOf(args, name);
	return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

// The arguments that are neither options nor their values.
static string[] Positionals(string[] args)
{
	var positionals = new List<string>();
	for (var i = 0; i < args.Length; i++)
	{
		if (args[i] is "--scenario" or "--speed" or "--frames" or "--orders" or "--seed")
			i++;
		else if (args[i] != "--all")
			positionals.Add(args[i]);
	}
	return [.. positionals];
}

static int? NumberOption(string[] args, string name)
{
	return Option(args, name) is { } text ? int.Parse(text) : null;
}

var positionalArgs = Positionals(args);
int? scenarioOption;
int? speedOption;
int? frameLimit;
int? ordersOption;
int? seedOption;
try
{
	scenarioOption = NumberOption(args, "--scenario");
	speedOption = NumberOption(args, "--speed");
	frameLimit = NumberOption(args, "--frames");
	ordersOption = NumberOption(args, "--orders");
	seedOption = NumberOption(args, "--seed");
}
catch (FormatException)
{
	positionalArgs = [];
	scenarioOption = speedOption = frameLimit = ordersOption = seedOption = null;
}

if (positionalArgs.Length is < 1 or > 2)
{
	Console.Error.WriteLine("Usage: OpenNspw.Recorder122 <NSPW_122.exe> [output directory] [--scenario <n> | --all] [--speed <n>] [--frames <n>] [--orders <n>] [--seed <n>]");
	return 2;
}

var playsAll = args.Contains("--all");
int[] scenarios = playsAll ? [.. OpenNspw.Singleplayer.Functions.Scenarios] : scenarioOption is { } one ? [one] : [];
if (scenarios.FirstOrDefault(s => !OpenNspw.Singleplayer.Functions.Scenarios.Contains(s)) is var unknown and not 0)
{
	Console.Error.WriteLine($"Not a scenario of NSPW 1.22: {unknown}. The scenarios are {string.Join(", ", OpenNspw.Singleplayer.Functions.Scenarios)}.");
	return 2;
}

var executable = Path.GetFullPath(positionalArgs[0]);
var output = Path.GetFullPath(positionalArgs.Length == 2 ? positionalArgs[1] : ".");
Directory.CreateDirectory(output);

var state = new byte[Trace122.StateSize];
var gate = new object();
Trace122Writer? writer = null;
string? path = null;
var frames = 0;
var isChecked = false;

// The scenario the recorder starts in this run of 1.22, or null when the player chooses; whether it has started it.
int? playedScenario = null;
var hasStartedBattle = false;

// The player's side's orders when the recorder plays, from a seed that is printed, so that a run can be made again.
var seed = seedOption ?? Random.Shared.Next();
var commander = new Commander(new Random(seed), ordersOption ?? 20);

// The combat menu's item the player clicked in this frame, for its unit, until the orders record it.
var menuOrder = new MenuOrder();

// The player's orders as the orders function is about to take them: null if there are none.
Trace122Orders? ReadPlayerOrders(Debugger debugger)
{
	var move = debugger.Read(PlayerMoveOrder, 32);
	var select = debugger.Read(PlayerSelectOrder, 32);
	if (BitConverter.ToInt16(move, 0) == 0 && BitConverter.ToInt32(select, 0) == 0 && menuOrder.Menu == CombatMenuItem.None)
		return null;

	var selection = debugger.Read(Selection, 2 * Trace122Orders.SelectionLength);
	var selections = new short[Trace122Orders.SelectionLength];
	for (var s = 0; s < selections.Length; s++)
		selections[s] = BitConverter.ToInt16(selection, 2 * s);
	return new Trace122Orders(
		new MoveOrder { Unit = BitConverter.ToInt16(move, 0), Destination = new WorldPosition(BitConverter.ToDouble(move, 8), BitConverter.ToDouble(move, 16)), ClearsPath = BitConverter.ToInt32(move, 24) },
		new SelectOrder
		{
			IsSet = new Bool32(BitConverter.ToInt32(select, 0)),
			SelectedUnit = BitConverter.ToInt16(select, 4),
			Unit = BitConverter.ToInt16(select, 8),
			GroundPosition = new WorldPosition(BitConverter.ToDouble(select, 16), BitConverter.ToDouble(select, 24)),
		},
		menuOrder,
		debugger.ReadInt16(SelectedUnit),
		debugger.ReadInt16(SelectionCount),
		selections);
}

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

// Plays in place of the player: past the title, the scenario, then the speed of each frame, and the orders. Ends 1.22 at the start of
// a frame once the frames asked for are recorded, or once the battle has a result, before the frame is recorded, so that
// the trace ends with a whole frame.
void Play(Debugger debugger, uint address)
{
	if (playedScenario is not { } scenario)
		return;

	switch (address)
	{
		case TitleClickTest:
			debugger.WriteInt16(TitleButton, Clicked);
			break;
		case BattleStartTest when !hasStartedBattle:
			debugger.WriteInt16(Scenario, (short)scenario);
			debugger.WriteInt16(Mode, BattleMode);
			hasStartedBattle = true;
			break;
		case Frame when writer is not null && debugger.ReadInt16(Mode) == BattleMode:
			if (frames >= frameLimit || debugger.ReadInt32(Result) != 0)
			{
				Console.WriteLine(frames >= frameLimit ? $"{frames} frames recorded, as asked." : $"The battle has a result (game_end {debugger.ReadInt32(Result)}).");
				Close();
				debugger.Terminate();
				break;
			}
			if (speedOption is { } speed)
				debugger.WriteInt16(Speed, (short)speed);
			break;
		case Orders when writer is not null && commander.Order(debugger, frames) is { } order:
			Console.WriteLine($"Frame {frames}: {order}.");
			break;
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
		Play(debugger, address);
		if (writer is null && address != GameInit)
			return;

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
				writer!.WriteRandom(TracePoint.Seeded, random);
				break;
			case Frame:
				if (debugger.ReadInt16(Mode) != BattleMode)
					break;
				writer!.WriteFrame(debugger.ReadInt16(Speed));
				frames++;
				if (frames % 1000 == 0)
					Console.WriteLine($"{frames} frames.");
				break;
			case CharaCont:
				debugger.Read(Units, Trace122.UnitCount * Trace122.UnitSize).CopyTo(state, 0);
				debugger.Read(Fires, Trace122.FireCount * Trace122.FireSize).CopyTo(state, Trace122.UnitCount * Trace122.UnitSize);
				writer!.WriteTick(debugger.ReadInt32(CcCount), debugger.ReadInt32(DrawnTick) != 0, random, state);
				break;
			case Decision:
				writer!.WriteRandom(TracePoint.Decision, random);
				break;
			case var _ when stackLocals.Any(local => local.Instruction == address):
			{
				var offset = stackLocals.First(local => local.Instruction == address).Offset;
				writer!.WriteStackLocal(address, debugger.ReadUInt32(debugger.ReadEsp() + offset));
				break;
			}
			case MenuItemApplied:
				menuOrder = new MenuOrder { Menu = (CombatMenuItem)debugger.ReadInt16(MenuItem), SelectedUnit = debugger.ReadInt16(SelectedUnit) };
				break;
			case Orders:
				if (ReadPlayerOrders(debugger) is { } orders)
					writer!.WritePlayerOrders(orders);
				menuOrder = new MenuOrder();
				writer!.WriteRandom(TracePoint.Orders, random);
				break;
			case DrawCmbtArea:
			{
				var camera = debugger.Read(Camera, 2 * sizeof(double));
				writer!.WriteDrawing(random, new WorldPosition(BitConverter.ToDouble(camera, 0), BitConverter.ToDouble(camera, sizeof(double))));
				break;
			}
		}
	}
}

// Runs 1.22 once, until it exits: played by the player, or by the recorder in a scenario.
void Run(int? scenario)
{
	playedScenario = scenario;
	hasStartedBattle = false;
	uint[] breakpoints = scenario is null
		? [GameInit, GameInitSeeded, Frame, CharaCont, Decision, Orders, DrawCmbtArea, MenuItemApplied, .. stackLocals.Select(local => local.Instruction)]
		: [GameInit, GameInitSeeded, Frame, CharaCont, Decision, Orders, DrawCmbtArea, MenuItemApplied, .. stackLocals.Select(local => local.Instruction), TitleClickTest, BattleStartTest];
	using (var debugger = new Debugger(executable, breakpoints, Hit))
	{
		Console.WriteLine(scenario is null
			? $"Running {executable}. Play a battle with the mouse and the speed keys, then close the game."
			: $"Running {executable} in scenario {scenario}, with the player's orders of seed {seed} (--seed). Click nothing in it until it closes.");
		debugger.Run();
	}

	Close();
}

Console.CancelKeyPress += (_, _) => Close();

if (scenarios.Length == 0)
	Run(null);
foreach (var scenario in scenarios)
	Run(scenario);
return 0;
