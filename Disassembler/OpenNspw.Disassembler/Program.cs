using System.Text;

namespace OpenNspw.Disassembler;

internal static class StringBuilderExtensions
{
	public static void AppendLineLF(this StringBuilder builder)
	{
		builder.Append('\n');
	}

	public static void AppendLineLF(this StringBuilder builder, string value)
	{
		builder.Append(value);
		builder.AppendLineLF();
	}
}

class Program
{
	private static readonly Dictionary<string, Subroutine> _subroutines = new();

	static void Main(string[] args)
	{
		AddSubroutine(new Subroutine("sub_403A70", 0x403A70, 0x70));
		AddSubroutine(new Subroutine("sub_403AE0", 0x403AE0, 0x126C));
		AddSubroutine(new Subroutine("cpu_strtgy", 0x404E30, 0x5C8E));
		AddSubroutine(new Subroutine("sub_404D80", 0x404D80, 0xA3));
		AddSubroutine(new Subroutine("sub_414DD0", 0x414DD0, 0xEE));
		AddSubroutine(new Subroutine("decision", 0x41C3C0, 0x1C0A));
		AddSubroutine(new Subroutine("sub_41F230", 0x41F230, 0xC1));
		AddSubroutine(new Subroutine("sub_41F3C0", 0x41F3C0, 0xC3));
		AddSubroutine(new Subroutine("sub_41F490", 0x41F490, 0xD1));
		AddSubroutine(new Subroutine("scenario_1", 0x42A2E0, 0x3D8));
		AddSubroutine(new Subroutine("scenario_2", 0x42A6C0, 0x39F));
		AddSubroutine(new Subroutine("scenario_3", 0x42AA60, 0x223));
		AddSubroutine(new Subroutine("scenario_4", 0x42AC90, 0x2A7));
		AddSubroutine(new Subroutine("scenario_5", 0x42AF40, 0x455));
		AddSubroutine(new Subroutine("scenario_6", 0x42B3A0, 0x4F7));
		AddSubroutine(new Subroutine("scenario_7", 0x42B8A0, 0xBDF));
		AddSubroutine(new Subroutine("scenario_8", 0x42C480, 0xA41));
		AddSubroutine(new Subroutine("scenario_9", 0x42CED0, 0x4EB));
		AddSubroutine(new Subroutine("scenario_10", 0x42D3C0, 0x6BF));
		AddSubroutine(new Subroutine("scenario_100", 0x41F570, 0x2B4));
		AddSubroutine(new Subroutine("scenario_101", 0x41F830, 0x524));
		AddSubroutine(new Subroutine("scenario_102", 0x41FD60, 0x4FF));
		AddSubroutine(new Subroutine("scenario_103", 0x420260, 0x558));
		AddSubroutine(new Subroutine("scenario_104", 0x4207C0, 0x61F));
		AddSubroutine(new Subroutine("scenario_105", 0x420DE0, 0x523));
		AddSubroutine(new Subroutine("scenario_106", 0x421310, 0x514));
		AddSubroutine(new Subroutine("scenario_107", 0x421830, 0x249));
		AddSubroutine(new Subroutine("scenario_108", 0x423940, 0x862));
		AddSubroutine(new Subroutine("scenario_109", 0x424DD0, 0x731));
		AddSubroutine(new Subroutine("scenario_200", 0x421DB0, 0xA61));
		AddSubroutine(new Subroutine("scenario_201", 0x422820, 0x4EA));
		AddSubroutine(new Subroutine("scenario_202", 0x422D10, 0x223));
		AddSubroutine(new Subroutine("scenario_203", 0x422F40, 0xA00));
		AddSubroutine(new Subroutine("scenario_204", 0x4241B0, 0x5A4));
		AddSubroutine(new Subroutine("scenario_205", 0x424760, 0x669));
		AddSubroutine(new Subroutine("scenario_206", 0x421A80, 0x32B));
		AddSubroutine(new Subroutine("scenario_207", 0x425D60, 0x504));
		AddSubroutine(new Subroutine("scenario_208", 0x425510, 0x849));
		AddSubroutine(new Subroutine("scenario_209", 0x426270, 0xB58));
		AddSubroutine(new Subroutine("scenario_300", 0x426DE0, 0x241));
		AddSubroutine(new Subroutine("scenario_301", 0x427030, 0x880));
		AddSubroutine(new Subroutine("scenario_302", 0x4278B0, 0x76A));
		AddSubroutine(new Subroutine("scenario_303", 0x428020, 0x7B1));
		AddSubroutine(new Subroutine("scenario_304", 0x4287E0, 0x47A));
		AddSubroutine(new Subroutine("scenario_305", 0x428C60, 0x6A3));
		AddSubroutine(new Subroutine("scenario_306", 0x429310, 0x935));
		AddSubroutine(new Subroutine("set_cpu_route", 0x42DA80, 0x12A7));
		AddSubroutine(new Subroutine("set_cpu_route2", 0x42ED30, 0x1235));
		AddSubroutine(new Subroutine("set_unit_data", 0x41E150, 0xC85));
		AddSubroutine(new Subroutine("set_new_unit", 0x41EE20, 0x23E));
		AddSubroutine(new Subroutine("set_new_unit_plane", 0x41F060, 0x1D0));
		AddSubroutine(new Subroutine("set_pos_of_parking", 0x416F40, 0x132));
		AddSubroutine(new Subroutine("rand", 0x436056, 0x1E));
		AddSubroutine(new Subroutine("plane_in_cv", 0x416F00, 0x3B));
		AddSubroutine(new Subroutine("cls_all_slct_unit_p2", 0x417930, 0x25));
		AddSubroutine(new Subroutine("sub_41F300", 0x41F300, 0xBC));
		//AddSubroutine(new Subroutine("__ftol", 0x4360C4, 0x27));
		AddSubroutine(new Subroutine("pt_in_rect3", 0x417BE0, 0x2A));
		AddSubroutine(new Subroutine("sub_417C10", 0x417C10, 0x2A));
		//AddSubroutine(new Subroutine("load_it2", 0x432A30, 0x7A));

		foreach (var subroutine in _subroutines.Values)
		{
			var code = GenerateCSharpCode(subroutine);
			File.WriteAllText($"Sub{subroutine.Start:X}.cs", code);
		}
	}

	private static void AddSubroutine(Subroutine s) => _subroutines.Add(s.Name, s);

	private static string GenerateCSharpCode(Subroutine subroutine)
	{
		var emulatorPrefix = "emulator.";
		var parseContext = new ParseContext
		{
			Offset = 0x400000,
			RegisterNameFormat = $"{emulatorPrefix}Cpu.{{0}}",
			MnemonicFormat = $"{emulatorPrefix}Cpu.{{0}}",
			Memory8Format = $"{emulatorPrefix}Memory8[{{0}}]",
			Memory16Format = $"{emulatorPrefix}Memory16[{{0}}]",
			Memory32Format = $"{emulatorPrefix}Memory32[{{0}}]",
			Memory64Format = $"{emulatorPrefix}Memory64[{{0}}]",
			FpuStackFormat = $"{emulatorPrefix}Cpu.Fpu.Stack[{{0}}]",
			FpuStackPopFormat = $"{emulatorPrefix}Cpu.Fpu.Stack.Pop();",
			Register8Format = $"new Register8({{0}})",
			Register16Format = $"new Register16({{0}})",
			Register32Format = $"new Register32({{0}})",
			Register64Format = $"new Register64({{0}})",
		};

		using (var stream = new MemoryStream(File.ReadAllBytes("NSPW_122.exe")))
		{
			var reader = new BinaryReader(stream);
			var disassembler = new Disassembler(reader, subroutine.Start, subroutine.Length, parseContext);
			while (disassembler.Disassemble()) ;

			var builder = new StringBuilder();
			foreach (var line in disassembler.Lines)
			{
				if (disassembler.Labels.Contains(line.Key))
				{
					builder.AppendLineLF();
					builder.AppendLineLF($"loc_{line.Key:X}:");
				}

				foreach (var l in line.Value)
					builder.AppendLineLF($"{l}");
			}

			var template = $$"""
				using Aigamo.Enzan;

				namespace OpenNspw.Interop.Subroutines;

				// {{subroutine.Name}}
				internal static class Sub{{subroutine.Start:X}}
				{
					public static void Call(Emulator emulator)
					{
				{{string.Join('\n', builder.ToString().Split('\n').Select(l => $"\t\t{l}"))}}
					}
				}
				""";
			return template;
		}
	}
}
