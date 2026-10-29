using System.IO.Enumeration;
using System.Text;

namespace OpenNspw.Desktop;

// A folder of Original embedded in the app (OpenNspw.Desktop.csproj, docs/BundledData.md), such as "NSPW_NET", as a layer
// of the game's files that is only read: each file a resource named by its path under Original. Their names are matched
// in any case and in either Unicode form, as macOS may give the build decomposed Japanese names.
internal sealed class BundledFiles(string folder) : IGameFileSource
{
	private static readonly System.Reflection.Assembly Assembly = typeof(BundledFiles).Assembly;

	private readonly string _prefix = folder + "/";

	// The paths of the folder's files, such as "wav/click1.wav".
	private readonly string[] _paths =
	[
		.. Assembly
			.GetManifestResourceNames()
			.Where(name => name.StartsWith(folder + "/", StringComparison.Ordinal))
			.Select(name => name[(folder.Length + 1)..]),
	];

	// A path in the form that the names are compared in.
	private static string Key(string path)
	{
		return string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries))
			.Normalize(NormalizationForm.FormC)
			.ToUpperInvariant();
	}

	// The paths of the folder's files.
	public IReadOnlyList<string> Paths => _paths;

	public Stream? Open(string path, FileShare share)
	{
		var key = Key(path);
		return _paths.FirstOrDefault(p => Key(p) == key) is { } found
			? Assembly.GetManifestResourceStream(_prefix + found)
			: null;
	}

	public IReadOnlyList<string> Find(string directory, string pattern)
	{
		var key = Key(directory);
		return
		[
			.. _paths
				.Where(p => Key(p.LastIndexOf('/') is var slash and >= 0 ? p[..slash] : "") == key)
				.Select(p => p[(p.LastIndexOf('/') + 1)..])
				.Where(name => FileSystemName.MatchesSimpleExpression(pattern, name))
				.Order(StringComparer.OrdinalIgnoreCase),
		];
	}
}
