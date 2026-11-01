using System.Globalization;

namespace OpenNspw.Arena;

// Every aerial torpedo of a game, either side's, from its drop to its hit or its end: how it was dropped and whether it
// hit, to find the drop that hits most (docs/StrongAIResearch.md). An aerial torpedo runs straight on the plane's
// heading at the drop, 1.8 a tick for 240 ticks, armed after 15 (PlaneDropTorpedo), and hits a ship whose strip of
// boxes along its heading it enters (CheckHit). It reads the whole state: it is for the developer, not the player.
internal sealed class TorpedoLog
{
	// The ticks an aerial torpedo runs (PlaneDropTorpedo), which tells it from a ship's.
	private const int AerialFlightTime = 240;

	// A torpedo followed: in which frame it was dropped, at what, how, and how its target was then.
	private sealed record Run(
		int Frame,
		bool ByPlayer,
		int Target,
		UnitKind TargetKind,
		double Angle,
		double Distance,
		double Miss,
		double TargetSpeed,
		bool Evading,
		double TargetHeading
	)
	{
		public int Ticks { get; set; }

		public int TargetHP { get; set; }

		public bool TargetHit { get; set; }
	}

	public const string Header =
		"frame,by,target_kind,angle,distance,miss,target_speed,evading,turned,hit,hit_target";

	private readonly Dictionary<int, Run> _runs = [];
	private readonly List<string> _rows = [];

	// The torpedoes ended so far, as rows of Header.
	public IReadOnlyList<string> Rows => _rows;

	// The angle between two headings, 0 to 180.
	private static double Between(double a, double b)
	{
		var d = Math.Abs((a - b) % 360);
		return d > 180 ? 360 - d : d;
	}

	// How near the torpedo's way comes to where the target would be, both going straight on, over its run: the miss a
	// target that neither turns nor slows leaves.
	private static double Miss(ref Fire fire, ref Unit target)
	{
		var (rx, ry) = (target.Position.X - fire.Position.X, target.Position.Y - fire.Position.Y);
		var (wx, wy) = (
			(target.Direction.Cos() * target.Speed) - (fire.Direction.Cos() * fire.Speed),
			(target.Direction.Sin() * target.Speed) - (fire.Direction.Sin() * fire.Speed)
		);
		var ticks = Math.Clamp(
			-((rx * wx) + (ry * wy)) / Math.Max((wx * wx) + (wy * wy), 1e-9),
			0,
			AerialFlightTime
		);
		return Math.Sqrt(Math.Pow(rx + (wx * ticks), 2) + Math.Pow(ry + (wy * ticks), 2));
	}

	private void End(Nspw game, Run run, bool hit)
	{
		ref var target = ref game.Units[run.Target];
		var turned = target.IsUsed && Between(target.Direction.Degrees, run.TargetHeading) > 10;
		_rows.Add(
			string.Create(
				CultureInfo.InvariantCulture,
				$"{run.Frame},{(run.ByPlayer ? "player" : "computer")},{run.TargetKind},{run.Angle:F0},{run.Distance:F0},{run.Miss:F0},{run.TargetSpeed:F2},{(run.Evading ? 1 : 0)},{(turned ? 1 : 0)},{(hit ? 1 : 0)},{(hit && run.TargetHit ? 1 : 0)}"
			)
		);
	}

	public void Write(Nspw game)
	{
		for (var m = 1; m < all_head.FIRE_MAX; m++)
		{
			ref var fire = ref game.Fires[m];
			var active = fire.Target != 0 && fire.Kind == FireKind.Torpedo;
			if (_runs.TryGetValue(m, out var run))
			{
				if (active && fire.Ticks >= run.Ticks)
				{
					run.Ticks = fire.Ticks;
					if (game.Units[run.Target].HP < run.TargetHP)
						run.TargetHit = true;
					run.TargetHP = game.Units[run.Target].HP;
					continue;
				}

				// A hit frees the fire before its run is out; a miss, once its ticks pass its flight time. A fire taken
				// again within the frame is a new one, and the old one's end is not known.
				_runs.Remove(m);
				if (!active && fire.Kind == FireKind.Torpedo)
				{
					var hit = fire.Ticks <= fire.FlightTime;
					if (hit && game.Units[run.Target].HP < run.TargetHP)
						run.TargetHit = true;
					End(game, run, hit);
				}
			}

			if (!active || fire.FlightTime != AerialFlightTime || fire.Ticks > 4)
				continue;

			ref var target = ref game.Units[fire.Target];
			if (!target.IsUsed || target.Category != UnitCategory.Ship)
				continue;

			var distance = Math.Sqrt(
				Math.Pow(target.Position.X - fire.Position.X, 2)
					+ Math.Pow(target.Position.Y - fire.Position.Y, 2)
			);
			_runs[m] = new Run(
				game.Renderer.FrameCount,
				target.Side != game.LocalSide,
				fire.Target,
				target.Kind,
				Between(fire.Direction.Degrees, target.Direction.Degrees),
				distance,
				Miss(ref fire, ref target),
				target.Speed,
				target.EmergencyFlags[0] > 0,
				target.Direction.Degrees
			)
			{
				Ticks = fire.Ticks,
				TargetHP = target.HP,
			};
		}
	}
}
