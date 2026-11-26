using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki;

/// <summary>
/// Threads of their own, each a loop on which peers' actors live: it reads its sockets (<see cref="UdpPoller"/>) and runs
/// the mailboxes that wait (<see cref="CallerScheduler"/>), then sleeps for what is left of the interval, so that a
/// message wakes no thread of its own, as each does on the thread pool: one pass of a loop handles all that came since
/// the last. For a process of many connections, such as a server of many clients, or of many peers, such as a load
/// test's clients; a game of a few peers is better served by the thread pool, the default, which costs nothing idle.
/// </summary>
/// <remarks>
/// A peer's tasks complete on the loops and continue on the thread pool, so the application awaits them as with the
/// default. Its events wait in its queue for the application's DoWork, as with the default. Each loop wakes about once an
/// interval even when idle, and a message to an actor of another loop waits for that loop's next pass. Close the peers
/// before disposing of the loops, which their closing needs.
/// </remarks>
public sealed class PollingLoops : IAsyncDisposable
{
	private sealed class Loop
	{
		public UdpPoller Poller { get; } = new();

		public CallerScheduler Scheduler { get; } = new();

		public TaskCompletionSource Stopped { get; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	private readonly ImmutableArray<Loop> _loops;
	private readonly TimeSpan _interval;
	private readonly CancellationTokenSource _stopping = new();

	// Hands each peer placed on one loop the next loop in turn.
	private readonly Sequence _placed = new();

	// Passes over the loop's sockets and mailboxes until stopping, each at least an interval after the last began.
	private static void Run(Loop loop, TimeSpan interval, CancellationToken stopping)
	{
		try
		{
			while (!stopping.IsCancellationRequested)
			{
				var start = Stopwatch.GetTimestamp();
				loop.Poller.Poll();
				loop.Scheduler.RunReady();
				// In whole milliseconds, rounded up: the system's sleep takes whole milliseconds and drops the rest, and
				// a sleep of none would spin.
				var left = interval - Stopwatch.GetElapsedTime(start);
				if (left > TimeSpan.Zero)
					Thread.Sleep((int)Math.Ceiling(left.TotalMilliseconds));
			}
		}
		finally
		{
			loop.Stopped.TrySetResult();
		}
	}

	/// <param name="count">How many loops, each a thread: one for each core the application gives to the peers.</param>
	/// <param name="interval">How often a loop passes over its sockets and mailboxes when it has time to spare: the most a
	/// datagram or a message waits for it. A millisecond if <see langword="null"/>.</param>
	public PollingLoops(int count, TimeSpan? interval = null)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
		_interval = interval ?? TimeSpan.FromMilliseconds(1);
		_loops = [.. Enumerable.Range(0, count).Select(_ => new Loop())];
		for (var i = 0; i < _loops.Length; i++)
		{
			var loop = _loops[i];
			new Thread(() => Run(loop, _interval, _stopping.Token))
			{
				IsBackground = true,
				Name = $"Otsuki loop {i}",
			}.Start();
		}
	}

	public int Count => _loops.Length;

	/// <summary>
	/// <paramref name="options"/> for one peer whose connections the loops share, such as a server: its session and its
	/// endpoint on the first loop, a connection loop on each, and a UDP socket for each loop on its port, which that loop
	/// reads. Where the system does not share a port, the first loop reads the one socket for all.
	/// </summary>
	public PeerOptions Spread(PeerOptions? options = null) =>
		(options ?? PeerOptions.Default) with
		{
			ConnectionLoops = _loops.Length,
			Scheduler = _loops[0].Scheduler,
			ConnectionLoopSchedulers = [.. _loops.Select(loop => (MailboxScheduler)loop.Scheduler)],
			TransportFactory = localEndPoint =>
				localEndPoint is IPEndPoint ipEndPoint
					? new UdpDatagramTransport(ipEndPoint, [.. _loops.Select(loop => loop.Poller)])
					: throw new NotSupportedException($"UDP cannot bind to {localEndPoint}."),
		};

	/// <summary>
	/// <paramref name="options"/> for one of many peers, such as a load test's clients: all of it on one loop, the next in
	/// turn, with one UDP socket, which that loop reads.
	/// </summary>
	public PeerOptions OnOneLoop(PeerOptions? options = null)
	{
		var loop = _loops[(int)((_placed.Next() - 1) % _loops.Length)];
		return (options ?? PeerOptions.Default) with
		{
			Scheduler = loop.Scheduler,
			ConnectionLoopSchedulers = [],
			TransportFactory = loop.Poller.CreateTransport,
		};
	}

	/// <summary>
	/// Stops the loops, once each has finished its pass, and the peers on them with it.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		await _stopping.CancelAsync().ConfigureAwait(false);
		await Task.WhenAll(_loops.Select(loop => loop.Stopped.Task)).ConfigureAwait(false);
		_stopping.Dispose();
	}
}
