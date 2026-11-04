namespace OpenNspw.AI;

// The settings the tuner found for a scenario (Tuner, docs/StrongAIResearch.md), over the strong player's own, as a
// player plays a scenario he knows otherwise than the others: each confirmed on fresh start dates against the defaults.
public static class ScenarioTuning
{
	// A scenario's settings, as --params takes them, or null.
	public static string? For(int scenario) =>
		scenario switch
		{
			_ => null,
		};
}
