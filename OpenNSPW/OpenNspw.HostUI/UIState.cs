using static OpenNspw.all_head;

namespace OpenNspw.HostUI;

// What the host's UI shows of the battle and the setting screens that the game works out as it draws them, put together
// from the game's queries (docs/UI.md, docs/Extensions.md). Not part of the original. On the game's thread, as it shows
// a frame.
public static class UIState
{
	private static string Decode(ReadOnlySpan<byte> text, int length)
	{
		return tchar.ShiftJis.GetString(text[..Math.Clamp(length, 0, text.Length)]);
	}

	// What the game writes in the screen's top left corner on any screen, as UpdateFrame does: that the players are out
	// of step, or that the rival is gone.
	public static string[] ReadNotices(this Nspw game)
	{
		List<string> notices = [];
		if (game.AreUnitsOutOfSync)
			notices.Add("ユニットデータ同期異常、ゲームを中断されたし。");
		if (game.IsTickOutOfSync)
			notices.Add("プログラム同期異常、ゲームを中断されたし。");
		if (game.IsRandomOutOfSync)
			notices.Add("ランダム同期異常、ゲームを中断されたし。");
		if (game.ActivePlayerCount != 2 && !game.IsSingleplayer)
			notices.Add("接続相手がいなくなりました。");
		return [.. notices];
	}

	// The player's and the rival's chat, each while the game shows it (DrawMyChat, DrawRivalChat), or empty.
	public static (string Mine, string Rival) ReadChat(this Nspw game)
	{
		Array128<byte> text = default;
		var mine = game.MyChatDisplayTime != 0 ? Decode(text, crt.wsprintf(text, game.MyChat)) : "";
		var rival =
			game.RivalChatDisplayTime != 0 ? Decode(text, crt.wsprintf(text, game.RivalChat)) : "";
		return (mine, rival);
	}

	// The tiles around the 11 by 11 of the battlefield that ReadSight reads too, on each side: the host's UI shows the
	// battlefield ahead of the game while it is dragged (ScrollAhead), so it shows tiles the game does not draw.
	public const int SightMargin = 2;

	// The tiles a side of what ReadSight reads.
	public const int SightSize = 11 + SightMargin * 2;

	// What the player sees on and around the battlefield seen from `camera`, which may be another than the game's, such
	// as a replay's viewer's: of the SightSize by SightSize tiles from (firstColumn, firstRow), those drawn (DrawMapTiles:
	// land, and the sea in sight), none off the map's tiles; and the effects drawn (UpdateLowerEffects,
	// UpdateUpperEffects). With `seesAll`, every tile and every effect, as a viewer that sees both sides.
	public static void ReadSight(
		this Nspw game,
		WorldPosition camera,
		bool seesAll,
		Span<bool> tiles,
		out int firstColumn,
		out int firstRow,
		Span<bool> effects
	)
	{
		firstColumn =
			(int)((camera.X - World.Left) / game.Renderer.Sprites[SpriteId.MapTiles].Size.Width)
			- SightMargin;
		firstRow =
			(int)((World.Top - camera.Y) / game.Renderer.Sprites[SpriteId.MapTiles].Size.Height)
			- SightMargin;
		for (var n = 0; n < SightSize; n++)
		{
			for (var m = 0; m < SightSize; m++)
			{
				var (column, row) = (firstColumn + m, firstRow + n);
				tiles[n * SightSize + m] =
					column is >= 0 and <= 255
					&& row is >= 0 and <= 255
					&& (
						seesAll
						|| game.IsEditingMap
						|| game.Result != GameResult.None
						|| game.MapTiles[row][column] != 0
						|| game.IsSeaTileSeen(column, row)
					);
			}
		}

		for (var m = 1; m < EFFECT_MAX; m++)
		{
			ref var effect = ref game.Effects[m];
			effects[m] =
				effect.Layer != EffectLayer.None
				&& (
					seesAll
					|| (
						game.Result != GameResult.None
						&& (
							effect.Layer == EffectLayer.Upper
							|| game.Rules.ResultShowsEveryLowerEffect
						)
					)
					|| (effect.Layer == EffectLayer.Lower && effect.Animation == 3)
					|| game.IsEffectSeen(effect.Position)
				);
		}
	}

	// What the player sees on and around the battlefield, from the game's camera.
	public static void ReadSight(
		this Nspw game,
		Span<bool> tiles,
		out int firstColumn,
		out int firstRow,
		Span<bool> effects
	)
	{
		game.ReadSight(game.CameraPosition, false, tiles, out firstColumn, out firstRow, effects);
	}

	// Whether a left click on each unit counts (CanClickUnit), unit 0 being the land where the selected transport lands.
	public static void ReadClickable(this Nspw game, Span<bool> clickable)
	{
		for (var m = 0; m <= game.MaxUnitId; m++)
			clickable[m] = game.CanClickUnit(m);
	}

	// Whether each unit is a parked plane shown on the deck in the sidebar (DrawUnit, IsOnShownDeck).
	public static void ReadShownOnDeck(this Nspw game, Span<bool> shown)
	{
		for (var m = 0; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			shown[m] =
				unit.IsUsed
				&& (unit.Side == game.LocalSide || unit.IsFound)
				&& unit.Category == UnitCategory.Plane
				&& unit.PlaneState == UnitState.Parked
				&& game.IsOnShownDeck(m);
		}
	}

	// The sheet each unit is drawn from on the battlefield and on the deck (GetUnitSprites).
	public static void ReadUnitSprites(this Nspw game, Span<SpriteId> sprites)
	{
		for (var m = 0; m <= game.MaxUnitId; m++)
			sprites[m] = game.Renderer.GetUnitSprites(ref game.Units[m]);
	}

	// The item of the combat menu in effect for the selected unit, as the host's UI shows it chosen: a ship's supply while
	// it is supplying, a flying plane's mode, a parked plane's arms (ListCombatMenu), or none. The game's own choice
	// (CombatMenuSelection) is set as an order is given but not as it ends, such as a ship's supply when it is done.
	public static CombatMenuItem ReadActiveCombatMenuItem(this Nspw game)
	{
		ref var unit = ref game.Units[game.SelectedUnit];
		if (game.SelectedUnit == 0)
			return CombatMenuItem.None;

		if (unit.Category == UnitCategory.Ship)
			return unit.IsSupplying ? CombatMenuItem.Supply : CombatMenuItem.None;

		if (unit.PlaneState == UnitState.Flying)
			return (CombatMenuItem)unit.Mode;

		return unit.Ammo == 0 ? (CombatMenuItem)FireKind.Unarmed : (CombatMenuItem)unit.Weapon;
	}

	// Whether the selected unit is shown, so that its path is drawn and a click gives it a destination or a right click
	// deselects it (DrawUnitMarks): a parked plane only on the deck shown in the sidebar (IsOnShownDeck).
	public static bool IsSelectedShown(this Nspw game)
	{
		ref var unit = ref game.Units[game.SelectedUnit];
		return game.SelectedUnit != 0
			&& unit.IsUsed
			&& (
				!(unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
				|| game.IsOnShownDeck(game.SelectedUnit)
			)
			&& !game.IsEditingMap;
	}

	// Whether unit m can be in the formation of the leader, as a click on it with the leader selected puts it there
	// (SelectFriendlyTarget): the player's, of the same category, and neither of them a base. A plane does not take a
	// carrier or an air base, which a click would make it land on, nor a ship a plane; a parked plane takes the planes
	// parked on the deck shown, and a flying one those flying.
	public static bool IsFormationMate(this Nspw game, int leader, int m)
	{
		ref var unit = ref game.Units[m];
		var isParked = unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked;
		var isLeaderParked =
			game.Units[leader].Category == UnitCategory.Plane
			&& game.Units[leader].PlaneState == UnitState.Parked;
		return m != leader
			&& unit.IsUsed
			&& unit.Side == game.LocalSide
			&& unit.Category == game.Units[leader].Category
			&& isParked == isLeaderParked
			&& (!isParked || game.IsOnShownDeck(m))
			&& !(unit.Kind >= UnitKind.AirBase && unit.Kind <= UnitKind.Fortress)
			&& !(
				game.Units[leader].Kind >= UnitKind.AirBase
				&& game.Units[leader].Kind <= UnitKind.Fortress
			);
	}

	// Whether a left click on the battlefield gives the selected unit a destination (DrawPathAndSetDestination).
	public static bool ReadCanSetDestination(this Nspw game)
	{
		return game.IsSelectedShown() && game.CanSetDestination(game.SelectedUnit);
	}
}
