using System.Collections.Immutable;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;

namespace OpenNspw.Tests;

// Bytes of a global, as the reference writes them: [global, offset, base64].
internal sealed record Run(string Global, int Offset, byte[] Bytes)
{
	public static Run Parse(JsonElement run)
	{
		return new Run(run[0].GetString()!, run[1].GetInt32(), Convert.FromBase64String(run[2].GetString()!));
	}

	public static ImmutableArray<Run> ParseAll(JsonElement runs)
	{
		return [.. runs.EnumerateArray().Select(Parse)];
	}
}

internal sealed record ReferenceField(int Offset, int Size);

internal sealed record ReferenceStruct(int Size, ImmutableDictionary<string, ReferenceField> Fields);

internal sealed record ReferenceGlobal(string Name, int Size);

// Layout.json: the layout of the structs and globals in the reference, and the initial values of the globals.
internal sealed record ReferenceLayout(
	ImmutableDictionary<string, ReferenceStruct> Structs,
	ImmutableArray<ReferenceGlobal> Globals,
	ImmutableArray<Run> Initial)
{
	private static ReferenceLayout Load()
	{
		using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Layout.json")));
		var root = document.RootElement;
		return new ReferenceLayout(
			root.GetProperty("structs").EnumerateObject().ToImmutableDictionary(
				s => s.Name,
				s => new ReferenceStruct(
					s.Value.GetProperty("size").GetInt32(),
					s.Value.GetProperty("fields").EnumerateObject().ToImmutableDictionary(
						f => f.Name,
						f => new ReferenceField(f.Value[0].GetInt32(), f.Value[1].GetInt32())))),
			[.. root.GetProperty("globals").EnumerateObject().Select(g => new ReferenceGlobal(g.Name, g.Value.GetInt32()))],
			Run.ParseAll(root.GetProperty("initial")));
	}

	public static ReferenceLayout Instance { get; } = Load();
}

// One recorded call of a function. See OpenNspw.Reference/function_tests.cpp.
internal sealed record FunctionCase(
	int Index,
	uint Seed,
	ImmutableArray<JsonElement> Args,
	ImmutableArray<Run> Before,
	JsonElement? Return,
	ImmutableArray<JsonElement> Outs,
	ImmutableArray<Run> After,
	ImmutableArray<int> Rand)
{
	public static double ParseDouble(JsonElement value)
	{
		return BitConverter.Int64BitsToDouble((long)ulong.Parse(value.GetString()!, NumberStyles.AllowHexSpecifier));
	}

	public int IntArg(int index)
	{
		return Args[index].GetInt32();
	}

	public double DoubleArg(int index)
	{
		return ParseDouble(Args[index]);
	}

	private static FunctionCase Parse(int index, JsonElement c)
	{
		return new FunctionCase(
			index,
			c.GetProperty("seed").GetUInt32(),
			[.. c.GetProperty("args").EnumerateArray()],
			Run.ParseAll(c.GetProperty("before")),
			c.TryGetProperty("return", out var ret) ? ret : null,
			c.TryGetProperty("outs", out var outs) ? [.. outs.EnumerateArray()] : [],
			Run.ParseAll(c.GetProperty("after")),
			[.. c.GetProperty("rand").EnumerateArray().Select(r => r.GetInt32())]);
	}

	// The recorded calls of `function` in `file` (a C++ file name without extension, or "math").
	public static IEnumerable<FunctionCase> Load(string file, string function)
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Functions", file, $"{function}.jsonl.gz");
		using var reader = new StreamReader(new GZipStream(File.OpenRead(path), CompressionMode.Decompress));
		var index = 0;
		while (reader.ReadLine() is { } line)
		{
			yield return Parse(index++, JsonSerializer.Deserialize<JsonElement>(line));
		}
	}
}
