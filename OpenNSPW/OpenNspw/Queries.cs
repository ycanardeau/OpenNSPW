namespace OpenNspw;

// What an extension may ask the game (docs/Extensions.md): the game's own checks and texts, which read and write
// nothing, on the game's thread, at a point. Not part of the original.
public unsafe partial class Nspw
{
	// Whether a left click on unit m counts, as it and the selected unit are (CanClickUnit); unit 0 is the land where the
	// selected transport lands.
	public bool CanClickUnit(int m)
	{
		return CanClickUnit(ref Units[m]);
	}

	// Whether unit m is a plane parked on the deck that the sidebar shows (IsOnShownDeck).
	public bool IsOnShownDeck(int m)
	{
		return Renderer.IsOnShownDeck(ref Units[m]);
	}

	// Whether unit m can be given a destination (CanSetDestination).
	public bool CanSetDestination(int m)
	{
		return CanSetDestination(ref Units[m]);
	}

	// The items that the combat menu lists for the selected unit (ListCombatMenu).
	public CombatMenuItem[] ListCombatMenuItems()
	{
		Array6<int> menu = default;
		Array6<CombatMenuItem> menuItems = default;
		var n = ListCombatMenu(ref menu, ref menuItems);
		var items = new CombatMenuItem[n];
		for (var i = 0; i < n; i++)
			items[i] = menuItems[i];
		return items;
	}

	private static string Decode(ReadOnlySpan<byte> text, int length)
	{
		return ShiftJis.GetString(text[..Math.Clamp(length, 0, text.Length)]);
	}

	// The texts of the unit whose information the sidebar shows (WriteUnitDetails), if it shows one: its kind, its damage
	// or building time, its ammunition and its fuel.
	public string[] ReadUnitDetails()
	{
		if (UnitInfoPanel[0] == 0)
			return [];

		Array8<int> length = default;
		Array8<Array128<byte>> text = default;
		WriteUnitDetails(ref length, ref text);
		return
		[
			Decode(text[0], length[0]),
			Decode(text[1], length[1]),
			Decode(text[2], length[2]),
			Decode(text[3], length[3]),
		];
	}

	// The name of the reinforcement chosen in the supply panel (WriteSupplyTargetName).
	public string ReadSupplyTargetName()
	{
		Array8<int> length = default;
		Array8<Array128<byte>> text = default;
		WriteSupplyTargetName(ref length, ref text);
		return Decode(text[0], length[0]);
	}

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
		Array12<int> length = default;
		Array12<Array128<byte>> text = default;
		var m = -1;
		ListScenarioNames(ref m, ref length, ref text);
		return [.. Enumerable.Range(0, m + 1).Select(n => Decode(text[n], length[n]))];
	}

	// What the setting screen says about the scenario chosen, a line each, the victory conditions last
	// (DrawScenarioDescription).
	public string[] ReadScenarioDescription()
	{
		Array12<int> length = default;
		Array12<Array128<byte>> text = default;
		DrawScenarioDescription(ref length, ref text);
		return [.. Enumerable.Range(1, 6).Select(n => Decode(text[n], length[n]))];
	}

	// The rule of the reinforcements, in the setting screen's words (WriteArrivalControl).
	public string ReadArrivalControl()
	{
		Array12<int> length = default;
		Array12<Array128<byte>> text = default;
		WriteArrivalControl(ref length, ref text);
		return Decode(text[0], length[0]);
	}

	// The result of the battle, as the game writes it over the battlefield once it is over (WriteResult), or none.
	public string[] ReadResult()
	{
		if (Result == GameResult.None)
			return [];

		Array5<int> length = default;
		Array5<Array128<byte>> text = default;
		WriteResult(ref length, ref text);
		return [Decode(text[0], length[0]), Decode(text[1], length[1]), Decode(text[2], length[2])];
	}

	// How far a unit sees an effect, as UpdateLowerEffects and UpdateUpperEffects test it: less while it is built.
	private int EffectSightOf(ref Unit unit)
	{
		int size = 0;
		switch (unit.Kind)
		{
			case UnitKind.Battleship:
				size = BB1_SIGHT;
				break;
			case UnitKind.Cruiser:
				size = CA1_SIGHT;
				break;
			case UnitKind.Destroyer:
				size = DD1_SIGHT;
				break;
			case UnitKind.Submarine:
				size = SS1_SIGHT;
				break;
			case UnitKind.Carrier:
				size = CV1_SIGHT;
				break;
			case UnitKind.LightCarrier:
				size = CVL1_SIGHT;
				break;
			case UnitKind.Transport:
				size = TR1_SIGHT;
				break;
			case UnitKind.Fighter:
				size = FT1_SIGHT;
				break;
			case UnitKind.Attacker:
				size = AT1_SIGHT;
				break;
			case UnitKind.Bomber:
				size = BM1_SIGHT;
				break;
			case UnitKind.AirBase:
			case UnitKind.NavalBase:
				size = AP_SIGHT;
				break;
			case UnitKind.City:
				size = CT1_SIGHT;
				break;
			case UnitKind.Mine:
				size = MN1_SIGHT;
				break;
			case UnitKind.InfantryBase:
				size = GF1_SIGHT;
				break;
			case UnitKind.Pillboxes:
				size = GF2_SIGHT;
				break;
			case UnitKind.Fortress:
				size = GF3_SIGHT;
				break;
		}

		if (unit.Kind >= UnitKind.AirBase && unit.Kind <= UnitKind.Fortress && unit.BuildTime != 0)
		{
			// 工事中は視界を制限
			size = FT1_SIGHT / 2;
		}

		return size;
	}

	// Whether one of the player's units, but parked planes, sees an effect at a point (UpdateLowerEffects).
	public bool IsEffectSeen(WorldPosition position)
	{
		for (var n = 1; n <= MaxUnitId; n++)
		{
			ref var unit = ref Units[n];
			if (unit.IsUsed && unit.Side == LocalSide && unit.PlaneState != UnitState.Parked)
			{
				if ((EyeOf(ref unit) - position).Length <= EffectSightOf(ref unit))
					return true;
			}
		}

		return false;
	}

	// Whether one of the player's units, but parked planes, sees the sea tile (column, row), as DrawMapTiles tests it
	// (GetDetectionSize).
	public bool IsSeaTileSeen(int column, int row)
	{
		for (var f = 1; f <= MaxUnitId; f++)
		{
			ref var unit = ref Units[f];
			if (unit.IsUsed && unit.Side == LocalSide && unit.PlaneState != UnitState.Parked)
			{
				var tile = new WorldPosition(
					(column * Renderer.Sprites[SpriteId.MapTiles].Size.Width) - World.Right,
					World.Top - (row * Renderer.Sprites[SpriteId.MapTiles].Size.Height)
				);
				if ((EyeOf(ref unit) - tile).Length <= (double)GetDetectionSize(f, 0))
					return true;
			}
		}

		return false;
	}
}
