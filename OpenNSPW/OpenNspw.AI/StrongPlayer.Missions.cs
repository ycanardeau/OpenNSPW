namespace OpenNspw.AI;

// The strong player's missions: the plans of the scenarios whose goals the general tasks do not reach, played before
// them (TryMission), which may hold the general strikes back (HoldsStrikes) and the carriers' moves (HoldsFleet).
public sealed partial class StrongPlayer
{
	// Scenario 300: a corner of the map beyond x < -8400 and y < -6000, where the bombers gather (ComputerPlayer.md,
	// decision).
	private static readonly WorldPosition DoolittleCorner = new(-9000, -6700);

	// Scenario 300: the share of the bombers' range they may fly, over Tokyo and on to the corner.
	private const double DoolittleRangeShare = 0.85;

	private readonly HashSet<int> _seenCities = [];
	private readonly HashSet<int> _placeTargets = [];
	private int _missionOrdered = -1000;

	// Scenario 205: the point the rescue flies by each way, north-east of Guadalcanal, clear of the enemy fighters that
	// guard the waters south of it.
	private static readonly WorldPosition RescueWaypoint = new(2500, 1500);

	private readonly List<int> _rescueStops = [];
	private int _rescueBomber;
	private int _rescueLeg;
	private bool _hasDefendedBougainville;
	private int _defenders;

	// Scenario 107: where the ships wait for Yamamoto's plane, west of Rabaul's airfield, which it comes round to land
	// at: a player who won it sailed there at once, and shot the plane down over it with the fighters and the
	// destroyers' guns.
	private static readonly WorldPosition YamamotoAmbush = new(-9100, 1700);

	// Scenario 107: the way there that player sailed, from the easternmost destroyer's start, west of the coast north-west
	// of the start, which a ship sailing straight there from farther west runs aground on.
	private static readonly WorldPosition[] AmbushWay =
	[
		new(-4260, -2950),
		new(-9188, 934),
		YamamotoAmbush,
	];

	private bool _hasSailedToAmbush;
	private readonly Dictionary<int, int> _gunsOnYamamoto = [];

	// Scenario 10: the player's air base that the raids strike, the southern one, where a player who won it gathered the
	// fighters of the base north of it at once, and shot both raids down over it; then swept Saipan's airfield with the
	// fighters based farthest forward, and shot down the last fighters there, which won it.
	private static readonly WorldPosition RaidedAirBase = new(-7600, 960);

	private readonly HashSet<int> _gatheredFrom = [];
	private readonly HashSet<int> _guarding = [];

	// Scenario 109: where the fleet strikes Yokosuka from, 5000 from it, beyond the guns of what guards it.
	private static readonly WorldPosition RaidPoint = new(-3500, 1800);

	private int _raidOrdered = int.MinValue / 2;

	// Scenario 201: where the American carrier waits until about frame 2400, before it sails west: a player who won it
	// sent the submarine there at once, and the carrier that can launch toward it, struck it there with its attackers,
	// and had the submarine fire at it from under it.
	private static readonly WorldPosition HornetStart = new(1200, 2000);

	// A torpedo's speed, a tick (TPD_SPD).
	private const double TorpedoSpeed = 2.0;

	private readonly HashSet<int> _intercepting = [];

	// Scenario 9: where the opening strike goes, west of where the U.S. fleet starts, which it sails west from: a player
	// who won it launched every attacker there at once, and found the carriers there about frame 1400. Scenario 201: just
	// short of where the American carrier waits.
	private static WorldPosition OpeningStrikePoint(Nspw game) =>
		game.ScenarioNumber == 201 ? PointAt(HornetStart, 260, 300) : new(4900, 1500);

	private readonly HashSet<int> _openedFrom = [];

	// Scenario 9: the opening strike's planes, which do not turn home short of fuel while they are armed (TryBingo), nor
	// fly round the escorts (TryApproach), but close on the carriers (TryCloseOnFormation): its point is at the edge of
	// their reach.
	private readonly HashSet<int> _oneWay = [];
	private readonly HashSet<int> _aimed = [];
	private int _lastSweep = int.MinValue / 2;

	// Scenario 10: the leader of the raided base's fighters that watch for Saipan's bombers (TryWatchForBombers), or 0,
	// and when it was last given one; and the frame the bases the watch keeps fighters on were last worked out, with them.
	private int _watchLeader;
	private int _lastWatchAim = int.MinValue / 2;
	private (int Frame, int Raided, int Forward) _watchBases = (int.MinValue, 0, 0);

	private bool IsMissionOrderDue(Nspw game, int frames) =>
		UnitFacts.Now(game) - _missionOrdered >= frames;

	// Scenario 107: Yamamoto's plane, found, or null.
	private Contact? Yamamoto(Nspw game) =>
		_contacts.All.FirstOrDefault(c =>
			c.Kind == UnitKind.Bomber && game.Units[c.Unit].IsUsed && game.Units[c.Unit].IsFound
		);

	// Scenario 107: whether Yamamoto's plane is at the ambush, within 2500 of it, found, the fighters held until then: its
	// seven escorts shoot down what meets it on its way, and it circles there before it lands.
	private bool IsAtAmbush(Nspw game) =>
		Yamamoto(game) is { } bomber
		&& UnitFacts.Distance(game.Units[bomber.Unit].Position, YamamotoAmbush) <= 2500;

	// The air base the mission gathers the fighters at, which they are not rebased from (TryRebase), or 0.
	private int RaidedBase(Nspw game) =>
		game.ScenarioNumber == 10 && Parameters.GatherAndSweep
			? NearestOwnAirBase(game, RaidedAirBase)
			: 0;

	// Scenario 8: the player's anchored ships nearest its air base with the most fighters, where they are guarded, or
	// null: the victory counts them, and the carriers' strikes sink what is not covered.
	private WorldPosition? Anchorage(Nspw game)
	{
		var bases = Decks(game)
			.Where(d => game.Units[d].Kind == UnitKind.AirBase)
			.OrderByDescending(d =>
				ParkedOn(game, d, n => game.Units[n].Kind == UnitKind.Fighter).Count
			)
			.ToList();
		if (bases.Count == 0)
			return null;

		var anchored = new List<WorldPosition>();
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (
				IsOwn(game, m)
				&& UnitFacts.IsAnchored(ref unit)
				&& UnitFacts.Distance(unit.Position, game.Units[bases[0]].Position) <= 1500
			)
				anchored.Add(unit.Position);
		}

		return anchored.Count == 0
			? null
			: new WorldPosition(anchored.Average(p => p.X), anchored.Average(p => p.Y));
	}

	// Scenario 8: each of the player's ships that sails, warship or carrier, sails to the anchorage once, as the fleet a
	// player keeps together over what it must hold: their guns, and the fighters' base, cover it.
	private bool TryGuardAnchorage(Nspw game)
	{
		if (Anchorage(game) is not { } anchorage)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| ship.Category != UnitCategory.Ship
				|| UnitFacts.IsBase(ship.Kind)
				|| UnitFacts.IsAnchored(ref ship)
				|| ship.Kind == UnitKind.Transport
				|| !_guarding.Add(m)
			)
				continue;

			SelectAlone(m, "guarding");
			var point = PointAt(anchorage, m * 53.0, 250 + (60.0 * (m % 4)));
			return SailSelected(game, m, point, "guards the anchorage at") || true;
		}

		return false;
	}

	// Scenario 109: whether the player's carriers are at the raid point, within 1500 of it, where the strikes go.
	private bool IsAtRaidPoint(Nspw game) =>
		Decks(game)
			.Any(d =>
				game.Units[d].Category == UnitCategory.Ship
				&& UnitFacts.Distance(game.Units[d].Position, RaidPoint) <= 1500
			);

	// Scenario 109: until Yokosuka's ships are sunk (ArePlacesDestroyed), each of the player's ships that leads or sails
	// alone, and is not on its way to the raid point, sails there, every 600 frames at most: the carriers keep away from
	// the places the briefing names otherwise (TryPosition), out of reach, and their strikes go at the first warships
	// found.
	private bool TrySailToRaid(Nspw game)
	{
		if (ArePlacesDestroyed(game) || UnitFacts.Now(game) - _raidOrdered < 600)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| ship.Category != UnitCategory.Ship
				|| UnitFacts.IsBase(ship.Kind)
				|| ship.GroupLeader != 0
				|| UnitFacts.Distance(UnitFacts.FinalDestination(ref ship), RaidPoint) < 800
			)
				continue;

			_raidOrdered = UnitFacts.Now(game);
			SelectAlone(m, "raiding");
			return SailSelected(game, m, RaidPoint, "sails to raid Yokosuka from") || true;
		}

		return false;
	}

	// Scenario 9: each deck's ready attackers, armed, all of them, launch at the opening strike point once, from the
	// start, before any carrier is found: the briefing's area is beyond their reach, but the fleet comes on.
	private bool TryOpeningStrike(Nspw game)
	{
		foreach (var deck in Decks(game))
		{
			if (_openedFrom.Contains(deck) || !UnitFacts.CanLaunch(ref game.Units[deck]))
				continue;

			var ready = ParkedOn(
				game,
				deck,
				n =>
					UnitFacts.IsStrikePlane(game.Units[n].Kind)
					&& game.Units[n].Ammo > 0
					&& UnitFacts.IsReady(ref game.Units[n])
			);
			if (ready.Count == 0)
				continue;

			_openedFrom.Add(deck);
			_oneWay.UnionWith(ready);
			ShowDeck(deck);
			SelectPlanes(ready[0], ready.Skip(1));
			LaunchToward(
				ready[0],
				OpeningStrikePoint(game),
				game.Units[deck].Position,
				Reach(ref game.Units[ready[0]]),
				Parameters.StraightOpening
			);
			return true;
		}

		return false;
	}

	// Scenario 9: each plane of the opening strike, armed, within 4000 of an enemy carrier found, waits ahead of it, 800
	// along its course and spread across it, 150 apart, and takes it as its target once it comes within 450, every 100
	// frames: as a player who won it lined the attackers up across the fleet's way, which sailed into their torpedoes,
	// where flown at the carrier among its escorts, most are shot down by their guns.
	private bool TryAmbushTrack(Nspw game)
	{
		if (!Parameters.AmbushTrack)
			return false;

		var index = 0;
		foreach (var m in _oneWay.Order())
		{
			ref var plane = ref game.Units[m];
			if (!IsOwn(game, m) || plane.PlaneState != UnitState.Flying || plane.Ammo == 0)
				continue;

			index++;
			if (UnitFacts.Now(game) - _gunsOnYamamoto.GetValueOrDefault(m, -1000) < 100)
				continue;

			var position = plane.Position;
			var carrier = _contacts
				.All.Where(c =>
					c.Kind is UnitKind.Carrier or UnitKind.LightCarrier
					&& game.Units[c.Unit].IsUsed
					&& game.Units[c.Unit].IsFound
					&& UnitFacts.Distance(position, game.Units[c.Unit].Position) <= 4000
				)
				.MinBy(c => UnitFacts.Distance(position, game.Units[c.Unit].Position));
			if (carrier is null || plane.Target == carrier.Unit)
				continue;

			_gunsOnYamamoto[m] = UnitFacts.Now(game);
			ref var target = ref game.Units[carrier.Unit];
			var unit = carrier.Unit;
			if (UnitFacts.Distance(position, target.Position) <= 450)
			{
				SelectFlying(m, "springing the ambush");
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != m || !g.Units[unit].IsUsed)
						return GiveUp(g, $"springing the ambush {m}: selected {g.SelectedUnit}");
					g.Commands.ClickUnit(unit);
					return true;
				});
				Deselect();
				return true;
			}

			var heading = target.Direction.Degrees;
			var across = ((index % 2 == 0 ? 1 : -1) * 150.0 * (index / 2)) + 75;
			var station = PointAt(PointAt(target.Position, heading, 800), heading + 90, across);
			if (UnitFacts.Distance(UnitFacts.FinalDestination(ref plane), station) < 150)
				continue;

			SelectFlying(m, "lying in wait");
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != m)
					return GiveUp(g, $"lying in wait {m}: selected {g.SelectedUnit}");
				g.Commands.SetDestination(station);
				return true;
			});
			Deselect();
			return true;
		}

		return false;
	}

	// Scenarios 9 and 201: each plane of the opening strike, armed, within 3000 of an enemy carrier found, takes it as its
	// target, once, as a player who won them clicked the carriers for the strike: left to the game's attack, a plane
	// drops at the escort nearest it, and the carrier, 41 HP, needs every torpedo.
	private bool TryAimOpening(Nspw game)
	{
		if (!Parameters.AimOpening)
			return false;

		foreach (var m in _oneWay)
		{
			ref var plane = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| plane.PlaneState != UnitState.Flying
				|| plane.Ammo == 0
				|| _aimed.Contains(m)
			)
				continue;

			var position = plane.Position;
			var carrier = _contacts
				.All.Where(c =>
					c.Kind is UnitKind.Carrier or UnitKind.LightCarrier
					&& game.Units[c.Unit].IsUsed
					&& game.Units[c.Unit].IsFound
					&& UnitFacts.Distance(position, game.Units[c.Unit].Position) <= 3000
				)
				.MinBy(c => UnitFacts.Distance(position, game.Units[c.Unit].Position));
			if (carrier is null || plane.Target == carrier.Unit)
				continue;

			_aimed.Add(m);
			var target = carrier.Unit;
			SelectFlying(m, "aiming");
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != m || !g.Units[target].IsUsed)
					return GiveUp(g, $"aiming {m}: selected {g.SelectedUnit}");
				g.Commands.ClickUnit(target);
				return true;
			});
			Deselect();
			return true;
		}

		return false;
	}

	// Scenario 201: the submarine sails to where the American carrier waits, and the carrier that can launch toward it,
	// each once, from the start: the cruisers and destroyers start with next to no fuel, and the other carriers burn
	// under a fifth of their HP, which launches nothing.
	private bool TrySailAtHornet(Nspw game)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| ship.Category != UnitCategory.Ship
				|| !(
					ship.Kind == UnitKind.Submarine
					|| (UnitFacts.IsDeck(ship.Kind) && UnitFacts.CanLaunch(ref ship))
				)
				|| !_intercepting.Add(m)
			)
				continue;

			var point =
				ship.Kind == UnitKind.Submarine
					? PointAt(HornetStart, 200, 250)
					: PointAt(HornetStart, BearingTo(HornetStart, ship.Position), 1200);
			SelectAlone(m, "sailing at the carrier");
			return SailSelected(game, m, point, "sails at the carrier, to") || true;
		}

		return false;
	}

	// Scenario 201: a submarine within 3000 of the American carrier found waits on its beam, 450 from its course and
	// ahead of it, turned toward the course, and takes it as its target only once the carrier is lined up, where its
	// torpedo meets it 150 to 600 ahead, within 10° of the bow: a submarine fires at its target only so, within 5°
	// (ShipFireTorpedoAtTarget), and a torpedo across a ship's course hits it from the side. Given the target sooner, it
	// chases the carrier bow on, among its escorts.
	private bool TryTorpedoHornet(Nspw game)
	{
		var carrier = _contacts.All.FirstOrDefault(c =>
			c.Kind is UnitKind.Carrier or UnitKind.LightCarrier
			&& game.Units[c.Unit].IsUsed
			&& game.Units[c.Unit].IsFound
		);
		if (carrier is null)
			return false;

		ref var target = ref game.Units[carrier.Unit];
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var sub = ref game.Units[m];
			var distance = UnitFacts.Distance(sub.Position, target.Position);
			if (
				!IsOwn(game, m)
				|| sub.Kind != UnitKind.Submarine
				|| distance > 3000
				|| sub.Target == carrier.Unit
				|| UnitFacts.Now(game) - _gunsOnYamamoto.GetValueOrDefault(m, -1000) < 60
			)
				continue;

			_gunsOnYamamoto[m] = UnitFacts.Now(game);
			var heading = target.Direction.Degrees;
			var meeting = PointAt(target.Position, heading, target.Speed * distance / TorpedoSpeed);
			var off = Math.Abs(
				((BearingTo(sub.Position, meeting) - sub.Direction.Degrees + 540) % 360) - 180
			);
			var reach = UnitFacts.Distance(sub.Position, meeting);
			var unit = carrier.Unit;
			if (reach is >= 150 and <= 600 && off <= 10)
			{
				SelectAlone(m, "torpedoing the carrier");
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != m || !g.Units[unit].IsUsed)
						return GiveUp(g, $"torpedoing with {m}: selected {g.SelectedUnit}");
					g.Commands.ClickUnit(unit);
					Log?.Invoke(
						$"{UnitFacts.Now(g)}: {g.Units[m].Kind} {m} fires at {g.Units[unit].Kind} {unit}, lined up"
					);
					return true;
				});
				Deselect();
				return true;
			}

			// The beam on the submarine's side, ahead of the carrier by about the time it takes the submarine there.
			var ahead = PointAt(
				target.Position,
				heading,
				target.Speed * (distance / Math.Max(sub.MaxSpeed, 0.1))
			);
			var side =
				((BearingTo(ahead, sub.Position) - heading + 540) % 360) - 180 >= 0 ? 90 : -90;
			var station = PointAt(ahead, heading + side, 450);
			// On station, within 300, it turns its bow to where its torpedo would meet the carrier, a short way.
			var point =
				UnitFacts.Distance(sub.Position, station) <= 300
					? PointAt(sub.Position, BearingTo(sub.Position, meeting), 80)
					: station;
			if (UnitFacts.Distance(UnitFacts.FinalDestination(ref sub), point) < 40)
				continue;

			SelectAlone(m, "lying in wait");
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != m)
					return GiveUp(g, $"lying in wait {m}: selected {g.SelectedUnit}");
				g.Commands.SetDestination(point);
				return true;
			});
			Deselect();
			return true;
		}

		return false;
	}

	// Scenario 10: the ready fighters of each other air base of the player's within 3000 of the raided one fly there,
	// once, and are based there (TryRebase brings them down there).
	private bool TryGatherFighters(Nspw game)
	{
		var raided = RaidedBase(game);
		if (raided == 0)
			return false;

		foreach (var deck in Decks(game))
		{
			if (
				deck == raided
				|| game.Units[deck].Kind != UnitKind.AirBase
				|| _gatheredFrom.Contains(deck)
				|| UnitFacts.Distance(game.Units[deck].Position, game.Units[raided].Position) > 3000
				|| !UnitFacts.CanLaunch(ref game.Units[deck])
			)
				continue;

			var fighters = ParkedOn(
				game,
				deck,
				n => game.Units[n].Kind == UnitKind.Fighter && UnitFacts.IsReady(ref game.Units[n])
			);
			if (fighters.Count == 0)
				continue;

			_gatheredFrom.Add(deck);
			var leader = fighters[0];
			ShowDeck(deck);
			SelectPlanes(leader, fighters.Skip(1));
			SendSelected(
				leader,
				game.Units[raided].Position,
				$"and {fighters.Count - 1} more gather at"
			);
			_clicks.Enqueue(g =>
			{
				if (IsOwn(g, leader))
					_rebasing[leader] = raided;
			});
			return true;
		}

		return false;
	}

	// Scenario 10: from frame 4000, the carrier's raid over, the ready fighters of the player's air base nearest Saipan,
	// three or more, with its attackers armed with bombs (TryArm), sweep its airfield, every 1200 frames, while no enemy
	// attacker or bomber has been seen in the air within 3000 of that base for 300 frames, nor do planes land on it: the
	// victory counts the enemy's planes, its last fighters are kept there, and its transports land troops at that base
	// later on.
	private bool TrySweepSaipan(Nspw game)
	{
		var from = NearestOwnAirBase(game, ScenarioKnowledge.Saipan.Center);
		if (
			from == 0
			|| UnitFacts.Now(game) < 4000
			|| UnitFacts.Now(game) - _lastSweep < 1200
			|| !UnitFacts.CanLaunch(ref game.Units[from])
			|| IsLandingOn(game, from)
			|| _contacts.All.Any(c =>
				c.Category == UnitCategory.Plane
				&& UnitFacts.IsStrikePlane(c.Kind)
				&& c.IsFlying
				&& UnitFacts.Now(game) - c.Frame < 300
				&& UnitFacts.Distance(c.Position, game.Units[from].Position) <= 3000
			)
		)
			return false;

		var fighters = ParkedOn(
			game,
			from,
			n => game.Units[n].Kind == UnitKind.Fighter && UnitFacts.IsReady(ref game.Units[n]),
			includeReserved: true
		);
		if (fighters.Count < 3)
			return false;

		// The attackers armed with bombs go too, waited for, to draw the fighters up, which the computer sends only
		// against attackers and bombers.
		var attackers = ParkedOn(
			game,
			from,
			n =>
				game.Units[n].Kind == UnitKind.Attacker
				&& game.Units[n].Weapon == FireKind.Bomb
				&& game.Units[n].Ammo > 0
				&& UnitFacts.IsReady(ref game.Units[n])
		);
		if (
			attackers.Count == 0
			&& ParkedOn(game, from, n => game.Units[n].Kind == UnitKind.Attacker).Count > 0
		)
			return false;

		_lastSweep = UnitFacts.Now(game);
		ShowDeck(from);
		SelectPlanes(fighters[0], fighters.Skip(1).Concat(attackers));
		// The deck takes the order only while no plane lands on it, nor takes off (CanSetDestination), which a plane
		// coming down by the time the clicks come forbids: then the sweep is given up, and tried again.
		var leader = fighters[0];
		var point = PointAt(
			ScenarioKnowledge.Saipan.Center,
			BearingTo(ScenarioKnowledge.Saipan.Center, game.Units[from].Position),
			550
		);
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != leader || !UnitFacts.CanLaunch(ref g.Units[from]))
			{
				_lastSweep = int.MinValue / 2;
				return GiveUp(g, $"sweeping with {leader}: selected {g.SelectedUnit}, deck busy");
			}

			g.Commands.SetDestination(point);
			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {g.Units[leader].Kind} {leader} and {g.SelectionCount - 1} more sweep Saipan at ({point.X:F0}, {point.Y:F0})"
			);
			return true;
		});
		Deselect();
		return true;
	}

	// Scenario 10: from this frame, the carrier's raid shot down, the raided base's fighters watch for Saipan's bombers
	// together.
	private const int WatchFrom = 2000;

	// Scenario 10: the raided air base and the forward one, nearest Saipan, worked out once a frame.
	private (int Raided, int Forward) WatchBases(Nspw game)
	{
		if (_watchBases.Frame != UnitFacts.Now(game))
			_watchBases = (
				UnitFacts.Now(game),
				RaidedBase(game),
				NearestOwnAirBase(game, ScenarioKnowledge.Saipan.Center)
			);
		return (_watchBases.Raided, _watchBases.Forward);
	}

	// Scenario 10, with Parameters.BomberWatch: whether a parked fighter is kept for the watch or the sweep, on the
	// raided base or the forward one, from WatchFrom.
	private bool IsKeptForWatch(Nspw game, int m)
	{
		ref var plane = ref game.Units[m];
		if (
			!Parameters.BomberWatch
			|| plane.Kind != UnitKind.Fighter
			|| UnitFacts.Now(game) < WatchFrom
		)
			return false;

		var (raided, forward) = WatchBases(game);
		return raided != 0 && (plane.Carrier == raided || plane.Carrier == forward);
	}

	// Scenario 10: the raided base's ready fighters go up together from WatchFrom, 1000 ahead of it toward Saipan, once
	// six are ready, most of them refuelled after the carrier's raid, or three, once an attacker or a bomber is found
	// within 5000 of the base; then their leader, for the formation, and every other fighter of the player's in the air
	// near the base takes as its target the enemy attacker or bomber found within 4000 of the base nearest it, one a
	// click, every 30 frames. Once the watch has landed, it goes up again.
	private bool TryWatchForBombers(Nspw game)
	{
		var (raided, _) = WatchBases(game);
		if (raided == 0 || UnitFacts.Now(game) < WatchFrom)
			return false;

		var basePosition = game.Units[raided].Position;
		var bomber = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Plane
				&& UnitFacts.IsStrikePlane(c.Kind)
				&& game.Units[c.Unit].IsUsed
				&& game.Units[c.Unit].IsFound
				&& game.Units[c.Unit].PlaneState == UnitState.Flying
				&& UnitFacts.Distance(c.Position, basePosition) <= 4000
			)
			.OrderBy(c => UnitFacts.Distance(c.Position, basePosition))
			.FirstOrDefault();
		// The watch's leader, then each other fighter of the player's in the air within 4000 of the base, armed, not on
		// its way home unless it can go back to the fight (CanReengage), and with the fuel to come back, takes the
		// bomber, one a click.
		var aiming =
			bomber is null || UnitFacts.Now(game) - _lastWatchAim < 30
				? 0
				: Enumerable
					.Range(1, game.MaxUnitId)
					.Where(m =>
						IsOwn(game, m)
						&& game.Units[m].Kind == UnitKind.Fighter
						&& game.Units[m].PlaneState == UnitState.Flying
						&& game.Units[m].GroupLeader == 0
						&& (game.Units[m].Mode != UnitMode.Return || CanReengage(game, m))
						&& game.Units[m].Ammo > 0
						&& game.Units[m].Target != bomber.Unit
						&& game.Units[m].Fuel > FuelToHome(game, m) + 20
						&& UnitFacts.Distance(game.Units[m].Position, basePosition) <= 4000
					)
					.OrderBy(m => m == _watchLeader ? 0 : 1)
					.FirstOrDefault();
		if (aiming != 0)
		{
			_lastWatchAim = UnitFacts.Now(game);
			var target = bomber!.Unit;
			SelectFlying(aiming, "watching");
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != aiming || !g.Units[target].IsUsed || !g.Units[target].IsFound)
					return GiveUp(
						g,
						$"{g.Units[target].Kind} {target} for {aiming}: selected {g.SelectedUnit}"
					);
				g.Commands.ClickUnit(target);
				Log?.Invoke(
					$"{UnitFacts.Now(g)}: Fighter {aiming} and {g.SelectionCount - 1} more go for {g.Units[target].Kind} {target}"
				);
				return true;
			});
			Deselect();
			return true;
		}

		if (
			_watchLeader != 0
			&& IsOwn(game, _watchLeader)
			&& game.Units[_watchLeader].PlaneState == UnitState.Flying
		)
			return false;

		if (!UnitFacts.CanLaunch(ref game.Units[raided]) || IsLandingOn(game, raided))
			return false;

		var fighters = ParkedOn(
			game,
			raided,
			n => game.Units[n].Kind == UnitKind.Fighter && UnitFacts.IsReady(ref game.Units[n]),
			includeReserved: true
		);
		var threatened = _contacts.All.Any(c =>
			c.Category == UnitCategory.Plane
			&& UnitFacts.IsStrikePlane(c.Kind)
			&& game.Units[c.Unit].IsUsed
			&& game.Units[c.Unit].IsFound
			&& UnitFacts.Distance(c.Position, basePosition) <= 5000
		);
		if (fighters.Count < (threatened ? 3 : 6))
			return false;

		_watchLeader = fighters[0];
		ShowDeck(raided);
		SelectPlanes(fighters[0], fighters.Skip(1));
		SendSelected(
			fighters[0],
			PointAt(basePosition, BearingTo(basePosition, ScenarioKnowledge.Saipan.Center), 1000),
			$"and {fighters.Count - 1} more watch for bombers at"
		);
		return true;
	}

	// Scenario 107: the ships sail to the ambush, each alone, once, from the start: formed (TryForm), the destroyers
	// keep their places on the carrier's beam, which runs them aground on the coast north-west of the start, and the
	// carrier waits for them.
	private bool TrySailToAmbush(Nspw game)
	{
		if (_hasSailedToAmbush)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| ship.Category != UnitCategory.Ship
				|| UnitFacts.IsBase(ship.Kind)
				|| UnitFacts.Distance(UnitFacts.FinalDestination(ref ship), YamamotoAmbush) < 600
			)
				continue;

			SelectAlone(m, "ambushing");
			foreach (var point in AmbushWay)
				_clicks.Enqueue(g =>
				{
					if (g.SelectedUnit != m)
						return GiveUp(g, $"ambushing {m}: selected {g.SelectedUnit}");
					g.Commands.SetDestination(point);
					return true;
				});
			_clicks.Enqueue(g =>
				Log?.Invoke($"{UnitFacts.Now(g)}: {g.Units[m].Kind} {m} sails to wait for Yamamoto")
			);
			Deselect();
			return true;
		}

		_hasSailedToAmbush = true;
		return false;
	}

	// Scenario 107: each warship within 1500 of Yamamoto's plane, found, takes it as its gun target and sails under it,
	// every 200 frames: its guns reach a plane only close.
	private bool TryGunsOnYamamoto(Nspw game)
	{
		if (Yamamoto(game) is not { } bomber)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| !IsWarship(ship.Kind)
				|| UnitFacts.Distance(ship.Position, game.Units[bomber.Unit].Position) > 1500
				|| UnitFacts.Now(game) - _gunsOnYamamoto.GetValueOrDefault(m, -1000) < 200
			)
				continue;

			_gunsOnYamamoto[m] = UnitFacts.Now(game);
			var target = bomber.Unit;
			SelectAlone(m, "firing at Yamamoto");
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != m || !g.Units[target].IsUsed)
					return GiveUp(g, $"firing at Yamamoto {m}: selected {g.SelectedUnit}");
				g.Commands.ClickUnit(target);
				Log?.Invoke(
					$"{UnitFacts.Now(g)}: {g.Units[m].Kind} {m} fires at Yamamoto's {g.Units[target].Kind} {target}"
				);
				return true;
			});
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != m || !g.Units[target].IsUsed)
					return GiveUp(g, $"sailing under Yamamoto {m}: selected {g.SelectedUnit}");
				g.Commands.SetDestination(g.Units[target].Position);
				return true;
			});
			Deselect();
			return true;
		}

		return false;
	}

	// The player's first unit of a kind, or 0.
	private static int FirstOwn(Nspw game, UnitKind kind)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (IsOwn(game, m) && game.Units[m].Kind == kind)
				return m;
		}

		return 0;
	}

	// The enemy cities destroyed: found once, gone since.
	private int CitiesDestroyed(Nspw game)
	{
		foreach (var contact in _contacts.All.Where(c => c.Kind == UnitKind.City))
		{
			_seenCities.Add(contact.Unit);
		}

		return _seenCities.Count(m => !game.Units[m].IsUsed || game.Units[m].Kind != UnitKind.City);
	}

	// Scenario 300: whether the carrier is where its bombers launch: their fuel carries them over Tokyo and on to the
	// corner, or the carrier's own fuel, 9 at the start, is all but spent.
	private bool IsDoolittleLaunching(Nspw game)
	{
		var carrier = FirstOwn(game, UnitKind.Carrier);
		var bomber = FirstOwn(game, UnitKind.Bomber);
		if (carrier == 0 || bomber == 0 || game.Units[carrier].Fuel <= 1)
			return true;

		ref var plane = ref game.Units[bomber];
		var range = plane.MaxSpeed * plane.FuelInterval * plane.Fuel;
		var tokyo = ScenarioKnowledge.Tokyo.Center;
		return UnitFacts.Distance(game.Units[carrier].Position, tokyo)
				+ UnitFacts.Distance(tokyo, DoolittleCorner)
			<= range * DoolittleRangeShare;
	}

	// Scenario 300: the carrier sails toward Tokyo until its bombers can reach the corner after it, or its fuel is
	// spent; then, two cities destroyed, each bomber, in the air or on the deck, flies to the corner.
	private bool TryDoolittle(Nspw game)
	{
		if (CitiesDestroyed(game) >= 2)
		{
			if (!IsMissionOrderDue(game, 60))
				return false;

			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var bomber = ref game.Units[m];
				if (
					!IsOwn(game, m)
					|| bomber.Kind != UnitKind.Bomber
					|| bomber.GroupLeader != 0
					|| UnitFacts.Distance(UnitFacts.FinalDestination(ref bomber), DoolittleCorner)
						< 300
				)
					continue;

				_missionOrdered = UnitFacts.Now(game);
				var plane = m;
				if (bomber.PlaneState == UnitState.Parked)
				{
					if (
						!UnitFacts.IsParked(ref bomber)
						|| !UnitFacts.CanLaunch(ref game.Units[bomber.Carrier])
						|| bomber.ReloadTime > 0
					)
						continue;

					ShowDeck(bomber.Carrier);
					SelectPlanes(m, []);
				}
				else
				{
					SelectAlone(m, "gathering");
					_clicks.Enqueue(g =>
					{
						if (g.SelectedUnit == plane && g.Units[plane].Mode == UnitMode.Return)
							g.Commands.ChooseCombatMenuItem(CombatMenuItem.Move);
					});
					_clicks.Wait(8);
				}

				SendSelected(m, DoolittleCorner, "gathers at");
				return true;
			}

			return false;
		}

		if (IsDoolittleLaunching(game) || !IsMissionOrderDue(game, 300))
			return false;

		var carrier = FirstOwn(game, UnitKind.Carrier);
		var tokyo = ScenarioKnowledge.Tokyo.Center;
		if (UnitFacts.Distance(UnitFacts.FinalDestination(ref game.Units[carrier]), tokyo) < 3000)
			return false;

		_missionOrdered = UnitFacts.Now(game);
		SelectAlone(carrier, "sailing to launch");
		return SailSelected(
				game,
				carrier,
				PointAt(tokyo, BearingTo(tokyo, game.Units[carrier].Position), 2500),
				"sails toward Tokyo, to"
			) || true;
	}

	// The player's air base nearest a point, or 0.
	private static int NearestOwnAirBase(Nspw game, WorldPosition position)
	{
		var best = 0;
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (
				IsOwn(game, m)
				&& game.Units[m].Kind == UnitKind.AirBase
				&& (
					best == 0
					|| UnitFacts.Distance(game.Units[m].Position, position)
						< UnitFacts.Distance(game.Units[best].Position, position)
				)
			)
				best = m;
		}

		return best;
	}

	// Scenario 205: the strategic bomber that flies the rescue, the first of Nouméa's.
	private int RescueBomber(Nspw game)
	{
		if (_rescueBomber == 0 || !IsOwn(game, _rescueBomber))
			_rescueBomber = FirstOwn(game, UnitKind.Bomber);
		return _rescueBomber;
	}

	// Gives the selected flying plane a base, and brings it home there: a click on the base (SelectFriendlyTarget),
	// then Return.
	private void LandSelectedAt(int plane, int airBase)
	{
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != plane)
				return GiveUp(g, $"landing {plane} at {airBase}: selected {g.SelectedUnit}");
			g.Commands.ClickUnit(airBase);
			return true;
		});
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != plane || g.Units[plane].Carrier != airBase)
				return GiveUp(
					g,
					$"landing {plane} at {airBase}: selected {g.SelectedUnit}, based at {g.Units[plane].Carrier}"
				);
			g.Commands.ChooseCombatMenuItem(CombatMenuItem.Return);
			Log?.Invoke(
				$"{UnitFacts.Now(g)}: {g.Units[plane].Kind} {plane} lands at {g.Units[airBase].Kind} {airBase}"
			);
			return true;
		});
		_clicks.Wait(8);
		Deselect();
	}

	// Scenario 205: the airfields the rescue lands at in turn: Bougainville's, then home, Nouméa's.
	private List<int> RescueStops(Nspw game)
	{
		if (_rescueStops.Count == 0)
			_rescueStops.AddRange([
				NearestOwnAirBase(game, ScenarioKnowledge.Bougainville.Center),
				game.Units[RescueBomber(game)].Carrier,
			]);
		return _rescueStops;
	}

	// Scenario 205: Guadalcanal's ready fighters fly to Bougainville's airfield, once, and are based there, to defend
	// it from the raids that would bring it under a fifth of its HP before the bomber lands, which loses the scenario.
	private bool TryDefendBougainville(Nspw game)
	{
		if (_hasDefendedBougainville)
			return false;

		var guadalcanal = NearestOwnAirBase(game, ScenarioKnowledge.Guadalcanal.Center);
		var bougainville = NearestOwnAirBase(game, ScenarioKnowledge.Bougainville.Center);
		if (
			guadalcanal == 0
			|| bougainville == 0
			|| guadalcanal == bougainville
			|| !UnitFacts.CanLaunch(ref game.Units[guadalcanal])
		)
			return false;

		var fighters = ParkedOn(
			game,
			guadalcanal,
			n => game.Units[n].Kind == UnitKind.Fighter && UnitFacts.IsReady(ref game.Units[n])
		);
		if (fighters.Count == 0)
			return false;

		_hasDefendedBougainville = true;
		_defenders = fighters[0];
		ShowDeck(guadalcanal);
		SelectPlanes(fighters[0], fighters.Skip(1));
		SendSelected(fighters[0], game.Units[bougainville].Position, "defends");
		return true;
	}

	// Scenario 205: the defenders, once in the air, are based at Bougainville's airfield.
	private bool TryBaseDefenders(Nspw game)
	{
		var bougainville = NearestOwnAirBase(game, ScenarioKnowledge.Bougainville.Center);
		if (
			_defenders == 0
			|| !IsOwn(game, _defenders)
			|| game.Units[_defenders].PlaneState != UnitState.Flying
			|| game.Units[_defenders].Carrier == bougainville
		)
			return false;

		var leader = _defenders;
		_defenders = 0;
		SelectAlone(leader, "basing");
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit == leader)
				g.Commands.ClickUnit(bougainville);
		});
		Deselect();
		return true;
	}

	// Scenario 205: Nouméa's bomber lands at Bougainville's airfield, and back at Nouméa (ComputerPlayer.md, decision):
	// parked, once serviced, it takes off for the next airfield, by a point clear of the enemy's fighters, with the
	// ready fighters there as escort; in the air, it and its escort are given that airfield as their base, so that the
	// fighters, short of fuel, come down there too; and once within 1000 of it, the bomber comes down (Return), not
	// before, as returning planes leave their formation.
	private bool TryRescue(Nspw game)
	{
		var bomber = RescueBomber(game);
		if (TryDefendBougainville(game) || TryBaseDefenders(game))
			return true;

		if (bomber == 0 || !IsMissionOrderDue(game, 20))
			return false;

		ref var plane = ref game.Units[bomber];
		var stops = RescueStops(game);
		if (
			plane.PlaneState == UnitState.Parked
			&& _rescueLeg < stops.Count
			&& plane.Carrier == stops[_rescueLeg]
		)
			_rescueLeg++;

		if (_rescueLeg >= stops.Count || !game.Units[stops[_rescueLeg]].IsUsed)
			return false;

		var next = stops[_rescueLeg];
		if (plane.PlaneState == UnitState.Parked)
		{
			var deck = plane.Carrier;
			if (
				!UnitFacts.IsParked(ref plane)
				|| plane.ReloadTime > 0
				|| !UnitFacts.CanLaunch(ref game.Units[deck])
			)
				return false;

			_missionOrdered = UnitFacts.Now(game);
			var escorts = ParkedOn(
					game,
					deck,
					n =>
						game.Units[n].Kind == UnitKind.Fighter
						&& UnitFacts.IsReady(ref game.Units[n])
				)
				.Take(6);
			ShowDeck(deck);
			SelectPlanes(bomber, escorts);
			SendSelected(bomber, RescueWaypoint, "flies the rescue by");
			return true;
		}

		if (
			UnitFacts.Distance(plane.Position, RescueWaypoint) <= 300
			&& UnitFacts.Distance(UnitFacts.FinalDestination(ref plane), game.Units[next].Position)
				> 300
		)
		{
			_missionOrdered = UnitFacts.Now(game);
			SelectAlone(bomber, "rescuing");
			SendSelected(bomber, game.Units[next].Position, "flies the rescue on to");
			return true;
		}

		var isNear = UnitFacts.Distance(plane.Position, game.Units[next].Position) <= 1000;
		if (plane.Carrier == next && (!isNear || plane.Mode == UnitMode.Return))
			return false;

		_missionOrdered = UnitFacts.Now(game);
		SelectAlone(bomber, "rescuing");
		if (plane.Carrier != next)
		{
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit == bomber)
					g.Commands.ClickUnit(next);
			});
			Deselect();
		}
		else
		{
			LandSelectedAt(bomber, next);
		}

		return true;
	}

	// Whether a unit is kept for the mission, out of the general tasks.
	private bool IsReserved(Nspw game, int m) =>
		game.ScenarioNumber switch
		{
			205 => m == RescueBomber(game),
			10 => IsKeptForWatch(game, m),
			_ => false,
		};

	// Whether the places the briefing names are destroyed: what the victory conditions count there has been found, and is
	// gone, all of it.
	private bool ArePlacesDestroyed(Nspw game)
	{
		foreach (
			var contact in _contacts.All.Where(c =>
				IsCounted(c.Kind)
				&& _knowledge.Enemy.Any(a =>
					a.IsPlace && UnitFacts.Distance(c.Position, a.Center) <= a.Radius + 800
				)
			)
		)
		{
			_placeTargets.Add(contact.Unit);
		}

		return _placeTargets.Count > 0 && _placeTargets.All(m => !game.Units[m].IsUsed);
	}

	// Once the places the briefing names are destroyed, every ship of the player's that leads or sails alone sails back to
	// where the briefing wants it at the end, its formation with it.
	private bool TryReturnHome(Nspw game)
	{
		if (
			_knowledge.Home is not { } home
			|| !ArePlacesDestroyed(game)
			|| !IsMissionOrderDue(game, 40)
		)
			return false;

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| ship.Category != UnitCategory.Ship
				|| UnitFacts.IsBase(ship.Kind)
				|| ship.GroupLeader != 0
				|| UnitFacts.IsAnchored(ref ship)
				|| UnitFacts.Distance(UnitFacts.FinalDestination(ref ship), home.Center)
					< home.Radius * 0.6
			)
				continue;

			_missionOrdered = UnitFacts.Now(game);
			SelectAlone(m, "coming home");
			var point = PointAt(home.Center, m * 47.0, Math.Min(home.Radius * 0.5, 80.0 * (m % 6)));
			return SailSelected(game, m, point, "comes home to") || true;
		}

		return false;
	}

	// The scenario's mission, if it has one: true if it gave an order.
	private bool TryMission(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => TryDoolittle(game),
			205 => TryRescue(game),
			8 when Parameters.GuardAnchorage => TryGuardAnchorage(game),
			10 when Parameters.GatherAndSweep => TryGatherFighters(game)
				|| (Parameters.BomberWatch && TryWatchForBombers(game))
				|| TrySweepSaipan(game),
			9 when Parameters.OpeningStrike => TryOpeningStrike(game)
				|| TryAmbushTrack(game)
				|| TryAimOpening(game),
			201 when Parameters.InterceptCarrier => TrySailAtHornet(game)
				|| TryTorpedoHornet(game)
				|| TryOpeningStrike(game)
				|| TryAimOpening(game),
			109 when Parameters.RaidYokosuka => TryReturnHome(game) || TrySailToRaid(game),
			107 when Parameters.AmbushYamamoto => TrySailToAmbush(game) || TryGunsOnYamamoto(game),
			_ => TryReturnHome(game),
		};

	// Whether the mission holds the general strikes back.
	private bool HoldsStrikes(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => !IsDoolittleLaunching(game) || CitiesDestroyed(game) >= 2,
			107 => Parameters.AmbushYamamoto,
			109 when Parameters.RaidYokosuka => !IsAtRaidPoint(game)
				|| (_knowledge.Home is not null && ArePlacesDestroyed(game)),
			_ => _knowledge.Home is not null && ArePlacesDestroyed(game),
		};

	// Whether the mission holds the ships back from the general tasks that form them and send them at the enemy
	// (TryForm, TryFormBattleGroups, TryEngage).
	private bool HoldsShips(Nspw game) =>
		(game.ScenarioNumber == 107 && Parameters.AmbushYamamoto)
		|| (game.ScenarioNumber == 201 && Parameters.InterceptCarrier);

	// Whether the mission holds the fighters back from intercepting, hunting and patrolling.
	private bool HoldsFighters(Nspw game) =>
		game.ScenarioNumber == 107 && Parameters.AmbushYamamoto && !IsAtAmbush(game);

	// Whether the mission holds the scouts back.
	private bool HoldsScouts(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 or 205 => true,
			_ => false,
		};

	// Whether the mission holds the carriers' general moves back.
	private bool HoldsFleet(Nspw game) =>
		game.ScenarioNumber switch
		{
			300 => true,
			107 => Parameters.AmbushYamamoto,
			8 => Parameters.GuardAnchorage,
			109 when Parameters.RaidYokosuka => true,
			201 => Parameters.InterceptCarrier,
			_ => _knowledge.Home is not null && ArePlacesDestroyed(game),
		};
}
