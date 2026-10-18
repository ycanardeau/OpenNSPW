using System.Buffers.Binary;
using System.Text;

namespace OpenNspw.Singleplayer;

// The side a scenario gives the player.
public enum ScenarioSide
{
	Japan,
	UnitedStates,
}

// A scenario of the single-player executable: its title, the side the player commands, and its briefing, the lines the
// setting screen writes under the side, ending with its victory conditions (勝敗条件), which scenario 303 does not state.
public sealed record ScenarioEntry(int Number, string Title, ScenarioSide Side, string SideName, IReadOnlyList<string> Briefing);

// A set of scenarios, as the scenario selection screen shows one at a time, with its heading.
public sealed record ScenarioSet(string Heading, IReadOnlyList<ScenarioEntry> Scenarios);

// The scenarios of the single-player executable as its scenario selection screen lists them, in four sets, with what its
// setting screen states about each. Every text is a Shift_JIS string of the executable: the headings and the titles,
// which the selection screen's code pushes from 0x40AE24, and the sides and the briefings, which the setting screen's
// code (0x40B406) pushes scenario after scenario, each starting with its side.
public static class ScenarioSelection
{
	private static readonly (uint Heading, (int Number, uint Title)[] Scenarios)[] Sets =
	[
		(0x441474, [
			(1, 0x441464), (2, 0x44144C), (3, 0x44143C), (4, 0x441430), (5, 0x44141C),
			(6, 0x441404), (7, 0x4413E0), (8, 0x4413CC), (9, 0x4413BC), (10, 0x4413AC),
		]),
		(0x441370, [
			(100, 0x441364), (101, 0x44134C), (102, 0x441334), (103, 0x44130C), (104, 0x4412FC),
			(105, 0x4412E4), (106, 0x4412CC), (107, 0x4412B8), (108, 0x4412A8), (109, 0x441288),
		]),
		(0x44124C, [
			(200, 0x441234), (201, 0x441218), (202, 0x441204), (203, 0x4411E4), (204, 0x4411BC),
			(205, 0x441198), (206, 0x441184), (207, 0x441170), (208, 0x44114C), (209, 0x441138),
		]),
		(0x4410FC, [
			(300, 0x4410E4), (301, 0x4410CC), (302, 0x4410B4), (303, 0x441090), (304, 0x441080),
			(305, 0x44106C), (306, 0x441058),
		]),
	];

	// The setting screen's code that writes the briefings, up to its last push of one, and the strings it pushes.
	private const uint BriefingCodeStart = 0x40B406;
	private const uint BriefingCodeEnd = 0x40C5AC;
	private const uint BriefingStringsStart = 0x43E0A4;
	private const uint BriefingStringsEnd = 0x441040;

	// The line a briefing starts with: 操作対象：日本海軍 or 操作対象：合衆国海軍.
	private const string SideLabel = "操作対象：";

	private const byte PushImmediate = 0x68;

	private static Encoding CreateShiftJis()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		return Encoding.GetEncoding(932);
	}

	private static readonly Encoding ShiftJis = CreateShiftJis();

	private static string ReadString(ReadOnlySpan<byte> executable, PortableExecutable image, uint address)
	{
		var bytes = executable[(int)image.FileOffsetOf(address)..];
		return ShiftJis.GetString(bytes[..bytes.IndexOf((byte)0)]);
	}

	// The lines of each scenario's briefing, side first, in the order of the scenarios.
	private static List<List<string>> ReadBriefings(ReadOnlySpan<byte> executable, PortableExecutable image)
	{
		var code = executable[(int)image.FileOffsetOf(BriefingCodeStart)..(int)image.FileOffsetOf(BriefingCodeEnd)];
		var briefings = new List<List<string>>();
		for (var i = 0; i + 5 <= code.Length; i++)
		{
			if (code[i] != PushImmediate)
				continue;

			var address = BinaryPrimitives.ReadUInt32LittleEndian(code[(i + 1)..]);
			if (address is < BriefingStringsStart or >= BriefingStringsEnd)
				continue;

			var line = ReadString(executable, image, address);
			if (line.StartsWith(SideLabel, StringComparison.Ordinal))
				briefings.Add([line]);
			else if (briefings.Count > 0)
				briefings[^1].Add(line);
			else
				throw new InvalidDataException($"The first briefing does not start with its side, but with {line}.");

			i += 4;
		}

		return briefings;
	}

	private static ScenarioEntry CreateEntry(int number, string title, List<string> briefing)
	{
		var sideName = briefing[0][SideLabel.Length..];
		var side = sideName switch
		{
			"日本海軍" => ScenarioSide.Japan,
			"合衆国海軍" => ScenarioSide.UnitedStates,
			_ => throw new InvalidDataException($"Scenario {number} gives the player {sideName}, not a side."),
		};
		return new(number, title, side, sideName, briefing[1..]);
	}

	public static IReadOnlyList<ScenarioSet> Read(ReadOnlySpan<byte> executable)
	{
		var image = PortableExecutable.Read(executable);
		var briefings = ReadBriefings(executable, image);
		var scenarioCount = Sets.Sum(s => s.Scenarios.Length);
		if (briefings.Count != scenarioCount)
			throw new InvalidDataException($"The setting screen has {briefings.Count} briefings, not {scenarioCount}.");

		var sets = new List<ScenarioSet>();
		var index = 0;
		foreach (var (heading, scenarios) in Sets)
		{
			var entries = new List<ScenarioEntry>();
			foreach (var (number, title) in scenarios)
				entries.Add(CreateEntry(number, ReadString(executable, image, title), briefings[index++]));

			sets.Add(new(ReadString(executable, image, heading), entries));
		}

		return sets;
	}
}
