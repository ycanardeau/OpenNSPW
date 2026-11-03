namespace OpenNspw;

// What the host's UI shows of the battle and the setting screens that the game writes as text, or works out, as it draws
// them (docs/UI.md). Not part of the original. On the game's thread, as it shows a frame.
public unsafe partial class Nspw
{
	// What the game writes in the screen's top left corner on any screen, as UpdateFrame does: that the players are out
	// of step, or that the rival is gone.
	public string[] ReadNotices()
	{
		List<string> notices = [];
		if (AreUnitsOutOfSync)
			notices.Add("ユニットデータ同期異常、ゲームを中断されたし。");
		if (IsTickOutOfSync)
			notices.Add("プログラム同期異常、ゲームを中断されたし。");
		if (IsRandomOutOfSync)
			notices.Add("ランダム同期異常、ゲームを中断されたし。");
		if (ActivePlayerCount != 2 && !IsSingleplayer)
			notices.Add("接続相手がいなくなりました。");
		return [.. notices];
	}

	// The player's and the rival's chat, each while the game shows it (DrawMyChat, DrawRivalChat), or empty.
	public (string Mine, string Rival) ReadChat()
	{
		Array128<byte> text = default;
		var mine = MyChatDisplayTime != 0 ? Decode(text, wsprintf(text, MyChat)) : "";
		var rival = RivalChatDisplayTime != 0 ? Decode(text, wsprintf(text, RivalChat)) : "";
		return (mine, rival);
	}

	// The sprite's column of a unit facing a direction: one of eight, as DrawUnit sets SpriteColumn as it draws it.
	public static int SpriteColumnOf(Angle direction)
	{
		return (int)(direction.Degrees / 22.5) switch
		{
			0 or 15 => 2,
			1 or 2 => 1,
			3 or 4 => 0,
			5 or 6 => 7,
			7 or 8 => 6,
			9 or 10 => 5,
			11 or 12 => 4,
			13 or 14 => 3,
			_ => 2,
		};
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
	public void ReadSight(
		WorldPosition camera,
		bool seesAll,
		Span<bool> tiles,
		out int firstColumn,
		out int firstRow,
		Span<bool> effects
	)
	{
		firstColumn =
			(int)((camera.X - World.Left) / Renderer.Sprites[SpriteId.MapTiles].Size.Width)
			- SightMargin;
		firstRow =
			(int)((World.Top - camera.Y) / Renderer.Sprites[SpriteId.MapTiles].Size.Height)
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
						|| IsEditingMap
						|| Result != GameResult.None
						|| MapTiles[row][column] != 0
						|| IsSeaTileSeen(column, row)
					);
			}
		}

		for (var m = 1; m < EFFECT_MAX; m++)
		{
			ref var effect = ref Effects[m];
			effects[m] =
				effect.Layer != EffectLayer.None
				&& (
					seesAll
					|| (
						Result != GameResult.None
						&& (effect.Layer == EffectLayer.Upper || Rules.ResultShowsEveryLowerEffect)
					)
					|| (effect.Layer == EffectLayer.Lower && effect.Animation == 3)
					|| IsEffectSeen(effect.Position)
				);
		}
	}

	// What the player sees on and around the battlefield, from the game's camera.
	public void ReadSight(
		Span<bool> tiles,
		out int firstColumn,
		out int firstRow,
		Span<bool> effects
	)
	{
		ReadSight(CameraPosition, false, tiles, out firstColumn, out firstRow, effects);
	}

	// Whether a left click on each unit counts (CanClickUnit), unit 0 being the land where the selected transport lands.
	public void ReadClickable(Span<bool> clickable)
	{
		for (var m = 0; m <= MaxUnitId; m++)
			clickable[m] = CanClickUnit(ref Units[m]);
	}

	// Whether each unit is a parked plane shown on the deck in the sidebar (DrawUnit, IsOnShownDeck).
	public void ReadShownOnDeck(Span<bool> shown)
	{
		for (var m = 0; m <= MaxUnitId; m++)
		{
			ref var unit = ref Units[m];
			shown[m] =
				unit.IsUsed
				&& (unit.Side == LocalSide || unit.IsFound)
				&& unit.Category == UnitCategory.Plane
				&& unit.PlaneState == UnitState.Parked
				&& Renderer.IsOnShownDeck(ref unit);
		}
	}

	// The sheet each unit is drawn from on the battlefield and on the deck (GetUnitSprites).
	public void ReadUnitSprites(Span<SpriteId> sprites)
	{
		for (var m = 0; m <= MaxUnitId; m++)
			sprites[m] = Renderer.GetUnitSprites(ref Units[m]);
	}

	// The item of the combat menu in effect for the selected unit, as the host's UI shows it chosen: a ship's supply while
	// it is supplying, a flying plane's mode, a parked plane's arms (ListCombatMenu), or none. The game's own choice
	// (CombatMenuSelection) is set as an order is given but not as it ends, such as a ship's supply when it is done.
	public CombatMenuItem ReadActiveCombatMenuItem()
	{
		ref var unit = ref Units[SelectedUnit];
		if (SelectedUnit == 0)
			return CombatMenuItem.None;

		if (unit.Category == UnitCategory.Ship)
			return unit.IsSupplying ? CombatMenuItem.Supply : CombatMenuItem.None;

		if (unit.PlaneState == UnitState.Flying)
			return (CombatMenuItem)unit.Mode;

		return unit.Ammo == 0 ? (CombatMenuItem)FireKind.Unarmed : (CombatMenuItem)unit.Weapon;
	}

	// Whether the selected unit is shown, so that its path is drawn and a click gives it a destination or a right click
	// deselects it (DrawUnitMarks): a parked plane only on the deck shown in the sidebar (IsOnShownDeck).
	public bool IsSelectedShown()
	{
		ref var unit = ref Units[SelectedUnit];
		return SelectedUnit != 0
			&& unit.IsUsed
			&& (
				!(unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
				|| Renderer.IsOnShownDeck(ref unit)
			)
			&& !IsEditingMap;
	}

	// Whether unit m can be in the formation of the leader, as a click on it with the leader selected puts it there
	// (SelectFriendlyTarget): the player's, of the same category, and neither of them a base. A plane does not take a
	// carrier or an air base, which a click would make it land on, nor a ship a plane; a parked plane takes the planes
	// parked on the deck shown, and a flying one those flying.
	public bool IsFormationMate(int leader, int m)
	{
		ref var unit = ref Units[m];
		var isParked = unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked;
		var isLeaderParked =
			Units[leader].Category == UnitCategory.Plane
			&& Units[leader].PlaneState == UnitState.Parked;
		return m != leader
			&& unit.IsUsed
			&& unit.Side == LocalSide
			&& unit.Category == Units[leader].Category
			&& isParked == isLeaderParked
			&& (!isParked || Renderer.IsOnShownDeck(ref unit))
			&& !(unit.Kind >= UnitKind.AirBase && unit.Kind <= UnitKind.Fortress)
			&& !(Units[leader].Kind >= UnitKind.AirBase && Units[leader].Kind <= UnitKind.Fortress);
	}

	// Whether a left click on the battlefield gives the selected unit a destination (DrawPathAndSetDestination).
	public bool ReadCanSetDestination()
	{
		return IsSelectedShown() && CanSetDestination(ref Units[SelectedUnit]);
	}
}
