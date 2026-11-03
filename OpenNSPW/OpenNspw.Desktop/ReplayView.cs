using Aigamo.Otsuki.Actors;
using static OpenNspw.all_head;

namespace OpenNspw.Desktop;

// What the viewer of a replay sees, as the UI last set it: the recorded player's camera, or a camera of the viewer's, and
// whether it sees both sides. Immutable, so that the game reads it on its thread.
internal sealed record ReplayViewState(bool FollowsPlayer, bool SeesAll, WorldPosition Camera)
{
	// The camera that the view shows, when the game's is `game`.
	public WorldPosition CameraFor(WorldPosition game) => FollowsPlayer ? game : Camera;
}

// What the viewer of a replay sees (docs/Replays.md): the battle from the recorded player's camera, or from a camera of
// the viewer's, which the battle does not follow; the recorded player's side only, or both. The UI's, on its thread: each
// change is posted as a new state (State), which the game takes as it copies its state (GameSnapshot).
internal sealed class ReplayView
{
	private readonly Latest<ReplayViewState> _state = new(new ReplayViewState(true, false, default));
	private WorldPosition _camera;
	private bool _followsPlayer = true;
	private bool _seesAll;

	// The view as the UI last set it, from any thread.
	public ReplayViewState State => _state.Value;

	// A camera kept on the map, as the game keeps its own (MoveCamera, ShowMinimapCell).
	private static WorldPosition OnMap(double x, double y) =>
		new(
			Math.Clamp(x, World.Left, World.Right - CMBT_WIDTH),
			Math.Clamp(y, World.Bottom + CMBT_HEIGHT, World.Top)
		);

	private void Publish()
	{
		_state.Post(new ReplayViewState(_followsPlayer, _seesAll, _camera));
	}

	// Whether the view is the recorded player's camera.
	public bool FollowsPlayer
	{
		get => _followsPlayer;
		set
		{
			_followsPlayer = value;
			Publish();
		}
	}

	// Whether the view shows both sides: every unit, tile and effect, not only what the recorded player sees.
	public bool SeesAll
	{
		get => _seesAll;
		set
		{
			_seesAll = value;
			Publish();
		}
	}

	// The view moved by (dx, dy) pixels, as a drag moves the battlefield, from where it showed it (`from`): it no longer
	// follows the player.
	public void Move(int dx, int dy, WorldPosition from)
	{
		var camera = _followsPlayer ? from : _camera;
		_camera = OnMap(camera.X + dx, camera.Y - dy);
		_followsPlayer = false;
		Publish();
	}

	// The view moved to cell (column, row) of the minimap, as a click there moves the battlefield (ShowMinimapCell).
	public void ShowMinimapCell(int column, int row)
	{
		_camera = OnMap(
			World.Left + column / Nspw.MinimapCellSize * 80,
			World.Top - row / Nspw.MinimapCellSize * 80
		);
		_followsPlayer = false;
		Publish();
	}
}

// Takes the view of a replay as a frame starts (Seen), and copies the game's state again when it has changed, so that
// the viewer sees the change at once, also while the replay is paused and shows no new frame. Added before the replay, as
// it never answers whether a frame starts. On the game's thread.
internal sealed class ReplayViewExtension(ReplayView view, DesktopPlatform platform)
	: IGameExtension
{
	private readonly ReplayView _view = view;
	private readonly DesktopPlatform _platform = platform;

	// The view as the game took it last.
	public ReplayViewState Seen { get; private set; } = view.State;

	public bool? StartsFrame(Nspw game)
	{
		if (_view.State is var state && !ReferenceEquals(state, Seen))
		{
			Seen = state;
			_platform.CaptureAgain();
		}

		return null;
	}
}
