namespace OpenNspw.Singleplayer;

// The scenarios of NSPW 1.22, in the four collections of its setting screen, with their titles there (the strings of the
// executable from 0x441044 to 0x441474).
public static class ScenarioCatalog
{
	public sealed record Scenario(int Number, string Title);

	public sealed record Collection(string Title, IReadOnlyList<Scenario> Scenarios);

	public static IReadOnlyList<Collection> Collections { get; } =
	[
		new(
			"第一集（標準シナリオ）",
			[
				new(1, "ハワイ沖大演習"),
				new(2, "パールハーバーアタック"),
				new(3, "ウェーク島防衛"),
				new(4, "珊瑚海海戦"),
				new(5, "運命のミッドウェイ"),
				new(6, "サンタクルーズ諸島海戦"),
				new(7, "ネーバルサウスパシフィックウォー"),
				new(8, "トラック島への攻勢"),
				new(9, "マリアナ沖海戦"),
				new(10, "日本上空迎撃戦"),
			]
		),
		new(
			"第二集",
			[
				new(100, "沈黙の艦隊"),
				new(101, "艦隊決戦（日本海軍）"),
				new(102, "艦隊決戦（合衆国海軍）"),
				new(103, "超時空航空戦隊！ディープブルー航空隊"),
				new(104, "ガ島に上陸せよ"),
				new(105, "空母決戦（日本海軍）"),
				new(106, "空母決戦（合衆国海軍）"),
				new(107, "ヤマモトを撃て！"),
				new(108, "大空のサムライ"),
				new(109, "特命指令！ディープブルー航空隊"),
			]
		),
		new(
			"第三集",
			[
				new(200, "ガダルカナルを巡る戦い"),
				new(201, "空母「飛龍」未ダ健在ナリ"),
				new(202, "トーキョーボミング"),
				new(203, "栄光の空母「エンタープライズ」"),
				new(204, "絶対制空大作戦！ディープブルー航空隊"),
				new(205, "救出大作戦！ディープブルー航空隊"),
				new(206, "シンク・ジ・ヤマト"),
				new(207, "モーニング艦隊。"),
				new(208, "激闘大奮戦！ディープブルー航空隊"),
				new(209, "ミッドウェイ島攻略"),
			]
		),
		new(
			"第四集",
			[
				new(300, "ドゥーリットルボミング"),
				new(301, "ポートモレスビー攻略"),
				new(302, "攻撃目標ミッドウェイ"),
				new(303, "不可能作戦！ディープブルー航空隊"),
				new(304, "日本近海の戦い"),
				new(305, "戦艦「大和」の戦い"),
				new(306, "沈黙の艦隊、再出撃"),
			]
		),
	];
}
