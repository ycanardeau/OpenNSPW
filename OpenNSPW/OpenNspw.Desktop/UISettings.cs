using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNspw.Desktop;

// What the UI remembers between runs, beside the game's own settings (DesktopPlatform): the language chosen.
internal static class UISettings
{
	private sealed record Saved(Language? Language);

	private static readonly string FilePath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"OpenNSPW",
		"ui.json"
	);

	private static readonly JsonSerializerOptions Options = new()
	{
		Converters = { new JsonStringEnumConverter() },
	};

	private static Saved Load()
	{
		try
		{
			return JsonSerializer.Deserialize<Saved>(File.ReadAllText(FilePath), Options)
				?? new(null);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
		{
			return new(null);
		}
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
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
			File.WriteAllText(
				FilePath,
				JsonSerializer.Serialize(Load() with { Language = language }, Options)
			);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			// The language is still used for this run.
		}
	}
}
