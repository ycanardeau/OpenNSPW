namespace OpenNspw;

// The rules a battle follows where NSPW 1.22 and NSPW NET differ: NSPW NET's, which network games play by, or 1.22's,
// which single-player games play by. Each of 1.22's is read from its machine code, at the address given
// (docs/Differences.md). The game holds the ones it plays by in Nspw.Rules. Both give every rule a value, by its name,
// since every rule is one where they differ.
//
// A rule marked BUG(1.22) is a bug of 1.22, which single-player keeps as is for compatibility with 1.22; the code marks
// the bugs it keeps the same way, BUG(1.22), BUG(NSPW NET) or BUG(1.22, NSPW NET) by the versions that have them. A rule
// is a bug when the original's own code shows that it does what its author did not mean: a value written to the wrong
// place, read before it is set, a field borrowed and never given back, a test that defeats its own loop. Another value,
// range or choice is not one, even where NSPW NET changed it, since NSPW NET is the same author's later version, which may
// change the design as well as fix it. BUG?(...) marks what may be a bug, where the code does not show what was meant.
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
	// Whether bombers drop torpedoes, and Japan's are armed with them from the combat menu.
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
	// Whether a fighter and its target of the same skill are told apart by the frame, rather than the tick: the first 300
	// of every 600 favor the fighter.
	bool FighterTiesBreakByFrame,
	// Torpedo planes: the last kind of ground unit, from air bases, that they cannot be ordered to attack.
	UnitKind LastKindRefusedToTorpedoes,
	// Attackers: whether Japan's dive and release their bombs close, and drop a second bomb as the Americans do; if not,
	// they release from 170 as the Americans do, and drop one bomb only.
	bool JapaneseAttackersDive,
	// Whether the planes that follow an attacker fly to where it released its bombs or its torpedo.
	bool FollowersFlyToRelease,
	// Whether, when a returning attacker or bomber hands its fighters over to a new leader (SetAttackerEmergencyDestination),
	// they are sent back if the old leader is flying, rather than each if it is itself.
	// BUG?(1.22): it tests the old leader where NSPW NET tests the plane sent back, so a parked fighter is sent back too
	// (0x415916, 0x415971); 1.22's own handover of a destroyed leader's (0x41ADB3) tests the new leader, as NSPW NET does.
	// Kept as is for compatibility with 1.22.
	bool HandoverTestsTheOldLeaderFlying,
	// Whether a flying plane leaves its leader once the leader is parked and returning, rather than staying in its group
	// (SetDynamicDestination).
	// BUG?(1.22): a flying plane keeps following a parked leader, toward its place on the deck, which is not on the map
	// (0x4149F0); NSPW NET has it leave, but 1.22's code does not show whether this was meant. Kept as is for compatibility
	// with 1.22.
	bool FlyingFollowersLeaveParkedLeaders,
	// Whether a mode chosen from the combat menu is given to every plane it is for, flying or parked, and a move clears a
	// plane's deck phase, rather than Move and Return only, and to flying planes, but a Return to attackers and bombers.
	bool MenuModesApplyToEveryPlane,
	// Whether a heading of 360, which none of the 16 cases of a plane's landing pattern and approach, or of a ship's place
	// in its formation takes, leaves their angle unset, rather than 0 (SetLandingDestination, UpdateLanding,
	// SetShipFormation).
	// BUG(1.22): a heading can be 360 (HeadingWrapsAt360), and these switches have no case for it, so they read what the
	// stack holds there: for the landing pattern and the approach, what an earlier call left ([esp+0Ch], 0x4173BF;
	// [esp+10h], 0x41764E); for the formation, the ship's x offset from its leader, which shares the angle's slot
	// ([esp+4], 0x417179). Kept as is for compatibility with 1.22.
	bool HeadingsOf360TakeNoCase,
	// Whether a ship that follows a leader has its route planned by 1.22's own set_cpu_route2, in the emulator, rather than
	// by NSPW NET's set_cpu_root2.
	bool FollowersRouteBy122sOwnCode,
	// Whether a torpedo plane checks for land, before it drops its torpedo, along a heading it has not set, rather than
	// along the bearing from its aim to it (PlaneDropTorpedo).
	// BUG(1.22): it never sets the heading, so it checks along what its stack holds there ([esp+38h], 0x413D23), what an
	// earlier call left. Kept as is for compatibility with 1.22.
	bool TorpedoPlanesCheckLandAlongUnsetHeading,
	// Whether a leader sends its attackers off, fanned out, at 500 to 510 from their target.
	bool AttackersBreakFormation,
	// Guns: whether every kind aims alike, and the hundredths of a bearing are drawn before its degrees.
	bool GunsAimAlike,
	// The ticks a shell flies beyond its distance over its speed, when it has no target.
	int ExtraGunShellTicks,
	// Whether rapid anti-aircraft fire plays one of two sounds, by a shared random number.
	bool RapidFireSoundVaries,
	// Whether an odd count of depth charges stops at 0, rather than going to -1.
	// BUG?(1.22, NSPW NET): every weapon tests the ammunition for 0 before it takes a shot from it, which reads as not to
	// overdraw it, but takes the whole shot from any count, so the count can go below 0 (0x4103B6); firing needs 1 or more,
	// so it does no harm. NSPW NET stops the depth charges' at 0, but none of the others'. Kept as is for compatibility
	// with 1.22.
	bool DepthChargeCountStopsAtZero,
	// A depth charge's damage: this much, plus a random number below the spread.
	int DepthChargeDamage,
	int DepthChargeDamageSpread,
	// Whether a depth charge keeps its fire once it has exploded, rather than freeing it.
	// BUG(1.22): the explosion passes each submerged submarine to hit_chk as the fire's target (used), and never clears it,
	// so the fire stays in use, with the submarine hit or tested last, to the end of the battle (0x4196DC to 0x4197EC). Kept
	// as is for compatibility with 1.22.
	bool ExplodedDepthChargesKeepTheirFire,
	// The size of a surfaced submarine, within which a fire hits it.
	int SurfacedSubmarineHitSize,
	// The ticks a submarine takes to reload its torpedoes.
	int SubmarineTorpedoReload,
	// Whether a submarine keeps its target once it has fired torpedoes at it, rather than clearing it.
	bool SubmarinesKeepTheirTargetAfterFiring,
	// Whether bullets damage a transport 1 time in 4 only.
	bool BulletsSpareTransports,
	// Transports: how far from their landing point they land their cargo.
	int CargoLandingRange,
	// Whether their cargo slows them.
	bool CargoSlowsTransports,
	// Whether the bases that they land are built over 8000 ticks or more, from a quarter of their HP.
	bool LandedBasesAreBuilt,
	// Whether a transport leaves the battle once it has landed its cargo, rather than staying, empty.
	bool TransportsLeaveAfterLanding,
	// Moves: whether fuel burns by the speed's share of the top speed, as a fraction, rather than 1 at a time.
	bool FuelBurnsBySpeed,
	// Whether a ship out of fuel brakes from its top speed times 0.1, rather than over 10, which can be less in the last
	// bit: 0.8 * 0.1 is 0.08000000000000002, 0.8 / 10 is 0.08.
	bool OutOfFuelShipsBrakeFromTopSpeedTimesATenth,
	// Whether a heading that turns to 360 becomes 0, rather than only one past 360.
	bool HeadingWrapsAt360,
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
	// Whether the headings meant to become whole degrees, a depth charge's from its ship, a fighter's to dodge a head-on
	// pass, a ship's to evade a torpedo or a plane and a ship's wake behind it, keep their fraction of a degree but its last
	// digits (ReduceHeading).
	// BUG(1.22): it stores the whole degrees into the low half of the double, and reads the double back (0x410452,
	// 0x415324, 0x416417, 0x416838, 0x41C327). Kept as is for compatibility with 1.22.
	bool ReducedHeadingsKeepTheirFraction,
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
	// Whether the marks, the leader's flag, the damaged ships' smoke and fire, the damaged submarines' bubbles, and the
	// fires' sprites, trails and wakes are made on the tick that is drawn only (IsEffectTick).
	bool EffectsOnDrawnTickOnly,
	// Whether a damaged plane tests the tick, and a ship its damage, before it draws the random number of its smoke.
	bool DamageEffectsTestBeforeDrawing,
	// Whether the Yamato class and the Essex, the anti-aircraft cruisers and the anti-submarine destroyers have their own
	// marks.
	bool MarksSecondTypes,
	// Whether every lower effect is drawn once the battle has a result, rather than those the player sees, with the random
	// numbers of their animations.
	bool ResultShowsEveryLowerEffect,
	// Whether the sonar plays.
	bool PlaysSonar,
	// The fires and effects in use at most, and one more.
	int FireLimit,
	int EffectLimit,
	// Whether scenario 303's captured fighters, the Americans' of type 1, are drawn as Japan's.
	bool DrawsCapturedFightersAsJapans
)
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
		FighterTiesBreakByFrame: false,
		LastKindRefusedToTorpedoes: UnitKind.Fortress,
		JapaneseAttackersDive: true,
		FollowersFlyToRelease: true,
		HandoverTestsTheOldLeaderFlying: false,
		FlyingFollowersLeaveParkedLeaders: true,
		MenuModesApplyToEveryPlane: false,
		HeadingsOf360TakeNoCase: false,
		FollowersRouteBy122sOwnCode: false,
		TorpedoPlanesCheckLandAlongUnsetHeading: false,
		AttackersBreakFormation: true,
		GunsAimAlike: false,
		ExtraGunShellTicks: 1,
		RapidFireSoundVaries: true,
		DepthChargeCountStopsAtZero: true,
		DepthChargeDamage: 1,
		DepthChargeDamageSpread: all_head.ASB_DMG + 2 + 1,
		ExplodedDepthChargesKeepTheirFire: false,
		SurfacedSubmarineHitSize: 8,
		SubmarineTorpedoReload: all_head.RELOAD_TPD_SS,
		SubmarinesKeepTheirTargetAfterFiring: false,
		BulletsSpareTransports: true,
		CargoLandingRange: 160,
		CargoSlowsTransports: true,
		LandedBasesAreBuilt: true,
		TransportsLeaveAfterLanding: true,
		FuelBurnsBySpeed: true,
		OutOfFuelShipsBrakeFromTopSpeedTimesATenth: false,
		HeadingWrapsAt360: true,
		ComputerReplansBlockedRoutes: false,
		SupplyNeedsNavalBase: true,
		ComputerShipsReturnIntoMap: true,
		ShipsFleeGroundUnits: false,
		ShipEvasionRange: all_head.BB1_SIGHT,
		ReducedHeadingsKeepTheirFraction: false,
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
		EffectsOnDrawnTickOnly: false,
		DamageEffectsTestBeforeDrawing: false,
		MarksSecondTypes: true,
		ResultShowsEveryLowerEffect: true,
		PlaysSonar: true,
		FireLimit: all_head.FIRE_MAX,
		EffectLimit: all_head.EFFECT_MAX,
		DrawsCapturedFightersAsJapans: false
	);

	public static readonly BattleRules Nspw122 = new(
		InfantryBaseGunInterval: 200, // 0x403140
		PillboxesGunInterval: 150, // 0x4031DE
		FortressGunInterval: 100, // 0x403275
		HasYamatoClass: false, // 0x40332C
		HasSecondCruiserAndDestroyerTypes: false, // 0x40348C, 0x403647
		BombersDropTorpedoes: false, // 0x40392C, 0x431D56
		TransportsLandNavalAndAirBases: false, // 0x403745
		JapanHoldsFireInScenario1: true, // 0x4030FF
		FightersAttackTransports: false, // 0x4133B2, 0x430618, 0x430679, 0x41650D
		FighterTargetRangeAgainstBombers: 400, // 0x4135C1
		ReturningFightersRetarget: true, // 0x413550
		FighterTiesBreakByFrame: true, // 0x4150A9
		LastKindRefusedToTorpedoes: UnitKind.NavalBase, // 0x4305F9
		JapaneseAttackersDive: false, // 0x414186 to 0x4144E0
		FollowersFlyToRelease: false, // 0x414186 to 0x4144E0, 0x413F51 to 0x413F7C
		HandoverTestsTheOldLeaderFlying: true, // 0x415916, 0x415971
		FlyingFollowersLeaveParkedLeaders: false, // 0x4149F0
		MenuModesApplyToEveryPlane: true, // 0x431D19 to 0x431D4E
		HeadingsOf360TakeNoCase: true, // 0x4173BF, 0x41764E, 0x417179
		FollowersRouteBy122sOwnCode: true, // 0x414D79
		TorpedoPlanesCheckLandAlongUnsetHeading: true, // 0x413D23
		AttackersBreakFormation: false, // 0x415A10
		GunsAimAlike: true, // 0x410A65, 0x4129AB
		ExtraGunShellTicks: 0, // 0x410BCE
		RapidFireSoundVaries: false, // 0x41019F
		DepthChargeCountStopsAtZero: false, // 0x4103B6
		DepthChargeDamage: 3, // 0x416D1F
		DepthChargeDamageSpread: 3, // 0x416D1F
		ExplodedDepthChargesKeepTheirFire: true, // 0x4196DC to 0x4197EC
		SurfacedSubmarineHitSize: 4, // 0x40F9D6
		SubmarineTorpedoReload: 2000, // 0x4110DB, 0x41176B
		SubmarinesKeepTheirTargetAfterFiring: true, // 0x4117F7
		BulletsSpareTransports: false, // 0x418F92
		CargoLandingRange: 360, // 0x413003
		CargoSlowsTransports: false, // 0x402955
		LandedBasesAreBuilt: false, // 0x418E23
		TransportsLeaveAfterLanding: false, // 0x413014 to 0x41312B
		FuelBurnsBySpeed: false, // 0x402F72
		OutOfFuelShipsBrakeFromTopSpeedTimesATenth: true, // 0x40285F
		HeadingWrapsAt360: false, // 0x4024B6
		ComputerReplansBlockedRoutes: true, // 0x401F95 to 0x4023EB, 0x402D2B
		SupplyNeedsNavalBase: false, // 0x4010BB
		ComputerShipsReturnIntoMap: false, // 0x416B35, 0x401966
		ShipsFleeGroundUnits: true, // 0x41616C
		ShipEvasionRange: 900, // 0x416277
		ReducedHeadingsKeepTheirFraction: true, // 0x410452, 0x415324, 0x416417, 0x416838, 0x41C327
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
		EffectsOnDrawnTickOnly: true, // 0x41AF02 to 0x41C18D, 0x419099 to 0x41A774
		DamageEffectsTestBeforeDrawing: true, // 0x41B4CC, 0x41B659, 0x41B7AF, 0x41B8EC, 0x41C18D
		MarksSecondTypes: false, // 0x41B991
		ResultShowsEveryLowerEffect: false, // 0x40CDEC
		PlaysSonar: false,
		FireLimit: 256, // seek_fire_no, 0x416E40
		EffectLimit: 256,
		DrawsCapturedFightersAsJapans: true
	); // 0x40E070
}
