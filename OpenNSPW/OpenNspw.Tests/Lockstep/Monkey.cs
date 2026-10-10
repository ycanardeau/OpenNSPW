using static OpenNspw.all_head;

namespace OpenNspw.Tests.Lockstep;

// A player that clicks, drags and presses keys at random, from a seed, so that a script reaches code that nobody wrote
// a script for. It acts between rounds, by the game's frames, so it does the same thing every time.
internal sealed class Monkey(LockstepGame game, uint seed)
{
	// The keys the battle reads (input.cpp), except those that open a dialog or end the game.
	private static readonly uint[] Keys = [dinput.DIK_W, dinput.DIK_A, dinput.DIK_S, dinput.DIK_D, dinput.DIK_Q, dinput.DIK_E, dinput.DIK_Z, dinput.DIK_C, dinput.DIK_V, dinput.DIK_X];

	private readonly LockstepGame _game = game;
	private readonly Queue<(int Frames, Action Act)> _steps = new();
	private uint _seed = seed;
	private int _nextFrame;
	private bool _cancelling;
	private int _lastShip;

	// The MSVC rand() algorithm, which never changes, unlike System.Random.
	private int Next(int max)
	{
		_seed = _seed * 214013 + 2531011;
		return (int)((_seed >> 16) & 0x7FFF) % max;
	}

	private void Press(int button, int x, int y)
	{
		_steps.Enqueue((0, () =>
		{
			_game.Game.SetCursorPos(x, y);
			_game.Game.PostMouseInput(button, true);
		}));
		_steps.Enqueue((2, () => _game.Game.PostMouseInput(button, false)));
		_steps.Enqueue((2, () => { }));
	}

	private void Drag(int x1, int y1, int x2, int y2)
	{
		_steps.Enqueue((0, () =>
		{
			_game.Game.SetCursorPos(x1, y1);
			_game.Game.PostMouseInput(0, true);
		}));
		_steps.Enqueue((2, () => _game.Game.SetCursorPos(x2, y2)));
		_steps.Enqueue((2, () => _game.Game.PostMouseInput(0, false)));
		_steps.Enqueue((2, () => { }));
	}

	// The next of the side's ships that can be ordered, after the last one ordered.
	private int NextShip()
	{
		var nspw = _game.Game;
		for (var i = 1; i <= USA_SHIP_END; i++)
		{
			var m = ((_lastShip + i - 1) % USA_SHIP_END) + 1;
			ref var unit = ref nspw.unit[m];
			if (unit.used == nspw.your_side && unit.ctgry == UnitCategory.Ship && !(unit.kind >= UnitKind.AirBase && unit.kind <= UnitKind.Fortress) && unit.spry == 0 && unit.hp[0] > 0)
			{
				_lastShip = m;
				return m;
			}
		}

		return 0;
	}

	// The position of the enemy unit nearest to a unit, or the unit's own position if no enemy is left.
	private (double X, double Y) NearestEnemy(int m)
	{
		var nspw = _game.Game;
		ref var unit = ref nspw.unit[m];
		var best = (unit.x, unit.y);
		var bestDistance = double.MaxValue;
		for (var n = 1; n <= nspw.max_unit; n++)
		{
			ref var other = ref nspw.unit[n];
			if (other.used != 0 && other.used != nspw.your_side)
			{
				var distance = Math.Abs(other.x - unit.x) + Math.Abs(other.y - unit.y);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = (other.x, other.y);
				}
			}
		}

		return best;
	}

	// Clicks the left button at a point that is computed when the click happens, from the game's state then.
	private void PressAt(Func<(int X, int Y)> point)
	{
		_steps.Enqueue((0, () =>
		{
			var (x, y) = point();
			_game.Game.SetCursorPos(x, y);
			_game.Game.PostMouseInput(0, true);
		}));
		_steps.Enqueue((2, () => _game.Game.PostMouseInput(0, false)));
		_steps.Enqueue((2, () => { }));
	}

	// Orders one of the side's ships towards the nearest enemy, as a player does: deselects, moves the battle area to the
	// ship by clicking the minimap (unit_info_cont.cpp), clicks the ship, then clicks its destination.
	private void Order()
	{
		var m = NextShip();
		if (m == 0)
		{
			_steps.Enqueue((20, () => { }));
			return;
		}

		var nspw = _game.Game;
		Press(1, CMBT_WIDTH / 2, CMBT_HEIGHT / 2);
		PressAt(() =>
		{
			ref var unit = ref nspw.unit[m];
			ref var minimap = ref nspw.sprt[MAP_BASE];
			var left = Math.Clamp(unit.x - (CMBT_WIDTH / 2), MAP_LEFT, MAP_RIGHT - CMBT_WIDTH);
			var top = Math.Clamp(unit.y + (CMBT_HEIGHT / 2), MAP_BOTTOM + CMBT_HEIGHT, MAP_TOP);
			return (CMBT_WIDTH + 8 + 5 + (int)((left - MAP_LEFT) / 80), CMBT_HEIGHT - minimap.ht + 8 + 5 + (int)((MAP_TOP - top) / 80));
		});
		PressAt(() => ((int)(nspw.unit[m].x - nspw.cmbt_x), (int)(nspw.cmbt_y - nspw.unit[m].y)));
		PressAt(() =>
		{
			var (x, y) = NearestEnemy(m);
			var dx = Math.Clamp(x - nspw.cmbt_x, 10, CMBT_WIDTH - 10);
			var dy = Math.Clamp(nspw.cmbt_y - y, 10, CMBT_HEIGHT - 10);
			return ((int)dx, (int)dy);
		});
	}

	private void Hold(uint key, int frames)
	{
		_steps.Enqueue((0, () => _game.Game.PostKeyboardInput(key, true)));
		_steps.Enqueue((frames, () => _game.Game.PostKeyboardInput(key, false)));
	}

	private void Plan()
	{
		var choice = Next(100);
		if (choice < 30)
		{
			Order();
		}
		else if (choice < 40)
		{
			Press(0, Next(SCRN_WIDTH), Next(SCRN_HEIGHT));
		}
		else if (choice < 50)
		{
			Press(1, Next(SCRN_WIDTH), Next(SCRN_HEIGHT));
		}
		else if (choice < 55)
		{
			Drag(Next(CMBT_WIDTH), Next(CMBT_HEIGHT), Next(CMBT_WIDTH), Next(CMBT_HEIGHT));
		}
		else if (choice < 65)
		{
			Hold(Keys[Next(Keys.Length)], 2);
		}
		else if (choice < 85)
		{
			Hold(dinput.DIK_SPACE, 60);
		}
		else
		{
			_steps.Enqueue((20, () => { }));
		}
	}

	// A dialog that the monkey opened, such as "Exit Without Saving?", which stops the game until it is answered.
	private HWND? OpenDialog()
	{
		return _game.Game.windows.FirstOrDefault(w => w is { Destroyed: false, DialogProc: not null });
	}

	// Called between rounds: cancels a dialog the monkey opened, or does the next step once its frame has come.
	public void Act()
	{
		if (OpenDialog() is { } dialog)
		{
			if (!_cancelling)
			{
				_cancelling = true;
				_game.Post(g =>
				{
					g.ClickDlgItem(dialog, windef.IDCANCEL);
					_cancelling = false;
				});
			}

			return;
		}

		if (_game.FrameCount < _nextFrame)
		{
			return;
		}

		if (_steps.Count == 0)
		{
			Plan();
		}

		var (_, act) = _steps.Dequeue();
		act();
		_nextFrame = _game.FrameCount + (_steps.TryPeek(out var next) ? next.Frames : 0);
	}
}
