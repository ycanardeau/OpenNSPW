namespace OpenNspw.AI;

// What a strike that hunts carriers attacks, once it finds the enemy: the nearest carrier (Carriers); the battleships
// and cruisers within 1500 of a carrier first, whose guns turn the attackers back, and the carriers once none is left
// (Escorts); or the cruisers only so (Cruisers), sparing the battleships' 41 HP; or both at once (Split): a share of
// each strike's planes (EscortShare) at the escorts, the rest at the carrier, so that the escorts' guns are spread
// or silenced as the carrier is struck, and the strike does not spend the battle on the escorts.
public enum Targeting
{
	Carriers,
	Escorts,
	Cruisers,
	Split,
}

// The numbers the strong player's tactics turn on, which the arena can set to tune them (--params, StrongAIResearch.md):
// the defaults are the ones chosen by reasoning, and checked on a few games each. Distances are in the map's units,
// times in frames of speed 1 (UnitFacts.Now), fuel in its units.
public sealed record StrongParameters
{
	// How far beyond the computer's strike range a carrier keeps from the enemy's carriers.
	public double KeepAwayMargin { get; init; } = 1000;

	// Whether the cruisers and destroyers screening the carriers leave them to finish a carrier the strikes have hit
	// (TryDetach).
	public bool Detach { get; init; } = true;

	// The cruisers and destroyers leave the carriers for a carrier hit (TryDetach) only when they are, screening and
	// detached together, this many times as many as the enemy's warships found near it.
	public double DetachRatio { get; init; } = 2;

	// How far a carrier keeps from the enemy's warships found lately (Threats), its escorts with it; 0 keeps from none.
	public double AvoidWarships { get; init; }

	// How much farther than its margin a carrier may be from what threatens it, before it closes in.
	public double ApproachBand { get; init; } = 1000;

	// The frames for which a carrier found is thought to be where it would be by now.
	public int CarrierMemoryFrames { get; init; } = 3000;

	// The frames between two scouts' launches, while the last is still on its way out, while hunting carriers.
	public int ScoutFrames { get; init; } = 900;

	// How far from a carrier a scout keeps it in sight: within an attacker's sight (1000), beyond its escorts' guns.
	public double ShadowDistance { get; init; } = 900;

	// The fewest planes a deck strikes with, unless it has fewer.
	public int MinStrike { get; init; } = 3;

	// The share of a deck's attackers and bombers, but its scouts, that must be ready before it strikes, those in the
	// air counted: 0 strikes from MinStrike, 1 waits for all, so that the escorts' guns are spread over more planes.
	public double StrikeShare { get; init; }

	// The share of a deck's fighters kept back from escorting its strikes, to meet the enemy's.
	public double FightersKeptBack { get; init; } = 0.5;

	// Whether a strike that hunts carriers takes fighters along, as one at other targets does (FightersKeptBack), rather
	// than none.
	public bool EscortHunts { get; init; }

	// Whether a fighter in the air comes home once it has fuel for little more than the way (TryBingo), as strike planes
	// do while hunting carriers; always with GatherAndSweep's mission (RaidedBase).
	public bool FighterBingo { get; init; }

	// Whether, where the player holds on, its planes rebase to the air base nearest the enemy (TryRebase).
	public bool Rebase { get; init; } = true;

	// Whether a rebasing formation is given the new base, every plane of it (SelectFormation), not its leader alone, whose
	// planes fly back to their own base short of fuel, and run dry on the way (TryRebase).
	public bool RebaseFormations { get; init; } = true;

	// Whether, in scenario 107, the ships sail at once to where Yamamoto's plane comes round to land, west of Rabaul's
	// airfield, and fire at it there (TrySailToAmbush, TryGunsOnYamamoto).
	public bool AmbushYamamoto { get; init; } = true;

	// Whether, in scenario 10, the fighters gather at the air base the raids strike, the southern one, and, once the raids
	// are over, the forward base's fighters sweep Saipan's airfield (TryGatherFighters, TrySweepSaipan).
	public bool GatherAndSweep { get; init; } = true;

	// Whether, in scenario 8, every ship that sails gathers at the anchored ships next to the air base with the most
	// fighters, and stays there (TryGuardAnchorage).
	public bool GuardAnchorage { get; init; }

	// Whether, in scenario 109, the fleet sails to strike Yokosuka from 5000 away, its strikes held until it is there,
	// and comes home once the ships there are sunk (TrySailToRaid, TryReturnHome).
	public bool RaidYokosuka { get; init; }

	// The frames before a target engaged may be engaged again.
	public int RetargetFrames { get; init; } = 400;

	// How far to one side of its way a strike flies past an enemy strike, or to a blind strike's waypoint.
	public double SideOffset { get; init; } = 2000;

	// How far from a carrier a strike comes round it, to the side its escorts cover least.
	public double ApproachRadius { get; init; } = 2000;

	// How far to one side of a carrier a strike holds off while enemy planes are over it.
	public double HoldOff { get; init; } = 3300;

	// How far each side of its way a strike that hunts carriers fans out, in degrees.
	public double FanAngle { get; init; } = 45;

	// The fuel, beyond what it needs to come home, at which a plane turns home.
	public double BingoMargin { get; init; } = 3;

	// The same, for a plane attacking a target found: below what it needs, as its deck sails toward it.
	public double AttackingBingoMargin { get; init; } = -8;

	// The fuel a plane keeps for coming home, beyond the way (FuelToHome).
	public double HomeFuelMargin { get; init; } = 10;

	// The share of its planes' reach within which a deck strikes a carrier, leaving fuel to look for it.
	public double TargetReach { get; init; } = 0.85;

	// The share of its range on a full tank that a scout flies out (ScoutDistance).
	public double ScoutFraction { get; init; } = 0.36;

	// Whether the submarines, while hunting carriers, sail across the way of the carriers found (TrySubmarines).
	public bool UseSubmarines { get; init; }

	// Whether a strike flies straight at its target when the way round by its waypoint is longer than its planes'
	// reach (Waypoint).
	public bool StraightWhenFar { get; init; }

	// Whether strikes that hunt carriers go first for the ones not thought out of the fight (IsThoughtCrippled).
	public bool SpareCrippled { get; init; }

	// How much nearer, while hunting carriers, a carrier counts for each weapon seen dropped on it (CountDrops), so that
	// the strikes finish the one they have hit before the next: every carrier must be sunk, and the hits spread over
	// several sink none. 0 takes the nearest.
	public double FocusDistance { get; init; }

	// Whether a flying plane whose deck is at a fifth of its HP or less is sent to another deck (TryDivert).
	public bool DivertFromCrippledDecks { get; init; }

	// Whether destroyers fight enemy cruisers and battleships from inside their guns' least range (TryCloseQuarters).
	public bool CloseQuarters { get; init; }

	// Whether the scouts that hunt carriers fly where the map of where they most likely are says (UpdateSearchMap).
	public bool ProbabilitySearch { get; init; }

	// Whether strikes come in on a carrier's beam, within 30° of square to its heading, the beam its escorts cover least
	// (ApproachBearing): a torpedo across a ship meets all its length, and one from ahead or astern little of it.
	public bool BeamApproach { get; init; }

	// Whether fighters go for the enemy's fighters before its attackers and bombers, but those within 1500 of a ship of
	// the player's, about to drop (ChooseTargetInFlight): the escorting fighters shoot down the fighters that go for
	// the attackers.
	public bool FightersFirst { get; init; }

	// Whether the warships of a ring or a screen take the places that sail them the least in all (WarshipOrder), rather
	// than by kind and number.
	public bool RingAssignment { get; init; }

	// Whether a plane sent home comes round the enemy's warships found on its way, beyond their guns, rather than over
	// them (HomeWaypoint).
	public bool ReturnAround { get; init; }

	// Whether a deck holds its strikes and scouts, but not its interceptions and patrols, while a plane of its own short
	// of fuel is coming home to it (MayLaunch): no plane lands on a deck while one takes off from it.
	public bool RecoverFirst { get; init; }

	// The fuel, beyond what it needs to come home, below which a plane coming home holds its deck's launches.
	public double RecoverMargin { get; init; } = 30;

	// The frames of speed 1 each click takes (ClickQueue): 4, as a hand gives a press and a release, or as few as 1, the
	// game's own limit of a click a frame, faster than a hand.
	public int FramesPerClick { get; init; } = 4;

	// Whether a deselection with nothing selected takes no time (ClickQueue.SkipsIdleClicks): no click is given.
	public bool SkipIdleClicks { get; init; }

	// Whether a strike's side to run in from is chosen by playing the candidates forward in copies of the battle
	// (LookAheadApproach), rather than by the escorts' cover alone.
	public bool LookaheadApproach { get; init; }

	// Whether an attacker too close to the ship it attacks to drop on it takes another it can drop on now
	// (TryRetargetWhenClose).
	public bool RetargetWhenClose { get; init; }

	// With LookaheadApproach, what a plane of the strike lost costs, in the HP of the enemy's counted ships: sparing planes
	// at 3 dropped fewer weapons and sank fewer carriers (StrongAIResearch.md).
	public double LookaheadPlaneCost { get; init; } = 3;

	// With LookaheadApproach, whether the player plays on in each copy of the battle, a copy of it, after the strike's first
	// orders, rather than the strike flying its path and being left to the game's attack.
	public bool LookaheadPlays { get; init; }

	// With LookaheadApproach, the frames each copy is played, and the seeds each candidate is played on.
	public int LookaheadFrames { get; init; } = 1800;
	public int LookaheadSeeds { get; init; } = 2;

	// Whether strikes run in on a ship only from where the sea lets a torpedo be dropped (ApproachBearing, IsSeaRun).
	public bool SeaApproach { get; init; }

	// Whether a carrier holds its course while its planes are landing, unless within the enemy's strike range
	// (TryPosition, IsLandingOn): they land only along its heading.
	public bool HoldCourse { get; init; }

	// The margin beyond the computer's strike range a carrier keeps while its strike is out (TryPosition, IsStrikeOut),
	// instead of KeepAwayMargin, so that it does not sail off from the planes, which come home on what fuel they have
	// left; negative keeps KeepAwayMargin.
	public double StrikeOutMargin { get; init; } = -1;

	// Whether a carrier whose strike is out (IsStrikeOut) stays where it is, neither closing in nor moving away, unless
	// it is within the computer's strike range and StrikeOutMargin, or KeepAwayMargin if that is negative (TryPosition).
	public bool HoldForStrike { get; init; }

	// Whether a carrier whose strike is out keeps away, holds or closes in as playing each forward in copies of the battle
	// shows best (LookAheadStance), rather than keeping away; for this many frames, on LookaheadSeeds seeds.
	public bool LookaheadPosition { get; init; } = true;
	public int LookaheadPositionFrames { get; init; } = 2500;

	// With LookaheadPosition, how often a carrier looks ahead at its stance, its strike out or not; 0 looks ahead only
	// as its strike takes off, keeping away otherwise.
	public int LookaheadPositionEvery { get; init; }

	// Whether a strike about to take off goes at its target, at another the victory conditions count within its reach,
	// or waits StrikeWaitFrames, as playing each forward in copies of the battle shows best (LookAheadStrike); for this
	// many frames, on LookaheadSeeds seeds.
	public bool LookaheadStrike { get; init; }
	public int LookaheadStrikeFrames { get; init; } = 3000;
	public int StrikeWaitFrames { get; init; } = 300;

	// What the player's own losses count for in a lookahead's points (Points), against the enemy's at 1.
	public double LookaheadOwnLoss { get; init; } = 1;

	// Whether the player chooses how it plays by looking ahead (TryChoosePlan): every LookaheadPlansEvery frames, each of
	// Plans, the parameters as set with some set otherwise (With), the plans `;` apart, is played LookaheadPlansFrames
	// frames forward in copies of the battle, and the one that scores best is played on. The first plan, empty, plays
	// as set; then one that closes in and strikes with what is ready, one that keeps farther away, and one that goes in
	// within the enemy's strike range.
	public bool LookaheadPlans { get; init; }
	public int LookaheadPlansEvery { get; init; } = 3000;
	public int LookaheadPlansFrames { get; init; } = 6000;
	public string Plans { get; init; } =
		";KeepAwayMargin=0,StrikeOutMargin=0,MinStrike=1,HuntScouts=1;KeepAwayMargin=2000;KeepAwayMargin=-1500";

	// Whether the battleships and cruisers take as their target the enemy attackers within their anti-aircraft reach
	// (TryTargetAttackers), which fire more at a target, and not at all with a ship as their target.
	public bool TargetAttackers { get; init; }

	// Whether a deck that hunts carriers keeps scouting while it knows where a carrier is (IsRemembered), its scouts kept
	// unarmed for it; or stops, and arms them to strike, until the carrier is lost.
	public bool KeepScouting { get; init; } = true;

	// The scouts a deck of eight attackers and bombers or more keeps while it hunts carriers (ScoutsOf), and the most in
	// all, if more than MaxScouts.
	public int HuntScouts { get; init; } = 2;

	// Whether a strike goes, early in the battle, to an area where the briefing expects the carriers, before any is found
	// (ExpectedCarriersToStrike).
	public bool EarlyStrike { get; init; } = true;

	// Whether a surface group led by a cruiser keeps 40 within its guns' reach from the enemy destroyer nearest it, outside
	// the destroyer's (KeepRangeOf): a cruiser's 500 against a destroyer's 400.
	public bool KeepRange { get; init; }

	// Whether fighters spread over the enemy planes, each counting one that others of the player's already attack 400
	// farther (ChooseTargetInFlight), rather than all going for the nearest.
	public bool SpreadFighters { get; init; }

	// Whether every ready fighter of a deck goes up against an enemy attacker or bomber, not four (TryIntercept).
	public bool InterceptWithAll { get; init; }

	// How close to a deck an enemy attacker or bomber is intercepted (TryIntercept). The computer's own fleets intercept
	// within its strike range, 3550, or 4350 when it plays Japan, and 1.5 times that from an air base
	// (docs/ComputerPlayer.md, Air strikes).
	public double InterceptRange { get; init; } = 3000;

	// Whether a strike that hunts carriers disperses once in the air (TryDisperse), each plane flying on alone at full
	// speed; or keeps its formation, massed, until it splits up near the target (TrySplitAttack).
	public bool Disperse { get; init; } = true;

	// How many degrees apart the two halves of a strike that hunts carriers run in (TryApproach), half each side of the
	// bearing: a ship that turns away from one half turns its beam to the other. 0 runs in as one.
	public double PincerAngle { get; init; }

	// Whether attackers carry bombs, not torpedoes, when the victory counts ground units, as the computer's do when its
	// fleet's first target is a ground facility (docs/ComputerPlayer.md, cpu_strtgy, Arming the planes): a torpedo never
	// hits one.
	public bool BombGround { get; init; }

	// Whether transports wait to land while the landing place is still defended, as the player has seen it (IsDefended),
	// and more than LandingTimeLeft ticks are left.
	public bool LandWhenClear { get; init; }

	// The ticks left under which transports land whether the place is defended or not (LandWhenClear).
	public int LandingTimeLeft { get; init; } = 20000;

	// Whether a player that wins by holding on keeps its strikes, scouts and fleets at home: strikes only within
	// DefenseRange of its decks or near the places it holds, no scouts sent out, its warships' targets only near the
	// places held. Its fighters defend at home either way.
	public bool StayHome { get; init; } = true;

	// What a strike that hunts carriers attacks.
	public Targeting Targeting { get; init; } = Targeting.Carriers;

	// With Targeting.Split, the share of the strike planes that attack the escorts.
	public double EscortShare { get; init; } = 1 / 3.0;

	// The parameters from "Name=value,Name=value", the others at their defaults.
	// These parameters with some set otherwise, as `Name=value,Name=value`.
	public StrongParameters With(string text)
	{
		var parameters = this with { };
		foreach (var pair in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = pair.Split('=');
			var property =
				typeof(StrongParameters).GetProperty(parts[0])
				?? throw new ArgumentException($"No parameter is named {parts[0]}.");
			property.SetValue(
				parameters,
				property.PropertyType.IsEnum
					? Enum.Parse(property.PropertyType, parts[1], ignoreCase: true)
					: Convert.ChangeType(
						parts[1],
						property.PropertyType,
						System.Globalization.CultureInfo.InvariantCulture
					)
			);
		}

		return parameters;
	}

	public static StrongParameters Parse(string text) => new StrongParameters().With(text);
}
