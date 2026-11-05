using Aigamo.Saruhashi;
using OpenNspw.Replay;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.Desktop.Controls;

// The frames of a replay, as a track that fills as it plays, with a knob where it is: pointed at, it marks the point,
// and a press there, or a drag from it, goes to where it is let go (Seek, the fraction of the replay's frames).
internal sealed class ReplayTimeline : Control
{
	private const int TrackHeight = 8;
	private const int KnobWidth = 10;
	private const int KnobHeight = 20;

	private bool _isDragged;

	public double Played { get; set; }

	// The point under the mouse, as a fraction of the frames, while the mouse is over the track or drags the knob.
	public double? Pointed { get; private set; }

	public event EventHandler<double>? Seek;

	public ReplayTimeline()
	{
		BackColor = DrawingColor.Transparent;
	}

	private double FractionAt(int x) => Width > 0 ? Math.Clamp(x / (double)Width, 0, 1) : 0;

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		Pointed = FractionAt(e.Location.X);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		Pointed = null;
		_isDragged = false;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button != MouseButtons.Left)
			return;

		_isDragged = true;
		Pointed = FractionAt(e.Location.X);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button != MouseButtons.Left || !_isDragged)
			return;

		_isDragged = false;
		Seek?.Invoke(this, FractionAt(e.Location.X));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		var track = new DrawingRectangle(0, (Height - TrackHeight) / 2, Width, TrackHeight);
		Painter.Fill(g, track, Theme.Alpha(Theme.Line, 0.55f));
		var played = (int)(Width * Math.Clamp(Played, 0, 1));
		Painter.Fill(g, track with { Width = played }, Theme.Brass);
		Painter.Frame(g, track, Theme.Line);

		if (Pointed is { } pointed && !_isDragged)
			Painter.Fill(
				g,
				new((int)(Width * pointed) - 1, track.Y - 4, 2, TrackHeight + 8),
				Theme.Alpha(Theme.BrassLight, 0.7f)
			);

		var at = (int)(Width * (_isDragged && Pointed is { } dragged ? dragged : Played));
		var knob = new DrawingRectangle(
			Math.Clamp(at - KnobWidth / 2, 0, Width - KnobWidth),
			(Height - KnobHeight) / 2,
			KnobWidth,
			KnobHeight
		);
		Painter.Fill(g, knob, Theme.BrassLight);
		Painter.Frame(g, knob, Theme.Ink);
	}
}

// The bar of a replay being watched, along the battlefield's bottom (docs/Replays.md): where it is, how it differs from
// the battle recorded, its last note, and its controls: play or pause, a step of one frame, the speed, the timeline, the
// player's view or the viewer's own, both sides or the player's, and taking the battle over, in a single-player one.
internal sealed class ReplayBar : Control
{
	public const int BarHeight = 64;

	private readonly UIFonts _fonts;
	private readonly ReplayWatch _watch;
	private readonly ReplayPlayer _player;
	private readonly ActionButton _play;
	private readonly ActionButton _speed;
	private readonly ReplayTimeline _timeline;
	private readonly ActionButton _view;
	private readonly ActionButton _sides;
	private readonly LastText<double> _speedText = new(speed => $"{speed:0.#}×");
	private readonly LastText<long> _pointedText = new(milliseconds =>
		$"→ {Texts.Duration(milliseconds)}"
	);
	private readonly LastText<(int Frame, long Milliseconds)> _placeText;

	public event EventHandler<double>? Seek;

	public event EventHandler? TakeOverChosen;

	private ActionButton AddButton(int x, int width, Action onClick)
	{
		var button = new ActionButton(_fonts)
		{
			Bounds = new DrawingRectangle(x, 30, width, 28),
			FontSize = 13,
		};
		button.Click += (_, _) => onClick();
		Controls.Add(button);
		return button;
	}

	// Pauses the replay, or plays it again.
	private void TogglePause()
	{
		_watch.IsPaused = !_watch.IsPaused;
		_player.Pause(_watch.IsPaused);
	}

	// Plays one frame, paused.
	public void Step()
	{
		_watch.IsPaused = true;
		_player.Pause(true);
		_player.Step();
	}

	// Shows the replay as it is: on the UI's thread, each frame.
	public void UpdateFrom()
	{
		_play.Text = _watch.IsPaused ? Texts.Play : Texts.Pause;
		_speed.Text = _speedText.Of(_watch.Speed);
		_view.Text = _watch.View.FollowsPlayer ? Texts.PlayersView : Texts.FreeView;
		_view.IsPrimary = !_watch.View.FollowsPlayer;
		_sides.Text = Texts.BothSides;
		_sides.IsPrimary = _watch.View.SeesAll;
		_timeline.Played =
			_watch.Summary.Frames == 0 ? 0 : _player.Progress.Frame / (double)_watch.Summary.Frames;
	}

	public ReplayBar(UIFonts fonts, ReplayWatch watch, ReplayPlayer player)
	{
		_fonts = fonts;
		_watch = watch;
		_player = player;
		_placeText = new(place =>
			$"{Texts.ReplayFrame(place.Frame, watch.Summary.Frames)}  ·  {Texts.Duration(place.Milliseconds)} / {Texts.Duration(watch.Summary.Milliseconds)}"
		);
		Bounds = new DrawingRectangle(
			0,
			all_head.CMBT_HEIGHT - BarHeight,
			all_head.CMBT_WIDTH,
			BarHeight
		);
		BackColor = Theme.Alpha(Theme.Ink, 0.86f);

		_play = AddButton(8, 76, TogglePause);
		AddButton(90, 72, Step).Text = Texts.Step;
		_speed = AddButton(
			168,
			52,
			() =>
			{
				_watch.Speed = _watch.NextSpeed();
				_player.ChangeSpeed(_watch.Speed);
			}
		);
		_timeline = new ReplayTimeline { Bounds = new DrawingRectangle(230, 30, 230, 28) };
		_timeline.Seek += (_, fraction) => Seek?.Invoke(this, fraction);
		Controls.Add(_timeline);
		_view = AddButton(470, 108, () => _watch.View.FollowsPlayer = !_watch.View.FollowsPlayer);
		_sides = AddButton(584, 88, () => _watch.View.SeesAll = !_watch.View.SeesAll);
		if (player.Start.Mode == ReplayMode.Singleplayer)
			AddButton(678, 82, () => TakeOverChosen?.Invoke(this, EventArgs.Empty)).Text =
				Texts.TakeOver;
		UpdateFrom();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		var font = _fonts.Regular(13);
		var progress = _player.Progress;
		var place = _placeText.Of((progress.Frame, progress.Milliseconds));
		Painter.Text(g, place, font, Theme.Text, 10, 7);

		// The time at the point of the timeline under the mouse, where a click goes.
		if (_timeline.Pointed is { } pointed)
		{
			var to = _pointedText.Of((long)(pointed * _watch.Summary.Milliseconds));
			Painter.Text(
				g,
				to,
				font,
				Theme.BrassLight,
				_timeline.Bounds.Right - Painter.Measure(g, to, font).Width,
				7
			);
		}

		var notes = progress.Notes;
		var (status, color) =
			progress.Divergence is { } divergence ? (Texts.ReplayDiffers(divergence), Theme.Alert)
			: progress.HasEnded ? (Texts.ReplayOver, Theme.Muted)
			: notes.Count > 0 ? (notes[^1].Text, Theme.Muted)
			: ("", Theme.Muted);
		var left = 10 + Painter.Measure(g, place, font).Width + 16;
		Painter.Text(g, Painter.Fit(g, status, font, Width - left - 10), font, color, left, 7);
	}
}
