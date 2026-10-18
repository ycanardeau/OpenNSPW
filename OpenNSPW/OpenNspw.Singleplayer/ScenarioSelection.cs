using System.Text;

namespace OpenNspw.Singleplayer;

// A scenario of the single-player executable, with its title.
public sealed record ScenarioTitle(int Number, string Title);

// A set of scenarios, as the scenario selection screen shows one at a time, with its heading.
public sealed record ScenarioSet(string Heading, IReadOnlyList<ScenarioTitle> Scenarios);

// The scenarios of the single-player executable as its scenario selection screen lists them, in four sets. The
// headings and the titles are Shift_JIS strings of the executable, which the screen's code pushes from 0x40AE24.
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

	public static IReadOnlyList<ScenarioSet> Read(ReadOnlySpan<byte> executable)
	{
		var image = PortableExecutable.Read(executable);
		var sets = new List<ScenarioSet>();
		foreach (var (heading, scenarios) in Sets)
		{
			var titles = new List<ScenarioTitle>();
			foreach (var (number, title) in scenarios)
				titles.Add(new(number, ReadString(executable, image, title)));

			sets.Add(new(ReadString(executable, image, heading), titles));
		}

		return sets;
	}
}
