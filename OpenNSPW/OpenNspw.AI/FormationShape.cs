namespace OpenNspw.AI;

// How the strong player's carriers form with their escorts: as the ships stand (Line, as they mostly start, in a
// line); around the carrier, evenly, Radius from it (Ring); across the way the enemy most likely is, 160° of a circle
// Radius from it (Screen); or a ring for a group with no battleship, and the line for one with them (Auto), as the
// ring won more games of 4 and 5, whose groups have none, and no more of 6, 105 and 106 (StrongAIResearch.md). An
// experiment's choice: the arena's --formation.
public enum FormationKind
{
	Line,
	Ring,
	Screen,
	Auto,
}

public sealed record FormationShape(FormationKind Kind, double Radius)
{
	// A shape from "line", or a kind and a radius, "ring:400".
	public static FormationShape Parse(string text)
	{
		var parts = text.Split(':');
		return new(
			Enum.Parse<FormationKind>(parts[0], ignoreCase: true),
			parts.Length > 1
				? double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)
				: 0
		);
	}
}
