using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNspw.Desktop;

// What the UI remembers between runs, beside the game's own settings (DesktopPlatform): the language chosen, and the
// Bluetooth device joined last.
internal static partial class UISettings
{
	private sealed record Saved(Language? Language, string? BluetoothHost = null);

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

	private static void Save(Func<Saved, Saved> change)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
			File.WriteAllText(
				FilePath,
				JsonSerializer.Serialize(change(Load()), SavedJson.Default.Saved)
			);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			// What was to be saved is still used for this run.
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
		Save(saved => saved with { Language = language });
	}

	// The address of the Bluetooth device joined last, as typed.
	public static string LoadBluetoothHost()
	{
		return Load().BluetoothHost ?? "";
	}

	public static void SaveBluetoothHost(string address)
	{
		Save(saved => saved with { BluetoothHost = address });
	}
}
