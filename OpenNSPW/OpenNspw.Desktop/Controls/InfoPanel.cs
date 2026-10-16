using Aigamo.Saruhashi;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.Desktop.Controls;

// The column right of the deck: the selected unit's details (DrawUnitDetails), the game's speed and time, the supply
// panel (UpdateSupplyPanel) and the host's system menu (UpdateSystemMenu). Drawn from the snapshot, in the game's words,
// and clicked with the commands of the host's UI.
internal sealed class InfoPanel : Control
{
	// Where the column is on the game's screen: right of the deck, down to the combat menu.
	public static readonly DrawingRectangle Area = new(all_head.CMBT_WIDTH + 120, 0, all_head.SCRN_WIDTH - all_head.CMBT_WIDTH - 120, 438);

	private const int Left = 8;
	private const int SupplyTop = 168;
	private const int SystemTop = 350;

	private readonly GameSession _session;
	private readonly UiFonts _fonts;
	private readonly ActionButton _previous;
	private readonly ActionButton _next;
	private readonly ActionButton _request;
	private readonly ActionButton _toScenarioSelection;
	private readonly ActionButton _resume;
	private BattleSnapshot _snapshot = new();

	// The right button deselects anywhere, as on the battlefield (DrawBattleArea): on the column and on its buttons.
	private void RightClick(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Right)
			_session.Game.Commands.RightClick();
	}

	private ActionButton AddButton(DrawingRectangle bounds, string text, Action onClick)
	{
		var button = new ActionButton(_fonts) { Bounds = bounds, Text = text, FontSize = 13 };
		button.Click += (_, _) => onClick();
		button.MouseDown += (_, e) => RightClick(e);
		Controls.Add(button);
		return button;
	}

	public InfoPanel(GameSession session, UiFonts fonts)
	{
		_session = session;
		_fonts = fonts;
		BackColor = System.Drawing.Color.Transparent;

		var commands = session.Game.Commands;
		var width = Area.Width - Left * 2;
		_previous = AddButton(new(Left, SupplyTop + 86, width, 22), "◀ 前の機種", () => commands.SelectSupplyTarget(next: false));
		_next = AddButton(new(Left, SupplyTop + 112, width, 22), "次の機種 ▶", () => commands.SelectSupplyTarget(next: true));
		_request = AddButton(new(Left, SupplyTop + 140, width, 28), "", commands.RequestSupply);
		_request.IsPrimary = true;
		_toScenarioSelection = AddButton(new(Left, SystemTop + 22, width, 26), "シナリオ選択へ", commands.ToScenarioSelection);
		_resume = AddButton(new(Left, SystemTop + 54, width, 26), "リジューム", commands.Resume);
	}

	// The request's state, in the game's words (RequestSupply), or that one is coming (要求中).
	private static string RequestText(BattleSnapshot snapshot) => snapshot.SupplyCount != 0 ? "要求中" : snapshot.SupplyRequestState switch
	{
		SupplyRequestState.Available => "要求する",
		SupplyRequestState.ShortOfPoints => "割当点数不足",
		SupplyRequestState.UnitLimit => "ユニットリミット",
		_ => "要求制限",
	};

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(BattleSnapshot snapshot)
	{
		_snapshot = snapshot;

		// While a reinforcement is coming, the panel takes no choice nor request (UpdateSupplyPanel).
		var isChoosing = snapshot.SupplyCount == 0;
		_previous.Enabled = _next.Enabled = isChoosing;
		_request.Text = RequestText(snapshot);
		_request.Enabled = isChoosing && snapshot.SupplyRequestState == SupplyRequestState.Available;

		// The system menu is the host's, and takes a click while the rival is in the battle and the player can order.
		_toScenarioSelection.Visible = _resume.Visible = snapshot.IsHost;
		_toScenarioSelection.Enabled = _resume.Enabled = snapshot.RivalMode == GameMode.Battle && snapshot.CanOrder;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		RightClick(e);
	}

	private void PaintSection(Aigamo.Saruhashi.Graphics graphics, string title, int top)
	{
		Painter.Fill(graphics, new(Left, top, Width - Left * 2, 1), Theme.Alpha(Theme.Line, 0.9f));
		Painter.SpacedText(graphics, title, _fonts.Bold(10), Theme.Brass, Left, top + 6, 2);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		var snapshot = _snapshot;
		var width = Width - Left * 2;
		Painter.Fill(g, ClientRectangle, Theme.Navy);
		Painter.Fill(g, new(0, 0, 1, Height), Theme.Alpha(Theme.Line, 0.9f));

		Painter.SpacedText(g, "UNIT", _fonts.Bold(10), Theme.Brass, Left, 8, 2);
		if (snapshot.UnitDetails.Length == 4)
		{
			Painter.Text(g, Painter.Fit(g, snapshot.UnitDetails[0], _fonts.Bold(15), width), _fonts.Bold(15), Theme.BrassLight, Left, 24);
			for (var i = 1; i < 4; i++)
			{
				Painter.Text(g, Painter.Fit(g, snapshot.UnitDetails[i], _fonts.Regular(13), width), _fonts.Regular(13), Theme.Text, Left, 30 + i * 18);
			}
		}

		PaintSection(g, "STATUS", 110);
		Painter.Text(g, $"スピード  {snapshot.GameSpeed}", _fonts.Regular(13), Theme.Text, Left, 128);
		Painter.Text(g, $"経過時間  {snapshot.BattleTime}", _fonts.Regular(13), Theme.Text, Left, 146);

		PaintSection(g, "SUPPLY", SupplyTop);
		Painter.Text(g, $"補給割当  {snapshot.SupplyPoints}", _fonts.Regular(13), Theme.Text, Left, SupplyTop + 22);
		Painter.Text(g, Painter.Fit(g, snapshot.SupplyTargetName, _fonts.Bold(14), width), _fonts.Bold(14), Theme.BrassLight, Left, SupplyTop + 42);
		Painter.Text(g, $"{snapshot.SupplyCost} 点", _fonts.Regular(13), Theme.Muted, Left, SupplyTop + 62);

		if (snapshot.IsHost)
		{
			PaintSection(g, "SYSTEM", SystemTop);
		}
	}
}
