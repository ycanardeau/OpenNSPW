using System.IO.Enumeration;
using System.Text;

namespace OpenNspw.Desktop;

// Files of the original that the game reads, such as NSPW NET's data or NSPW 1.22's directory: read only, by their path
// with '/' separators, with their names matched without case, as on Windows (GameFiles).
internal interface IOriginalFiles
{
	// The file, opened to read, or null where there is none.
	Stream? Open(string path);

	// The names of the files in a directory ("" for the top) that match a pattern such as "*.dat".
	IReadOnlyList<string> Find(string directory, string pattern);
}

// The files in a directory on disk, given with --data or --exe (Program.cs).
internal sealed class DirectoryFiles(string directory) : IOriginalFiles
{
	private readonly string _directory = directory;

	public Stream? Open(string path)
	{
		var full = GameFiles.Resolve(_directory, path);
		return File.Exists(full)
			? new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read)
			: null;
	}

	public IReadOnlyList<string> Find(string directory, string pattern)
	{
		return GameFiles.Find(_directory, directory, pattern);
	}
}

// The files of a folder of Original, such as "NSPW_NET", embedded in the app (OpenNspw.Desktop.csproj), whose author
// lets them be redistributed (Original/README.md).
internal sealed class EmbeddedFiles(string folder) : IOriginalFiles
{
	private static readonly System.Reflection.Assembly Assembly = typeof(EmbeddedFiles).Assembly;

	private readonly string _prefix = folder + "/";

	// The paths of the files under the folder, such as "wav/click1.wav".
	private readonly string[] _paths =
	[
		.. Assembly
			.GetManifestResourceNames()
			.Where(name => name.StartsWith(folder + "/", StringComparison.Ordinal))
			.Select(name => name[(folder.Length + 1)..]),
	];

	// The path of the embedded file at a path, in any case and either Unicode normalization, or null.
	private string? PathOf(string path)
	{
		var wanted = string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries))
			.Normalize(NormalizationForm.FormC);
		return _paths.FirstOrDefault(p =>
			string.Equals(
				p.Normalize(NormalizationForm.FormC),
				wanted,
				StringComparison.OrdinalIgnoreCase
			)
		);
	}

	public Stream? Open(string path)
	{
		return PathOf(path) is { } found
			? Assembly.GetManifestResourceStream(_prefix + found)
			: null;
	}

	public IReadOnlyList<string> Find(string directory, string pattern)
	{
		var top = string.Join('/', directory.Split('/', StringSplitOptions.RemoveEmptyEntries));
		return
		[
			.. _paths
				.Where(p =>
					string.Equals(
						p.LastIndexOf('/') is var slash and >= 0 ? p[..slash] : "",
						top,
						StringComparison.OrdinalIgnoreCase
					)
				)
				.Select(p => p[(p.LastIndexOf('/') + 1)..])
				.Where(name => FileSystemName.MatchesSimpleExpression(pattern, name))
				.Order(StringComparer.OrdinalIgnoreCase),
		];
	}
}

// Files of the original, with their sprite sheet put together from the sprites (SpriteSheet) instead of read: the app
// ships no sheet.
internal sealed class WithSpriteSheet(IOriginalFiles files, string sheetName, Func<byte[]> sheet)
	: IOriginalFiles
{
	private readonly IOriginalFiles _files = files;
	private readonly string _sheetName = sheetName;
	private readonly Func<byte[]> _sheet = sheet;

	public Stream? Open(string path)
	{
		return string.Equals(path.Trim('/'), _sheetName, StringComparison.OrdinalIgnoreCase)
			? new MemoryStream(_sheet(), writable: false)
			: _files.Open(path);
	}

	public IReadOnlyList<string> Find(string directory, string pattern)
	{
		return
			directory.Trim('/') == "" && FileSystemName.MatchesSimpleExpression(pattern, _sheetName)
			?
			[
				.. _files
					.Find(directory, pattern)
					.Append(_sheetName)
					.Order(StringComparer.OrdinalIgnoreCase),
			]
			: _files.Find(directory, pattern);
	}
}

// A file of the original that the game opens to write: its bytes in memory, written to a file in the user's folder when
// it is closed, if the game wrote to it. The game changes that copy, and reads it from then on, never the original's.
internal sealed class CopyOnWriteStream : MemoryStream
{
	private readonly string _path;
	private bool _isWritten;

	// original: the original's file, which it copies and closes. path: the copy's path in the user's folder.
	public CopyOnWriteStream(Stream original, string path)
	{
		using (original)
		{
			original.CopyTo(this);
		}

		Position = 0;
		_isWritten = false;
		_path = path;
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
