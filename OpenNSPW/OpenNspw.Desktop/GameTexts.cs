using System.Text.RegularExpressions;

namespace OpenNspw.Desktop;

// What the game writes in Japanese, as the UI reads it from the snapshot, in the language chosen (Texts.Language): in
// English, in the words of the game's English edition (its LNGG_VER code), their spelling and grammar mended, and the
// Japanese as it is where the edition has none.
internal static partial class GameTexts
{
	private static readonly Dictionary<string, string> English = new()
	{
		// The sides, and the single-player sets' names, as NSPW 1.22's screens name them (ScenarioSelection).
		["日本海軍"] = "Japan Navy",
		["合衆国海軍"] = "U.S. Navy",
		["第一集"] = "Set 1",
		["第二集"] = "Set 2",
		["第三集"] = "Set 3",
		["第四集"] = "Set 4",

		// The units' kinds (WriteUnitKindName, WriteSupplyTargetName).
		["大和級戦艦"] = "Class Yamato",
		["戦艦"] = "Battleship",
		["巡洋艦"] = "Cruiser",
		["防空巡洋艦"] = "AntiAir Cruiser",
		["駆逐艦"] = "Destroyer",
		["対潜駆逐艦"] = "AntiSub Destroyer",
		["潜水艦"] = "Submarine",
		["エセックス型空母"] = "Class Essex",
		["正規空母"] = "Carrier",
		["軽空母"] = "Light Carrier",
		["輸送船(歩兵基地)"] = "Transport (Trenches)",
		["輸送船(トーチカ群)"] = "Transport (Pillboxes)",
		["輸送船(要塞)"] = "Transport (Fortress)",
		["輸送船(航空基地)"] = "Transport (Airfield)",
		["輸送船(軍港)"] = "Transport (Port)",
		["輸送船"] = "Transport",
		["戦闘機"] = "Car. Fighter",
		["陸上戦闘機"] = "Grd. Fighter",
		["攻撃機"] = "Car. Bomber",
		["戦略爆撃機"] = "Bomber",
		["軍港"] = "Military Port",
		["航空基地"] = "Airfield",
		["都市"] = "City",
		["鉱山"] = "Mine",
		["歩兵基地"] = "Trenches",
		["トーチカ群"] = "Pillboxes",
		["要塞"] = "Fortress",

		// Their state (WriteUnitDetails).
		["大破"] = "Heavily damaged",
		["中破"] = "Moderately damaged",
		["小破"] = "Lightly damaged",
		["軽損傷"] = "Slightly damaged",
		["損傷無"] = "No damage",
		["停泊艦船"] = "Anchored",

		// The result (WriteResult).
		["ゲーム終了"] = "Battle is Over.",
		["日本海軍は勝利条件を達成した。"] = "Japan Navy got a victory.",
		["日本海軍の勝利"] = "Japan Navy won.",
		["合衆国海軍は勝利条件を達成した。"] = "U.S. Navy got a victory.",
		["合衆国海軍の勝利"] = "U.S. Navy won.",
		["日本海軍は勝利条件を達成できなかった。"] = "Japan Navy lost a victory.",
		["合衆国海軍は勝利条件を達成できなかった。"] = "U.S. Navy lost a victory.",
		["日米両海軍は勝利目標を達成できなかった。"] = "Both navies could not get a victory.",
		["引き分け。"] = "Draw.",

		// The notices (ReadNotices).
		["ユニットデータ同期異常、ゲームを中断されたし。"] = "Unit data synchronization error! Exit the game.",
		["プログラム同期異常、ゲームを中断されたし。"] = "Program synchronization error! Exit the game.",
		["ランダム同期異常、ゲームを中断されたし。"] = "Random synchronization error! Exit the game.",
		["接続相手がいなくなりました。"] = "The rival has gone.",

		// The multiplayer scenarios (ListScenarioNames).
		["空母機動部隊の戦い１"] = "Battle of Carriers 1",
		["空母機動部隊の戦い２"] = "Battle of Carriers 2",
		["空母機動部隊の戦い３"] = "Battle of Carriers 3",
		["艦隊決戦１"] = "Fleet Battle 1",
		["艦隊決戦２"] = "Fleet Battle 2",
		["ミッドウェイ攻略１"] = "Invasion of Midway 1",
		["ミッドウェイ攻略２"] = "Invasion of Midway 2",
		["中部太平洋の戦い１"] = "Battle of Middle Pacific 1",
		["ユーザーシナリオ"] = "Battles on User Mission",
		["ガダルカナルを巡る戦い１"] = "Battles around Guadalcanal 1",
		["ガダルカナルを巡る戦い２"] = "Battles around Guadalcanal 2",
		["日本近海の戦い１"] = "Battles off Japan 1",
		["南太平洋の戦い１"] = "Battles of South Pacific 1",
		["南太平洋の戦い２"] = "Battles of South Pacific 2",
		["ガ島争奪戦"] = "Scramble for Guadalcanal",
	};

	// What the setting screen says about each multiplayer scenario (DrawScenarioDescription), the victory conditions
	// last.
	private static readonly Dictionary<int, string[]> EnglishDescriptions = new()
	{
		[1] = ["Battle of small carrier fleets.", "Victory Line : Sink the enemy's carrier or destroy its trenches."],
		[2] = ["Battle of middle-sized carrier fleets.", "Victory Line : Sink the enemy's carrier or destroy its trenches."],
		[3] = ["Battle of the biggest carrier fleets.", "Victory Line : Sink the enemy's carrier or destroy its trenches."],
		[4] = ["Battle of combat ships only.", "Victory Line : Sink all the enemy's battleships and cruisers, or destroy its trenches."],
		[5] = ["Battle of combat ships only.", "Victory Line : Sink all the enemy's battleships and cruisers, or destroy its trenches."],
		[6] = ["Invasion of Midway by the Japan Navy.", "Victory Line : Total annihilation of the U.S. bases on Midway."],
		[7] = ["Invasion of Midway by the Japan Navy.", "Victory Line : Total annihilation of the U.S. bases on Midway, and the completion of a Japanese fortress on the island."],
		[8] = ["Battles of the middle Pacific.", "Victory Line : Total annihilation of the U.S. bases on Midway, and the completion of a Japanese fortress on the island. The opposite conditions on Wake Island are the U.S. Navy's victory."],
		[9] = ["Battles on the user's mission.", "Victory Line : It depends on your rule."],
		[101] = ["Battles around Guadalcanal.", "Victory Line : Total annihilation of the Japanese bases on Guadalcanal is the U.S. Navy's victory."],
		[102] = ["Battles around Guadalcanal.", "Victory Line : Total annihilation of the Japanese bases on Guadalcanal, and the completion of the U.S. Navy's airfield, is its victory."],
		[103] = ["Battles off and over Japan.", "Victory Line : Total annihilation of all the Japanese cities on their land is the U.S. Navy's victory."],
		[104] = ["Battle of the South Pacific.", "Victory Line : Total annihilation of all the Japanese bases on Bougainville Island."],
		[105] = ["Battle of the South Pacific.", "Victory Line : Total annihilation of all the Japanese bases on Bougainville Island. The opposite conditions on Guadalcanal, which the U.S. Navy defends, are the Japanese victory."],
		[106] = ["Battles at Guadalcanal.", "Victory Line : Total annihilation of all the enemy's bases, and keeping over 4 bases on Guadalcanal Island."],
	};

	[GeneratedRegex(@"^工事:(-?\d+)$")]
	private static partial Regex BuildTime();

	[GeneratedRegex(@"^弾薬：(-?\d+)$")]
	private static partial Regex Ammunition();

	[GeneratedRegex(@"^燃料：(-?\d+)$")]
	private static partial Regex Fuel();

	[GeneratedRegex(@"^全種増援可\((\d+)\)$")]
	private static partial Regex AllArrive();

	[GeneratedRegex(@"^全種増援不可\((\d+)\)$")]
	private static partial Regex NoneArrive();

	[GeneratedRegex(@"^輸送船以外可\((\d+)\)$")]
	private static partial Regex AllButTransports();

	[GeneratedRegex(@"^輸送船のみ可\((\d+)\)$")]
	private static partial Regex TransportsOnly();

	[GeneratedRegex(@"^戦闘艦船のみ可\((\d+)\)$")]
	private static partial Regex CombatShipsOnly();

	[GeneratedRegex(@"^航空機のみ可\((\d+)\)$")]
	private static partial Regex PlanesOnly();

	[GeneratedRegex(@"^輸送船\(軍港\)以外可\((\d+)\)$")]
	private static partial Regex AllButPortTransports();

	// The texts with numbers: the building time, the ammunition and the fuel (WriteUnitDetails), and the rules of the
	// reinforcements (WriteArrivalControl).
	private static readonly (Regex Japanese, string English)[] EnglishPatterns =
	[
		(BuildTime(), "Building:$1"),
		(Ammunition(), "Ammo:$1"),
		(Fuel(), "Fuel:$1"),
		(AllArrive(), "All possible($1)"),
		(NoneArrive(), "None possible($1)"),
		(AllButTransports(), "All but transports($1)"),
		(TransportsOnly(), "Transports only($1)"),
		(CombatShipsOnly(), "Combat ships only($1)"),
		(PlanesOnly(), "Planes only($1)"),
		(AllButPortTransports(), "All but transports (port)($1)"),
	];

	// A text the game writes, in the language chosen.
	public static string Of(string japanese)
	{
		if (Texts.Language == Language.Japanese || japanese.Length == 0)
			return japanese;

		if (English.TryGetValue(japanese, out var english))
			return english;

		foreach (var (pattern, replacement) in EnglishPatterns)
		{
			if (pattern.IsMatch(japanese))
				return pattern.Replace(japanese, replacement);
		}

		return japanese;
	}

	// The texts the game writes, in the language chosen.
	public static string[] Of(string[] japanese) => [.. japanese.Select(Of)];

	// What the setting screen says about a multiplayer scenario, a line each, in the language chosen; the Japanese lines
	// as the game writes them.
	public static string[] DescriptionOf(int scenario, string[] japanese)
	{
		return Texts.Language == Language.English && EnglishDescriptions.TryGetValue(scenario, out var english) ? english : japanese;
	}
}
