namespace OpenNspw;

// NSPW 1.22's computer player: cpu_strtgy, rewritten from its machine code (docs/ComputerPlayer.md, cpu_strtgy).
public sealed class ComputerPlayer122 : IComputerPlayer
{
	public void Plan(Nspw game) => game.PlanComputerStrategy();
}
