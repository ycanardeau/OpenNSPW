namespace OpenNspw.AI;

// The strong player's map of where the enemy's carriers it cannot see most likely are (Parameters.ProbabilitySearch):
// a particle filter, as Bayesian search theory does at sea. Each particle is a place a carrier may be, and a weight.
// They start spread over the areas where the briefing expects the enemy's fleets, move as the computer's carriers move
// (ComputerPlayer.md: they close on the player's units to their strike range, and turn away within it), and lose
// almost all their weight where a unit of the player's would see a carrier and sees none; a carrier found draws most
// of them to it, and an enemy strike seen draws some back along its way. The scouts fly where they are thickest.
public sealed partial class StrongPlayer
{
	// A place an enemy carrier may be, heading somewhere at a speed a tick, and how likely.
	private sealed record Particle(
		WorldPosition Position,
		double Direction,
		double Speed,
		double Weight
	);

	// The particles of the map.
	private const int ParticleCount = 600;

	// The frames between the map's updates.
	private const int SearchFrames = 50;

	// How much of its weight a particle keeps where a unit of the player's would see a carrier and sees none: little, as
	// a carrier within a unit's sight is found, but not none, as the map's motion is only a guess.
	private const double MissedWeight = 0.05;

	private readonly Random _searchRandom = new(1);
	private List<Particle> _particles = [];
	private int _searchedAt = int.MinValue / 2;

	// A random place within an area.
	private WorldPosition RandomIn(Area area)
	{
		var angle = _searchRandom.NextDouble() * 360;
		var distance = Math.Sqrt(_searchRandom.NextDouble()) * area.Radius;
		return PointAt(area.Center, angle, distance);
	}

	private Particle NewParticle(WorldPosition position) =>
		new(
			position,
			_searchRandom.NextDouble() * 360,
			0.35 + (_searchRandom.NextDouble() * 0.35),
			1.0
		);

	// Particles spread over the areas where the briefing expects the enemy's fleets, as many in each.
	private List<Particle> Spread(int count)
	{
		var areas = _knowledge.Enemy.Where(a => !a.IsPlace).ToList();
		return areas.Count == 0
			? []
			:
			[
				.. Enumerable
					.Range(0, count)
					.Select(i => NewParticle(RandomIn(areas[i % areas.Count]))),
			];
	}

	// The player's ships and flying planes, each with how far it sees (GetSightRangeAs122).
	private static List<(WorldPosition Position, int Sight)> Eyes(Nspw game)
	{
		var eyes = new List<(WorldPosition, int)>();
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (
				IsOwn(game, m)
				&& (
					unit.Category == UnitCategory.Ship
					|| (unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Flying)
				)
			)
				eyes.Add((unit.Position, Nspw.GetSightRangeAs122(unit.Kind)));
		}

		return eyes;
	}

	// A particle moved `ticks` on, as a computer's carrier sails: toward the player's nearest unit while farther than
	// its strike range, away from it within it, a little off its course each time; a tenth of them wander.
	private Particle Move(
		Particle particle,
		List<(WorldPosition Position, int Sight)> eyes,
		double range,
		double ticks
	)
	{
		var direction = particle.Direction;
		if (eyes.Count > 0 && _searchRandom.NextDouble() >= 0.1)
		{
			var nearest = eyes.MinBy(e =>
				UnitFacts.Distance(e.Position, particle.Position)
			).Position;
			var bearing = BearingTo(particle.Position, nearest);
			direction =
				UnitFacts.Distance(nearest, particle.Position) > range ? bearing : bearing + 180;
		}

		direction += (_searchRandom.NextDouble() - 0.5) * 40;
		return particle with
		{
			Position = PointAt(particle.Position, direction, particle.Speed * ticks),
			Direction = direction,
		};
	}

	// Draws `count` particles from the map by their weights, each with a weight of 1.
	private List<Particle> Resample(List<Particle> particles, int count)
	{
		var total = particles.Sum(p => p.Weight);
		var drawn = new List<Particle>(count);
		var step = total / count;
		var at = _searchRandom.NextDouble() * step;
		var sum = 0.0;
		foreach (var particle in particles)
		{
			sum += particle.Weight;
			while (at < sum && drawn.Count < count)
			{
				drawn.Add(particle with { Weight = 1.0 });
				at += step;
			}
		}

		return drawn;
	}

	// Moves the map on, every SearchFrames, while the player hunts carriers and Parameters.ProbabilitySearch says so: the
	// particles sail; the ones a unit of the player's would see keep MissedWeight of their weight; a carrier in sight
	// draws seven in ten of them around it, and an enemy strike seen one in ten back along its way, as far as the computer
	// strikes from; then they are drawn again by their weights, a twentieth spread anew over the expected areas, so that
	// the map never gives up on them.
	private void UpdateSearchMap(Nspw game)
	{
		if (
			!Parameters.ProbabilitySearch
			|| !HuntsCarriers
			|| UnitFacts.Now(game) - _searchedAt < SearchFrames
		)
			return;

		var ticks =
			_particles.Count == 0 ? 0 : Math.Min(UnitFacts.Now(game) - _searchedAt, 500) * 2;
		_searchedAt = UnitFacts.Now(game);
		if (_particles.Count == 0)
			_particles = Spread(ParticleCount);
		if (_particles.Count == 0)
			return;

		var eyes = Eyes(game);
		var range = ComputerStrikeRange(game);
		var seen = _contacts
			.All.Where(c =>
				c.Category == UnitCategory.Ship
				&& IsCounted(c.Kind)
				&& game.Units[c.Unit].IsUsed
				&& UnitFacts.Now(game) - c.Frame < SearchFrames
			)
			.ToList();
		var moved = _particles
			.Select(p => Move(p, eyes, range, ticks))
			.Select(p =>
				eyes.Any(e => UnitFacts.Distance(e.Position, p.Position) <= e.Sight)
					? p with
					{
						Weight = p.Weight * MissedWeight,
					}
					: p
			)
			.ToList();
		var kept = Resample(moved, ParticleCount - (ParticleCount / 20));
		if (seen.Count > 0)
		{
			for (var i = 0; i < kept.Count * 7 / 10; i++)
			{
				var carrier = seen[i % seen.Count];
				kept[i] = NewParticle(
					PointAt(
						carrier.Position,
						_searchRandom.NextDouble() * 360,
						_searchRandom.NextDouble() * 300
					)
				) with
				{
					Direction = carrier.Direction,
				};
			}
		}
		else if (EnemyStrikeOrigin(game) is { } origin)
		{
			for (var i = 0; i < kept.Count / 10; i++)
				kept[i] = NewParticle(
					PointAt(
						origin,
						_searchRandom.NextDouble() * 360,
						_searchRandom.NextDouble() * 1500
					)
				);
		}

		_particles = [.. kept, .. Spread(ParticleCount / 20)];
	}

	// The likeliest place within `reach` of a deck for a scout to fly to: of fifty particles, the one with the most
	// weight around it, within 1000, but within 1500 of where another scout is already going; or null, with no map.
	private WorldPosition? LikeliestPlace(Nspw game, WorldPosition from, double reach)
	{
		if (!Parameters.ProbabilitySearch || _particles.Count == 0)
			return null;

		var taken = _scoutLegs
			.Where(s => IsOwn(game, s.Key) && game.Units[s.Key].PlaneState == UnitState.Flying)
			.Select(s => UnitFacts.FinalDestination(ref game.Units[s.Key]))
			.ToList();
		return _particles
			.Where(
				(p, i) =>
					i % (ParticleCount / 50) == 0
					&& UnitFacts.Distance(from, p.Position) <= reach
					&& taken.All(t => UnitFacts.Distance(t, p.Position) > 1500)
			)
			.Select(p =>
				(
					p.Position,
					Weight: _particles
						.Where(q => UnitFacts.Distance(q.Position, p.Position) <= 1000)
						.Sum(q => q.Weight)
				)
			)
			.OrderByDescending(c => c.Weight)
			.Select(c => (WorldPosition?)c.Position)
			.FirstOrDefault();
	}
}
