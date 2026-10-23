namespace OpenNspw.AI;

// A part of the map: a place the briefing names, which does not move, or an area where enemy fleets are expected.
public sealed record Area(string Name, WorldPosition Center, double Radius, bool IsPlace);

// What a player knows of a scenario before the battle (docs/StrongAI.md, Scenario knowledge): where the enemy is
// expected, the places to attack among them, the enemy kinds the victory conditions count, and whether the player wins by
// holding on until the end. Written from the briefings, the victory conditions and the maps; never from where a hidden
// unit is.
public sealed record ScenarioKnowledge(
	IReadOnlyList<Area> Enemy,
	IReadOnlyList<UnitKind> Counted,
	bool HoldsOn = false
)
{
	// The places of the maps, where the scenarios' bases stand.
	private static class Places
	{
		// The central Pacific (Hawaii to the Marianas).
		public static readonly Area PearlHarbor = new("Pearl Harbor", new(7200, -700), 500, true);
		public static readonly Area Midway = new("Midway", new(160, 3360), 400, true);
		public static readonly Area CentralTruk = new("Truk", new(-7600, -4720), 500, true);

		// The South Pacific (the Solomons).
		public static readonly Area Guadalcanal = new(
			"Guadalcanal's airfield",
			new(-850, -200),
			400,
			true
		);
		public static readonly Area Rabaul = new("Rabaul", new(-7550, 1780), 500, true);
		public static readonly Area Truk = new("Truk", new(-7120, 6640), 500, true);
		public static readonly Area Bougainville = new("Bougainville", new(-4100, 1000), 500, true);
		public static readonly Area PortMoresby = new("Port Moresby", new(-9000, -1100), 600, true);

		// Japanese waters.
		public static readonly Area Tokyo = new("Tokyo", new(-7700, 5200), 500, true);
		public static readonly Area Yokosuka = new("Yokosuka", new(-7400, 4850), 400, true);
		public static readonly Area Saipan = new("Saipan", new(5250, 2200), 400, true);
		public static readonly Area Okinawa = new("Okinawa", new(-5000, -2780), 400, true);
	}

	private static Area Fleet(string name, double x, double y, double radius) =>
		new(name, new WorldPosition(x, y), radius, false);

	private static readonly UnitKind[] Carriers = [UnitKind.Carrier, UnitKind.LightCarrier];
	private static readonly UnitKind[] BattleLine = [UnitKind.Battleship, UnitKind.Cruiser];
	private static readonly UnitKind[] Warships =
	[
		UnitKind.Battleship,
		UnitKind.Cruiser,
		UnitKind.Destroyer,
	];
	private static readonly UnitKind[] Ground =
	[
		UnitKind.AirBase,
		UnitKind.InfantryBase,
		UnitKind.Pillboxes,
		UnitKind.Fortress,
	];
	private static readonly UnitKind[] Air = [UnitKind.Fighter, UnitKind.Attacker, UnitKind.Bomber];

	public static ScenarioKnowledge For(int scenario) =>
		scenario switch
		{
			// Target ships south of Hawaii.
			1 => new([Fleet("the target ships south of Hawaii", 7200, -2900, 1500)], Warships),
			// The ships in Pearl Harbor.
			2 => new([Places.PearlHarbor], [.. BattleLine, UnitKind.AirBase]),
			// Japan's carriers, back from Pearl Harbor, come at Wake from the east.
			3 => new(
				[Fleet("the carriers east of Wake", -2000, -300, 2000)],
				Carriers,
				HoldsOn: true
			),
			// The U.S. carrier comes from the Coral Sea, south.
			4 => new([Fleet("the U.S. carrier in the Coral Sea", -500, -5800, 2500)], Carriers),
			// Japan's carriers come at Midway from the west.
			5 => new([Fleet("Japan's carriers west of Midway", -7000, 4500, 2000)], Carriers),
			// Japan's fleets come down from the north-west.
			6 => new(
				[Fleet("Japan's fleets north of the Santa Cruz Islands", 1200, 4000, 3000)],
				Carriers
			),
			// The Japan Navy comes at Guadalcanal from Rabaul and Truk.
			7 => new(
				[Places.Rabaul, Places.Truk],
				[.. Warships, UnitKind.Attacker, UnitKind.Bomber],
				HoldsOn: true
			),
			// The U.S. fleets come at Truk and Wake from the south-east and the east.
			8 => new(
				[
					Fleet("the U.S. carriers in the south", 3000, -6500, 2500),
					Fleet("the U.S. fleet in the north", -3800, 2000, 2000),
				],
				Carriers,
				HoldsOn: true
			),
			// "Straight to the right of the Japanese fleet on the map."
			9 => new([Fleet("the U.S. fleet to the right", 6300, 1400, 1500)], Carriers),
			// The bombers come from Saipan.
			10 => new([Places.Saipan], Air, HoldsOn: true),
			// The carrier Wasp, south of Guadalcanal.
			100 => new([Fleet("the carrier Wasp south of Guadalcanal", 0, -4000, 2500)], Carriers),
			// The U.S. battle line, off Nouméa in the south-east.
			101 => new(
				[Fleet("the U.S. battle line in the south-east", 4650, -3900, 1500)],
				BattleLine
			),
			// Japan's battle line, off Truk in the south-west.
			102 => new(
				[Fleet("Japan's battle line in the south-west", -5700, -3950, 1500)],
				BattleLine
			),
			// Japan's carriers come at Pearl Harbor from the south-west.
			103 => new(
				[Fleet("Japan's carriers south-west of Hawaii", 4000, -4700, 2500)],
				[UnitKind.Attacker],
				HoldsOn: true
			),
			// The U.S. garrison on Guadalcanal.
			104 => new([Places.Guadalcanal], Ground),
			// The U.S. fleet, off Pearl Harbor.
			105 => new([Fleet("the U.S. fleet off Pearl Harbor", 6400, -1200, 1500)], Carriers),
			// Japan's fleet, off Truk.
			106 => new([Fleet("Japan's fleet off Truk", -6800, 6400, 1500)], Carriers),
			// Yamamoto's plane passes over the large island straight north of the carrier.
			107 => new(
				[Fleet("Yamamoto's way over Bougainville", -4500, 1000, 1500)],
				[UnitKind.Bomber]
			),
			// The U.S. forces on Guadalcanal and the carrier south of Rabaul.
			108 => new(
				[Places.Guadalcanal, Fleet("the U.S. carrier in the south", -3000, -4000, 2500)],
				Carriers,
				HoldsOn: true
			),
			// The ships anchored at Yokosuka.
			109 => new([Places.Yokosuka], [.. Carriers, .. BattleLine, UnitKind.Transport]),
			// The U.S. facilities on Guadalcanal, and the U.S. carrier south of it.
			200 => new(
				[
					Places.Guadalcanal,
					Fleet("the U.S. carrier south of Guadalcanal", -2000, -6500, 2500),
				],
				Ground
			),
			// The U.S. carrier, east of the burning fleet.
			201 => new([Fleet("the U.S. carrier to the east", 1200, 2000, 2500)], Carriers),
			// Tokyo's cities.
			202 => new([Places.Tokyo], [UnitKind.City]),
			// Japan's fleets come at Midway from the west.
			203 => new(
				[
					Fleet("Japan's carriers in the west", -7400, 3700, 2000),
					Fleet("Japan's fleet before Midway", 3500, -200, 2000),
				],
				[.. Carriers, .. Warships, UnitKind.Transport],
				HoldsOn: true
			),
			// Japan's carriers south of Guadalcanal, and its bases at Rabaul and Truk.
			204 => new(
				[Fleet("Japan's carrier south of Guadalcanal", -1000, -5000, 2500), Places.Rabaul],
				Carriers,
				HoldsOn: true
			),
			// Bougainville's airfield, where the bombers land.
			205 => new([Places.Bougainville], []),
			// The Yamato comes down the Bungo Channel, north-west of Okinawa.
			206 => new(
				[Fleet("the Yamato in the Bungo Channel", -7300, 1200, 1500)],
				[UnitKind.Battleship]
			),
			// Rabaul's airfield.
			207 => new([Places.Rabaul], [UnitKind.AirBase]),
			// Japan's bases at Rabaul and Truk, and its fleet in the north.
			208 => new(
				[Places.Rabaul, Places.Truk, Fleet("Japan's fleet in the north", 0, 7000, 3000)],
				[.. Warships, UnitKind.Attacker, UnitKind.Bomber],
				HoldsOn: true
			),
			// The U.S. bases on Midway, and the U.S. carrier north-east of it.
			209 => new(
				[Places.Midway, Fleet("the U.S. carrier north-east of Midway", 3400, 6500, 2500)],
				Ground
			),
			// Tokyo's cities.
			300 => new([Places.Tokyo], [UnitKind.City]),
			// Port Moresby, and the U.S. carriers south-east of it.
			301 => new(
				[
					Places.PortMoresby,
					Fleet("the U.S. carriers in the Coral Sea", -4000, -4500, 3000),
				],
				[.. Carriers, .. Ground]
			),
			// Midway, and the U.S. carriers east of it.
			302 => new(
				[Places.Midway, Fleet("the U.S. carriers east of Midway", 4000, 0, 4500)],
				Ground
			),
			// The airfield at Nouméa that the soldiers take.
			303 => new([Fleet("Japan's fleets in the north-west", -3000, 2000, 4000)], []),
			// The U.S. fleets come from the east.
			304 => new(
				[Fleet("the U.S. fleets in the east", 9500, 2500, 3000)],
				[.. Carriers, .. Warships],
				HoldsOn: true
			),
			// The U.S. bases on Okinawa, and the U.S. fleets around it.
			305 => new(
				[Places.Okinawa, Fleet("the U.S. fleets east of Okinawa", -3500, -1000, 3000)],
				Ground
			),
			// The U.S. forces on Guadalcanal, and the U.S. fleet in the east.
			306 => new(
				[Places.Guadalcanal, Fleet("the U.S. fleet in the east", 9300, 3500, 2500)],
				Ground
			),
			_ => new([], []),
		};
}
