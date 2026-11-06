namespace OpenNspw;

// OpenNSPW's own options for a single-player battle (docs/Options.md), which neither original has: kept apart from
// BattleRules, which records what the originals do. Each is off unless the player turns it on, so that a battle stays
// 1.22's. The game holds the ones it plays by in Nspw.Options; network games have none, and play by NSPW NET's rules.
//
// Three kinds: those that change the battle's rules, which the game applies to 1.22's (Apply) as the battle starts, so
// that the battle's code does not know them; those that change the battle's setup, which the game applies once 1.22's
// scenario is set up; and those that change when the frames run, not the battle, which the host plays by with an
// extension.
public sealed record BattleOptions(
	// The effects draw from SharedRandom, as NSPW NET's do, rather than from rand, as 1.22's do, and the place of a plane
	// landed on deck still from rand: the battle draws no random number for the effects, and plays the same whether they
	// are drawn or not.
	bool SharedEffectRandom = false,
	// The battle plays the same at every speed (docs/SteadySpeed.md): the host plays it with the extension SteadySpeed.
	bool SteadySpeed = false,
	// The player commands the side that the computer commands in 1.22's scenario, and the computer the player's, in fleets
	// that the game forms for it (Nspw.SwitchSides).
	bool SwitchSides = false
)
{
	// Every option off: the battle as 1.22 plays it.
	public static readonly BattleOptions None = new();

	// The rules of a battle with these options: `rules`, changed by the options that change them.
	public BattleRules Apply(BattleRules rules) =>
		SharedEffectRandom ? rules with { EffectsDrawFromRand = false } : rules;
}
