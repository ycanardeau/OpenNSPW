using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Actors.Tests;

public sealed class MailboxTests
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	private abstract record Message;

	private sealed record Number(int Value) : Message;

	private sealed record Text(string Value) : Message;

	private static List<T> ReceiveAll<T>(Mailbox<T> mailbox)
		where T : notnull
	{
		var received = new List<T>();
		while (mailbox.TryReceive(out var message))
			received.Add(message);
		return received;
	}

	[Fact]
	public void ReceivesInTheOrderPosted()
	{
		var mailbox = new Mailbox<int>();
		mailbox.Post(1);
		mailbox.Post(2);
		mailbox.Post(3);

		ReceiveAll(mailbox).Should().Equal(1, 2, 3);
	}

	[Fact]
	public void ReceivesOneKindKeepingTheOthersInOrder()
	{
		var mailbox = new Mailbox<Message>();
		mailbox.Post(new Number(1));
		mailbox.Post(new Text("a"));
		mailbox.Post(new Number(2));
		mailbox.Post(new Text("b"));

		mailbox.TryReceive<Text>(out var first).Should().BeTrue();
		first.Should().Be(new Text("a"));
		mailbox.TryPeek<Text>(out var peeked).Should().BeTrue();
		peeked.Should().Be(new Text("b"));
		mailbox.TryReceive<Number>(out var number).Should().BeTrue();
		number.Should().Be(new Number(1));
		mailbox.Post(new Number(3));

		ReceiveAll(mailbox).Should().Equal(new Number(2), new Text("b"), new Number(3));
	}

	[Fact]
	public void DropsWhatIsPostedBeyondItsCapacity()
	{
		var mailbox = new Mailbox<int>(capacity: 2);

		mailbox.Post(1).Should().BeTrue();
		mailbox.Post(2).Should().BeTrue();
		mailbox.Post(3).Should().BeFalse();
		mailbox.TryReceive(out _).Should().BeTrue();
		mailbox.Post(4).Should().BeTrue();

		ReceiveAll(mailbox).Should().Equal(2, 4);
	}

	[Fact]
	public void TakesNoMoreOnceCompleted()
	{
		var mailbox = new Mailbox<int>();
		mailbox.Post(1);
		mailbox.Complete();

		mailbox.Post(2).Should().BeFalse();
		mailbox.IsCompleted.Should().BeFalse();
		ReceiveAll(mailbox).Should().Equal(1);
		mailbox.IsCompleted.Should().BeTrue();
		mailbox.Wait().Should().BeFalse();
	}

	[Fact]
	public async Task WaitsForTheKindItWaitsFor()
	{
		var mailbox = new Mailbox<Message>();
		var poster = Task.Run(async () =>
		{
			await Task.Delay(50);
			mailbox.Post(new Text("a"));
			await Task.Delay(50);
			mailbox.Post(new Number(1));
		});

		mailbox.WaitFor(m => m is Number, Timeout).Should().BeTrue();
		await poster.WaitAsync(Timeout);

		ReceiveAll(mailbox).Should().Equal(new Text("a"), new Number(1));
	}

	[Fact]
	public void StopsWaitingAfterTheTimeout()
	{
		var mailbox = new Mailbox<Message>();
		mailbox.Post(new Text("a"));

		mailbox.WaitFor(m => m is Number, TimeSpan.FromMilliseconds(50)).Should().BeFalse();
	}

	// Posters race the owner, which sleeps whenever its mailbox is empty: no message is missed, and each poster's are in
	// its order.
	[Fact]
	public async Task MissesNoMessageOfPostersThatRaceASleepingOwner()
	{
		const int Posters = 4;
		const int Messages = 20_000;
		var mailbox = new Mailbox<(int Poster, int Index)>();
		var posters = Enumerable
			.Range(0, Posters)
			.Select(poster =>
				Task.Run(() =>
				{
					for (var i = 0; i < Messages; i++)
					{
						mailbox.Post((poster, i));
						if (i % 1000 == 0)
							Thread.Sleep(1);
					}
				})
			)
			.ToArray();

		var next = new int[Posters];
		for (var received = 0; received < Posters * Messages; received++)
		{
			mailbox.WaitFor(_ => true, Timeout).Should().BeTrue();
			mailbox.TryReceive(out var message).Should().BeTrue();
			message.Index.Should().Be(next[message.Poster]);
			next[message.Poster]++;
		}

		await Task.WhenAll(posters).WaitAsync(Timeout);
		mailbox.TryReceive(out _).Should().BeFalse();
	}

	[Fact]
	public async Task RunsAPooledHandlerOneMessageAtATime()
	{
		const int Posters = 4;
		const int Messages = 10_000;
		var running = 0;
		var overlapped = false;
		var next = new int[Posters];
		var outOfOrder = false;
		var handled = 0;
		var mailbox = new Mailbox<(int Poster, int Index)>(
			message =>
			{
				overlapped |= Interlocked.Increment(ref running) != 1;
				outOfOrder |= message.Index != next[message.Poster];
				next[message.Poster]++;
				handled++;
				Interlocked.Decrement(ref running);
			},
			_ => { }
		);
		mailbox.Start();

		await Task.WhenAll(
				Enumerable
					.Range(0, Posters)
					.Select(poster =>
						Task.Run(() =>
						{
							for (var i = 0; i < Messages; i++)
								mailbox.Post((poster, i));
						})
					)
			)
			.WaitAsync(Timeout);
		mailbox.Complete();

		await mailbox.Completion.WaitAsync(Timeout);
		overlapped.Should().BeFalse();
		outOfOrder.Should().BeFalse();
		handled.Should().Be(Posters * Messages);
	}

	[Fact]
	public async Task HoldsAPooledMailboxsMessagesUntilItStarts()
	{
		var handled = new List<int>();
		var mailbox = new Mailbox<int>(handled.Add, _ => { });
		mailbox.Post(1);
		mailbox.Post(2);
		Thread.Sleep(50);
		handled.Should().BeEmpty();

		mailbox.Start();
		mailbox.Complete();

		await mailbox.Completion.WaitAsync(Timeout);
		handled.Should().Equal(1, 2);
	}

	[Fact]
	public async Task GoesOnAfterAHandlerThrows()
	{
		var handled = new List<int>();
		var errors = new List<Exception>();
		var mailbox = new Mailbox<int>(
			message =>
			{
				if (message == 1)
					throw new InvalidOperationException();
				handled.Add(message);
			},
			errors.Add
		);
		mailbox.Start();
		mailbox.Post(1);
		mailbox.Post(2);
		mailbox.Complete();

		await mailbox.Completion.WaitAsync(Timeout);
		handled.Should().Equal(2);
		errors.Should().ContainSingle().Which.Should().BeOfType<InvalidOperationException>();
	}

	[Fact]
	public async Task CompletesAPooledMailboxThatHasNoMessages()
	{
		var mailbox = new Mailbox<int>(_ => { }, _ => { });
		mailbox.Start();
		Thread.Sleep(10);

		mailbox.Complete();

		await mailbox.Completion.WaitAsync(Timeout);
	}

	[Fact]
	public async Task AwaitsEachMessageUntilCompleted()
	{
		var mailbox = new Mailbox<int>();
		var poster = Task.Run(async () =>
		{
			for (var i = 0; i < 100; i++)
			{
				mailbox.Post(i);
				if (i % 10 == 0)
					await Task.Delay(1);
			}

			mailbox.Complete();
		});

		var received = new List<int>();
		await foreach (
			var message in mailbox
				.ReadAllAsync()
				.WithCancellation(new CancellationTokenSource(Timeout).Token)
		)
			received.Add(message);
		await poster;

		received.Should().Equal(Enumerable.Range(0, 100));
	}

	[Fact]
	public async Task StopsAwaitingWhenCancelled()
	{
		var mailbox = new Mailbox<int>();
		using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

		var waiting = async () => await mailbox.WaitToReceiveAsync(cancellation.Token);

		await waiting.Should().ThrowAsync<OperationCanceledException>();
		mailbox.Post(1);
		(await mailbox.WaitToReceiveAsync()).Should().BeTrue();
	}
}
