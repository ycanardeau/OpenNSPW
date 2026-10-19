namespace OpenNspw;

// The rules a battle follows where NSPW 1.22 and NSPW NET differ: NSPW NET's, which network games play by, or 1.22's,
// which single-player games play by. Each of 1.22's is read from its machine code, at the address given
// (docs/Differences.md). The game holds the ones it plays by in Nspw.Rules. Both give every rule a value, by its name,
// since every rule is one where they differ.
public sealed record BattleRules(
	// Firing (ControlFiring): one shot of a ground unit's gun in this many ticks, doubled when it is damaged.
	int InfantryBaseGunInterval,
	int PillboxesGunInterval,
	int FortressGunInterval,

	// Whether Japan's battleships of type 1, the Yamato class, fire as their own class.
	bool HasYamatoClass,

	// Whether cruisers and destroyers of type 1, the anti-aircraft cruisers and anti-submarine destroyers, fire as their own
	// types.
	bool HasSecondCruiserAndDestroyerTypes,

	// Whether bombers drop torpedoes.
	bool BombersDropTorpedoes,

	// Whether transports can land naval bases and air bases, not only infantry bases, pillboxes and fortresses.
	bool TransportsLandNavalAndAirBases,

	// Whether Japan holds its fire in scenario 1.
	bool JapanHoldsFireInScenario1,

	// Fighters: whether they shoot transports, can be ordered to attack them, and transports flee them.
	bool FightersAttackTransports,

	// How far a fighter takes an attacker or a bomber as its target; any other plane, within 400.
	int FighterTargetRangeAgainstBombers,

	// Whether the computer's returning fighters take a target within 300, with ammunition and fuel left.
	bool ReturningFightersRetarget,

	// Torpedo planes: the last kind of ground unit, from air bases, that they cannot be ordered to attack.
	UnitKind LastKindRefusedToTorpedoes,

	// Attackers: whether Japan's dive and release their bombs close, and drop a second bomb as the Americans do; if not,
	// they release from 170 as the Americans do, and drop one bomb only.
	bool JapaneseAttackersDive,

	// Whether the planes that follow an attacker fly to where it released its bombs.
	bool FollowersFlyToRelease,

	// Whether a leader sends its attackers off, fanned out, at 500 to 510 from their target.
	bool AttackersBreakFormation,

	// Guns: whether every kind aims alike, and the hundredths of a bearing are drawn before its degrees.
	bool GunsAimAlike,

	// The ticks a shell flies beyond its distance over its speed, when it has no target.
	int ExtraGunShellTicks,

	// Whether rapid anti-aircraft fire plays one of two sounds, by a shared random number.
	bool RapidFireSoundVaries,

	// Whether an odd count of depth charges stops at 0, rather than going to -1.
	bool DepthChargeCountStopsAtZero,

	// A depth charge's damage: this much, plus a random number below the spread.
	int DepthChargeDamage,
	int DepthChargeDamageSpread,

	// The size of a surfaced submarine, within which a fire hits it.
	int SurfacedSubmarineHitSize,

	// The ticks a submarine takes to reload its torpedoes.
	int SubmarineTorpedoReload,

	// Whether bullets damage a transport 1 time in 4 only.
	bool BulletsSpareTransports,

	// Transports: how far from their landing point they land their cargo.
	int CargoLandingRange,

	// Whether their cargo slows them.
	bool CargoSlowsTransports,

	// Whether the bases that they land are built over 8000 ticks or more, from a quarter of their HP.
	bool LandedBasesAreBuilt,

	// Moves: whether fuel burns by the speed's share of the top speed, as a fraction, rather than 1 at a time.
	bool FuelBurnsBySpeed,

	// Whether a ship looks 40 ahead, with only air bases and naval bases out of its way, and the computer replans the
	// routes of its units that ships or land block (CheckCourseAheadAs122, ReplanBlockedRoute).
	bool ComputerReplansBlockedRoutes,

	// Whether supply stops away from a naval base of the side.
	bool SupplyNeedsNavalBase,

	// Whether the computer's ships, and submarines, are brought back into the map.
	bool ComputerShipsReturnIntoMap,

	// Whether a stopped ship flees ground units too, and how far it flees ships.
	bool ShipsFleeGroundUnits,
	int ShipEvasionRange,

	// Whether only the computer's destroyers hunt submarines, chasing one (HuntSubmarineAs122).
	bool OnlyComputersDestroyersHunt,

	// Targets: whether a submerged submarine, and a parked plane's target, are dropped.
	bool DropsHiddenTargets,

	// Detection: whether 1.22's sight ranges apply (GetDetectionSizeAs122).
	bool HasSightRangesOf122,

	// Whether supplying units of the player's side look out.
	bool SupplyingUnitsLookOut,

	// Whether a submarine dives at any unit it finds, also while it supplies.
	bool SubmarinesDiveAtAnyUnit,

	// Whether finding a submerged submarine stops the computer's destroyers that chase it.
	bool FoundSubmarinesStopChasers,

	// The number that finds a submerged submarine, out of rand() % range, and whether the contact's spread depends on the
	// finder's type.
	int SubmarineContactDraw,
	bool ContactSpreadByType,

	// Planes: whether a landed plane's launch lock counts the other planes landing.
	bool LaunchLockCountsLandingPlanes,

	// Whether returning planes adjust their approach on the drawn tick only, and to any base still in use.
	bool ApproachesOnDrawnTick,

	// Whether planes cannot be added to a full carrier.
	bool FullCarriersRefusePlanes,

	// The tick: whether it counts first, then the clouds and the detection, then the end, and cities run its unit loop.
	bool TickOrderOf122,

	// Effects: whether they draw from rand, as everything else, rather than from SharedRandom.
	bool EffectsDrawFromRand,

	// Whether the sonar plays.
	bool PlaysSonar,

	// The fires and effects in use at most, and one more.
	int FireLimit,
	int EffectLimit,

	// Whether scenario 303's captured fighters, the Americans' of type 1, are drawn as Japan's.
	bool DrawsCapturedFightersAsJapans)
{
	public static readonly BattleRules NspwNet = new(
		InfantryBaseGunInterval: 250,
		PillboxesGunInterval: 200,
		FortressGunInterval: 150,
		HasYamatoClass: true,
		HasSecondCruiserAndDestroyerTypes: true,
		BombersDropTorpedoes: true,
		TransportsLandNavalAndAirBases: true,
		JapanHoldsFireInScenario1: false,
		FightersAttackTransports: true,
		FighterTargetRangeAgainstBombers: 650,
		ReturningFightersRetarget: false,
		LastKindRefusedToTorpedoes: UnitKind.Fortress,
		JapaneseAttackersDive: true,
		FollowersFlyToRelease: true,
		AttackersBreakFormation: true,
		GunsAimAlike: false,
		ExtraGunShellTicks: 1,
		RapidFireSoundVaries: true,
		DepthChargeCountStopsAtZero: true,
		DepthChargeDamage: 1,
		DepthChargeDamageSpread: all_head.ASB_DMG + 2 + 1,
		SurfacedSubmarineHitSize: 8,
		SubmarineTorpedoReload: all_head.RELOAD_TPD_SS,
		BulletsSpareTransports: true,
		CargoLandingRange: 160,
		CargoSlowsTransports: true,
		LandedBasesAreBuilt: true,
		FuelBurnsBySpeed: true,
		ComputerReplansBlockedRoutes: false,
		SupplyNeedsNavalBase: true,
		ComputerShipsReturnIntoMap: true,
		ShipsFleeGroundUnits: false,
		ShipEvasionRange: all_head.BB1_SIGHT,
		OnlyComputersDestroyersHunt: false,
		DropsHiddenTargets: true,
		HasSightRangesOf122: false,
		SupplyingUnitsLookOut: false,
		SubmarinesDiveAtAnyUnit: false,
		FoundSubmarinesStopChasers: false,
		SubmarineContactDraw: 2,
		ContactSpreadByType: true,
		LaunchLockCountsLandingPlanes: true,
		ApproachesOnDrawnTick: false,
		FullCarriersRefusePlanes: true,
		TickOrderOf122: false,
		EffectsDrawFromRand: false,
		PlaysSonar: true,
		FireLimit: all_head.FIRE_MAX,
		EffectLimit: all_head.EFFECT_MAX,
		DrawsCapturedFightersAsJapans: false);

	public static readonly BattleRules Nspw122 = new(
		InfantryBaseGunInterval: 200, // 0x403140
		PillboxesGunInterval: 150, // 0x4031DE
		FortressGunInterval: 100, // 0x403275
		HasYamatoClass: false, // 0x40332C
		HasSecondCruiserAndDestroyerTypes: false, // 0x40348C, 0x403647
		BombersDropTorpedoes: false, // 0x40392C
		TransportsLandNavalAndAirBases: false, // 0x403745
		JapanHoldsFireInScenario1: true, // 0x4030FF
		FightersAttackTransports: false, // 0x4133B2, 0x430618, 0x430679, 0x41650D
		FighterTargetRangeAgainstBombers: 400, // 0x4135C1
		ReturningFightersRetarget: true, // 0x413550
		LastKindRefusedToTorpedoes: UnitKind.NavalBase, // 0x4305F9
		JapaneseAttackersDive: false, // 0x414186 to 0x4144E0
		FollowersFlyToRelease: false, // 0x414186 to 0x4144E0
		AttackersBreakFormation: false, // 0x415A10
		GunsAimAlike: true, // 0x410A65, 0x4129AB
		ExtraGunShellTicks: 0, // 0x410BCE
		RapidFireSoundVaries: false, // 0x41019F
		DepthChargeCountStopsAtZero: false, // 0x4103B6
		DepthChargeDamage: 3, // 0x416D1F
		DepthChargeDamageSpread: 3, // 0x416D1F
		SurfacedSubmarineHitSize: 4, // 0x40F9D6
		SubmarineTorpedoReload: 2000, // 0x4110DB, 0x41176B
		BulletsSpareTransports: false, // 0x418F92
		CargoLandingRange: 360, // 0x413003
		CargoSlowsTransports: false, // 0x402955
		LandedBasesAreBuilt: false, // 0x418E23
		FuelBurnsBySpeed: false, // 0x402F72
		ComputerReplansBlockedRoutes: true, // 0x401F95 to 0x4023EB, 0x402D2B
		SupplyNeedsNavalBase: false, // 0x4010BB
		ComputerShipsReturnIntoMap: false, // 0x416B35, 0x401966
		ShipsFleeGroundUnits: true, // 0x41616C
		ShipEvasionRange: 900, // 0x416277
		OnlyComputersDestroyersHunt: true, // 0x416706 to 0x416B35
		DropsHiddenTargets: false, // 0x402FD2
		HasSightRangesOf122: true, // 0x418080
		SupplyingUnitsLookOut: true, // 0x41829B
		SubmarinesDiveAtAnyUnit: true, // 0x4184DF
		FoundSubmarinesStopChasers: true, // 0x418724
		SubmarineContactDraw: 10, // 0x417FC9
		ContactSpreadByType: false, // 0x417FE7
		LaunchLockCountsLandingPlanes: false, // 0x4018F1
		ApproachesOnDrawnTick: true, // 0x401AB4, 0x417555
		FullCarriersRefusePlanes: false, // 0x41F060
		TickOrderOf122: true, // 0x401009 to 0x4010A3
		EffectsDrawFromRand: true, // 0x419190, 0x402CA3
		PlaysSonar: false,
		FireLimit: 256, // seek_fire_no, 0x416E40
		EffectLimit: 256,
		DrawsCapturedFightersAsJapans: true); // 0x40E070
}
