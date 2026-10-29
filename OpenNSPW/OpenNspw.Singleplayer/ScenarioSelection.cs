using System.Buffers.Binary;
using System.Text;

namespace OpenNspw.Singleplayer;

// The side a scenario gives the player.
public enum ScenarioSide
{
	Japan,
	UnitedStates,
}

// A scenario of the single-player executable: its title, the side the player commands, and its briefing, the lines the
// setting screen writes under the side, ending with its victory conditions (勝敗条件), which scenario 303 does not state.
public sealed record ScenarioEntry(
	int Number,
	string Title,
	ScenarioSide Side,
	string SideName,
	IReadOnlyList<string> Briefing
);

// A set of scenarios, as the scenario selection screen shows one at a time, with its heading.
public sealed record ScenarioSet(string Heading, IReadOnlyList<ScenarioEntry> Scenarios);

// The scenarios of the single-player executable as its scenario selection screen lists them, in four sets, with what its
// setting screen states about each. Every text is a Shift_JIS string of the executable: the headings and the titles,
// which the selection screen's code pushes from 0x40AE24, and the sides and the briefings, which the setting screen's
// code (0x40B406) pushes scenario after scenario, each starting with its side. The game lists them from Sets, which keeps
// them in C#; Read reads them from the executable, for the tests to compare them with it.
public static class ScenarioSelection
{
	// The address of each set's heading and of each of its scenarios' titles.
	private static readonly (uint Heading, (int Number, uint Title)[] Scenarios)[] Addresses =
	[
		(
			0x441474,
			[
				(1, 0x441464),
				(2, 0x44144C),
				(3, 0x44143C),
				(4, 0x441430),
				(5, 0x44141C),
				(6, 0x441404),
				(7, 0x4413E0),
				(8, 0x4413CC),
				(9, 0x4413BC),
				(10, 0x4413AC),
			]
		),
		(
			0x441370,
			[
				(100, 0x441364),
				(101, 0x44134C),
				(102, 0x441334),
				(103, 0x44130C),
				(104, 0x4412FC),
				(105, 0x4412E4),
				(106, 0x4412CC),
				(107, 0x4412B8),
				(108, 0x4412A8),
				(109, 0x441288),
			]
		),
		(
			0x44124C,
			[
				(200, 0x441234),
				(201, 0x441218),
				(202, 0x441204),
				(203, 0x4411E4),
				(204, 0x4411BC),
				(205, 0x441198),
				(206, 0x441184),
				(207, 0x441170),
				(208, 0x44114C),
				(209, 0x441138),
			]
		),
		(
			0x4410FC,
			[
				(300, 0x4410E4),
				(301, 0x4410CC),
				(302, 0x4410B4),
				(303, 0x441090),
				(304, 0x441080),
				(305, 0x44106C),
				(306, 0x441058),
			]
		),
	];

	// The setting screen's code that writes the briefings, up to its last push of one, and the strings it pushes.
	private const uint BriefingCodeStart = 0x40B406;
	private const uint BriefingCodeEnd = 0x40C5AC;
	private const uint BriefingStringsStart = 0x43E0A4;
	private const uint BriefingStringsEnd = 0x441040;

	// The line a briefing starts with: 操作対象：日本海軍 or 操作対象：合衆国海軍.
	private const string SideLabel = "操作対象：";

	private const byte PushImmediate = 0x68;

	private static Encoding CreateShiftJis()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		return Encoding.GetEncoding(932);
	}

	private static readonly Encoding ShiftJis = CreateShiftJis();

	private static string ReadString(
		ReadOnlySpan<byte> executable,
		PortableExecutable image,
		uint address
	)
	{
		var bytes = executable[(int)image.FileOffsetOf(address)..];
		return ShiftJis.GetString(bytes[..bytes.IndexOf((byte)0)]);
	}

	// The lines of each scenario's briefing, side first, in the order of the scenarios.
	private static List<List<string>> ReadBriefings(
		ReadOnlySpan<byte> executable,
		PortableExecutable image
	)
	{
		var code = executable[
			(int)image.FileOffsetOf(BriefingCodeStart)..(int)image.FileOffsetOf(BriefingCodeEnd)
		];
		var briefings = new List<List<string>>();
		for (var i = 0; i + 5 <= code.Length; i++)
		{
			if (code[i] != PushImmediate)
				continue;

			var address = BinaryPrimitives.ReadUInt32LittleEndian(code[(i + 1)..]);
			if (address is < BriefingStringsStart or >= BriefingStringsEnd)
				continue;

			var line = ReadString(executable, image, address);
			if (line.StartsWith(SideLabel, StringComparison.Ordinal))
				briefings.Add([line]);
			else if (briefings.Count > 0)
				briefings[^1].Add(line);
			else
				throw new InvalidDataException(
					$"The first briefing does not start with its side, but with {line}."
				);

			i += 4;
		}

		return briefings;
	}

	private static ScenarioEntry CreateEntry(int number, string title, List<string> briefing)
	{
		var sideName = briefing[0][SideLabel.Length..];
		var side = sideName switch
		{
			"日本海軍" => ScenarioSide.Japan,
			"合衆国海軍" => ScenarioSide.UnitedStates,
			_ => throw new InvalidDataException(
				$"Scenario {number} gives the player {sideName}, not a side."
			),
		};
		return new(number, title, side, sideName, briefing[1..]);
	}

	public static IReadOnlyList<ScenarioSet> Read(ReadOnlySpan<byte> executable)
	{
		var image = PortableExecutable.Read(executable);
		var briefings = ReadBriefings(executable, image);
		var scenarioCount = Addresses.Sum(s => s.Scenarios.Length);
		if (briefings.Count != scenarioCount)
			throw new InvalidDataException(
				$"The setting screen has {briefings.Count} briefings, not {scenarioCount}."
			);

		var sets = new List<ScenarioSet>();
		var index = 0;
		foreach (var (heading, scenarios) in Addresses)
		{
			var entries = new List<ScenarioEntry>();
			foreach (var (number, title) in scenarios)
				entries.Add(
					CreateEntry(number, ReadString(executable, image, title), briefings[index++])
				);

			sets.Add(new(ReadString(executable, image, heading), entries));
		}

		return sets;
	}

	// The scenarios' texts as Read gives them from NSPW_122.exe, so that the game needs no executable to list them: each
	// briefing starts with the side's line, as the setting screen pushes it. ScenarioSelectionTests compares them with the
	// executable's.
	public static IReadOnlyList<ScenarioSet> Sets { get; } =
	[
		new(
			"シナリオ選択画面（第一集、本ゲームの標準シナリオです。）",
			[
				CreateEntry(
					1,
					"ハワイ沖大演習",
					[
						"操作対象：合衆国海軍",
						"練習シナリオです。ハワイ南方に標的艦船が浮かんでいますので、",
						"これに対して攻撃してください。",
						"史実ではこの後、米国艦隊はアメリカ本土への海軍基地へ戻らず",
						"ハワイの真珠湾内に投錨する事になります。",
						"勝敗条件：標的艦船が全て沈没すれば勝利です。制限時間オーバー",
						"で敗北です。",
					]
				),
				CreateEntry(
					2,
					"パールハーバーアタック",
					[
						"操作対象：日本海軍",
						"ハワイ、真珠湾にいるだろう米海軍の艦船に攻撃をかけます。",
						"日本海軍の艦船には正規空母が複数あります。",
						"ここは史実どおり、航空機による攻撃をかけるのがベストです。",
						"勝敗条件：真珠湾の米軍艦船に多くの損害を与えてください。",
						"制限時間オーバーで敗北です。",
					]
				),
				CreateEntry(
					3,
					"ウェーク島防衛",
					[
						"操作対象：合衆国海軍",
						"真珠湾を攻撃し終えた日本海軍航空艦隊は一路本国への航路を進みます。",
						"しかし、数隻の空母がウェーク島攻略に向けられました。",
						"あなたは、ウェーク島の航空部隊を指揮し同島を防衛してください。確かに、",
						"同島の降伏は時間の問題ですが、少しでも敵に損害を与えるべきです。",
						"勝敗条件：制限時間内、ウェーク島の歩兵基地を守るか、敵空母撃沈で勝利",
						"、同基地か航空戦力の壊滅で敗北です。",
					]
				),
				CreateEntry(
					4,
					"珊瑚海海戦",
					[
						"操作対象：日本海軍",
						"この海戦は互いの艦隊が直接視認しあうことなく、その空母から飛び",
						"立った攻撃機のみで行われた、海戦史上初の空母艦隊の海戦です。",
						"史実では、日本海軍が米空母を一隻沈めますが、日本海軍の主目的",
						"であるポートモレスビー攻略作戦はこの戦闘で停止します。",
						"勝敗条件：先に敵空母を１隻撃沈すること。",
						"制限時間オーバーで敗北です。",
					]
				),
				CreateEntry(
					5,
					"運命のミッドウェイ",
					[
						"操作対象：合衆国海軍",
						"日本海軍はミッドウェイへの戦略的奇襲を狙って、同島への攻略を",
						"開始します。しかし、その近海にて米海軍の空母を発見、ここに日米両海軍",
						"の空母部隊の戦闘が始まります。史実では米海軍の大勝利ですが、",
						"本シナリオで、日本海軍は同じ過ちを犯しません。",
						"勝敗条件：敵空母の全滅。制限時間オーバーで敗北です。",
					]
				),
				CreateEntry(
					6,
					"サンタクルーズ諸島海戦",
					[
						"操作対象：合衆国海軍",
						"ガダルカナル島の攻防で行われた海戦です。日本陸海軍は同島奪還に全戦力を",
						"投入します。そしてまた、米海軍もその戦力をこの海域に集中展開したのです。",
						"史実では、日本海軍が米空母と駆逐艦を各１隻沈め、日本海軍側に沈没艦は無しで",
						"した。しかしこの戦闘で、日本海軍は訓練をつんだパイロットの殆どを失ったのです。",
						"勝敗条件：敵空母の全滅。制限時間オーバーで敗北です。",
					]
				),
				CreateEntry(
					7,
					"ネーバルサウスパシフィックウォー",
					[
						"操作対象：合衆国海軍",
						"サンタクルーズ諸島の空母戦で、日米両海軍とも空母艦隊の再編成を強いられます。",
						"しかし、戦闘は終結した分けではありません。米海軍はなんとしてでもガダルカナル島",
						"を守り切らなくてはなりません。そして日本海軍は全戦力をもって同島の航空基地",
						"を攻撃します。例えそれが、いかに鉄底海峡を築こうともです。",
						"勝敗条件：ガダルカナル島の航空基地を制限時間内守り",
						"切るか、日本海軍の壊滅で米海軍の勝利です。",
					]
				),
				CreateEntry(
					8,
					"トラック島への攻勢",
					[
						"操作対象：日本海軍",
						"米海軍の大攻勢が始まります。日本海軍はこれを迎え撃ちます。",
						"この大海戦の勝利は、日本海軍の太平洋からの完全駆逐を意味します。",
						"スタート時に、修理中の艦船がありますが、その艦船は常に停泊しています。",
						"トラック、ウェークにいる、この停泊艦船を日本海軍は守ってください。",
						"勝敗条件：制限時間終了時に停泊艦船が残っていれは勝利。",
						"停泊艦船全滅で敗北です。",
					]
				),
				CreateEntry(
					9,
					"マリアナ沖海戦",
					[
						"操作対象：日本海軍",
						"日本海軍最後の大反撃が始まります。マリアナ群島サイパン島を巡る戦い",
						"がそれです。この時点で日本海軍はほぼ南太平洋から駆逐され、もしサイパン",
						"を失えば、日本本土が敵爆撃機の攻撃圏内に入ってしまいます。",
						"尚、敵艦隊の位置は判っており、日本艦隊よりマップの右へまっすぐです。",
						"勝敗条件：敵全空母の壊滅で勝利、制限時間オーバーか全空母",
						"の損失で敗北です。",
					]
				),
				CreateEntry(
					10,
					"日本上空迎撃戦",
					[
						"操作対象：日本海軍",
						"サイパン島に上陸を果たした米軍は直ちに航空施設を完成させ、長距離爆撃機多数",
						"を配備します。そしてそれは、パールハーバーより始まったこの戦いの最終局面を",
						"意味します。なぜなら、攻撃目標は日本本土の各都市、特に最重要攻撃目標は",
						"日本の首都東京であるからです。",
						"勝敗条件：都市をすべて失うと日本海軍の負け。航空戦力をすべて失うと",
						"合衆国海軍の敗北です。制限時間オーバーで引き分け。",
					]
				),
			]
		),
		new(
			"シナリオ選択画面（第二集、難易度順には列記されていません）",
			[
				CreateEntry(
					100,
					"沈黙の艦隊",
					[
						"操作対象：日本海軍",
						"ガダルカナル島にて奮戦する日本陸軍、しかし同島への補給は",
						"困難を極める。特に空母からの艦載機の攻撃が脅威である。",
						"そこで潜水艦による、兵力斬減が求めらる。沈黙の艦隊を率いて",
						"敵空母「ワスプ」を撃沈されたし。",
						"勝敗条件：ガダルカナル島の日本軍基地の壊滅、または潜水艦隊の",
						"壊滅で敗北です。勝利条件は敵空母撃沈です。",
					]
				),
				CreateEntry(
					101,
					"艦隊決戦（日本海軍）",
					[
						"操作対象：日本海軍",
						"水上戦力のみの決戦です。登場する兵種は戦艦、巡洋艦、駆逐艦",
						"のみで行われます。またそれぞれの艦数も敵味方とも同じです。",
						"勝敗条件：敵の戦艦、巡洋艦の全滅で勝利です。",
					]
				),
				CreateEntry(
					102,
					"艦隊決戦（合衆国海軍）",
					[
						"操作対象：合衆国海軍",
						"水上戦力のみの決戦です。登場する兵種は戦艦、巡洋艦、駆逐艦",
						"のみで行われます。またそれぞれの艦数も敵味方とも同じです。",
						"このシナリオはひとつ上段のシナリオの指揮するサイドのみを",
						"変更したものです。",
						"勝敗条件：敵の戦艦、巡洋艦の全滅で勝利です。",
					]
				),
				CreateEntry(
					103,
					"超時空航空戦隊！ディープブルー航空隊",
					[
						"操作対象：合衆国海軍",
						"ガ島接戦の最中、米巡洋艦に座乗する准将ウィリアム＝コンペキーは日本軍艦船の攻撃を",
						"うけ戦死した。艦数の不足が原因だった。しかし、彼はなんと時空間を移動し太平洋",
						"戦争前に生き返った！パールハーバーの悲劇は繰り返さない。そう誓った彼はひそかに",
						"「ディープブルー」なる会を結成。そして、これを駆使してハワイ島航空守備隊指揮官に",
						"着任した。彼にとって２度目の１２月７日の朝は今、明けようとしている。",
						"勝敗条件：敵航空部隊の撃滅で勝利、真珠湾の艦船全滅で敗北です。",
					]
				),
				CreateEntry(
					104,
					"ガ島に上陸せよ",
					[
						"操作対象：日本海軍",
						"クエゼリンに停泊する大小さまざまな艦船、その中にはあなたの指揮下にある駆逐艦、",
						"輸送船が各数隻、今、出撃の準備を整えています。輸送船には陸軍兵士がすでに",
						"乗り込んでおり、駆逐艦甲板には水兵たちがあわただしく最後の出航支度に入って",
						"おります。作戦目標はガ島に陸軍兵士を上陸させ、敵守備隊を撃滅することです。",
						"勝敗条件：制限時間内にガ島にいる敵地上兵力を撃滅することで勝利、その他は",
						"すべて敗北です。",
					]
				),
				CreateEntry(
					105,
					"空母決戦（日本海軍）",
					[
						"操作対象：日本海軍",
						"空母と各艦船を織り交ぜた対戦です。戦力はほぼ同じです。",
						"勝敗条件：敵空母の撃滅で勝利、やられると敗北です。",
					]
				),
				CreateEntry(
					106,
					"空母決戦（合衆国海軍）",
					[
						"操作対象：合衆国海軍",
						"空母と各艦船を織り交ぜた対戦です。戦力はほぼ同じです。",
						"勝敗条件：敵空母の撃滅で勝利、やられると敗北です。",
					]
				),
				CreateEntry(
					107,
					"ヤマモトを撃て！",
					[
						"操作対象：合衆国海軍",
						"あなたに託された任務はたった一機の敵機を撃墜することです。あなたの指揮下の",
						"空母から艦載戦闘機を発進させ、まっすぐ北上させます。すると大きな島の上空に",
						"達するはずです。その地点こそが、日本海軍の最高指揮官アドミラル・ヤマモトを",
						"乗せた大型機が通過する場所です。ここは情報部の言葉を信じましょう。",
						"勝敗条件：ヤマモトを乗せた大型機を撃墜すれば勝利です。",
					]
				),
				CreateEntry(
					108,
					"大空のサムライ",
					[
						"操作対象：日本海軍",
						"南太平洋の戦いはほぼ決しました。ガダルカナルとその航空基地は完全に米軍の",
						"手中に落ちました。米海軍はとてつもなく強力です。しかし、その中にあっても",
						"ラバウル航空隊には敵軍の侵攻に対する命令が下されました。まさにそれは、",
						"押し寄せる津波の様な大軍を素手で食い止める様な戦いです。",
						"勝敗条件：ラバウル航空基地以東の島々にある日本軍基地を制限時間内に",
						"ただ一つでも残してください。",
					]
				),
				CreateEntry(
					109,
					"特命指令！ディープブルー航空隊",
					[
						"操作対象：合衆国海軍",
						"時空間を超えて再生した准将ウィリアム＝コンペキー。彼の活躍によって",
						"パールハーバーの悲劇は最小限に抑えられた。しかし、日本海軍は侮りがたい",
						"敵で、各地で巧みな攻勢に出ています。そこで、ディープブルー航空隊には",
						"彼らを守勢に立たせるための作戦、横須賀強襲作戦が発動されました。",
						"勝敗条件：横須賀に停泊する空母、戦艦、巡洋艦、輸送船を撃沈し、",
						"スタート地点の辺りに帰還してください。制限時間オーバーで敗北です。",
					]
				),
			]
		),
		new(
			"シナリオ選択画面（第三集、難易度順には列記されていません）",
			[
				CreateEntry(
					200,
					"ガダルカナルを巡る戦い",
					[
						"操作対象：日本海軍",
						"もはや明白となりました。ガダルカナルにいる敵は、小規模な威力偵察隊では",
						"ありません。米海軍は同島を橋頭堡とし、南太平洋を制圧するつもりです。",
						"敵機動部隊も発見され、この海域こそが日米決戦の場所となりつつあります。",
						"損傷した兵力への増援が到着しました。これを持ってして敵に必殺の一撃を！",
						"勝敗条件：ガ島の敵施設全ての壊滅で勝利です。制限時間オーバー",
						"または、空母を失うとで敗北です。",
					]
				),
				CreateEntry(
					201,
					"空母「飛龍」未ダ健在ナリ",
					[
						"操作対象：日本海軍",
						"敵の急降下爆撃の一撃で、空母「赤城」「加賀」「蒼龍」が爆炎に包まれた！",
						"もはや、ミッドウェイ島の占領は考えられない。目標は唯一つ、のたうち苦しむ",
						"三空母の敵討ちである。未だ健在なる空母「飛龍」の艦載機によって必殺の一撃が",
						"求められる！",
						"勝敗条件：米空母を撃沈すれば日本海軍の勝利。それ以外は全て敗北です。",
					]
				),
				CreateEntry(
					202,
					"トーキョーボミング",
					[
						"操作対象：合衆国海軍",
						"サイパン・テニアン島に巨大な銀翼を並べる４発エンジンの爆撃機、その数は",
						"アメリカの工業力の凄まじさを見せつけます。あなたはこれらを指揮して、本大戦",
						"の最重要戦略目標である東京を爆撃し、破壊してください。",
						"勝敗条件：制限時間内に東京の都市を破壊し尽くせないと敗北です。",
					]
				),
				CreateEntry(
					203,
					"栄光の空母「エンタープライズ」",
					[
						"操作対象：合衆国海軍",
						"誰しも認める太平洋戦争最殊勲の航空母艦「エンタープライズ」。幾度となく",
						"損傷を受けながらもその姿を戦列に現し数々の敵艦を葬り去った、まさに",
						"栄光の空母です。その空母がミッドウェイに迫る日本海軍と対戦します。",
						"勝敗条件：多くの敵艦船を撃破すれば勝利、ミッドウェイのトーチカ群が",
						"破壊されると敗北です。",
					]
				),
				CreateEntry(
					204,
					"絶対制空大作戦！ディープブルー航空隊",
					[
						"操作対象：合衆国海軍",
						"米海軍はガダルカナル島に上陸した。海兵隊が基地を設営しさらならる進出拠点となる",
						"まで、この島を防衛をしなくてはならない。しかし、空港施設は日本軍の空爆で使い",
						"ものにならない、おまけに我が軍の空母はこの海域を離れている。さらに敵の航空攻撃",
						"は激しくなるだろう。",
						"コンペキーがヌーメアの司令部への出頭命令を受けたのはこの様な時である。",
						"勝敗条件：ガダルカナル島の地上兵力施設を制限時間内守り切ること。",
					]
				),
				CreateEntry(
					205,
					"救出大作戦！ディープブルー航空隊",
					[
						"操作対象：合衆国海軍",
						"ブーゲンビルに対して日本海軍の反攻が開始された。進出を急ぎすぎた結果、",
						"同島の航空基地は危機に瀕している。そこで、戦略爆撃機を使っての",
						"パイロット、その他の作業員の救出作戦がディープブルー航空隊に発動された。",
						"ウィリアム＝コンペキーの戦いが始まります。",
						"勝敗条件：ヌーメアの戦略爆撃機をブーゲンビルの航空基地に着陸させ",
						"再びヌーメアの航空基地に戻ってください。",
					]
				),
				CreateEntry(
					206,
					"シンク・ジ・ヤマト",
					[
						"操作対象：合衆国海軍",
						"沖縄での戦闘は終息しつつあります。しかし、豊後水道を日本海軍の象徴的な",
						"巨大戦艦「大和」が沖縄方面に向けて航行中との連絡がありました。",
						"おそらく、沖縄本島への援護射撃と思われます。",
						"勝敗条件：戦艦「大和」撃沈で勝利です。制限時間オーバーで敗北です。",
					]
				),
				CreateEntry(
					207,
					"モーニング艦隊。",
					[
						"操作対象：合衆国海軍",
						"ガダルカナルの防衛は危機を迎えていた。日本海軍は執拗で、強力だった。",
						"だが、それを逆転すべく計画が、シャラン＝ツンクー大佐の下で進められていた。",
						"それは、一度は戦力からはずされた艦船で作られた艦隊を戦線に投入するという",
						"ものだった。艦隊名「モーニング艦隊。」はラバウル撃破の為、出撃した。",
						"勝敗条件：ラバウル航空基地を撃破し、スタート位置の辺りまで",
						"戻ってください。戦艦、巡洋艦の全てを失うと敗北です。",
					]
				),
				CreateEntry(
					208,
					"激闘大奮戦！ディープブルー航空隊",
					[
						"操作対象：合衆国海軍",
						"ガダルカナルを防衛するカクタス航空隊は壊滅した！コンペキーは不吉な考え",
						"を押さえ切れなかった。あまりにも日本海軍が強すぎるのだ！もしや自分と同じ",
						"様な人間が彼等を指揮しているのでは？しかし、考える間もなくヌーメア司令",
						"部から命令が下った。ディープブルー航空隊、ガダルカナルを死守をせよ！",
						"勝敗条件：制限時間内、ガダルカナルの歩兵基地を防衛するか、敵を壊滅すれば",
						"勝利です。",
					]
				),
				CreateEntry(
					209,
					"ミッドウェイ島攻略",
					[
						"操作対象：日本海軍",
						"ドゥーリットル空襲によって、日本海軍は本土防衛能力の低さを露呈した。",
						"日本海軍は南洋での戦いの前にその脅威を取り除く必要を感じ、中部太平洋",
						"を完全に制圧する作戦を発動した。攻略目標はミッドウェイ島、同島を占領",
						"確保し、防衛網を敷けばその目標は達せられるであろう。",
						"勝敗条件：制限時間終了時点にミッドウェイ島に基地を一つでも残し、敵の",
						"地上の兵力の基地を壊滅すること。",
					]
				),
			]
		),
		new(
			"シナリオ選択画面（第四集、難易度順には列記されていません）",
			[
				CreateEntry(
					300,
					"ドゥーリットルボミング",
					[
						"操作対象：合衆国海軍",
						"米国の大戦参戦にあたり、国民の戦意を高める為、奇想天外な作戦が発案された。",
						"それは、正規空母に単発エンジンの攻撃機ではなく、遥かに大型の爆撃機を搭載し",
						"日本を直接爆撃するというものだった。１９４２年４月１８日、午前８時３０分",
						"ジミー・ドゥーリットル少佐指揮下の爆撃機が空母ホーネットを飛び立った。",
						"勝敗条件：制限時間内に東京の都市を２つ壊滅させマップ左下に",
						"残存爆撃機のすべてを集めてください。",
					]
				),
				CreateEntry(
					301,
					"ポートモレスビー攻略",
					[
						"操作対象：日本海軍",
						"フィリピン、シンガポール、インドネシアと破竹の勢いで占領範囲を拡大する日本陸海軍。",
						"そこで、日本海軍首脳部は壮大な計画を発案した。それはアメリカとオーストラリア間",
						"の交通を分断するというものだった。そのため、その交通を妨害するに絶対必要な",
						"ポートモレスビーの攻略作戦が発動された。",
						"勝敗条件：制限時間内に敵空母２隻とポートモレスビーの歩兵基地、トーチカ群、要塞を",
						"全て殲滅、ただし、都市は保存し、その都市の隣に陸上基地を最低１つ設営すること。",
					]
				),
				CreateEntry(
					302,
					"攻撃目標ミッドウェイ",
					[
						"操作対象：日本海軍",
						"ドゥーリットル少佐による東京爆撃は戦術的には全く意味が無く、「東京爆撃」と",
						"いう言葉の割にはその損害は軽微と判断できた。しかし、爆撃を許してしまった",
						"と考える海軍中枢に与えた影響は凄まじく、ミッドウェイ島攻略に踏み出させた。",
						"勝敗条件：制限時間内にミッドウェイ島の地上兵力施設を壊滅してください。",
						"ただし、１隻の空母も失ってはなりません。",
					]
				),
				CreateEntry(
					303,
					"不可能作戦！ディープブルー航空隊",
					[
						"操作対象：合衆国海軍",
						"駆逐艦艦橋に座上するコンペキー、その艦橋から１隻の輸送船が近づいてくるのが見えた。",
						"彼とその他数人しか知らされていない極秘のモールスランプを発信しながら。「おはよう、",
						"コンペキー君。我が軍の多くの爆撃機がクエゼリン環礁を拠点とする新型迎撃機”シンデン”に",
						"よって失われている。で、今回の任務だが、その新型迎撃機を奪い取って、ヌーメアまで届",
						"けて欲しい。ヌーメアの輸送船の兵士達を敵の空港施設の隣に上陸させれば、目的の機体",
						"以外は破壊し、その空港を乗っ取れるだろう、なお、この命令は自動的に消去される、、、",
					]
				),
				CreateEntry(
					304,
					"日本近海の戦い",
					[
						"操作対象：日本海軍",
						"米海軍が日本近海に迫りつつあります。彼我の戦力は大きく開きつつあります。",
						"しかし、この戦いに敗れれば日本には後がありません。",
						"絶対国防圏は冒されてはなりません。日本の防衛は全てこれからの戦いに",
						"かかっています。",
						"勝敗条件：地上兵力施設を制限時間内守り切ってください。",
					]
				),
				CreateEntry(
					305,
					"戦艦「大和」の戦い",
					[
						"操作対象：日本海軍",
						"米軍は沖縄に上陸しました。とてつもない兵力です。すでに、この戦争の行方は決しました。",
						"もはや、日本に勝利はありません。これから行われるいかなる戦闘も無駄なものに終わるでしょう。",
						"１９４５年４月６日、この様な戦況下にあっても、日本の勝利を信じ、沖縄を救うため、",
						"日本海軍の誇る巨大戦艦「大和」は出撃した。",
						"勝敗条件：制限時間内に沖縄の米軍基地を殲滅して下さい。",
					]
				),
				CreateEntry(
					306,
					"沈黙の艦隊、再出撃",
					[
						"操作対象：日本海軍",
						"米海軍の大反攻の前に日本海軍の戦力はじりじりと失われていく。しかし、ガダルカナルの",
						"奪還は必ず成功しなくてはならない。",
						"トラックよりクエゼリンに輸送船がじき到着するだろう、彼等を安全にガダルカナルまで",
						"届け、敵の攻撃を食い止めなくてはならない。沈黙の艦隊は再び出撃した。",
						"勝敗条件：制限時間内にガダルカナル島の敵を殲滅し日本軍の陸上施設を",
						"最低２つ作ってください。",
					]
				),
			]
		),
	];
}
