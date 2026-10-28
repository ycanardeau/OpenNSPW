using System.IO.Enumeration;

namespace OpenNspw.Desktop;

// The files of one of the original games, to read only: bundled in the app (BundledOriginal), or in a directory given
// to it (OriginalDirectory). Paths are relative to the game's directory, with '/' separators, and match without case, as
// on Windows.
internal interface IOriginalFiles
{
	bool Contains(string path);

	// The file, to read, or null where there is none.
	Stream? OpenRead(string path);

	// The names of the files in a directory ("" for the game's directory) that match a pattern such as "*.dat".
	IReadOnlyList<string> Find(string directory, string pattern);
}

// The files of an original game in a directory, such as Original/NSPW_NET of this repository.
internal sealed class OriginalDirectory(string path) : IOriginalFiles
{
	private readonly string _path = path;

	public bool Contains(string path) => File.Exists(GameFiles.Resolve(_path, path));

	public Stream? OpenRead(string path)
	{
		var full = GameFiles.Resolve(_path, path);
		return File.Exists(full)
			? new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read)
			: null;
	}

	public IReadOnlyList<string> Find(string directory, string pattern) =>
		GameFiles.Find(_path, directory, pattern);
}

// The files of an original game that the app reads, embedded in it from Original (OpenNspw.Desktop.csproj): NSPW NET's
// data, and NSPW 1.22 with the files of its directory that a single-player game reads.
internal sealed class BundledOriginal(string name) : IOriginalFiles
{
	// The names of the resources under the prefix, by their paths after it.
	private static Dictionary<string, string> Resources(string prefix) =>
		typeof(BundledOriginal)
			.Assembly.GetManifestResourceNames()
			.Where(r => r.StartsWith(prefix, StringComparison.Ordinal))
			.ToDictionary(r => r[prefix.Length..], StringComparer.OrdinalIgnoreCase);

	private static string Normalize(string path) =>
		string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries));

	private readonly Dictionary<string, string> _resources = Resources($"Original/{name}/");

	public static BundledOriginal NspwNet { get; } = new("NSPW_NET");

	public static BundledOriginal Nspw122 { get; } = new("Nspw122");

	public bool Contains(string path) => _resources.ContainsKey(Normalize(path));

	public Stream? OpenRead(string path) =>
		_resources.TryGetValue(Normalize(path), out var resource)
			? typeof(BundledOriginal).Assembly.GetManifestResourceStream(resource)
			: null;

	public IReadOnlyList<string> Find(string directory, string pattern)
	{
		var prefix = Normalize(directory) is { Length: > 0 } normalized ? normalized + '/' : "";
		return
		[
			.. _resources
				.Keys.Where(p =>
					p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
					&& p.IndexOf('/', prefix.Length) < 0
					&& FileSystemName.MatchesWin32Expression(
						pattern,
						p.AsSpan(prefix.Length),
						ignoreCase: true
					)
				)
				.Select(p => p[prefix.Length..])
				.Order(StringComparer.OrdinalIgnoreCase),
		];
	}
}

// A file of an original game, opened to write: a copy in memory, saved to the path in the player's folder when it is
// closed, if the game wrote to it. The game opens files it only reads, such as the maps, to write too (OPEN_ALWAYS).
internal sealed class CopyOnWriteStream : MemoryStream
{
	private readonly string _path;
	private bool _isWritten;

	public CopyOnWriteStream(Stream original, string path)
	{
		_path = path;
		original.CopyTo(this);
		Position = 0;
		// Copying the original was not the game's writing.
		_isWritten = false;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		_isWritten = true;
		base.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		_isWritten = true;
		base.Write(buffer);
	}

	public override void WriteByte(byte value)
	{
		_isWritten = true;
		base.WriteByte(value);
	}

	public override void SetLength(long value)
	{
		_isWritten = true;
		base.SetLength(value);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && _isWritten)
		{
			_isWritten = false;
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
				File.WriteAllBytes(_path, ToArray());
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				Console.Error.WriteLine($"{_path} cannot be written: {e.Message}");
			}
		}

		base.Dispose(disposing);
	}
}
