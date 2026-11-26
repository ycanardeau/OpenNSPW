using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Actors.Tests;

public sealed class CallerSchedulerTests
{
	[Fact]
	public void RunsNothingUntilAsked()
	{
		var scheduler = new CallerScheduler();
		var handled = new List<string>();
		var mailbox = new Mailbox<string>(handled.Add, _ => { }, scheduler: scheduler);
		mailbox.Start();

		mailbox.Post("a");
		mailbox.Post("b");

		handled.Should().BeEmpty();
		scheduler.RunReady().Should().Be(1, "one run handles all that a mailbox has");
		handled.Should().Equal("a", "b");
		scheduler.RunReady().Should().Be(0);
	}

	// What a run posts to another mailbox runs in the same call, after it.
	[Fact]
	public void RunsTheMailboxesThatARunWakes()
	{
		var scheduler = new CallerScheduler();
		var handled = new List<string>();
		var b = new Mailbox<string>(handled.Add, _ => { }, scheduler: scheduler);
		var a = new Mailbox<string>(
			message =>
			{
				handled.Add(message);
				b.Post(message + " to b");
			},
			_ => { },
			scheduler: scheduler
		);
		a.Start();
		b.Start();

		a.Post("a");
		scheduler.RunReady();

		handled.Should().Equal("a", "a to b");
	}

	// A message from another thread, such as a timer's, waits for the next call, and runs on the caller's thread.
	[Fact]
	public async Task RunsWhatAnotherThreadPostsOnTheCallersThread()
	{
		var scheduler = new CallerScheduler();
		var threads = new List<int>();
		var mailbox = new Mailbox<string>(
			_ => threads.Add(Environment.CurrentManagedThreadId),
			_ => { },
			scheduler: scheduler
		);
		mailbox.Start();

		await Task.Run(() => mailbox.Post("from elsewhere"));
		threads.Should().BeEmpty();
		var caller = Environment.CurrentManagedThreadId;
		scheduler.RunReady();

		threads.Should().Equal(caller);
	}
}
