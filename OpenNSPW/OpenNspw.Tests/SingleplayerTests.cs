using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.all_head;

namespace OpenNspw.Tests;

// Whole single-player games: the port's battle with 1.22's scenarios, computer and victory conditions emulated
// (docs/Singleplayer.md).
public class SingleplayerTests
{
	public static TheoryData<int> Scenarios()
	{
		var data = new TheoryData<int>();
		foreach (var scenario in Singleplayer.Functions.Scenarios)
		{
			data.Add(scenario);
		}

		return data;
	}

	private static List<(int Unit, WorldPosition Position)> PositionsOf(Nspw game, Side side)
	{
		var positions = new List<(int, WorldPosition)>();
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (unit.Side == side && unit.Category == UnitCategory.Ship)
			{
				positions.Add((m, unit.Position));
			}
		}

		return positions;
	}

	// Keeps where a side's ships are as the game has set up its scenario, before the battle's first tick.
	private sealed class SetupCapture(Side side) : ISingleplayerTrace
	{
		private readonly Side _side = side;

		public List<(int Unit, WorldPosition Position)> Positions { get; private set; } = [];

		public (uint Start, uint Battle)? Seeds => null;

		public bool TakesInput => true;

		public int? TicksOfNextFrame() => null;

		public void ReachPlayerOrders(Nspw game)
		{
		}

		public void Reach(Nspw game, TracePoint point)
		{
			if (point == TracePoint.Seeded)
				Positions = PositionsOf(game, _side);
		}
	}

	private static List<(int Unit, WorldPosition Position)> SetUpAt(int scenario, Side side, int startDate)
	{
		var capture = new SetupCapture(side);
		using var run = new SingleplayerRun(scenario, game => game.Trace = capture, startDate);
		run.Frames(1);
		return capture.Positions;
	}

	// The computer's fleets start where 1.22's random setup puts them, from rand as the time the game started seeded it
	// (WinMain, as 1.22 does at 0x434456): the same for the same time, elsewhere at another. In scenario 4 the American
	// fleets start at random.
	[Fact]
	public void ScenariosAreSetUpFromTheTimeTheGameStarted()
	{
		var first = SetUpAt(4, Side.UnitedStates, 1_000_000_000);

		Assert.NotEmpty(first);
		Assert.Equal(first, SetUpAt(4, Side.UnitedStates, 1_000_000_000));
		Assert.NotEqual(first, SetUpAt(4, Side.UnitedStates, 1_000_000_001));
	}

	[Theory]
	[InlineData(3, Side.Japan)]
	[InlineData(4, Side.UnitedStates)]
	public void TheComputerOrdersAndMovesItsShips(int scenario, Side computer)
	{
		using var run = new SingleplayerRun(scenario);
		run.Frames(2);
		var game = run.Game;
		Assert.Equal(GameMode.Battle, game.Mode);
		Assert.Equal(computer, game.CpuSide);
		Assert.NotEqual(computer, game.LocalSide);
		var start = PositionsOf(game, computer);
		Assert.NotEmpty(start);
		var time = game.BattleTime;

		// cpu_strtgy's order stays in new_pp[0] until the next frame draws the battle.
		var orders = 0;
		for (var frame = 0; frame < 2000; frame++)
		{
			run.Frames(1);
			var unit = game.MoveOrders[0].Unit;
			if (unit != 0)
			{
				Assert.Equal(computer, game.Units[unit].Side);
				orders++;
			}
		}

		Assert.True(orders > 0, "The computer gave no move order.");
		Assert.Empty(run.Player.MessageBoxes);
		Assert.True(game.BattleTime < time, $"The time limit went from {time} to {game.BattleTime}.");
		var moved = start.Count(s => !game.Units[s.Unit].Position.Equals(s.Position));
		Assert.True(moved > 0, "None of the computer's ships moved.");
	}

	// decision shows the result itself, through the port's GDI stand-ins and wsprintf.
	[Fact]
	public void DecisionShowsTheResult()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		run.Game.Result = GameResult.JapanWon;
		run.Player.Texts.Clear();

		run.Frames(1);

		Assert.Contains("ゲーム終了", run.Player.Texts);
		Assert.Contains("日本海軍は勝利条件を達成した。", run.Player.Texts);
		Assert.Contains("日本海軍の勝利", run.Player.Texts);
	}

	// Every scenario plays for a while with the computer, without failing.
	[Theory]
	[MemberData(nameof(Scenarios))]
	public void ScenarioPlays(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		run.Frames(1000);

		Assert.Equal(GameMode.Battle, run.Game.Mode);
		Assert.Empty(run.Player.MessageBoxes);
	}

	// A single-player game reads 1.22's own files: its japan_off.dat, without the island that NSPW NET's has at tiles 56
	// to 64 and 164 to 180, and its sprite sheet, whose title is 599 by 387.
	[Fact]
	public void TheGameReads122sFiles()
	{
		using var run = new SingleplayerRun(202);
		run.Frames(2);
		var game = run.Game;

		Assert.Equal(2, game.CurrentMap);
		Assert.Equal(0, game.MapTiles[164][62]);
		Assert.Equal(599, game.Sprites[SpriteId.TitleBackground].Width);
		Assert.Equal("tst_cg1.bmp", game.SpriteSheetName);
	}

	// Orders go to every unit selected, as in 1.22, also to the units of the player's side that have the other side's
	// numbers: in scenario 303, the fighters taken with a Japanese air base (41 to 43) are sent to another base together.
	[Fact]
	public void OrdersGoToCapturedPlanes()
	{
		using var run = new SingleplayerRun(303);
		var game = run.Game;

		// Away from the edges, which scroll the battle area.
		game.SetCursorPos(CMBT_WIDTH / 2, CMBT_HEIGHT / 2);
		run.Frames(2);
		Assert.Equal(Side.UnitedStates, game.LocalSide);
		var airBase = game.Units[21].Position;
		for (var m = 41; m <= 43; m++)
		{
			ref var plane = ref game.Units[m];
			Assert.Equal(UnitKind.Fighter, plane.Kind);
			plane.Side = Side.UnitedStates;
			plane.PlaneState = UnitState.Flying;
			plane.Position = new WorldPosition(airBase.X - 300, airBase.Y + (m - 42) * 60);
			game.Selections[1][m] = (short)(m - 40);
		}

		game.SelectionCount = 3;
		game.SelectedUnit = 41;
		// The base in the middle of the battle area, under the cursor.
		game.CameraPosition = new WorldPosition(airBase.X - (CMBT_WIDTH / 2), airBase.Y + (CMBT_HEIGHT / 2));
		run.Frames(1);
		Assert.Equal(airBase.X - (CMBT_WIDTH / 2), game.CameraPosition.X);
		game.PostMouseInput(0, true);
		run.Frames(2);
		game.PostMouseInput(0, false);
		run.Frames(2);

		Assert.All([41, 42, 43], m => Assert.Equal(21, game.Units[m].Carrier));
	}

	// In scenario 303 the Americans' fighters of type 1, the ones taken from Japan's air bases, are drawn from Japan's
	// sheet, as 1.22 draws them, in the game's drawing and in the UI's (ReadUnitSprites).
	[Fact]
	public void CapturedFightersAreDrawnAsJapans()
	{
		using var run = new SingleplayerRun(303);
		run.Frames(2);
		var game = run.Game;
		ref var fighter = ref game.Units[41];
		Assert.Equal(UnitKind.Fighter, fighter.Kind);
		fighter.Side = Side.UnitedStates;
		fighter.Variant = 1;
		var sprites = new SpriteId[256];

		game.ReadUnitSprites(sprites);

		Assert.Equal(SpriteId.JapanUnits, sprites[41]);
		fighter.Variant = 0;
		game.ReadUnitSprites(sprites);
		Assert.Equal(SpriteId.UnitedStatesUnits, sprites[41]);
	}

	// A computer's ship supplies wherever it is, as in 1.22, whose computer starts supplying within 580 of its naval base:
	// NSPW NET's check for a naval base within 280 stopped it at once.
	[Fact]
	public void TheComputersShipsSupplyAwayFromTheBase()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var m = 0;
		for (var i = 1; i <= game.MaxUnitId && m == 0; i++)
		{
			ref var unit = ref game.Units[i];
			if (unit.Side == game.CpuSide && unit.Category == UnitCategory.Ship && unit.Kind <= UnitKind.LightCarrier && unit.MaxHp >= 2)
			{
				m = i;
			}
		}

		Assert.NotEqual(0, m);
		for (var i = 1; i <= game.MaxUnitId; i++)
		{
			ref var unit = ref game.Units[i];
			if (unit.Side == game.CpuSide && unit.Kind == UnitKind.NavalBase)
			{
				Assert.True(Math.Abs(unit.Position.X - game.Units[m].Position.X) > 280 || Math.Abs(unit.Position.Y - game.Units[m].Position.Y) > 280);
			}
		}

		game.Units[m].Hp = game.Units[m].MaxHp - 1;
		game.Units[m].SupplyTime = 1;
		run.Frames(300);
		Assert.Equal(game.Units[m].MaxHp, game.Units[m].Hp);
	}

	// Outside the map, 1.22 brings back the player's ships only: the computer's sail on, to their naval bases outside it,
	// such as scenario 304's, where they supply and get new carriers.
	[Fact]
	public void OnlyThePlayersShipsComeBackIntoTheMap()
	{
		using var run = new SingleplayerRun(4);
		run.Frames(2);
		var game = run.Game;
		int ShipOf(Side side)
		{
			for (var i = 1; i <= game.MaxUnitId; i++)
			{
				ref var unit = ref game.Units[i];
				if (unit.Side == side && unit.Kind is UnitKind.Cruiser or UnitKind.Destroyer)
				{
					return i;
				}
			}

			return 0;
		}

		var computers = ShipOf(game.CpuSide);
		var players = ShipOf(game.LocalSide);
		Assert.NotEqual(0, computers);
		Assert.NotEqual(0, players);
		var y = 0.0;
		foreach (var m in new[] { computers, players })
		{
			ref var ship = ref game.Units[m];
			ship.Position = new WorldPosition(all_head.MAP_RIGHT + 100, y);
			ship.PathX[0] = all_head.MAP_RIGHT + 800;
			ship.PathY[0] = y;
			ship.PathX[1] = all_head.MAP_RIGHT + 1;
			ship.EmergencyFlags[0] = 0;
			ship.IsStopping = false;
			ship.GroupLeader = 0;
			y += 400;
		}

		run.Frames(20);
		Assert.True(game.Units[computers].Position.X > all_head.MAP_RIGHT, $"The computer's ship is at x {game.Units[computers].Position.X}.");
		Assert.NotEqual(all_head.MAP_RIGHT - 40, game.Units[computers].EmergencyDestination.X);
		Assert.Equal(all_head.MAP_RIGHT - 40, game.Units[players].EmergencyDestination.X);
	}

	// The units that the battle creates in a single-player game, such as the bases that transports land, have NSPW
	// 1.22's data, from its set_unit_data, as the scenario's have; a full carrier still gets the planes added to it.
	[Fact]
	public void UnitsCreatedInTheBattleHave122sData()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;

		var battleship = game.AddUnit(Side.Japan, UnitKind.Battleship, 0, 0, 0);
		Assert.NotEqual(0, battleship);
		Assert.Equal(41, game.Units[battleship].MaxHp);
		Assert.Equal(9000, game.Units[battleship].FuelInterval);

		var carrier = game.AddUnit(Side.UnitedStates, UnitKind.Carrier, 0, 400, 0);
		Assert.NotEqual(0, carrier);
		Assert.Equal(41, game.Units[carrier].MaxHp);
		Assert.Equal(12, game.Units[carrier].Capacity);

		var planes = game.Units[carrier].PlaneCount;
		Assert.Equal(1, game.AddPlane(Side.UnitedStates, UnitKind.Attacker, 0, carrier, game.Units[carrier].Capacity + 2, FireKind.Bomb));
		Assert.Equal(planes + game.Units[carrier].Capacity + 2, game.Units[carrier].PlaneCount);
	}

	// Every attacker releases its bombs from 170 to 180 of its aim and speeds up, as in 1.22, where NSPW NET's Japanese
	// ones dive to 35 to 65; only the Americans' drop a second bomb.
	[Theory]
	[InlineData(Side.Japan, 175, true, 1)]
	[InlineData(Side.Japan, 50, false, 0)]
	[InlineData(Side.UnitedStates, 175, true, 2)]
	public void AttackersReleaseTheirBombsFrom170(Side side, int distance, bool releases, int bombs)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var other = side == Side.Japan ? Side.UnitedStates : Side.Japan;
		var target = game.AddUnit(other, UnitKind.Cruiser, 0, 0, 0);
		var carrier = game.AddUnit(side, UnitKind.Carrier, 0, -2000, 0);
		Assert.NotEqual(0, target);
		Assert.NotEqual(0, carrier);
		Assert.Equal(1, game.AddPlane(side, UnitKind.Attacker, 0, carrier, 1, FireKind.Bomb));
		var attacker = 0;
		for (var i = 1; i <= game.MaxUnitId; i++)
		{
			if (game.Units[i].IsUsed && game.Units[i].Kind == UnitKind.Attacker && game.Units[i].Carrier == carrier)
			{
				attacker = i;
			}
		}

		Assert.NotEqual(0, attacker);
		game.Units[target].Speed = 0;
		game.Units[target].IsFound = true;
		ref var unit = ref game.Units[attacker];
		unit.PlaneState = UnitState.Flying;
		unit.Position = new WorldPosition(-distance, 0);
		unit.Direction = 0;
		unit.Ammo = 1;
		unit.Target = target;
		var speed = unit.Speed;
		int Bombs()
		{
			var count = 0;
			for (var s = 1; s < all_head.FIRE_MAX; s++)
			{
				if (game.Fires[s].Target != 0 && game.Fires[s].Kind == FireKind.Bomb)
				{
					count++;
				}
			}

			return count;
		}

		var before = Bombs();
		game.FireWeapons(attacker, target, FireKind.Bomb);

		Assert.Equal(releases, game.Units[attacker].Ammo == 0);
		Assert.Equal(releases, game.Units[attacker].Speed > speed);
		Assert.Equal(bombs, Bombs() - before);
	}

	// A transport lands its cargo within 360 of its landing point, as in 1.22, not 160, and stays, empty.
	[Theory]
	[InlineData(300, true)]
	[InlineData(400, false)]
	public void TransportsLandFrom360(int distance, bool lands)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var transport = game.AddUnit(Side.Japan, UnitKind.Transport, 0, 0, 0);
		Assert.NotEqual(0, transport);
		ref var unit = ref game.Units[transport];
		unit.Direction = 0;
		unit.Ammo = 1;
		unit.LandingX = (int)unit.Position.X + distance;
		unit.LandingY = (int)unit.Position.Y;

		game.FireWeapons(transport, 0, FireKind.CargoInfantryBase);

		Assert.Equal(lands, game.Units[transport].Ammo == 0);
		Assert.True(game.Units[transport].IsUsed);
	}

	// The base a transport lands is complete at once, with its full HP, as in 1.22, where NSPW NET builds it over 8000
	// ticks or more, from a quarter of its HP.
	[Fact]
	public void LandedBasesAreCompleteAtOnce()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var transport = game.AddUnit(Side.Japan, UnitKind.Transport, 0, 0, 0);
		Assert.NotEqual(0, transport);
		ref var unit = ref game.Units[transport];
		unit.Direction = 0;
		unit.LandingX = (int)unit.Position.X + 100;
		unit.LandingY = (int)unit.Position.Y;
		var landing = new WorldPosition(unit.LandingX, unit.LandingY);
		var full = game.AddUnit(Side.Japan, UnitKind.Pillboxes, 0, -2000, 0);
		Assert.NotEqual(0, full);

		game.FireWeapons(transport, 0, FireKind.CargoPillboxes);

		var pillboxes = 0;
		for (var frame = 0; frame < 1000 && pillboxes == 0; frame++)
		{
			run.Frames(1);
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				if (m != full && game.Units[m].IsUsed && game.Units[m].Kind == UnitKind.Pillboxes && game.Units[m].Position.Equals(landing))
				{
					pillboxes = m;
				}
			}
		}

		Assert.NotEqual(0, pillboxes);
		Assert.Equal(0, game.Units[pillboxes].BuildTime);
		Assert.Equal(game.Units[full].MaxHp, game.Units[pillboxes].MaxHp);
		Assert.Equal(game.Units[pillboxes].MaxHp, game.Units[pillboxes].Hp);
	}

	// A transport lands an infantry base, pillboxes or a fortress, but not the naval bases and air bases that 1.22 does not
	// have. It stays, empty once it has landed.
	[Theory]
	[InlineData(FireKind.CargoInfantryBase, true)]
	[InlineData(FireKind.CargoNavalBase, false)]
	[InlineData(FireKind.CargoAirBase, false)]
	public void TransportsLandGroundUnitsOnly(FireKind cargo, bool lands)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var transport = game.AddUnit(Side.Japan, UnitKind.Transport, 0, 0, 0);
		Assert.NotEqual(0, transport);
		ref var unit = ref game.Units[transport];
		unit.Direction = 0;
		unit.Weapon = cargo;
		unit.Ammo = 1;
		unit.Target = game.MaxUnitId + 1;
		unit.LandingX = (int)unit.Position.X + 100;
		unit.LandingY = (int)unit.Position.Y;

		run.Frames(5);

		Assert.Equal(lands, game.Units[transport].Ammo == 0);
		Assert.True(game.Units[transport].IsUsed);
	}

	// A unit sees as far as in 1.22: a battleship 900, where NSPW NET's sees 800.
	[Theory]
	[InlineData(850, true)]
	[InlineData(950, false)]
	public void UnitsSeeAsFarAsIn122(int distance, bool found)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var battleship = game.AddUnit(game.LocalSide, UnitKind.Battleship, -9000, -7000, 0);
		var cruiser = game.AddUnit(game.CpuSide, UnitKind.Cruiser, -9000 + distance, -7000, 0);
		Assert.NotEqual(0, battleship);
		Assert.NotEqual(0, cruiser);

		game.Detect();

		Assert.Equal(found, (bool)game.Units[cruiser].IsFound);
	}

	// How far a submerged submarine can be found, as in 1.22: by a destroyer within 280, 140 above a third of its top
	// speed, and 50 more if the submarine is stopped; by a cruiser within 80; by a battleship not at all.
	[Theory]
	[InlineData(UnitKind.Destroyer, 0.0, 1.0, 280)]
	[InlineData(UnitKind.Destroyer, 1.0, 1.0, 140)]
	[InlineData(UnitKind.Destroyer, 0.0, 0.0, 330)]
	[InlineData(UnitKind.Cruiser, 0.0, 1.0, 80)]
	[InlineData(UnitKind.Battleship, 0.0, 1.0, 0)]
	public void SubmergedSubmarinesAreFoundAsIn122(UnitKind kind, double speed, double submarineSpeed, int size)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var ship = game.AddUnit(game.LocalSide, kind, -9000, -7000, 0);
		var submarine = game.AddUnit(game.CpuSide, UnitKind.Submarine, -8900, -7000, 0);
		Assert.NotEqual(0, ship);
		Assert.NotEqual(0, submarine);
		game.Units[ship].Speed = game.Units[ship].MaxSpeed * speed;
		game.Units[submarine].IsSubmerged = true;
		game.Units[submarine].Speed = submarineSpeed;

		Assert.Equal(size, game.GetDetectionSize(ship, submarine));
	}

	// 255 fires and 255 effects at most are in use, as in 1.22.
	[Fact]
	public void FiresAndEffectsAreLimitedTo255()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		for (var n = 1; n < 256; n++)
		{
			game.Fires[n].Target = 1;
			game.Effects[n].Layer = EffectLayer.Lower;
		}

		Assert.Equal(0, game.FindFreeFire());
		Assert.Equal(0, game.FindFreeEffect());
	}

	// In scenario 305 the American transports at Okinawa, stopped and empty, move away from the Japanese ground units
	// there at the start, as in 1.22, whose stopped ships flee every enemy unit within 900 but air bases, naval bases and
	// submarines; NSPW NET's flee no ground unit.
	[Fact]
	public void StoppedShipsFleeGroundUnits()
	{
		using var run = new SingleplayerRun(305);
		run.Frames(1);
		var game = run.Game;
		var start = new[] { game.Units[25].Position, game.Units[26].Position };
		Assert.All([25, 26], m => Assert.Equal(UnitKind.Transport, game.Units[m].Kind));

		run.Frames(100);

		Assert.All([25, 26], m => Assert.NotEqual(start[m - 25], game.Units[m].Position));
	}

	// Only the computer's destroyers hunt submarines, as in 1.22: one within 600 of a submarine found chases it for 600
	// ticks, and remembers it (em_flg[1]).
	[Theory]
	[InlineData(true, 500, true)]
	[InlineData(true, 700, false)]
	[InlineData(false, 500, false)]
	public void TheComputersDestroyersHuntSubmarines(bool computers, int distance, bool hunts)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var side = computers ? game.CpuSide : game.LocalSide;
		var destroyer = game.AddUnit(side, UnitKind.Destroyer, -9000, -7000, 0);
		var submarine = game.AddUnit(side == Side.Japan ? Side.UnitedStates : Side.Japan, UnitKind.Submarine, -9000 + distance, -7000, 0);
		Assert.NotEqual(0, destroyer);
		Assert.NotEqual(0, submarine);
		ref var sub = ref game.Units[submarine];
		sub.IsFound = true;
		sub.IsSubmerged = true;
		sub.ContactX = (int)sub.Position.X;
		sub.ContactY = (int)sub.Position.Y;

		game.SetShipEmergencyDestination(destroyer);

		Assert.Equal(hunts ? submarine : 0, game.Units[destroyer].EmergencyFlags[1]);
		Assert.Equal(hunts ? 600 : 0, game.Units[destroyer].EmergencyFlags[0]);
	}

	// A returning plane approaches its base whatever its kind, as in 1.22, where NSPW NET forgets a base that is not a
	// carrier or an air base.
	[Fact]
	public void ReturningPlanesApproachAnyBase()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var navalBase = game.AddUnit(Side.Japan, UnitKind.NavalBase, -9000, -7000, 0);
		var attacker = game.AddUnit(Side.Japan, UnitKind.Attacker, -8000, -7000, 0);
		Assert.NotEqual(0, navalBase);
		Assert.NotEqual(0, attacker);
		ref var plane = ref game.Units[attacker];
		plane.Carrier = navalBase;
		plane.PathX[0] = -8500;
		plane.PathX[1] = -8600;
		plane.PathX[2] = all_head.MAP_RIGHT + 1;

		game.UpdateLanding(attacker);

		Assert.Equal(navalBase, game.Units[attacker].Carrier);
	}

	// A moving unit burns its fuel 1 at a time, as 1.22 does with its integer gas[0], whatever its speed.
	[Fact]
	public void FuelBurnsOneAtATime()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var m = 0;
		for (var i = 1; i <= game.MaxUnitId && m == 0; i++)
		{
			ref var unit = ref game.Units[i];
			if (unit.IsUsed && unit.Category == UnitCategory.Ship && unit.Kind <= UnitKind.LightCarrier && unit.FuelInterval >= 1)
			{
				m = i;
			}
		}

		Assert.NotEqual(0, m);
		ref var ship = ref game.Units[m];
		var fuel = ship.Fuel;
		var interval = (int)ship.FuelInterval;
		// Until the next frame's two ticks reach the next burn.
		while (game.Tick % interval < interval - 2)
		{
			run.Frames(1);
		}

		fuel = game.Units[m].Fuel;
		game.Units[m].Speed = game.Units[m].MaxSpeed / 2;
		run.Frames(1);

		Assert.Equal(fuel - 1, game.Units[m].Fuel);
	}

	// A fighter shoots the flying plane it attacks, but not a transport, as in 1.22.
	[Theory]
	[InlineData(UnitKind.Attacker, true)]
	[InlineData(UnitKind.Transport, false)]
	public void FightersShootPlanesOnly(UnitKind kind, bool shoots)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var fighter = game.AddUnit(Side.Japan, UnitKind.Fighter, 0, 0, 0);
		var target = game.AddUnit(Side.UnitedStates, kind, 100, 0, 0);
		Assert.NotEqual(0, fighter);
		Assert.NotEqual(0, target);
		game.Units[fighter].PlaneState = UnitState.Flying;
		if (game.Units[target].Category == UnitCategory.Plane)
		{
			game.Units[target].PlaneState = UnitState.Flying;
		}

		game.Units[fighter].Target = target;
		var ammo = game.Units[fighter].Ammo;

		game.FireWeapons(fighter, 0, FireKind.Bullet);

		Assert.Equal(shoots, game.Units[fighter].Ammo < ammo);
	}

	// The player's fighter can be sent to attack a plane, but not a transport, as in 1.22.
	[Theory]
	[InlineData(UnitKind.Attacker, true)]
	[InlineData(UnitKind.Transport, false)]
	public void FightersAreOrderedToAttackPlanesOnly(UnitKind kind, bool attacks)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var fighter = game.AddUnit(game.LocalSide, UnitKind.Fighter, 0, 0, 0);
		var target = game.AddUnit(game.CpuSide, kind, 100, 0, 0);
		Assert.NotEqual(0, fighter);
		Assert.NotEqual(0, target);
		game.SelectOrders[0] = new SelectOrder { IsSet = true, SelectedUnit = (short)fighter, Unit = (short)target };

		game.ApplyOrders();

		Assert.Equal(attacks ? target : 0, game.Units[fighter].Target);
	}

	// A torpedo plane can be sent against any ground unit but an air base or a naval base, as in 1.22, which refuses
	// those two only; it still drops no torpedo on any of them.
	[Theory]
	[InlineData(UnitKind.InfantryBase, true)]
	[InlineData(UnitKind.Fortress, true)]
	[InlineData(UnitKind.AirBase, false)]
	[InlineData(UnitKind.NavalBase, false)]
	public void TorpedoPlanesAreOrderedAgainstGroundUnitsButBases(UnitKind kind, bool attacks)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var attacker = game.AddUnit(game.LocalSide, UnitKind.Attacker, 0, 0, 0);
		var target = game.AddUnit(game.CpuSide, kind, 100, 0, 0);
		Assert.NotEqual(0, attacker);
		Assert.NotEqual(0, target);
		game.Units[attacker].Weapon = FireKind.Torpedo;
		game.SelectOrders[0] = new SelectOrder { IsSet = true, SelectedUnit = (short)attacker, Unit = (short)target };

		game.ApplyOrders();

		Assert.Equal(attacks ? target : 0, game.Units[attacker].Target);
	}

	// A depth charge does 3 to 5 damage, as in 1.22 (0x416D1F), where NSPW NET's does 1 to 6.
	[Fact]
	public void DepthChargesDamageAsIn122()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		game.Fires[1].Kind = FireKind.AntiSubmarineBomb;
		var damages = Enumerable.Range(0, 1000).Select(_ => game.GetDamagePoints(1)).ToHashSet();

		Assert.Equal([3, 4, 5], damages.Order());
	}

	// A fire hits a surfaced submarine within 4 of it, as in 1.22 (0x40F9D6), where NSPW NET's hits within 8.
	[Fact]
	public void SurfacedSubmarinesAreHitAsIn122()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		ref var submarine = ref game.Units[1];
		submarine.Kind = UnitKind.Submarine;
		submarine.IsSubmerged = false;
		submarine.Direction = 0.0;
		game.Fires[1].Target = 1;

		bool Hits(double dx, double dy)
		{
			game.Fires[1].Position = new WorldPosition(game.Units[1].Position.X + dx, game.Units[1].Position.Y + dy);
			return game.CheckHit(1) != 0;
		}

		Assert.True(Hits(0, 0));
		Assert.True(Hits(5, 1));
		Assert.False(Hits(0, 3));
		Assert.False(Hits(7, 0));
	}

	// A frame runs speed + 1 ticks, as 1.22's does (0x4345DF), on its timer of 88 ms: 2 at speed 1, the speed it starts at.
	[Fact]
	public void AFrameRunsSpeedPlusOneTicks()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;

		int TicksOfAFrame()
		{
			var tick = game.Tick;
			run.Frames(1);
			return game.Tick - tick;
		}

		Assert.Equal(1, game.GameSpeed);
		Assert.Equal(2, TicksOfAFrame());
	}

	// The speed keys are those of a network game, F faster, G slower, R back to 1, and a frame runs speed + 1 ticks.
	[Fact]
	public void TheSpeedKeysAreThoseOfANetworkGame()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;

		void Press(uint dik, int times = 1)
		{
			for (var i = 0; i < times; i++)
			{
				run.Player.Post(g => g.PostKeyboardInput(dik, true));
				run.Frames(1);
				run.Player.Post(g => g.PostKeyboardInput(dik, false));
				run.Frames(1);
			}
		}

		int TicksOfAFrame()
		{
			var tick = game.Tick;
			run.Frames(1);
			return game.Tick - tick;
		}

		Press(dinput.DIK_F, 2);
		Assert.Equal(4, game.GameSpeed);
		Assert.Equal(5, TicksOfAFrame());

		Press(dinput.DIK_G, 3);
		Assert.Equal(0, game.GameSpeed);
		Assert.Equal(0, TicksOfAFrame());

		Press(dinput.DIK_R);
		Assert.Equal(1, game.GameSpeed);
		Assert.Equal(2, TicksOfAFrame());
	}

	// The marks, here a destroyer's torpedoes, are made on the tick that is drawn only, as in 1.22 (0x41B991).
	[Theory]
	[InlineData(false, 0)]
	[InlineData(true, 1)]
	public void MarksAreMadeOnTheDrawnTickOnly(bool drawn, int marks)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		ref var destroyer = ref game.Units[1];
		destroyer.Category = UnitCategory.Ship;
		destroyer.Kind = UnitKind.Destroyer;
		destroyer.Side = game.LocalSide;
		destroyer.Hp = destroyer.MaxHp;
		destroyer.SupplyTime = 0;
		destroyer.IsGroupLeader = 0;
		destroyer.Ammo = 1;
		destroyer.ReloadTime = 0;
		destroyer.Speed = 0;
		int EffectsInUse() => Enumerable.Range(1, 255).Count(f => game.Effects[f].Layer != EffectLayer.None);

		var effects = EffectsInUse();
		game.IsDrawnTick = drawn;
		game.UpdateUnitEffects(1);

		Assert.Equal(effects + marks, EffectsInUse());
	}

	// A ship that is not damaged, and a damaged plane but on every tenth tick, draw no random number for their smoke, as in
	// 1.22, which tests the damage and the tick first (0x41C18D, 0x41B8EC); NSPW NET's draw one every tick.
	[Theory]
	[InlineData(UnitCategory.Ship, UnitKind.Destroyer, 1.0)]
	[InlineData(UnitCategory.Plane, UnitKind.Fighter, 0.6)]
	public void UnitsDrawForTheirSmokeAsIn122(UnitCategory category, UnitKind kind, double hp)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		ref var unit = ref game.Units[1];
		unit.Category = category;
		unit.Kind = kind;
		unit.Side = game.LocalSide;
		unit.PlaneState = UnitState.Flying;
		unit.IsGroupLeader = 0;
		unit.Ammo = 0;
		unit.Speed = 0;
		unit.MaxHp = 10;
		unit.Hp = (int)(unit.MaxHp * hp);
		if (game.Tick % 10 == 0)
		{
			game.Tick++;
		}

		var draws = game.RandomCount;
		game.IsDrawnTick = true;
		game.UpdateUnitEffects(1);

		Assert.Equal(draws, game.RandomCount);
	}

	// A burst of rapid anti-aircraft fire hits every plane of the target's side within 40, as in 1.22 (0x419576); NSPW NET's
	// hits the first only.
	[Fact]
	public void RapidFireHitsEveryPlaneInItsBurst()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var planes = new[] { 41, 42 };
		foreach (var m in planes)
		{
			ref var plane = ref game.Units[m];
			plane.Side = Side.UnitedStates;
			plane.Category = UnitCategory.Plane;
			plane.Kind = UnitKind.Attacker;
			plane.PlaneState = UnitState.Flying;
			plane.Position = new WorldPosition(1000 + (m - 41) * 20, 1000);
			plane.MaxHp = 100;
			plane.Hp = 100;
		}

		ref var fire = ref game.Fires[1];
		fire.Kind = FireKind.RapidAntiAircraftShell;
		fire.Target = planes[0];
		fire.Ticks = 1;
		fire.Speed = 0;
		fire.Position = new WorldPosition(1010, 1000);
		game.UpdateFires();

		Assert.All(planes, m => Assert.True(game.Units[m].Hp < 100));
	}

	// Drawing a smoke that the player's units see draws three random numbers from rand, as 1.22's drawing does
	// (0x40C9EF), where NSPW NET's draws them from SharedRandom; the battle's next random numbers depend on it.
	[Fact]
	public void DrawingEffectsDrawsFromRand()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var m = Enumerable.Range(1, game.MaxUnitId).First(n => game.Units[n].IsUsed && game.Units[n].Side == game.LocalSide && game.Units[n].Category == UnitCategory.Ship);
		for (var f = 1; f < 256; f++)
		{
			game.Effects[f].Layer = EffectLayer.None;
		}

		ref var smoke = ref game.Effects[1];
		smoke.Layer = EffectLayer.Upper;
		smoke.Animation = 2;
		smoke.TimeLeft = 10;
		smoke.SpriteNumber = 6;
		smoke.Position = game.Units[m].Position;

		var draws = game.RandomCount;
		game.DrawBattleArea();

		Assert.Equal(draws + 3, game.RandomCount);
	}

	// A battle as the desktop app shows it, in real time, saved as singleplayer_battle.png next to the test assembly.
	[Fact]
	public void TheBattleShows()
	{
		using var data = GameData.CreateCopy();
		using var player = new RunningGame(data.Path, "Alice");
		player.Game.IsSingleplayer = true;
		player.Game.ScenarioNumber = 3;
		player.Game.SingleplayerExecutable = File.ReadAllBytes(Path.Combine(GameData.FindSingleplayer(), "NSPW_122.exe"));
		player.Platform.SingleplayerDirectory = GameData.FindSingleplayer();

		// Away from the edges, which scroll the battle area.
		player.Game.SetCursorPos(384, 384);
		player.Start();

		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
		while (player.Platform.FrameCount < 200 && player.Error is null && DateTime.UtcNow < deadline)
		{
			Thread.Sleep(100);
		}

		Assert.Null(player.Error);
		Assert.True(player.Platform.FrameCount >= 200, $"The game showed {player.Platform.FrameCount} frames.");
		Assert.Equal(GameMode.Battle, player.Game.Mode);
		Assert.Empty(player.Platform.MessageBoxes);
		TitleScreenTests.WritePng(Path.Combine(AppContext.BaseDirectory, "singleplayer_battle.png"), player.Platform.LastFrame(), all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
	}
}
