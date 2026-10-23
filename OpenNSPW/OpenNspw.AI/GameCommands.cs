using OpenNspw.HostUI;

namespace OpenNspw.AI;

// The host UI's commands of a game (UICommands, docs/UI.md), which a computer player gives its orders with, as the
// player's clicks: the game's own, if an extension of it already gives them, or else new ones added to it.
public static class GameCommands
{
	extension(Nspw game)
	{
		public UICommands Commands
		{
			get
			{
				foreach (var extension in game.Extensions.All)
				{
					if (extension is UICommands commands)
						return commands;
				}

				var added = new UICommands();
				game.Extensions.Add(added);
				return added;
			}
		}
	}
}
