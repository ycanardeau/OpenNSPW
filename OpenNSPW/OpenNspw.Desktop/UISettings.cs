using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNspw.Desktop;

// What the UI remembers between runs, beside the game's own settings (DesktopPlatform): the language chosen, and whether
// single-player battles play at a steady speed (docs/SteadySpeed.md).
internal static partial class UISettings
{
	private sealed record Saved(Language? Language, bool? IsSpeedSteady = null);

	// How Saved is read and written, generated, so that trimming keeps what it needs: the language by its name.
	[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
	[JsonSerializable(typeof(Saved))]
	private sealed partial class SavedJson : JsonSerializerContext;

	private static readonly string FilePath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"OpenNSPW",
		"ui.json"
	);

	private static Saved Load()
	{
		try
		{
			return JsonSerializer.Deserialize(File.ReadAllText(FilePath), SavedJson.Default.Saved)
				?? new(null);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
		{
			return new(null);
		}
	}

	// Writes what is saved, changed by `change`. What cannot be written is still used for this run.
	private static void Save(Func<Saved, Saved> change)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
			File.WriteAllText(FilePath, JsonSerializer.Serialize(change(Load()), SavedJson.Default.Saved));
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
	}

	// The language chosen last, or the system's: Japanese on a Japanese system, English on any other.
	public static Language LoadLanguage()
	{
		return Load().Language
			?? (
				CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja"
					? Language.Japanese
					: Language.English
			);
	}

	public static void SaveLanguage(Language language)
	{
		Save(saved => saved with { Language = language });
	}

	// Whether single-player battles play at a steady speed, as chosen last: they do, unless the player chose otherwise.
	public static bool LoadIsSpeedSteady()
	{
		return Load().IsSpeedSteady ?? true;
	}

	public static void SaveIsSpeedSteady(bool isSpeedSteady)
	{
		Save(saved => saved with { IsSpeedSteady = isSpeedSteady });
	}
}
