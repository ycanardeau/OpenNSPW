using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace OpenNspw.Porter;

// A C# project of the solution, compiled without MSBuild: its .cs files, with the implicit usings, the <Using> items
// and the conditional compilation symbols of its project file.
internal sealed partial class CSharpProject(string directory)
{
	private static readonly string[] ImplicitUsings =
	[
		"System", "System.IO", "System.Linq", "System.Collections.Generic", "System.Threading", "System.Threading.Tasks",
	];

	// The assemblies of the runtime, which stand in for the framework's reference assemblies.
	public static ImmutableArray<MetadataReference> FrameworkReferences { get; } =
	[
		.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p)),
	];

	public string Directory { get; } = directory;

	private string ProjectFile => System.IO.Directory.GetFiles(Directory, "*.csproj").Single();

	public string Name => Path.GetFileNameWithoutExtension(ProjectFile);

	[GeneratedRegex(@"<Using Include=""([^""]+)""( Static=""true"")?( Alias=""([^""]+)"")? />")]
	private static partial Regex UsingItem();

	[GeneratedRegex(@"<DefineConstants>([^<]*)</DefineConstants>")]
	private static partial Regex DefineConstants();

	private static bool IsBuildOutput(string path)
	{
		return path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}");
	}

	private string GlobalUsings()
	{
		var project = File.ReadAllText(ProjectFile);
		var lines = ImplicitUsings.Select(u => $"global using {u};").ToList();
		foreach (Match m in UsingItem().Matches(project))
		{
			lines.Add(m.Groups[4].Success
				? $"global using {m.Groups[4].Value} = {m.Groups[1].Value};"
				: m.Groups[2].Success ? $"global using static {m.Groups[1].Value};" : $"global using {m.Groups[1].Value};");
		}

		return string.Join('\n', lines);
	}

	private IEnumerable<string> Symbols()
	{
		var defines = DefineConstants().Match(File.ReadAllText(ProjectFile)).Groups[1].Value;
		return defines.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0 && !s.StartsWith('$'));
	}

	public CSharpParseOptions ParseOptions()
	{
		return new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: Symbols());
	}

	// The project's .cs files and their text.
	public Dictionary<string, string> ReadFiles()
	{
		return System.IO.Directory.GetFiles(Directory, "*.cs", SearchOption.AllDirectories)
			.Where(f => !IsBuildOutput(f))
			.ToDictionary(f => f, File.ReadAllText);
	}

	// The assemblies that the build copied to the project's output directory, except the named ones: the references of
	// a project that uses packages.
	public IEnumerable<MetadataReference> OutputReferences(IReadOnlySet<string> except)
	{
		var output = System.IO.Directory.GetDirectories(Path.Combine(Directory, "bin"), "net*", SearchOption.AllDirectories).FirstOrDefault()
			?? throw new InvalidOperationException($"{Name} is not built.");
		return System.IO.Directory.GetFiles(output, "*.dll")
			.Where(f => !except.Contains(Path.GetFileNameWithoutExtension(f)))
			.Select(f => MetadataReference.CreateFromFile(f));
	}

	public CSharpCompilation Compile(IReadOnlyDictionary<string, string> files, IEnumerable<MetadataReference> references)
	{
		var options = ParseOptions();
		var trees = files.Select(f => CSharpSyntaxTree.ParseText(f.Value, options, f.Key)).ToList();
		trees.Add(CSharpSyntaxTree.ParseText(GlobalUsings(), options, "GlobalUsings.cs"));
		return CSharpCompilation.Create(
			Name,
			trees,
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Annotations));
	}
}
