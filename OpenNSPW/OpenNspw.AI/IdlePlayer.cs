namespace OpenNspw.AI;

// A player that gives no order: the baseline of what a scenario gives to doing nothing.
public sealed class IdlePlayer : IPlayer
{
	public string Name => "idle";

	public void Act(Nspw game) { }
}
