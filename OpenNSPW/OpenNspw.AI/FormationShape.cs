namespace OpenNspw.AI;

// How the strong player's carriers form with their escorts: as the ships stand (Line, as they mostly start, in a
// line); around the carrier, evenly, Radius from it (Ring); or across the way the enemy most likely is, 160° of a circle
// Radius from it (Screen). An experiment's choice: the arena's --formation.
public enum FormationKind
{
	Line,
	Ring,
	Screen,
}

public sealed record FormationShape(FormationKind Kind, double Radius);
