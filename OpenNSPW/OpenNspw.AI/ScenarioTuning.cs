namespace OpenNspw.AI;

// The settings found for a scenario, by the tuner (Tuner) or by benchmarks (docs/StrongAIResearch.md), over the strong
// player's own, as a player plays a scenario they know otherwise than the others: each confirmed on fresh start dates
// against the defaults.
public static class ScenarioTuning
{
	// A scenario's settings, as --params takes them, or null.
	public static string? For(int scenario) =>
		scenario switch
		{
			10 => "BomberWatch=true,Reengage=true",
			_ => null,
		};
}
