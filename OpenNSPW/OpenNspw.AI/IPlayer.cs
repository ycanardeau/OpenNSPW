namespace OpenNspw.AI;

// A player of the player's side of a single-player game (docs/StrongAI.md): it sees what the player's screen shows, and
// gives orders only with the commands of the host's UI (UICommands, through GameCommands).
public interface IPlayer
{
	// The player's name in the arena's table.
	string Name { get; }

	// Called once a frame of the battle, on the game's thread, between frames: the game waits until it returns.
	void Act(Nspw game);
}
