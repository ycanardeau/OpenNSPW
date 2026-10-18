using static OpenNspw.all_head;

namespace OpenNspw.Tests.Lockstep;

// How a script gives a game's battle its clicks: with the mouse, as a player does (MouseInput), or with the commands of
// the host's UI (CommandInput, docs/UI.md). A click takes as many frames either way, and counts in the same frame.
internal abstract class BattleInput(LockstepRun run, LockstepGame game)
{
	protected LockstepRun Run { get; } = run;

	protected LockstepGame Game { get; } = game;

	// A left click on unit m on the battle area.
	public abstract void ClickUnit(int m);

	// A left click on the land at a point, where the selected transport lands.
	public abstract void ClickLand(WorldPosition position);

	// A left click on the battle area at a point, that gives the selected unit a destination.
	public abstract void SetDestination(WorldPosition position);

	public abstract void RightClick();

	// A click held on the minimap at cell (column, row).
	public abstract void ShowMinimapCell(int column, int row);

	// Moves the battle area so that its center is near a point, with the minimap (unit_info_cont.cpp).
	public void CenterOn(WorldPosition position)
	{
		var left = Math.Clamp(position.X - (CMBT_WIDTH / 2), MAP_LEFT, MAP_RIGHT - CMBT_WIDTH);
		var top = Math.Clamp(position.Y + (CMBT_HEIGHT / 2), MAP_BOTTOM + CMBT_HEIGHT, MAP_TOP);
		ShowMinimapCell((int)((left - MAP_LEFT) / 80), (int)((MAP_TOP - top) / 80));
	}
}

// Clicks with the mouse: the cursor on a point of the screen, then the button pressed and released.
internal sealed class MouseInput(LockstepRun run, LockstepGame game) : BattleInput(run, game)
{
	// Clicks a point of the world on the battle area.
	private void ClickAt(WorldPosition position)
	{
		var nspw = Game.Game;
		BattleScripts.Click(Run, Game, (int)(position.X - nspw.CameraPosition.X), (int)(nspw.CameraPosition.Y - position.Y));
	}

	public override void ClickUnit(int m)
	{
		ClickAt(Game.Game.Units[m].Position);
	}

	public override void ClickLand(WorldPosition position)
	{
		ClickAt(position);
	}

	public override void SetDestination(WorldPosition position)
	{
		ClickAt(position);
	}

	public override void RightClick()
	{
		var nspw = Game.Game;
		nspw.SetCursorPos(CMBT_WIDTH / 2, CMBT_HEIGHT / 2);
		nspw.PostMouseInput(1, true);
		Run.Frames(Game, 2);
		nspw.PostMouseInput(1, false);
		Run.Frames(Game, 2);
	}

	public override void ShowMinimapCell(int column, int row)
	{
		ref Sprite minimap = ref Game.Game.Sprites[SpriteId.Minimap];
		BattleScripts.Click(Run, Game, CMBT_WIDTH + 8 + 5 + column, CMBT_HEIGHT - minimap.Height + 8 + 5 + row);
	}
}

// Clicks with the commands of the host's UI. A command given between frames runs in the next, as the press of a click
// counts in the next; the minimap's runs a frame later, as a click there counts once the button is held.
internal sealed class CommandInput(LockstepRun run, LockstepGame game) : BattleInput(run, game)
{
	// Gives a command, and waits as long as a click takes: the press, then the release.
	private void Give(Action<Nspw.UICommands> command)
	{
		command(Game.Game.Commands);
		Run.Frames(Game, 2);
		Run.Frames(Game, 2);
	}

	// The point of the battle area where the mouse would click a point of the world.
	private (int X, int Y) ScreenPoint(WorldPosition position)
	{
		var camera = Game.Game.CameraPosition;
		return ((int)(position.X - camera.X), (int)(camera.Y - position.Y));
	}

	public override void ClickUnit(int m)
	{
		Give(c => c.ClickUnit(m));
	}

	public override void ClickLand(WorldPosition position)
	{
		var (x, y) = ScreenPoint(position);
		Give(c => c.ClickLand(x, y));
	}

	// The destination is the clicked point of the battle area, in the world, as DrawPathAndSetDestination takes it.
	public override void SetDestination(WorldPosition position)
	{
		var (x, y) = ScreenPoint(position);
		var camera = Game.Game.CameraPosition;
		Give(c => c.SetDestination(new WorldPosition(x + camera.X, camera.Y - y)));
	}

	public override void RightClick()
	{
		Give(c => c.RightClick());
	}

	public override void ShowMinimapCell(int column, int row)
	{
		Run.Frames(Game, 1);
		Game.Game.Commands.ShowMinimapCell(column, row, isPressed: true);
		Run.Frames(Game, 1);
		Run.Frames(Game, 2);
	}
}
