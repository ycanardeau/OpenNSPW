namespace OpenNspw;

// What the host's UI shows of the battle and the setting screens that the game writes as text, or works out, as it draws
// them (docs/UI.md). Not part of the original. On the game's thread, as it shows a frame.
public unsafe partial class Nspw
{
	private static string Decode(ReadOnlySpan<byte> text, int length)
	{
		return ShiftJis.GetString(text[..Math.Clamp(length, 0, text.Length)]);
	}

	// The texts of the unit whose information the sidebar shows (WriteUnitDetails), if it shows one: its kind, its damage
	// or building time, its ammunition and its fuel.
	public string[] ReadUnitDetails()
	{
		if( UnitInfoPanel[0]==0 )
			return [];

		Array8<int> len = default;
		Array8<Array128<byte>> ach = default;
		WriteUnitDetails(ref len, ref ach);
		return [Decode(ach[0], len[0]), Decode(ach[1], len[1]), Decode(ach[2], len[2]), Decode(ach[3], len[3])];
	}

	// The name of the reinforcement chosen in the supply panel (WriteSupplyTargetName).
	public string ReadSupplyTargetName()
	{
		Array8<int> len = default;
		Array8<Array128<byte>> ach = default;
		WriteSupplyTargetName(ref len, ref ach);
		return Decode(ach[0], len[0]);
	}

	// Whether the last frame showed the chosen reinforcement's picture (UpdateUnitInfo): in every frame of a battle but
	// the map editor's, and while one is coming, in every other frame.
	public bool IsSupplyTargetShown;

	// The frame of the chosen reinforcement's picture on the sheet of the player's units (ChooseSupplyTargetFrame): its
	// row and its column.
	public (int Row, int Column) ReadSupplyTargetFrame()
	{
		var m = 0;
		ChooseSupplyTargetFrame(ref m, out var n);
		return (m, n);
	}

	// Whether the chosen reinforcement can be requested, or why not (GetSupplyRequestState).
	public SupplyRequestState ReadSupplyRequestState()
	{
		return GetSupplyRequestState();
	}

	// The scenarios of the setting screen's list, on the page of the scenario chosen (ListScenarioNames).
	public string[] ReadScenarioNames()
	{
		Array12<int> len = default;
		Array12<Array128<byte>> ach = default;
		var m = -1;
		ListScenarioNames(ref m, ref len, ref ach);
		return [.. Enumerable.Range(0, m + 1).Select(n => Decode(ach[n], len[n]))];
	}

	// What the setting screen says about the scenario chosen, a line each, the victory conditions last
	// (DrawScenarioDescription).
	public string[] ReadScenarioDescription()
	{
		Array12<int> len = default;
		Array12<Array128<byte>> ach = default;
		DrawScenarioDescription(ref len, ref ach);
		return [.. Enumerable.Range(1, 6).Select(n => Decode(ach[n], len[n]))];
	}

	// The rule of the reinforcements, in the setting screen's words (WriteArrivalControl).
	public string ReadArrivalControl()
	{
		Array12<int> len = default;
		Array12<Array128<byte>> ach = default;
		WriteArrivalControl(ref len, ref ach);
		return Decode(ach[0], len[0]);
	}

	// The result of the battle, as the game writes it over the battlefield once it is over (WriteResult), or none.
	public string[] ReadResult()
	{
		if( Result==GameResult.None )
			return [];

		Array5<int> len = default;
		Array5<Array128<byte>> ach = default;
		WriteResult(ref len, ref ach);
		return [Decode(ach[0], len[0]), Decode(ach[1], len[1]), Decode(ach[2], len[2])];
	}

	// What the game writes in the screen's top left corner on any screen, as UpdateFrame does: that the players are out
	// of step, or that the rival is gone.
	public string[] ReadNotices()
	{
		List<string> notices = [];
		if( AreUnitsOutOfSync )
			notices.Add("ユニットデータ同期異常、ゲームを中断されたし。");
		if( IsTickOutOfSync )
			notices.Add("プログラム同期異常、ゲームを中断されたし。");
		if( IsRandomOutOfSync )
			notices.Add("ランダム同期異常、ゲームを中断されたし。");
		if( ActivePlayerCount!=2 && !IsSingleplayer )
			notices.Add("接続相手がいなくなりました。");
		return [.. notices];
	}

	// The player's and the rival's chat, each while the game shows it (DrawMyChat, DrawRivalChat), or empty.
	public (string Mine, string Rival) ReadChat()
	{
		Array128<byte> ach = default;
		var mine = MyChatDisplayTime!=0 ? Decode(ach, wsprintf(ach, MyChat)) : "";
		var rival = RivalChatDisplayTime!=0 ? Decode(ach, wsprintf(ach, RivalChat)) : "";
		return (mine, rival);
	}

	// The sprite's column of a unit facing a direction: one of eight, as DrawUnit sets SpriteColumn as it draws it.
	public static int SpriteColumnOf(double direction)
	{
		switch( (int)(direction/22.5) )
			{
			case 0: case 15:	return 2;
			case 1: case 2:		return 1;
			case 3: case 4:		return 0;
			case 5: case 6:		return 7;
			case 7: case 8:		return 6;
			case 9: case 10:	return 5;
			case 11: case 12:	return 4;
			case 13: case 14:	return 3;
			default:			return 2;
			}
	}

	// How far a unit sees an effect, as UpdateLowerEffects and UpdateUpperEffects test it: less while it is built.
	private int EffectSightOf(ref Unit unit)
	{
		int size=0;
		switch( unit.Kind )
			{
			case UnitKind.Battleship:		size=BB1_SIGHT;		break;
			case UnitKind.Cruiser:		size=CA1_SIGHT;		break;
			case UnitKind.Destroyer:		size=DD1_SIGHT;		break;
			case UnitKind.Submarine:		size=SS1_SIGHT;		break;
			case UnitKind.Carrier:		size=CV1_SIGHT;		break;
			case UnitKind.LightCarrier:		size=CVL1_SIGHT;	break;
			case UnitKind.Transport:		size=TR1_SIGHT;		break;
			case UnitKind.Fighter:		size=FT1_SIGHT;		break;
			case UnitKind.Attacker:		size=AT1_SIGHT;		break;
			case UnitKind.Bomber:		size=BM1_SIGHT;		break;
			case UnitKind.AirBase: case UnitKind.NavalBase:		size=AP_SIGHT;		break;
			case UnitKind.City:		size=CT1_SIGHT;		break;
			case UnitKind.Mine:		size=MN1_SIGHT;		break;
			case UnitKind.InfantryBase:		size=GF1_SIGHT;		break;
			case UnitKind.Pillboxes:		size=GF2_SIGHT;		break;
			case UnitKind.Fortress:		size=GF3_SIGHT;		break;
			}

		if( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress && unit.BuildTime!=0 )
			{
			// 工事中は視界を制限
			size=FT1_SIGHT/2;
			}

		return size;
	}

	// Where a unit looks from: a fighter a little ahead of itself (FT_EYE).
	private (double X, double Y) EyeOf(ref Unit unit)
	{
		var x=unit.Position.X;
		var y=unit.Position.Y;
		if( unit.Kind==UnitKind.Fighter )
			{
			x+=CosDegrees(unit.Direction)*FT_EYE;
			y+=SinDegrees(unit.Direction)*FT_EYE;
			}

		return (x, y);
	}

	// Whether one of the player's units, but parked planes, sees an effect at a point (UpdateLowerEffects).
	private bool IsEffectSeen(WorldPosition position)
	{
		for( var n=1; n<=MaxUnitId; n++ )
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.Side==LocalSide && unit.PlaneState!=UnitState.Parked )
				{
				var (x, y) = EyeOf(ref unit);
				if( Distance(x-position.X, y-position.Y)<=EffectSightOf(ref unit) )
					return true;
				}
			}

		return false;
	}

	// Whether one of the player's units, but parked planes, sees the sea tile (column, row), as DrawMapTiles tests it
	// (GetDetectionSize).
	private bool IsSeaTileSeen(int column, int row)
	{
		for( var f=1; f<=MaxUnitId; f++ )
			{
			ref var unit = ref Units[f];
			if( unit.IsUsed && unit.Side==LocalSide && unit.PlaneState!=UnitState.Parked )
				{
				var (x, y) = EyeOf(ref unit);
				x=x-((column*Sprites[SpriteId.MapTiles].Width)-MAP_RIGHT);
				y=y-(MAP_TOP-(row*Sprites[SpriteId.MapTiles].Height));
				if( Distance(x, y)<=(double)GetDetectionSize(f,0) )
					return true;
				}
			}

		return false;
	}

	// The tiles around the 11 by 11 of the battlefield that ReadSight reads too, on each side: the host's UI shows the
	// battlefield ahead of the game while it is dragged (ScrollAhead), so it shows tiles the game does not draw.
	public const int SightMargin = 2;

	// The tiles a side of what ReadSight reads.
	public const int SightSize = 11 + SightMargin * 2;

	// What the player sees on and around the battlefield: of the SightSize by SightSize tiles from (firstColumn,
	// firstRow), those drawn (DrawMapTiles: land, and the sea in sight), none off the map's tiles; and the effects drawn
	// (UpdateLowerEffects, UpdateUpperEffects).
	public void ReadSight(Span<bool> tiles, out int firstColumn, out int firstRow, Span<bool> effects)
	{
		firstColumn=(int)((CameraPosition.X-MAP_LEFT)/Sprites[SpriteId.MapTiles].Width)-SightMargin;
		firstRow=(int)((MAP_TOP-CameraPosition.Y)/Sprites[SpriteId.MapTiles].Height)-SightMargin;
		for( var n=0; n<SightSize; n++ )
			{
			for( var m=0; m<SightSize; m++ )
				{
				var (column, row) = (firstColumn+m, firstRow+n);
				tiles[n*SightSize+m] = column is >= 0 and <= 255 && row is >= 0 and <= 255
					&& ( IsEditingMap || Result!=GameResult.None || MapTiles[row][column]!=0 || IsSeaTileSeen(column, row) );
				}
			}

		for( var m=1; m<EFFECT_MAX; m++ )
			{
			ref var effect = ref Effects[m];
			effects[m] = effect.Layer!=EffectLayer.None
				&& ( (Result!=GameResult.None && (effect.Layer==EffectLayer.Upper || Rules.ResultShowsEveryLowerEffect)) || (effect.Layer==EffectLayer.Lower && effect.Animation==3) || IsEffectSeen(effect.Position) );
			}
	}

	// Whether a left click on each unit counts (CanClickUnit), unit 0 being the land where the selected transport lands.
	public void ReadClickable(Span<bool> clickable)
	{
		for( var m=0; m<=MaxUnitId; m++ )
			clickable[m]=CanClickUnit(ref Units[m]);
	}

	// Whether each unit is a parked plane shown on the deck in the sidebar (DrawUnit, IsOnShownDeck).
	public void ReadShownOnDeck(Span<bool> shown)
	{
		for( var m=0; m<=MaxUnitId; m++ )
			{
			ref var unit = ref Units[m];
			shown[m]=unit.IsUsed && (unit.Side==LocalSide || unit.IsFound) && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && IsOnShownDeck(ref unit);
			}
	}

	// The sheet each unit is drawn from on the battlefield and on the deck (GetUnitSprites).
	public void ReadUnitSprites(Span<SpriteId> sprites)
	{
		for( var m=0; m<=MaxUnitId; m++ )
			sprites[m]=GetUnitSprites(ref Units[m]);
	}

	// The item of the combat menu in effect for the selected unit, which the host's UI shows chosen
	// (ActiveCombatMenuItem).
	public CombatMenuItem ReadActiveCombatMenuItem()
	{
		return ActiveCombatMenuItem();
	}

	// Whether a left click on the battlefield gives the selected unit a destination (DrawPathAndSetDestination).
	public bool ReadCanSetDestination()
	{
		return IsSelectedShown() && CanSetDestination(ref Units[SelectedUnit]);
	}
}
