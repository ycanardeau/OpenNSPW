using System.Text.RegularExpressions;
using OpenNspw.Porter;

// Ports C++ files of the original to the C# port: see OpenNSPW/README.md.
//
//   OpenNspw.Porter port <port project directory> <C++ file>...
//       Converts each C++ file to <name>.cs in the project (stage 1), then fixes the errors with a mechanical answer
//       (stage 2), and lists the errors left to port by hand.
//   OpenNspw.Porter fix <port project directory> <C# file>...
//       Runs stage 2 only, on C# files of the project.
//   OpenNspw.Porter rename <port project directory> <renames file> <user project directory>...
//       Renames members of the port and their uses in it and in the projects that use it (see Renamer), in the
//       order given, each after the projects it uses. The projects must be built.
//   OpenNspw.Porter split-fields <project directory>
//       Puts each field of the project on a line of its own (see FieldSplitter).
//   OpenNspw.Porter ref-locals <project directory> [<method>,<method>...]
//       Gives the element of a table that a loop works on a ref local, and the unit that the int parameter of the named
//       methods numbers (see RefLocals).

if (args.Length >= 3 && args[0] == "rename")
{
	var renamer = new Renamer(new CSharpProject(args[1]), [.. args[3..].Select(d => new CSharpProject(d))], Renamer.ReadRenames(args[2]));
	foreach (var path in renamer.Run())
	{
		Console.WriteLine(path);
	}

	return 0;
}

if (args.Length is 2 or 3 && args[0] == "ref-locals")
{
	var methods = args.Length == 3 ? args[2].Split(',').ToHashSet() : [];
	foreach (var path in new RefLocals(new CSharpProject(args[1]), methods).Run())
	{
		Console.WriteLine(path);
	}

	return 0;
}

if (args.Length == 2 && args[0] == "split-fields")
{
	foreach (var path in new FieldSplitter(new CSharpProject(args[1])).Run())
	{
		Console.WriteLine(path);
	}

	return 0;
}

if (args.Length < 3 || args[0] is not ("port" or "fix"))
{
	Console.Error.WriteLine("Usage: OpenNspw.Porter port|fix <port project directory> <file>...");
	return 1;
}

var projectDirectory = args[1];
var files = args[2..];

// The constants that array lengths can use.
Dictionary<string, long> ReadConstants()
{
	var constants = new Dictionary<string, long>();
	foreach (var file in new[] { "all_head.cs", "windef.cs", "resource.cs" })
	{
		var path = Path.Combine(projectDirectory, file);
		if (!File.Exists(path))
		{
			continue;
		}

		foreach (Match m in Regex.Matches(File.ReadAllText(path), @"const\s+(?:int|uint)\s+(\w+)\s*=\s*([^;]+);"))
		{
			var expression = Regex.Replace(m.Groups[2].Value, @"\(\s*0x01\s*<<\s*(\d+)\s*\)", x => (1L << int.Parse(x.Groups[1].Value)).ToString());
			if (Regex.IsMatch(expression, @"^[\d\s+\-*/()]+$"))
			{
				constants[m.Groups[1].Value] = (long)System.Convert.ToDouble(new System.Data.DataTable().Compute(expression, ""));
			}
		}
	}

	return constants;
}

// Adds the array lengths the ported files use to InlineArrays.cs.
void UpdateInlineArrays(IEnumerable<long> lengths)
{
	var path = Path.Combine(projectDirectory, "InlineArrays.cs");
	var text = File.ReadAllText(path);
	var existing = Regex.Matches(text, @"\[InlineArray\((\d+)\)\]").Select(m => long.Parse(m.Groups[1].Value)).ToHashSet();
	foreach (var length in lengths.Where(l => !existing.Contains(l)))
	{
		text = text.TrimEnd() + $$"""


			[InlineArray({{length}})]
			public struct Array{{length}}<T> : IInlineArray
			{
				private T _element0;

				public readonly byte[] ToBytes()
				{
					return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * {{length}}).ToArray();
				}
			}
			""".Replace("\n\t\t\t", "\n").Replace("\r", "") + "\n";
	}

	File.WriteAllText(path, text);
}

var targets = new HashSet<string>();
if (args[0] == "port")
{
	var converter = new CppToCs(ReadConstants());
	foreach (var file in files)
	{
		var name = Path.GetFileNameWithoutExtension(file) + ".cs";
		File.WriteAllText(Path.Combine(projectDirectory, name), converter.Port(File.ReadAllText(file), Path.GetFileName(file)));
		targets.Add(name);
	}

	UpdateInlineArrays(converter.ArrayLengths);
}
else
{
	targets.UnionWith(files.Select(Path.GetFileName)!);
}

var errors = new Fixer(projectDirectory, targets).Run();
var left = errors.Where(e => targets.Contains(Path.GetFileName(e.Location.SourceTree?.FilePath ?? ""))).ToList();
foreach (var error in left)
{
	var position = error.Location.GetLineSpan();
	Console.WriteLine($"{Path.GetFileName(position.Path)}({position.StartLinePosition.Line + 1}): {error.Id} {error.GetMessage()}");
}

Console.WriteLine($"{left.Count} errors left to port by hand.");
return 0;
