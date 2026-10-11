using System.IO.Compression;
using System.Reflection;
using System.Text;
using Xunit.Sdk;

namespace OpenNspw.Tests.Lockstep;

// What the games of a run did, round by round (see docs/Refactoring.md, Characterization traces). After each round,
// one line per game:
//
//   r <round> <game> f=<frames> s=<state> o=<output> rand=<rand() state>
//
// where <state> is the hash of all the globals of Layout.json, and <output> the hash of what the game output in the
// round. Every CheckpointInterval rounds, and after the last, one line per game and global, so that a difference can
// be traced to a global:
//
//   c <round> <game> <global> <hash>
internal sealed class TraceRecorder(IReadOnlyList<LockstepGame> games)
{
	public const int CheckpointInterval = 50;

	private static readonly FieldInfo HoldRand = typeof(Nspw).GetField("_holdrand", BindingFlags.NonPublic | BindingFlags.Instance)
		?? throw new InvalidOperationException("Nspw has no _holdrand.");

	private readonly IReadOnlyList<LockstepGame> _games = games;
	private readonly List<string> _lines = [];

	public IReadOnlyList<string> Lines => _lines;

	private static ulong[] GlobalHashes(Nspw game)
	{
		return [.. Global.All.Select(g => Hash64.Of(g.BytesIn(game)))];
	}

	private void Checkpoint(int round, LockstepGame game, ulong[] hashes)
	{
		for (var i = 0; i < hashes.Length; i++)
		{
			_lines.Add($"c {round} {game.Name} {Global.All[i].Name} {hashes[i]:x16}");
		}
	}

	public void Record(int round)
	{
		foreach (var game in _games)
		{
			var hashes = GlobalHashes(game.Game);
			var state = new Hash64();
			foreach (var hash in hashes)
			{
				state.Add(hash);
			}

			var rand = (uint)HoldRand.GetValue(game.Game)!;
			_lines.Add($"r {round} {game.Name} f={game.FrameCount} s={state.Value:x16} o={game.TakeOutput():x16} rand={rand:x8}{(game.Ended ? " ended" : "")}");
			if (round % CheckpointInterval == 0)
			{
				Checkpoint(round, game, hashes);
			}
		}
	}

	// Ends the trace with a checkpoint, unless the last round was one.
	public void Finish(int round)
	{
		if (round % CheckpointInterval == 0)
		{
			return;
		}

		foreach (var game in _games)
		{
			Checkpoint(round, game, GlobalHashes(game.Game));
		}
	}
}

// The committed traces (Traces/*.trace.gz), and their comparison with a run.
internal static class Traces
{
	// Set to 1 to write the traces of the runs to OpenNspw.Tests/Traces instead of comparing them.
	private const string RecordVariable = "OPENNSPW_RECORD_TRACES";

	private static bool Recording => Environment.GetEnvironmentVariable(RecordVariable) == "1";

	private static string SourceDirectory()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "OpenNspw.Tests.csproj")))
			{
				return directory.FullName;
			}
		}

		throw new InvalidOperationException("OpenNspw.Tests.csproj is not found.");
	}

	private static void Write(string path, IReadOnlyList<string> lines)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		using var file = File.Create(path);
		using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
		using var writer = new StreamWriter(gzip, new UTF8Encoding(false)) { NewLine = "\n" };
		foreach (var line in lines)
		{
			writer.WriteLine(line);
		}
	}

	private static List<string> Read(string path)
	{
		using var file = File.OpenRead(path);
		using var gzip = new GZipStream(file, CompressionMode.Decompress);
		using var reader = new StreamReader(gzip, Encoding.UTF8);
		var lines = new List<string>();
		while (reader.ReadLine() is { } line)
		{
			lines.Add(line);
		}

		return lines;
	}

	private static string Round(string line)
	{
		return line.Split(' ')[1];
	}

	// The first difference between two traces, with the globals that differ at the first checkpoint after it, or null.
	public static string? Difference(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
	{
		var index = 0;
		while (index < expected.Count && index < actual.Count && expected[index] == actual[index])
		{
			index++;
		}

		if (index == expected.Count && index == actual.Count)
		{
			return null;
		}

		var message = new StringBuilder();
		message.AppendLine($"The run differs from the trace at line {index + 1}:");
		message.AppendLine($"  expected: {(index < expected.Count ? expected[index] : "(end)")}");
		message.AppendLine($"  actual:   {(index < actual.Count ? actual[index] : "(end)")}");

		var expectedCheckpoints = expected.Skip(index).Where(l => l.StartsWith("c ", StringComparison.Ordinal)).ToList();
		var actualCheckpoints = actual.Skip(index).Where(l => l.StartsWith("c ", StringComparison.Ordinal)).ToHashSet();
		if (expectedCheckpoints.Count > 0)
		{
			var round = Round(expectedCheckpoints[0]);
			var globals = expectedCheckpoints.Where(l => Round(l) == round && !actualCheckpoints.Contains(l)).Select(l => string.Join(' ', l.Split(' ')[2..4])).ToList();
			message.AppendLine($"Globals that differ at the checkpoint of round {round}: {(globals.Count > 0 ? string.Join(", ", globals) : "none")}");
		}

		return message.ToString();
	}

	// Compares a run's trace with the committed one, or writes it when recording. A run that failed is compared too,
	// because a difference from the trace explains the failure better than the failure itself.
	public static void Check(string name, IReadOnlyList<string> actual, Exception? failure)
	{
		var fileName = $"{name}.trace.gz";
		if (Recording)
		{
			if (failure is not null)
			{
				throw new InvalidOperationException($"The run of {name} failed, so its trace is not recorded.", failure);
			}

			Write(Path.Combine(SourceDirectory(), "Traces", fileName), actual);
			return;
		}

		var path = Path.Combine(AppContext.BaseDirectory, "Traces", fileName);
		if (!File.Exists(path))
		{
			throw new XunitException($"No trace {fileName}. Record it with {RecordVariable}=1.");
		}

		if (Difference(Read(path), actual) is { } difference)
		{
			throw new XunitException(failure is null ? difference : $"{difference}The run also failed: {failure}");
		}

		if (failure is not null)
		{
			throw new InvalidOperationException($"The run of {name} failed, but matches its trace as far as it got.", failure);
		}
	}
}
