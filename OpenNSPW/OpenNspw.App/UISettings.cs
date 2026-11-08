using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNspw.App;

// What the UI remembers between runs, beside the game's own settings (AppPlatform): the language chosen, and the
// options of single-player battles (docs/Options.md).
internal static partial class UISettings
{
	// IsSpeedSteady: BattleOptions.SteadySpeed, by the name it was first saved under.
	private sealed record Saved(
		Language? Language,
		bool? IsSpeedSteady = null,
		bool? SharedEffectRandom = null,
		float? BattleZoom = null,
		bool? IsCommandBarShown = null,
		TouchOptions? Touch = null
	);

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
			File.WriteAllText(
				FilePath,
				JsonSerializer.Serialize(change(Load()), SavedJson.Default.Saved)
			);
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

	// The options of single-player battles, as chosen last in a Debug build: off unless chosen otherwise.
	public static BattleOptions LoadOptions()
	{
		var saved = Load();
		return new BattleOptions(
			SharedEffectRandom: saved.SharedEffectRandom ?? false,
			SteadySpeed: saved.IsSpeedSteady ?? false
		);
	}

	public static void SaveOptions(BattleOptions options)
	{
		Save(saved =>
			saved with
			{
				IsSpeedSteady = options.SteadySpeed,
				SharedEffectRandom = options.SharedEffectRandom,
			}
		);
	}

	// How much the battlefield was magnified last, or `initial` if the player never zoomed it.
	public static float LoadBattleZoom(float initial)
	{
		return Load().BattleZoom ?? initial;
	}

	public static void SaveBattleZoom(float zoom)
	{
		Save(saved => saved with { BattleZoom = zoom });
	}

	// Whether the battle's command bar was shown last, or `initial` if the player never showed or hid it.
	public static bool LoadCommandBar(bool initial)
	{
		return Load().IsCommandBarShown ?? initial;
	}

	public static void SaveCommandBar(bool isShown)
	{
		Save(saved => saved with { IsCommandBarShown = isShown });
	}

	// How a touch screen is played, as chosen last, or the default.
	public static TouchOptions LoadTouch()
	{
		return Load().Touch ?? new TouchOptions();
	}

	public static void SaveTouch(TouchOptions options)
	{
		Save(saved => saved with { Touch = options });
	}
}
