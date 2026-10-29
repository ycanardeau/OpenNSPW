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

		public void ReachPlayerOrders(Nspw game) { }

		public void Reach(Nspw game, TracePoint point)
		{
			if (point == TracePoint.Seeded)
				Positions = PositionsOf(game, _side);
		}
	}

	private static List<(int Unit, WorldPosition Position)> SetUpAt(
		int scenario,
		Side side,
		int startDate
	)
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

	// Scenario 208's event, while more than 20000 is left, reads an uninitialized local as the count of planes at fleet 2's
	// base, and draws a random number to give it a plane when the count is 1 to 15 (0x4050C7). Windows' stack holds 0
	// there, so 1.22 draws none, and neither does the port, whatever the emulator's stack held: here 5, as the emulated
	// code left it in a trace.
	[Fact]
	public void Scenario208ReadsItsUninitializedCountAsWindowsHoldsIt()
	{
		uint RandomStateAfter(uint? emulatorsLeftover)
		{
			using var run = new SingleplayerRun(208);
			run.Frames(1);
			for (var frame = 0; frame < 30; frame++)
			{
				if (emulatorsLeftover is { } value && run.Game.Emulator is { } emulator)
					BitConverter.TryWriteBytes(
						emulator.At(emulator.ImageBase - 0xA8 + 0x30, sizeof(uint)),
						value
					);
				run.Frames(1);
			}

			Assert.True(run.Game.BattleTime > 20000);
			return run.Game.RandomState;
		}

		Assert.Equal(RandomStateAfter(null), RandomStateAfter(5));
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
		Assert.True(
			game.BattleTime < time,
			$"The time limit went from {time} to {game.BattleTime}."
		);
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
		Assert.Equal(599, game.Renderer.Sprites[SpriteId.TitleBackground].Size.Width);
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
		game.CameraPosition = new WorldPosition(
			airBase.X - (CMBT_WIDTH / 2),
			airBase.Y + (CMBT_HEIGHT / 2)
		);
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
			if (
				unit.Side == game.CpuSide
				&& unit.Category == UnitCategory.Ship
				&& unit.Kind <= UnitKind.LightCarrier
				&& unit.MaxHP >= 2
			)
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
				Assert.True(
					Math.Abs(unit.Position.X - game.Units[m].Position.X) > 280
						|| Math.Abs(unit.Position.Y - game.Units[m].Position.Y) > 280
				);
			}
		}

		game.Units[m].HP = game.Units[m].MaxHP - 1;
		game.Units[m].SupplyTime = 1;
		run.Frames(300);
		Assert.Equal(game.Units[m].MaxHP, game.Units[m].HP);
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
			ship.Position = new WorldPosition(World.Right + 100, y);
			ship.PathX[0] = World.Right + 800;
			ship.PathY[0] = y;
			ship.EndPath(1);
			ship.EmergencyFlags[0] = 0;
			ship.IsStopping = false;
			ship.GroupLeader = 0;
			y += 400;
		}

		run.Frames(20);
		Assert.True(
			game.Units[computers].Position.X > World.Right,
			$"The computer's ship is at x {game.Units[computers].Position.X}."
		);
		Assert.NotEqual(World.Right - 40, game.Units[computers].EmergencyDestination.X);
		Assert.Equal(World.Right - 40, game.Units[players].EmergencyDestination.X);
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
		Assert.Equal(41, game.Units[battleship].MaxHP);
		Assert.Equal(9000, game.Units[battleship].FuelInterval);

		var carrier = game.AddUnit(Side.UnitedStates, UnitKind.Carrier, 0, 400, 0);
		Assert.NotEqual(0, carrier);
		Assert.Equal(41, game.Units[carrier].MaxHP);
		Assert.Equal(12, game.Units[carrier].Capacity);

		var planes = game.Units[carrier].PlaneCount;
		Assert.Equal(
			1,
			game.AddPlane(
				Side.UnitedStates,
				UnitKind.Attacker,
				0,
				carrier,
				game.Units[carrier].Capacity + 2,
				FireKind.Bomb
			)
		);
		Assert.Equal(planes + game.Units[carrier].Capacity + 2, game.Units[carrier].PlaneCount);
	}

	// A ship out of fuel brakes from its top speed times 0.1, as 1.22 computes it (0x40285F), so a cruiser at 0.08 of 0.8
	// still speeds up, 0.8 * 0.1 being 0.08000000000000002; NSPW NET's top speed over 10 is 0.08, and it brakes.
	[Fact]
	public void ShipsOutOfFuelBrakeFromTopSpeedTimesATenth()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var cruiser = game.AddUnit(Side.Japan, UnitKind.Cruiser, -9000, -7000, 0);
		Assert.NotEqual(0, cruiser);
		ref var unit = ref game.Units[cruiser];
		unit.MaxSpeed = 0.8;
		unit.Speed = 0.08;
		unit.Fuel = 0;
		unit.IsStopping = false;
		unit.PathX[0] = -8000;
		unit.PathY[0] = -7000;
		unit.EndPath(1);

		game.UpdateBattle();

		Assert.True(game.Units[cruiser].Speed > 0.08);
	}

	// A torpedo plane checks for land along the heading its stack holds, as 1.22 does (0x413D23), which never sets it;
	// with no trace to give it, the port's is 0, due east, which is nearly always what 1.22's holds. So a plane at sea
	// aiming east drops its torpedo with land behind it, to the west, where NSPW NET checks: along the bearing from its aim
	// to it.
	[Fact]
	public void TorpedoPlanesCheckLandAlongTheHeadingTheirStackHolds()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		bool IsSea(int r, int c) =>
			r is > 0 and < 255 && c is > 0 and < 255 && game.MapTiles[r][c] == 0;
		var (row, column) = (
			from r in Enumerable.Range(1, 253)
			from c in Enumerable.Range(3, 249)
			where
				Enumerable
					.Range(-1, 3)
					.All(dr => Enumerable.Range(-1, 3).All(dc => IsSea(r + dr, c + dc)))
				&& Enumerable.Range(1, 3).All(dc => IsSea(r, c + dc))
				&& game.MapTiles[r][c - 2] >= 1
			select (r, c)
		).First();
		var x = World.Left + column * 80 + 20;
		var y = World.Top - row * 80 - 20;
		var target = game.AddUnit(Side.UnitedStates, UnitKind.Cruiser, x + 200, y, 0);
		var attacker = game.AddUnit(Side.Japan, UnitKind.Attacker, x, y, 0);
		Assert.NotEqual(0, target);
		Assert.NotEqual(0, attacker);
		game.Units[target].Position = new WorldPosition(x + 200, y);
		game.Units[target].Speed = 0;
		game.Units[target].IsFound = true;
		ref var unit = ref game.Units[attacker];
		unit.Position = new WorldPosition(x, y);
		unit.Direction = 0;
		unit.PlaneState = UnitState.Flying;
		unit.Ammo = 1;
		unit.Target = target;

		game.FireWeapons(attacker, target, FireKind.Torpedo);

		Assert.Equal(0, game.Units[attacker].Ammo);
	}

	// A torpedo plane at the map's top edge tests the tiles around it beyond the map, as 1.22 does, reading its memory
	// around cmbt_map, rather than failing.
	[Fact]
	public void TorpedoPlanesAtTheMapsEdgeTestTheTilesBeyondIt()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var target = game.AddUnit(Side.UnitedStates, UnitKind.Cruiser, 0, World.Top - 10, 0);
		var attacker = game.AddUnit(Side.Japan, UnitKind.Attacker, -200, World.Top - 10, 0);
		Assert.NotEqual(0, target);
		Assert.NotEqual(0, attacker);
		game.Units[target].Position = new WorldPosition(0, World.Top - 10);
		game.Units[target].IsFound = true;
		ref var unit = ref game.Units[attacker];
		unit.Position = new WorldPosition(-200, World.Top - 10);
		unit.Direction = 0;
		unit.PlaneState = UnitState.Flying;
		unit.Ammo = 1;
		unit.Target = target;

		game.FireWeapons(attacker, target, FireKind.Torpedo);
	}

	// An attacker that turns back, low on fuel, hands its fighters over to a new leader and sends them back if it is itself
	// flying, as 1.22 tests (0x415916), so a parked fighter is sent back too; NSPW NET tests the fighter.
	[Fact]
	public void HandoversSendBackParkedFightersAsIn122()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var carrier = game.AddUnit(Side.Japan, UnitKind.Carrier, -9000, -7000, 0);
		var attacker = game.AddUnit(Side.Japan, UnitKind.Attacker, -8000, -7000, 0);
		var fighter = game.AddUnit(Side.Japan, UnitKind.Fighter, -9000, -7000, 0);
		var other = game.AddUnit(Side.Japan, UnitKind.Fighter, -9000, -7000, 0);
		Assert.NotEqual(0, carrier);
		Assert.NotEqual(0, attacker);
		Assert.NotEqual(0, fighter);
		Assert.NotEqual(0, other);
		ref var unit = ref game.Units[attacker];
		unit.PlaneState = UnitState.Flying;
		unit.Mode = UnitMode.Move;
		unit.Carrier = (short)carrier;
		unit.Fuel = 10;
		unit.IsGroupLeader = 3;
		foreach (var (f, number) in new[] { (fighter, 1), (other, 2) })
		{
			ref var plane = ref game.Units[f];
			plane.PlaneState = UnitState.Parked;
			plane.Mode = UnitMode.Move;
			plane.GroupLeader = (short)attacker;
			plane.FormationNumber = (short)number;
		}

		game.SetAttackerEmergencyDestination(attacker);

		Assert.Equal(0, game.Units[fighter].GroupLeader);
		Assert.Equal(UnitMode.Return, game.Units[fighter].Mode);
		Assert.Equal(UnitMode.Return, game.Units[other].Mode);
	}

	// The combat menu arms no bomber with a torpedo, Japan's either, as in 1.22 (0x431D56); NSPW NET arms Japan's.
	[Fact]
	public void TheCombatMenuArmsNoBomberWithATorpedo()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var bomber = game.AddUnit(Side.Japan, UnitKind.Bomber, -9000, -7000, 0);
		Assert.NotEqual(0, bomber);
		game.Units[bomber].Weapon = FireKind.Bomb;
		game.BufferedMenuOrders[1] = new MenuOrder
		{
			Menu = CombatMenuItem.ArmWithTorpedo,
			SelectedUnit = (short)bomber,
		};

		game.ApplyUnitInfoInput();

		Assert.Equal(FireKind.Bomb, game.Units[bomber].Weapon);
	}

	// A ship's place in the formation of a leader heading 360, which none of the 16 cases takes, is reckoned from its x
	// offset taken as an angle, as 1.22's does (0x417179), whose angle shares the offset's stack slot.
	[Fact]
	public void FormationsOfALeaderHeading360TakeTheOffsetAsTheAngle()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var leader = game.AddUnit(Side.Japan, UnitKind.Cruiser, -9000, -7000, 0);
		var ship = game.AddUnit(Side.Japan, UnitKind.Destroyer, -8900, -6950, 0);
		Assert.NotEqual(0, leader);
		Assert.NotEqual(0, ship);
		game.Units[leader].Position = new WorldPosition(-9000, -7000);
		game.Units[leader].Direction = 360;
		game.Units[ship].Position = new WorldPosition(-8900, -6950);
		game.Units[ship].GroupLeader = (short)leader;

		game.SetShipFormation(ship);

		Assert.Equal(
			Math.Atan2(50, 100) * 180 / Math.PI - 100 + 360,
			game.Units[ship].DirectionToLeader.Degrees,
			6
		);
	}

	// A mode chosen from the combat menu goes to a parked plane too, and Move clears its deck phase, as in 1.22
	// (0x431D19 to 0x431D4E); NSPW NET gives Move to flying planes only.
	[Fact]
	public void MenuModesGoToParkedPlanesToo()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var fighter = game.AddUnit(Side.Japan, UnitKind.Fighter, -9000, -7000, 0);
		Assert.NotEqual(0, fighter);
		ref var unit = ref game.Units[fighter];
		unit.PlaneState = UnitState.Parked;
		unit.DeckPhase = 4;
		unit.Mode = UnitMode.Return;
		game.BufferedMenuOrders[1] = new MenuOrder
		{
			Menu = CombatMenuItem.Move,
			SelectedUnit = (short)fighter,
		};

		game.ApplyUnitInfoInput();

		Assert.Equal(0, game.Units[fighter].DeckPhase);
		Assert.Equal(UnitMode.Move, game.Units[fighter].Mode);
	}

	// A flying plane keeps following its leader once the leader is parked and returning, as in 1.22 (0x4149F0); NSPW NET
	// has it leave.
	[Fact]
	public void FlyingFollowersStayWithParkedLeaders()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var leader = game.AddUnit(Side.Japan, UnitKind.Bomber, -9000, -7000, 0);
		var follower = game.AddUnit(Side.Japan, UnitKind.Bomber, -8800, -7000, 0);
		Assert.NotEqual(0, leader);
		Assert.NotEqual(0, follower);
		game.Units[leader].PlaneState = UnitState.Parked;
		game.Units[leader].Mode = UnitMode.Return;
		game.Units[follower].PlaneState = UnitState.Flying;
		game.Units[follower].GroupLeader = (short)leader;
		game.Units[follower].FormationNumber = 1;

		game.SetDynamicDestination(follower);

		Assert.Equal(leader, game.Units[follower].GroupLeader);
	}

	// The planes that follow a torpedo plane return once it drops its torpedo, and keep their path, as in 1.22
	// (0x413F51 to 0x413F7C), where NSPW NET sends them to where it dropped it.
	[Fact]
	public void TorpedoPlanesFollowersKeepTheirPath()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var target = game.AddUnit(Side.UnitedStates, UnitKind.Cruiser, -9000, -7000, 0);
		var attacker = game.AddUnit(Side.Japan, UnitKind.Attacker, -9200, -7000, 0);
		var fighter = game.AddUnit(Side.Japan, UnitKind.Fighter, -9220, -7000, 0);
		Assert.NotEqual(0, target);
		Assert.NotEqual(0, attacker);
		Assert.NotEqual(0, fighter);
		game.Units[target].Speed = 0;
		game.Units[target].IsFound = true;
		ref var unit = ref game.Units[attacker];
		unit.PlaneState = UnitState.Flying;
		unit.Ammo = 1;
		unit.Target = target;
		unit.IsGroupLeader = 2;
		ref var follower = ref game.Units[fighter];
		follower.PlaneState = UnitState.Flying;
		follower.GroupLeader = (short)attacker;
		follower.PathX[0] = -5000;
		follower.PathY[0] = -5000;

		game.FireWeapons(attacker, target, FireKind.Torpedo);

		Assert.Equal(0, game.Units[attacker].Ammo);
		Assert.Equal(UnitMode.Return, game.Units[fighter].Mode);
		Assert.Equal(-5000, game.Units[fighter].PathX[0]);
		Assert.Equal(-5000, game.Units[fighter].PathY[0]);
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
			if (
				game.Units[i].IsUsed
				&& game.Units[i].Kind == UnitKind.Attacker
				&& game.Units[i].Carrier == carrier
			)
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
				if (
					m != full
					&& game.Units[m].IsUsed
					&& game.Units[m].Kind == UnitKind.Pillboxes
					&& game.Units[m].Position.Equals(landing)
				)
				{
					pillboxes = m;
				}
			}
		}

		Assert.NotEqual(0, pillboxes);
		Assert.Equal(0, game.Units[pillboxes].BuildTime);
		Assert.Equal(game.Units[full].MaxHP, game.Units[pillboxes].MaxHP);
		Assert.Equal(game.Units[pillboxes].MaxHP, game.Units[pillboxes].HP);
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
	public void SubmergedSubmarinesAreFoundAsIn122(
		UnitKind kind,
		double speed,
		double submarineSpeed,
		int size
	)
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
		var submarine = game.AddUnit(
			side == Side.Japan ? Side.UnitedStates : Side.Japan,
			UnitKind.Submarine,
			-9000 + distance,
			-7000,
			0
		);
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

	// The computer's destroyer stops chasing its submarine once it is found, and keeps chasing it, to its last contact,
	// while it is not, as 1.22 tests it (0x416A70).
	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void TheComputersDestroyersChaseSubmarinesNotFound(bool found, bool chases)
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var destroyer = game.AddUnit(game.CpuSide, UnitKind.Destroyer, -9000, -7000, 0);
		var submarine = game.AddUnit(game.LocalSide, UnitKind.Submarine, -8500, -7000, 0);
		Assert.NotEqual(0, destroyer);
		Assert.NotEqual(0, submarine);
		ref var sub = ref game.Units[submarine];
		sub.IsFound = found;
		sub.IsSubmerged = true;
		game.Units[destroyer].EmergencyFlags[1] = submarine;
		game.srand(1); // Its first draw, 41, is not 1 in 10.

		game.SetShipEmergencyDestination(destroyer);

		Assert.Equal(chases ? submarine : 0, game.Units[destroyer].EmergencyFlags[1]);
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
		plane.EndPath(2);

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
			if (
				unit.IsUsed
				&& unit.Category == UnitCategory.Ship
				&& unit.Kind <= UnitKind.LightCarrier
				&& unit.FuelInterval >= 1
			)
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
		game.SelectOrders[0] = new SelectOrder
		{
			IsSet = true,
			SelectedUnit = (short)fighter,
			Unit = (short)target,
		};

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
		game.SelectOrders[0] = new SelectOrder
		{
			IsSet = true,
			SelectedUnit = (short)attacker,
			Unit = (short)target,
		};

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

	// A submarine keeps its target once it has fired torpedoes at it, as in 1.22 (0x4117F7), where NSPW NET's clears it.
	[Fact]
	public void SubmarinesKeepTheirTargetAfterFiring()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var submarine = game.AddUnit(Side.Japan, UnitKind.Submarine, -9000, -7000, 0);
		var cruiser = game.AddUnit(Side.UnitedStates, UnitKind.Cruiser, -8700, -7000, 0);
		Assert.NotEqual(0, submarine);
		Assert.NotEqual(0, cruiser);
		game.Units[cruiser].IsFound = true;
		game.Units[submarine].Target = cruiser;

		game.FireWeapons(submarine, cruiser, FireKind.Torpedo);

		Assert.Equal(2000, game.Units[submarine].ReloadTime);
		Assert.Equal(cruiser, game.Units[submarine].Target);
	}

	// An exploded depth charge keeps its fire, with the last submerged submarine it tested as its target, as in 1.22
	// (0x4196DC to 0x4197EC), where NSPW NET's frees it.
	[Fact]
	public void ExplodedDepthChargesKeepTheirFireAsIn122()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var submarine = game.AddUnit(game.CpuSide, UnitKind.Submarine, 1000, 1000, 0);
		Assert.NotEqual(0, submarine);
		game.Units[submarine].IsSubmerged = true;
		var hp = game.Units[submarine].HP;
		ref var fire = ref game.Fires[255];
		fire.Kind = FireKind.AntiSubmarineBomb;
		fire.Target = submarine;
		fire.Position = game.Units[submarine].Position;
		fire.FlightTime = 100;
		fire.Ticks = 99;

		game.UpdateFires();

		Assert.True(game.Units[submarine].HP < hp);
		Assert.Equal(submarine, game.Fires[255].Target);
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
			game.Fires[1].Position = new WorldPosition(
				game.Units[1].Position.X + dx,
				game.Units[1].Position.Y + dy
			);
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
		destroyer.HP = destroyer.MaxHP;
		destroyer.SupplyTime = 0;
		destroyer.IsGroupLeader = 0;
		destroyer.Ammo = 1;
		destroyer.ReloadTime = 0;
		destroyer.Speed = 0;
		int EffectsInUse() =>
			Enumerable.Range(1, 255).Count(f => game.Effects[f].Layer != EffectLayer.None);

		var effects = EffectsInUse();
		game.Renderer.IsDrawnTick = drawn;
		game.UpdateUnitEffects(1);

		Assert.Equal(effects + marks, EffectsInUse());
	}

	// A ship leaves its wake behind it at its heading's fraction of a degree, as 1.22 does (0x41C327), which means to reduce
	// it to whole degrees but stores them into the low half of the double; NSPW NET's is at whole degrees.
	[Fact]
	public void WakesKeepTheHeadingsFractionAsIn122()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		ref var destroyer = ref game.Units[1];
		destroyer.Category = UnitCategory.Ship;
		destroyer.Kind = UnitKind.Destroyer;
		destroyer.Side = game.LocalSide;
		destroyer.HP = destroyer.MaxHP;
		destroyer.Position = new WorldPosition(1000, 1000);
		destroyer.Direction = 10.3;
		destroyer.Speed = destroyer.MaxSpeed;
		game.Tick = 15;
		game.Renderer.IsDrawnTick = true;
		var used = Enumerable
			.Range(1, 255)
			.Where(f => game.Effects[f].Layer != EffectLayer.None)
			.ToHashSet();

		game.UpdateUnitEffects(1);

		var wake = Enumerable
			.Range(1, 255)
			.Single(f =>
				!used.Contains(f)
				&& game.Effects[f].Animation == 4
				&& game.Effects[f].SpriteNumber == 8
			);
		var heading = 190.3 * Math.PI / 180;
		Assert.Equal(1000 + Math.Cos(heading) * 16, game.Effects[wake].Position.X, 3);
		Assert.Equal(1000 + Math.Sin(heading) * 16, game.Effects[wake].Position.Y, 3);
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
		unit.MaxHP = 10;
		unit.HP = (int)(unit.MaxHP * hp);
		if (game.Tick % 10 == 0)
		{
			game.Tick++;
		}

		var draws = game.RandomCount;
		game.Renderer.IsDrawnTick = true;
		game.UpdateUnitEffects(1);

		Assert.Equal(draws, game.RandomCount);
	}

	// A burst of rapid anti-aircraft fire hits the first plane of the target's side within 40 only, as in 1.22: it clears
	// the fire's target on the hit (0x41957B), and compares the next planes' side with unit 0's, none.
	[Fact]
	public void RapidFireHitsTheFirstPlaneOfItsBurst()
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
			plane.MaxHP = 100;
			plane.HP = 100;
		}

		ref var fire = ref game.Fires[1];
		fire.Kind = FireKind.RapidAntiAircraftShell;
		fire.Target = planes[0];
		fire.Ticks = 1;
		fire.Speed = 0;
		fire.Position = new WorldPosition(1010, 1000);
		game.UpdateFires();

		Assert.True(game.Units[planes[0]].HP < 100);
		Assert.Equal(100, game.Units[planes[1]].HP);
	}

	// Drawing a smoke that the player's units see draws three random numbers from rand, as 1.22's drawing does
	// (0x40C9EF), where NSPW NET's draws them from SharedRandom; the battle's next random numbers depend on it.
	[Fact]
	public void DrawingEffectsDrawsFromRand()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var m = Enumerable
			.Range(1, game.MaxUnitId)
			.First(n =>
				game.Units[n].IsUsed
				&& game.Units[n].Side == game.LocalSide
				&& game.Units[n].Category == UnitCategory.Ship
			);
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
		game.Renderer.DrawBattleArea();

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
		player.Game.SingleplayerExecutable = File.ReadAllBytes(
			Path.Combine(GameData.FindSingleplayer(), "NSPW_122.exe")
		);
		player.Platform.SingleplayerDirectory = GameData.FindSingleplayer();

		// Away from the edges, which scroll the battle area.
		player.Game.SetCursorPos(384, 384);
		player.Start();

		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
		while (
			player.Platform.FrameCount < 200 && player.Error is null && DateTime.UtcNow < deadline
		)
		{
			Thread.Sleep(100);
		}

		Assert.Null(player.Error);
		Assert.True(
			player.Platform.FrameCount >= 200,
			$"The game showed {player.Platform.FrameCount} frames."
		);
		Assert.Equal(GameMode.Battle, player.Game.Mode);
		Assert.Empty(player.Platform.MessageBoxes);
		TitleScreenTests.WritePng(
			Path.Combine(AppContext.BaseDirectory, "singleplayer_battle.png"),
			player.Platform.LastFrame(),
			all_head.SCRN_WIDTH,
			all_head.SCRN_HEIGHT
		);
	}

	// Around the map, where 1.22 reads beyond it (MapTileOrBeyond), the port's view of 1.22's memory (ByteAs122) is the
	// emulator's memory with the state copied in.
	[Theory]
	[InlineData(4)]
	[InlineData(303)]
	public void TheMemoryAroundTheMapIsTheEmulators(int scenario)
	{
		const uint MapAddress = 0x443AA0;
		using var run = new SingleplayerRun(scenario);
		run.Frames(50);
		var game = run.Game;

		// Copies the state in, and leaves it there: plane_in_cv(0) writes nothing.
		game.RunEmulated(OpenNspw.Singleplayer.Functions.CountPlanesIn, 0);

		for (var address = MapAddress - 0x1000; address < MapAddress + 0x20000 + 0x1000; address++)
		{
			var emulated = game.Emulator!.At(address, 1)[0];
			var port = game.ByteAs122(address);
			Assert.True(emulated == port, $"0x{address:X}: emulator {emulated}, port {port}");
		}
	}

	// A single-player game runs none of 1.22's code: its setups, its computer and its victory conditions are C#, and the
	// emulator is created only by the tests that compare them with 1.22's own.
	[Theory]
	[InlineData(3)]
	[InlineData(203)]
	[InlineData(303)]
	[InlineData(304)]
	public void TheGameRunsWithoutTheEmulator(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		run.Frames(500);

		Assert.Equal(GameMode.Battle, run.Game.Mode);
		Assert.False(run.Game.HasEmulator);
	}

	// The longest tests, in a class of their own, which xUnit runs in parallel with the others: the tests of one class run
	// one after another.
	public class Plays
	{
		// Every scenario plays for a while with the computer, without failing.
		[Theory]
		[MemberData(nameof(Scenarios), MemberType = typeof(SingleplayerTests))]
		public void ScenarioPlays(int scenario)
		{
			using var run = new SingleplayerRun(scenario);
			run.Frames(1000);

			Assert.Equal(GameMode.Battle, run.Game.Mode);
			Assert.Empty(run.Player.MessageBoxes);
		}
	}
}
