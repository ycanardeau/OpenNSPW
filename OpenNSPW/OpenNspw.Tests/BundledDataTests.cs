using System.Text;
using OpenNspw.Desktop;
using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.Tests.Lockstep.BattleScripts;

namespace OpenNspw.Tests;

// The game's files in layers (GameFileLayers), and the original's files that the desktop app embeds (BundledFiles,
// docs/BundledData.md).
public class BundledDataTests
{
	// A layer of files in memory, by their paths.
	private sealed class MemoryFiles(params (string Path, string Text)[] files) : IGameFileSource
	{
		private readonly (string Path, string Text)[] _files = files;

		public Stream? Open(string path, FileShare share)
		{
			return
				_files.FirstOrDefault(f =>
					string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase)
				)
					is { Text: { } text }
				? new MemoryStream(Encoding.UTF8.GetBytes(text), writable: false)
				: null;
		}

		public IReadOnlyList<string> Find(string directory, string pattern)
		{
			return
			[
				.. _files
					.Select(f => f.Path)
					.Where(p => p.StartsWith(directory + "/", StringComparison.OrdinalIgnoreCase))
					.Select(p => p[(directory.Length + 1)..]),
			];
		}
	}

	private static string ReadAll(Stream stream)
	{
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	// The files that the game reads in 1.22's directory, by their names in NSPW NET's: the maps that 1.22's code names
	// (Map\South_pacific.dat, Map\Middle_pacific.dat, Map\japan_off.dat) and the sounds.
	private static IEnumerable<string> ReadBy122(string original) =>
		[
			"Map/South_pacific.dat",
			"Map/Middle_pacific.dat",
			"Map/Japan_off.dat",
			.. Directory
				.GetFiles(Path.Combine(original, "NSPW_NET", "wav"))
				.Select(f => $"wav/{Path.GetFileName(f)}"),
		];

	[Fact]
	public void A_file_is_read_from_the_first_layer_that_has_it()
	{
		using var directory = GameData.CreateEmpty();
		File.WriteAllText(Path.Combine(directory.Path, "Map.dat"), "directory");
		var layers = new GameFileLayers(
			[new MemoryFiles(("wav/a.wav", "before"))],
			directory.Path,
			[new MemoryFiles(("wav/a.wav", "after"), ("map.dat", "after"), ("wav/b.wav", "after"))]
		);

		Assert.Equal(
			"before",
			ReadAll(layers.Open("WAV/A.wav", FileMode.Open, FileAccess.Read, FileShare.Read)!)
		);
		Assert.Equal(
			"directory",
			ReadAll(layers.Open("map.dat", FileMode.Open, FileAccess.Read, FileShare.Read)!)
		);
		Assert.Equal(
			"after",
			ReadAll(layers.Open("wav/b.wav", FileMode.Open, FileAccess.Read, FileShare.Read)!)
		);
		Assert.Null(layers.Open("wav/c.wav", FileMode.Open, FileAccess.Read, FileShare.Read));
	}

	// The original opens the maps it only reads to read and write (load_it2): they are not copied.
	[Fact]
	public void A_file_is_copied_when_it_is_first_written_not_when_it_is_opened_to_write()
	{
		using var directory = GameData.CreateEmpty();
		var layers = new GameFileLayers(
			[],
			directory.Path,
			[new MemoryFiles(("Scenario/a.dat", "bundled"))]
		);
		var copy = Path.Combine(directory.Path, "Scenario", "a.dat");

		using (
			var read = layers.Open(
				"Scenario/a.dat",
				FileMode.OpenOrCreate,
				FileAccess.ReadWrite,
				FileShare.Read
			)!
		)
		{
			Assert.Equal('b', read.ReadByte());
		}

		Assert.False(File.Exists(copy));

		using (
			var written = layers.Open(
				"Scenario/a.dat",
				FileMode.OpenOrCreate,
				FileAccess.ReadWrite,
				FileShare.Read
			)!
		)
		{
			written.Position = 3;
			written.Write("NDLE"u8);
			Assert.True(File.Exists(copy));
			Assert.Equal(7, written.Position);
		}

		Assert.Equal("bunNDLE", File.ReadAllText(copy));
		Assert.Equal(
			"bunNDLE",
			ReadAll(layers.Open("Scenario/a.dat", FileMode.Open, FileAccess.Read, FileShare.Read)!)
		);
	}

	// OPEN_ALWAYS creates a file that no layer has, as on Windows; OPEN_EXISTING does not.
	[Fact]
	public void A_file_that_no_layer_has_is_created_in_the_directory()
	{
		using var directory = GameData.CreateEmpty();
		var layers = new GameFileLayers([], directory.Path, [new MemoryFiles()]);

		Assert.Null(
			layers.Open("Saved/auto_save.dat", FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
		);
		using (
			layers.Open(
				"Saved/resume_1.dat",
				FileMode.OpenOrCreate,
				FileAccess.ReadWrite,
				FileShare.Read
			)
		) { }

		Assert.True(File.Exists(Path.Combine(directory.Path, "Saved", "resume_1.dat")));
	}

	[Fact]
	public void A_directory_lists_the_names_of_every_layer_once()
	{
		using var directory = GameData.CreateEmpty();
		Directory.CreateDirectory(Path.Combine(directory.Path, "Scenario"));
		File.WriteAllText(Path.Combine(directory.Path, "Scenario", "TST3.dat"), "");
		File.WriteAllText(Path.Combine(directory.Path, "Scenario", "mine.dat"), "");
		var layers = new GameFileLayers(
			[],
			directory.Path,
			[
				new MemoryFiles(
					("Scenario/tst3.dat", ""),
					("Scenario/てすと.dat", ""),
					("Scenario/てすと.dat".Normalize(NormalizationForm.FormD), "")
				),
			]
		);

		Assert.Equal(["mine.dat", "TST3.dat", "てすと.dat"], layers.Find("Scenario", "*.dat"));
	}

	[Fact]
	public void Bundled_files_are_found_in_any_case_and_either_Unicode_form()
	{
		var files = new BundledFiles("NSPW_NET");

		using var decomposed = files.Open(
			"scenario/てすと.DAT".Normalize(NormalizationForm.FormD),
			FileShare.Read
		);

		Assert.NotNull(decomposed);
		Assert.Contains("てすと.dat", files.Find("Scenario", "*.dat"));
	}

	[Fact]
	public void The_bundled_files_are_the_originals()
	{
		var original = GameData.FindOriginalDirectory();
		foreach (var folder in new[] { "NSPW_NET", "Nspw122" })
		{
			var files = new BundledFiles(folder);
			Assert.NotEmpty(files.Paths);
			foreach (var path in files.Paths)
			{
				using var bundled = files.Open(path, FileShare.Read)!;
				using var bytes = new MemoryStream();
				bundled.CopyTo(bytes);
				Assert.True(
					bytes
						.ToArray()
						.AsSpan()
						.SequenceEqual(File.ReadAllBytes(Path.Combine(original, folder, path))),
					$"{folder}/{path} differs from the original's."
				);
			}
		}
	}

	// A single-player game reads NSPW NET's files where 1.22's are not embedded: those it reads must be the same.
	[Fact]
	public void The_files_of_122_that_are_not_bundled_are_NSPW_NETs()
	{
		var original = GameData.FindOriginalDirectory();
		var nspw122 = new BundledFiles("Nspw122");
		var nspwNet = new DirectoryFileSource(Path.Combine(original, "NSPW_NET"));
		var directory = new DirectoryFileSource(Path.Combine(original, "Nspw122"));
		foreach (var path in ReadBy122(original))
		{
			using var bundled = nspw122.Open(path, FileShare.Read);
			using var own = directory.Open(path, FileShare.Read);
			if (bundled is not null || own is null)
				continue;

			using var net = nspwNet.Open(path, FileShare.Read)!;
			using var ownBytes = new MemoryStream();
			using var netBytes = new MemoryStream();
			own.CopyTo(ownBytes);
			net.CopyTo(netBytes);
			Assert.True(
				ownBytes.ToArray().AsSpan().SequenceEqual(netBytes.ToArray()),
				$"1.22's {path} differs from NSPW NET's."
			);
		}
	}

	[Fact]
	public void The_bundled_executable_is_122s()
	{
		Assert.Equal(
			File.ReadAllBytes(Path.Combine(GameData.FindSingleplayer(), "NSPW_122.exe")),
			BundledFiles.Read("Nspw122/NSPW_122.exe")
		);
	}

	// The committed trace of the title screen, which hashes every frame, text and sound, is the same from the bundled
	// files as from the original's data.
	[Fact]
	public void A_network_game_plays_the_bundled_files_as_the_originals()
	{
		using var run = new LockstepRun(playsBundledFiles: true);
		Connect(run);
		run.Frames(run.Host, 100);

		Traces.Check("title", run.FinishTrace(), null);
	}

	[Fact]
	public void A_single_player_game_plays_the_bundled_files_as_the_originals()
	{
		using var bundled = new SingleplayerRun(202, playsBundledFiles: true);
		using var original = new SingleplayerRun(202, drawsFromSpriteFiles: true);
		for (var i = 0; i < 8; i++)
		{
			bundled.Frames(25);
			original.Frames(25);
			Assert.Equal(original.Player.TakeOutput(), bundled.Player.TakeOutput());
		}
	}
}
