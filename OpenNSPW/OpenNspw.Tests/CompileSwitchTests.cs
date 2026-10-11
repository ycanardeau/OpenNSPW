using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace OpenNspw.Tests;

// The original's compile-time switches (#define in all_head.h, such as NSPW_THE_NET and SND_SW), which the port tests
// with #if. A C# #if only sees the symbols defined for the compilation, so the port defines in OpenNspw.csproj the
// switches that the original sets to a value other than 0. A switch that is missing there compiles the other branch,
// silently: see docs/KnownIssues.md.
public partial class CompileSwitchTests
{
	[GeneratedRegex(@"^\s*#\s*define\s+(\w+)\s+(-?\d+)\b", RegexOptions.Multiline)]
	private static partial Regex DefineRegex();

	[GeneratedRegex(@"^\s*#\s*(?:if|elif)\b(.*)$", RegexOptions.Multiline)]
	private static partial Regex ConditionRegex();

	[GeneratedRegex(@"[A-Za-z_]\w*")]
	private static partial Regex IdentifierRegex();

	private static DirectoryInfo FindRepository()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "Original", "NSPW_NET", "all_head.h")) && File.Exists(Path.Combine(directory.FullName, "OpenNSPW", "OpenNspw", "OpenNspw.csproj")))
			{
				return directory;
			}
		}

		throw new DirectoryNotFoundException("The repository, with Original/NSPW_NET and OpenNSPW/OpenNspw, is not above the test assembly.");
	}

	private static readonly DirectoryInfo Repository = FindRepository();

	private static string PortDirectory => Path.Combine(Repository.FullName, "OpenNSPW", "OpenNspw");

	// The numeric #defines of all_head.h, which include the switches, with all the values of each: a few names are
	// defined in both branches of an #if.
	private static ILookup<string, int> OriginalDefines()
	{
		var text = File.ReadAllText(Path.Combine(Repository.FullName, "Original", "NSPW_NET", "all_head.h"));
		return DefineRegex().Matches(text).ToLookup(m => m.Groups[1].Value, m => int.Parse(m.Groups[2].Value));
	}

	// The identifiers in the #if and #elif conditions of the files.
	private static HashSet<string> ConditionSymbols(IEnumerable<string> files)
	{
		return [.. files.SelectMany(file => ConditionRegex().Matches(File.ReadAllText(file)))
			.SelectMany(m => IdentifierRegex().Matches(m.Groups[1].Value.Split("//")[0]).Select(i => i.Value))];
	}

	// The symbols that OpenNspw.csproj defines for the port.
	private static HashSet<string> PortDefinedSymbols()
	{
		var project = XDocument.Load(Path.Combine(PortDirectory, "OpenNspw.csproj"));
		return [.. project.Descendants("DefineConstants")
			.SelectMany(e => e.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			.Where(s => s != "$(DefineConstants)")];
	}

	// The switches of all_head.h that the original tests with #if. Each has one value.
	private static Dictionary<string, int> OriginalSwitches()
	{
		var defines = OriginalDefines();
		var original = Path.Combine(Repository.FullName, "Original", "NSPW_NET");
		var used = ConditionSymbols(Directory.GetFiles(original, "*.cpp").Concat(Directory.GetFiles(original, "*.h")));
		return defines.Where(d => used.Contains(d.Key)).ToDictionary(d => d.Key, d => d.Single());
	}

	public static TheoryData<string, int> Switches()
	{
		var data = new TheoryData<string, int>();
		foreach (var (name, value) in OriginalSwitches().OrderBy(s => s.Key, StringComparer.Ordinal))
		{
			data.Add(name, value);
		}

		return data;
	}

	[Fact]
	public void The_original_has_switches()
	{
		Assert.Contains("NSPW_THE_NET", OriginalSwitches().Keys);
		Assert.Contains("SND_SW", OriginalSwitches().Keys);
	}

	// Every symbol of the port's #if conditions is a switch of the original (or a C# literal), so none is left undefined
	// by mistake.
	[Fact]
	public void The_port_tests_only_the_original_switches()
	{
		var switches = OriginalSwitches();
		var used = ConditionSymbols(Directory.GetFiles(PortDirectory, "*.cs"));
		used.ExceptWith(["true", "false"]);
		Assert.Empty(used.Where(s => !switches.ContainsKey(s)));
	}

	// A switch is defined for the port exactly when the original sets it to a value other than 0, so that `#if X` takes
	// the branch the original takes, and `#if !X` (for `#if X==0`) the other.
	[Theory]
	[MemberData(nameof(Switches))]
	public void A_switch_is_defined_as_in_the_original(string name, int value)
	{
		Assert.Equal(value != 0, PortDefinedSymbols().Contains(name));
	}

	// all_head.cs keeps the switches as constants too, for the code that uses them as values (`if( CONN_DBG==0 && ...)`).
	// They have the original's values.
	[Theory]
	[MemberData(nameof(Switches))]
	public void A_switch_constant_has_the_original_value(string name, int value)
	{
		var field = typeof(all_head).GetField(name, BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(value, Convert.ToInt32(field.GetValue(null)));
	}
}
