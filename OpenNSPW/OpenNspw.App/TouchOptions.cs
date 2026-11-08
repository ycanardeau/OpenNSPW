namespace OpenNspw.App;

// How a finger on the battlefield gives the selected unit a waypoint, rather than scrolling the map.
internal enum WaypointGesture
{
	// A tap on the sea; a drag scrolls.
	Tap,

	// Holding a finger still on the sea for a moment (TouchOptions.HoldSeconds); a tap there does nothing, and a drag
	// scrolls.
	Hold,

	// A tap on the sea; only two fingers scroll, and one dragged does not.
	TwoFingers,

	// A tap on the sea while the Move button is on, when a drag does not scroll; while it is off, a tap there does nothing,
	// and a drag scrolls.
	MoveButton,
}

// How a finger on the minimap moves the battlefield.
internal enum MinimapTouch
{
	// A box shows where it would go, moving a third of the finger's way, and it goes there once the finger is lifted.
	Aim,

	// A tap moves it there; a drag moves it a third of the finger's way as the finger goes.
	Jump,

	// As the mouse: it goes where the finger presses, and follows the finger.
	Follow,
}

// The choices of how a touch screen is played, to try them (TouchSettingsForm): each touch's size in dp, the
// window's pixels times the host's density (IAppHost.Density).
internal sealed record TouchOptions(
	WaypointGesture Waypoint = WaypointGesture.Tap,
	MinimapTouch Minimap = MinimapTouch.Aim,
	bool Glides = true,
	int SlopDp = 16,
	int ReachDp = 24,
	bool IsPanelLeft = true,
	bool PanelFollowsSelection = true
)
{
	// How long a finger is held still on the sea for a waypoint (WaypointGesture.Hold).
	public const float HoldSeconds = 0.35f;
}

// The touch options in use, which the battle reads as it takes each touch, so that a change takes effect at once; saved
// as they change (UISettings).
internal sealed class TouchSettings(TouchOptions options)
{
	public TouchOptions Options { get; private set; } = options;

	public void Change(TouchOptions options)
	{
		Options = options;
		UISettings.SaveTouch(options);
	}
}
