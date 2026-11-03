using static OpenNspw.all_head;

namespace OpenNspw.Desktop;

// What the viewer of a replay sees (docs/Replays.md): the battle from the recorded player's camera, or from a camera of
// the viewer's, which the battle does not follow; the recorded player's side only, or both. Set on the UI's thread,
// read on the game's as it copies its state (GameSnapshot).
internal sealed class ReplayView
{
	private readonly Lock _lock = new();
	private WorldPosition _camera;
	private bool _followsPlayer = true;
	private bool _seesAll;
	private bool _hasChanged;

	// A camera kept on the map, as the game keeps its own (MoveCamera, ShowMinimapCell).
	private static WorldPosition OnMap(double x, double y) =>
		new(
			Math.Clamp(x, World.Left, World.Right - CMBT_WIDTH),
			Math.Clamp(y, World.Bottom + CMBT_HEIGHT, World.Top)
		);

	// Whether the view is the recorded player's camera.
	public bool FollowsPlayer
	{
		get
		{
			lock (_lock)
				return _followsPlayer;
		}
		set
		{
			lock (_lock)
			{
				_followsPlayer = value;
				_hasChanged = true;
			}
		}
	}

	// Whether the view shows both sides: every unit, tile and effect, not only what the recorded player sees.
	public bool SeesAll
	{
		get
		{
			lock (_lock)
				return _seesAll;
		}
		set
		{
			lock (_lock)
			{
				_seesAll = value;
				_hasChanged = true;
			}
		}
	}

	// The camera that the view shows, when the game's is `game`.
	public WorldPosition CameraFor(WorldPosition game)
	{
		lock (_lock)
			return _followsPlayer ? game : _camera;
	}

	// The view moved by (dx, dy) pixels, as a drag moves the battlefield, from where it showed it (`from`): it no longer
	// follows the player.
	public void Move(int dx, int dy, WorldPosition from)
	{
		lock (_lock)
		{
			var camera = _followsPlayer ? from : _camera;
			_camera = OnMap(camera.X + dx, camera.Y - dy);
			_followsPlayer = false;
			_hasChanged = true;
		}
	}

	// The view moved to cell (column, row) of the minimap, as a click there moves the battlefield (ShowMinimapCell).
	public void ShowMinimapCell(int column, int row)
	{
		lock (_lock)
		{
			_camera = OnMap(
				World.Left + column / Nspw.MinimapCellSize * 80,
				World.Top - row / Nspw.MinimapCellSize * 80
			);
			_followsPlayer = false;
			_hasChanged = true;
		}
	}

	// Whether the view has changed since this was last asked.
	public bool TakeChange()
	{
		lock (_lock)
		{
			var hasChanged = _hasChanged;
			_hasChanged = false;
			return hasChanged;
		}
	}
}

// Copies the game's state again when the view of a replay changes, so that the viewer sees the change at once, also while
// the replay is paused and shows no new frame. Added before the replay, as it never answers whether a frame starts.
internal sealed class ReplayViewExtension(ReplayView view, DesktopPlatform platform)
	: IGameExtension
{
	private readonly ReplayView _view = view;
	private readonly DesktopPlatform _platform = platform;

	public bool? StartsFrame(Nspw game)
	{
		if (_view.TakeChange())
			_platform.CaptureAgain();
		return null;
	}
}
