using System.Runtime.CompilerServices;

namespace OpenNspw;

// The target lists of the computer's fleets (docs/ComputerPlayer.md, Fleets), which 1.22's scenarios build their fleets
// with, and its computer player changes as the scenarios' events say.
public partial class Nspw
{
	// Element i of the fleet's members, read on into the fields that follow them as 1.22 reads past the last.
	public static ref short MemberAt(ref ComputerFleet fleet, int i)
	{
		return ref Unsafe.Add(ref fleet.Members[0], i);
	}

	// Element i of the fleet's targets, read on into its members as 1.22 reads index 18.
	public static ref short TargetAt(ref ComputerFleet fleet, int i)
	{
		return ref Unsafe.Add(ref fleet.Targets[0], i);
	}

	// sub_41F230, sub_41F300, sub_41F3C0, sub_41F490: adds every enemy unit of the kinds to fleet f's targets, from the
	// first empty element of 1 to 17, unless it is in elements 1 to 17 already. There is no limit: past element 17, 1.22
	// writes on into the members.
	private void AddTargets(int f, Func<UnitKind, bool> isTarget)
	{
		ref var fleet = ref FleetAt(f);
		var next = 1;
		while (next <= 17 && fleet.Targets[next] != 0)
			next++;

		for (var i = 1; i <= MaxUnitId; i++)
		{
			ref var unit = ref Units[i];
			if (!unit.IsUsed || unit.Side == CpuSide || !isTarget(unit.Kind))
				continue;

			var isListed = false;
			for (var k = 1; k <= 17; k++)
			{
				if (fleet.Targets[k] == i)
					isListed = true;
			}

			if (!isListed)
				TargetAt(ref fleet, next++) = (short)i;
		}
	}

	public void AddTargetsOfKind(int f, UnitKind kind) => AddTargets(f, k => k == kind);

	public void AddNavalBaseTargets(int f) => AddTargets(f, k => k == UnitKind.NavalBase);

	// sub_41F3C0: the carriers and light carriers.
	public void AddCarrierTargets(int f) =>
		AddTargets(f, k => k is UnitKind.Carrier or UnitKind.LightCarrier);

	// sub_41F490: the battleships, cruisers and carriers.
	public void AddShipTargets(int f) =>
		AddTargets(
			f,
			k =>
				k
					is UnitKind.Battleship
						or UnitKind.Cruiser
						or UnitKind.Carrier
						or UnitKind.LightCarrier
		);

	// sub_404D80: shuffles fleet f's targets, up to the first empty element, with rand()%18+1 random swaps.
	public void ShuffleTargets(int f)
	{
		ref var fleet = ref FleetAt(f);
		var count = 0;
		if (fleet.Targets[1] != 0)
		{
			count = 1;
			while (TargetAt(ref fleet, count + 1) != 0)
				count++;
		}

		if (count <= 1)
			return;

		for (var swaps = rand() % 18 + 1; swaps > 0; swaps--)
		{
			var i = rand() % count + 1;
			var j = rand() % count + 1;
			if (i != j)
				(TargetAt(ref fleet, i), TargetAt(ref fleet, j)) = (
					TargetAt(ref fleet, j),
					TargetAt(ref fleet, i)
				);
		}
	}
}
