using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Actors.Tests;

public sealed class ManualSchedulerTests
{
	// Two pooled mailboxes, one given "a1" and "a2", the other "b": what each handles is written, in the order handled.
	private static string RunTwoMailboxes(ManualScheduler scheduler)
	{
		var handled = new List<string>();
		var a = new Mailbox<string>(handled.Add, _ => { }, scheduler: scheduler);
		var b = new Mailbox<string>(handled.Add, _ => { }, scheduler: scheduler);
		a.Start();
		b.Start();
		a.Post("a1");
		a.Post("a2");
		b.Post("b");
		while (scheduler.RunNext()) { }
		return string.Join(" ", handled);
	}

	[Fact]
	public void ExploresEveryOrderOfASmallTest()
	{
		var orders = new HashSet<string>();

		var (runs, isExhausted) = ManualScheduler.ExploreAll(
			scheduler => orders.Add(RunTwoMailboxes(scheduler)),
			maxRuns: 100
		);

		// A run that finds its mailbox empty is a step too, so that there are more orders of steps than of messages.
		isExhausted.Should().BeTrue();
		runs.Should().BeGreaterOrEqualTo(3);
		orders.Should().BeEquivalentTo(["a1 a2 b", "a1 b a2", "b a1 a2"]);
	}

	[Fact]
	public void StopsExploringAtItsLimit()
	{
		var (runs, isExhausted) = ManualScheduler.ExploreAll(
			scheduler => RunTwoMailboxes(scheduler),
			maxRuns: 2
		);

		isExhausted.Should().BeFalse();
		runs.Should().Be(2);
	}

	// From the second step on, the first stays what it is in the order of scheduling: "a1".
	[Fact]
	public void ExploresOnlyFromTheStepGiven()
	{
		var orders = new HashSet<string>();

		var (_, isExhausted) = ManualScheduler.ExploreAll(
			scheduler => orders.Add(RunTwoMailboxes(scheduler)),
			maxRuns: 100,
			fromStep: 1
		);

		isExhausted.Should().BeTrue();
		orders.Should().BeEquivalentTo(["a1 b a2", "a1 a2 b"]);
	}

	// With no delay, only the order of scheduling; with one, also each order that passes over one mailbox once.
	[Fact]
	public void ExploresTheOrdersWithinTheDelaysGiven()
	{
		var orders = new HashSet<string>();

		var (runs, isExhausted) = ManualScheduler.ExploreAll(
			scheduler => orders.Add(RunTwoMailboxes(scheduler)),
			maxRuns: 100,
			maxDelays: 0
		);

		isExhausted.Should().BeTrue();
		runs.Should().Be(1);
		orders.Should().BeEquivalentTo(["a1 b a2"]);

		ManualScheduler.ExploreAll(
			scheduler => orders.Add(RunTwoMailboxes(scheduler)),
			maxRuns: 100,
			maxDelays: 1
		);
		orders.Should().BeEquivalentTo(["a1 b a2", "a1 a2 b", "b a1 a2"]);
	}

	// A mailbox that has handled a message is scheduled again, after those that wait.
	[Fact]
	public void FollowsTheChoicesGiven()
	{
		RunTwoMailboxes(ManualScheduler.Following([])).Should().Be("a1 b a2");
		RunTwoMailboxes(ManualScheduler.Following([1])).Should().Be("b a1 a2");
		RunTwoMailboxes(ManualScheduler.Following([0, 1])).Should().Be("a1 a2 b");
	}

	[Fact]
	public void TakesTheSameOrderAgainWithTheSameSeed()
	{
		for (var seed = 0; seed < 20; seed++)
		{
			RunTwoMailboxes(ManualScheduler.WithPriorities(seed, depth: 2, steps: 3))
				.Should()
				.Be(RunTwoMailboxes(ManualScheduler.WithPriorities(seed, depth: 2, steps: 3)));
			RunTwoMailboxes(new ManualScheduler(seed))
				.Should()
				.Be(RunTwoMailboxes(new ManualScheduler(seed)));
		}
	}

	// A bug of depth 2: it shows only if "b" is handled between "a1" and "a2". PCT with that depth takes every order of
	// the three in a few seeds.
	[Fact]
	public void PrioritiesTakeEachOrderOfTheDepthTheyAreGiven()
	{
		var orders = Enumerable
			.Range(0, 50)
			.Select(seed =>
				RunTwoMailboxes(ManualScheduler.WithPriorities(seed, depth: 2, steps: 3))
			)
			.ToHashSet();

		orders.Should().BeEquivalentTo(["a1 a2 b", "a1 b a2", "b a1 a2"]);
	}
}
