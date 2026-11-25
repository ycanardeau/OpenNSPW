using System.Net;
using Aigamo.Otsuki.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Runs the simulation's scenarios (<see cref="SimulationScenarios"/>) on a hundred seeds of each way of choosing the
/// order, in fixed conditions: many orders, and a failing seed is a failing test that repeats. The swarm
/// (Aigamo.Otsuki.Swarm) runs them on many more seeds, each in conditions of its own.
/// </summary>
public class SimulationTests
{
	private const int SeedsPerOrder = 100;

	private const int Seeds = SeedsPerOrder * 3;

	// At random, then by PCT's priorities of depth 2 and 3, a hundred seeds each.
	private static SimulationConditions Conditions(
		int seed,
		double lossRate = 0,
		double duplicateRate = 0
	) =>
		new()
		{
			Order = (SimulationOrder)(seed / SeedsPerOrder),
			LossRate = lossRate,
			DuplicateRate = duplicateRate,
		};

	[Fact]
	public void ClientServer_JoinsSendsAndCloses_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.ClientServerSession(seed, Conditions(seed));
	}

	// A fifth of the datagrams lost, the rest reordered: the reliable protocol still gets every guaranteed message
	// through, once, in order.
	[Fact]
	public void ClientServer_LosingAFifthOfTheDatagrams_StillDeliversEachMessageOnceInOrder()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.ClientServerSession(seed, Conditions(seed, lossRate: 0.2));
	}

	// A tenth of the datagrams delivered twice, on top of the losses: a copy is acknowledged and dropped, never
	// delivered to the application again.
	[Fact]
	public void ClientServer_DuplicatingDatagrams_StillDeliversEachMessageOnceInOrder()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.ClientServerSession(
				seed,
				Conditions(seed, lossRate: 0.1, duplicateRate: 0.1)
			);
	}

	// The same seed takes the same order again, to every byte on the wire and every tick of the clock, losses
	// included; another seed takes another. So does a seed of the swarm, with its delays and partitions.
	[Fact]
	public void SameSeed_TakesTheSameOrderAgain()
	{
		List<string> Trace(int seed, SimulationConditions conditions) =>
			SimulationScenarios
				.ClientServerSession(seed, conditions with { KeepsTrace = true })
				.Network.Trace;

		List<(int Seed, SimulationConditions Conditions)> runs =
		[
			.. Enumerable.Range(0, 5).Select(seed => (seed, Conditions(seed, lossRate: 0.2))),
			.. Enumerable
				.Range(0, 5)
				.Select(seed => (seed, SimulationConditions.Draw(seed) with { Partitions = 0 })),
		];
		foreach (var (seed, conditions) in runs)
		{
			var first = Trace(seed, conditions);
			var second = Trace(seed, conditions);
			var differs = Enumerable
				.Range(0, Math.Min(first.Count, second.Count))
				.FirstOrDefault(i => first[i] != second[i], -1);
			if (differs >= 0)
				throw new Exception(
					$"seed {seed} differs at {differs} of {first.Count}/{second.Count}:\n{string.Join("\n", first.Skip(Math.Max(0, differs - 3)).Take(5))}\n---\n{string.Join("\n", second.Skip(Math.Max(0, differs - 3)).Take(5))}"
				);
			second.Should().Equal(first, $"seed {seed} repeats");
		}

		Trace(1, Conditions(1, lossRate: 0.2))
			.Should()
			.NotEqual(Trace(2, Conditions(2, lossRate: 0.2)));
	}

	// Which loop runs a partner's connections depends on its address alone, not on a hash that each process makes
	// differently, so that a seed repeats from one run of the tests to the next.
	[Fact]
	public void LoopOfAnAddress_IsTheSameInEveryProcess()
	{
		var loops = Enumerable
			.Range(1, 8)
			.Select(i =>
				ReliableEndpoint.LoopIndex(new IPEndPoint(IPAddress.Parse($"10.7.1.{i}"), 40000), 4)
			)
			.ToList();

		loops.Should().Equal(0, 1, 2, 3, 0, 1, 2, 3);
	}

	[Fact]
	public void RejectedJoin_SaysGoodbyeBeforeClosing_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.RejectedJoin(seed, Conditions(seed));
	}

	// Three peers join a host at once: each connects to the host, then to the others as the host instructs, whatever
	// the order of all their handshakes.
	[Fact]
	public void PeerToPeer_JoinAtOnceAndBroadcast_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.PeerToPeerMesh(seed, Conditions(seed));
	}

	[Fact]
	public void PeerToPeer_LosingAndDuplicatingDatagrams_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.PeerToPeerMesh(
				seed,
				Conditions(seed, lossRate: 0.1, duplicateRate: 0.1)
			);
	}

	// The host of four peers leaves, by closing or by stopping: the three others elect the same new host, each hears
	// of it once, and the new host's name table changes reach them all.
	[Fact]
	public void HostMigration_TheOthersAgreeOnANewHost_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.HostMigration(seed, Conditions(seed));
	}

	[Fact]
	public void HostMigration_LosingDatagrams_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.HostMigration(seed, Conditions(seed, lossRate: 0.1));
	}

	// A peer joins a session of three, and the host leaves at a point of the join that the seed chooses: whether the
	// joiner gets in or not, the peers left agree on who is in and who hosts, and reach each other.
	[Fact]
	public void HostLeavesWhileAPeerJoins_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.HostLeavesWhileAPeerJoins(seed, Conditions(seed));
	}

	// The host of four leaves, and then, at a point of the migration that the seed chooses, one of the others too: the
	// two left agree on who is in and who hosts.
	[Fact]
	public void PeerLeavesDuringMigration_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.PeerLeavesDuringMigration(seed, Conditions(seed));
	}

	// Seeds beyond those tried every run, on which a peer once followed a host that had left: it kept one claim to be
	// host, and a later host's claim, which came first, was replaced by the earlier host's.
	[Theory]
	[InlineData(3196)]
	[InlineData(9204)]
	public void PeerLeavesDuringMigration_OnSeedsThatOnceLostAClaim(int seed) =>
		SimulationScenarios.PeerLeavesDuringMigration(seed, Conditions(seed));

	// A peer joins a session of three, and one of the peers already in leaves at a point of the join that the seed
	// chooses: the joiner gets in or not, and the peers left agree and reach each other.
	[Fact]
	public void PeerLeavesWhileAnotherJoins_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.PeerLeavesWhileAnotherJoins(seed, Conditions(seed));
	}

	// One peer of four loses its way to two others but not to the host: the host removes someone on each side of every
	// link lost, and those left reach each other.
	[Fact]
	public void PeerCutOffFromTwo_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.PeerCutOffFromTwo(seed, Conditions(seed));
	}

	// Peers leave, crash, join, rename themselves, change groups and send at points the seed chooses, under the faults
	// of the swarm's conditions, rough ones included.
	[Fact]
	public void Churn_InTheSwarmsConditions()
	{
		for (var seed = 0; seed < Seeds; seed++)
			SimulationScenarios.Churn(seed, SimulationConditions.Draw(seed, mayBeRough: true));
	}

	// Every order within one delay of what follows the departures of each small case (SimulationExplorations): a host of
	// three or four leaving, a second peer with it, and a join racing a leave. The swarm's explore command takes more.
	[Fact]
	public void SmallCases_InEveryOrderWithinOneDelay()
	{
		foreach (var exploration in SimulationExplorations.All)
			SimulationExplorations
				.Explore(exploration, maxRuns: 10_000, maxDelays: 1)
				.IsExhausted.Should()
				.BeTrue(exploration.Name);
	}
}
