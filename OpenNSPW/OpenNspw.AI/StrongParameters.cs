namespace OpenNspw.AI;

// What a strike that hunts carriers attacks, once it finds the enemy: the nearest carrier (Carriers); the battleships
// and cruisers within 1500 of a carrier first, whose guns turn the attackers back, and the carriers once none is left
// (Escorts); or the cruisers only so (Cruisers), sparing the battleships' 41 HP.
public enum Targeting
{
	Carriers,
	Escorts,
	Cruisers,
}

// The numbers the strong player's tactics turn on, which the arena can set to tune them (--params, StrongAIResearch.md):
// the defaults are the ones chosen by reasoning, and checked on a few games each. Distances are in the map's units,
// times in frames of speed 1 (UnitFacts.Now), fuel in its units.
public sealed record StrongParameters
{
	// How far beyond the computer's strike range a carrier keeps from the enemy's carriers.
	public double KeepAwayMargin { get; init; } = 1000;

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

	// Whether a deck that hunts carriers keeps scouting while it knows where a carrier is (IsRemembered), its scouts kept
	// unarmed for it; or stops, and arms them to strike, until the carrier is lost.
	public bool KeepScouting { get; init; } = true;

	// What a strike that hunts carriers attacks.
	public Targeting Targeting { get; init; } = Targeting.Carriers;

	// The parameters from "Name=value,Name=value", the others at their defaults.
	public static StrongParameters Parse(string text)
	{
		var parameters = new StrongParameters();
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
}
