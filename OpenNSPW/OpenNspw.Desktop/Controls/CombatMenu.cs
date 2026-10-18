using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.Desktop.Controls;

// The selected unit's orders (the combat menu, UpdateCombatMenu) over its side's flag, and the switch of a carrier's or
// an air base's view between its flight deck and its hangar (UpdateDeckView): drawn from the snapshot, and clicked with
// the commands of the host's UI.
internal sealed class CombatMenu : Control
{
	// Where the menu is on the game's screen: from under the deck, with the switch, down to the radar.
	public static readonly DrawingRectangle Area = new(all_head.CMBT_WIDTH, 438, all_head.SCRN_WIDTH - all_head.CMBT_WIDTH, Radar.Top - 438);

	// The flag is 20 below the deck (ButtonBase), under the switch.
	private const int FlagTop = 20;

	private readonly GameSession _session;
	private readonly BattleArt _art;
	private readonly ActionButton _flightDeck;
	private readonly ActionButton _hangar;
	private readonly ActionButton[] _items;
	private CombatMenuItem[] _shown = [];
	private Side _side;

	// The wording of the original's buttons (Buttons1 of t3.bmp).
	private static string Label(CombatMenuItem item) => item switch
	{
		CombatMenuItem.Move => Texts.Move,
		CombatMenuItem.Slow => Texts.SlowMove,
		CombatMenuItem.Return => Texts.ReturnToBase,
		CombatMenuItem.Supply => Texts.RepairAndSupply,
		CombatMenuItem.ArmWithTorpedo => Texts.Torpedoes,
		CombatMenuItem.ArmWithBomb => Texts.Bombs,
		CombatMenuItem.Disarm => Texts.Unarmed,
		_ => item.ToString(),
	};

	private static bool HasDeckView(UnitKind kind) => kind is UnitKind.Carrier or UnitKind.LightCarrier or UnitKind.AirBase;

	// The right button deselects anywhere, as on the battlefield (DrawBattleArea): on the menu and on its buttons.
	private void RightClick(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Right)
			_session.Game.Commands.RightClick();
	}

	private ActionButton AddButton(UIFonts fonts, DrawingRectangle bounds, int fontSize, Action onClick)
	{
		var button = new ActionButton(fonts) { Bounds = bounds, FontSize = fontSize, Visible = false };
		button.Click += (_, _) => onClick();
		button.MouseDown += (_, e) => RightClick(e);
		Controls.Add(button);
		return button;
	}

	public CombatMenu(GameSession session, UIFonts fonts, BattleArt art)
	{
		_session = session;
		_art = art;
		BackColor = System.Drawing.Color.Transparent;

		var commands = session.Game.Commands;
		_flightDeck = AddButton(fonts, new(6, 2, 120, 17), 12, commands.ToggleDeckView);
		_hangar = AddButton(fonts, new(130, 2, 120, 17), 12, commands.ToggleDeckView);
		_flightDeck.Text = Texts.FlightDeck;
		_hangar.Text = Texts.Hangar;

		_items = new ActionButton[3];
		for (var i = 0; i < _items.Length; i++)
		{
			var index = i;
			_items[i] = AddButton(fonts, new(8, 26 + i * 34, 240, 30), 15, () => commands.ChooseCombatMenuItem(_shown[index]));
		}
	}

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(GameSnapshot snapshot)
	{
		_side = snapshot.LocalSide;

		var hasDeckView = HasDeckView((UnitKind)snapshot.UnitInfoPanel[0]);
		var isHangar = snapshot.UnitInfoPanel[1] != 0;
		_flightDeck.Visible = _hangar.Visible = hasDeckView;
		_flightDeck.IsPrimary = !isHangar;
		_hangar.IsPrimary = isHangar;

		_shown = [.. Enumerable.Range(0, Math.Min(snapshot.CombatMenuCount, _items.Length)).Select(i => snapshot.CombatMenu[i])];
		for (var i = 0; i < _items.Length; i++)
		{
			var button = _items[i];
			button.Visible = i < _shown.Length;
			if (button.Visible)
			{
				var isChosen = _shown[i] == snapshot.CombatMenuSelection;
				button.Text = Label(_shown[i]);
				button.IsPrimary = isChosen;
			}
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		RightClick(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		Painter.Fill(g, ClientRectangle, Theme.Navy);
		Painter.Fill(g, new(0, 0, Width, 1), Theme.Alpha(Theme.Line, 0.9f));

		// The flag, dimmed under the orders.
		var flag = _art.Flag(_side);
		var height = Math.Min(flag.Height, Height - FlagTop);
		var opacity = _shown.Length > 0 ? 0.3f : 0.85f;
		var region = new Texture2DRegion(flag, 0, 0, flag.Width, height);
		g.DrawImage(MonoGameImage.Create(region, Color.White * opacity, SamplerState.PointClamp), new DrawingPoint((Width - flag.Width) / 2, FlagTop));
	}
}
