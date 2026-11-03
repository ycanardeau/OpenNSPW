using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aigamo.Otsuki.Actors;

/// <summary>
/// The messages posted to an actor, in the order they were posted: any thread posts, and only the actor that owns
/// the mailbox receives. It is the only concurrent code of Otsuki and of what is built on it: no thread shares
/// state with another, which reaches it only by posting messages.
/// </summary>
/// <remarks>
/// A mailbox runs in one of three ways:
/// <list type="bullet">
/// <item><b>Pooled</b>: given a handler, it runs it on the thread pool while it has messages, one at a time, once
/// <see cref="Start"/> is called (<see cref="Actor{TMessage}"/>).</item>
/// <item><b>Thread-bound</b>: an actor with a thread of its own takes the messages at points of its own choosing
/// (<see cref="TryReceive(out T)"/>), of one kind at one point and of another at another
/// (<see cref="TryReceive(Func{T, bool}, out T)"/>), keeping each kind's order, and sleeps until a message it waits
/// for is there (<see cref="WaitFor(Func{T, bool})"/>).</item>
/// <item><b>Awaited</b>: an async reader awaits the next message (<see cref="WaitToReceiveAsync"/>).</item>
/// </list>
/// A bounded mailbox drops what is posted beyond its capacity, as a full socket buffer drops datagrams.
/// </remarks>
public sealed class Mailbox<T>
	where T : notnull
{
	// _state is the number of messages posted and not yet taken out of the queue, times OneMessage, plus Completed once
	// the mailbox is completed. A message is counted before it is enqueued, so a count above zero with an empty queue is
	// a post on its way.
	private const long Completed = 1;
	private const long OneMessage = 2;

	private readonly ConcurrentQueue<T> _queue = new();
	private readonly int _capacity;
	private readonly Action<T>? _receive;
	private readonly Action<Exception>? _onError;

	// Released when a message is posted to an owner that may sleep, once until the owner wakes (_isSignaled).
	private readonly SemaphoreSlim _signal = new(0);

	private readonly TaskCompletionSource _completion = new(
		TaskCreationOptions.RunContinuationsAsynchronously
	);

	// The messages taken out of the queue that the owner has not received yet, in order: those it passed over as it
	// received or looked for another kind. Only the owner touches it.
	private readonly List<T> _passed = [];

	private long _state;
	private int _isSignaled;

	// Whether a pooled mailbox's handler is scheduled or runs, or is held until Start; stays set once it has completed.
	private int _isScheduled;

	/// <summary>
	/// A mailbox whose owner receives on a thread of its own, or awaits its messages.
	/// </summary>
	public Mailbox(int capacity = int.MaxValue)
	{
		_capacity = capacity;
	}

	/// <summary>
	/// A pooled mailbox: once <see cref="Start"/> is called, it runs <paramref name="receive"/> on the thread pool for
	/// each message, one at a time, and <paramref name="onError"/> for what it throws.
	/// </summary>
	public Mailbox(Action<T> receive, Action<Exception> onError, int capacity = int.MaxValue)
	{
		_capacity = capacity;
		_receive = receive;
		_onError = onError;
		_isScheduled = 1;
	}

	/// <summary>
	/// Completes when a pooled mailbox is completed and has run its handler for each of its messages.
	/// </summary>
	public Task Completion => _completion.Task;

	/// <summary>
	/// Whether the mailbox is completed and its owner has received each of its messages. On the owner's thread.
	/// </summary>
	public bool IsCompleted => Interlocked.Read(ref _state) == Completed && _passed.Count == 0;

	private void Signal()
	{
		if (Interlocked.CompareExchange(ref _isSignaled, 1, 0) == 0)
			_signal.Release();
	}

	private bool TryDequeue([MaybeNullWhen(false)] out T message)
	{
		if (!_queue.TryDequeue(out message))
			return false;

		Interlocked.Add(ref _state, -OneMessage);
		return true;
	}

	// Runs the handler for each message, until the queue is empty; then lets the next post schedule it again, and looks
	// at the queue once more, so that a message posted in between is not left.
	private void Run()
	{
		while (true)
		{
			while (TryDequeue(out var message))
			{
				try
				{
					_receive!(message);
				}
				catch (Exception exception)
				{
					_onError!(exception);
				}
			}

			if (Interlocked.Read(ref _state) == Completed)
			{
				_completion.TrySetResult();
				return;
			}

			Volatile.Write(ref _isScheduled, 0);
			if (
				(_queue.IsEmpty && Interlocked.Read(ref _state) != Completed)
				|| Interlocked.CompareExchange(ref _isScheduled, 1, 0) != 0
			)
				return;
		}
	}

	private void Schedule()
	{
		if (Interlocked.CompareExchange(ref _isScheduled, 1, 0) == 0)
			ThreadPool.UnsafeQueueUserWorkItem(
				static mailbox => mailbox.Run(),
				this,
				preferLocal: false
			);
	}

	private void Notify()
	{
		if (_receive is null)
			Signal();
		else
			Schedule();
	}

	/// <summary>
	/// Posts a message, from any thread. Returns <see langword="false"/> if the mailbox is completed, or full.
	/// </summary>
	public bool Post(T message)
	{
		var state = Interlocked.Read(ref _state);
		while (true)
		{
			if ((state & Completed) != 0 || state / OneMessage >= _capacity)
				return false;

			var seen = Interlocked.CompareExchange(ref _state, state + OneMessage, state);
			if (seen == state)
				break;

			state = seen;
		}

		_queue.Enqueue(message);
		Notify();
		return true;
	}

	/// <summary>
	/// Takes no more messages, from any thread. The messages already posted are still received.
	/// </summary>
	public void Complete()
	{
		if ((Interlocked.Or(ref _state, Completed) & Completed) == 0)
			Notify();
	}

	/// <summary>
	/// Starts running a pooled mailbox's handler, for the messages posted so far and those to come. Called once.
	/// </summary>
	public void Start()
	{
		if (_receive is null)
			throw new InvalidOperationException("Only a pooled mailbox is started.");

		Volatile.Write(ref _isScheduled, 0);
		if (!_queue.IsEmpty || Interlocked.Read(ref _state) == Completed)
			Schedule();
	}

	// The first message that matches (any, if matches is null), taken out or only looked at: first among those passed
	// over, then from the queue, passing over those that do not match.
	private bool TryTake(Func<T, bool>? matches, bool remove, [MaybeNullWhen(false)] out T message)
	{
		for (var i = 0; i < _passed.Count; i++)
		{
			if (matches is null || matches(_passed[i]))
			{
				message = _passed[i];
				if (remove)
					_passed.RemoveAt(i);
				return true;
			}
		}

		while (TryDequeue(out var next))
		{
			if (matches is null || matches(next))
			{
				if (!remove)
					_passed.Add(next);
				message = next;
				return true;
			}

			_passed.Add(next);
		}

		message = default;
		return false;
	}

	/// <summary>
	/// Receives the next message, if there is one. On the owner's thread.
	/// </summary>
	public bool TryReceive([MaybeNullWhen(false)] out T message) =>
		TryTake(null, remove: true, out message);

	/// <summary>
	/// Receives the first message that <paramref name="matches"/>, if there is one, leaving the others in their order.
	/// On the owner's thread.
	/// </summary>
	public bool TryReceive(Func<T, bool> matches, [MaybeNullWhen(false)] out T message) =>
		TryTake(matches, remove: true, out message);

	/// <summary>
	/// The first message that <paramref name="matches"/>, if there is one, left in the mailbox. On the owner's thread.
	/// </summary>
	public bool TryPeek(Func<T, bool> matches, [MaybeNullWhen(false)] out T message) =>
		TryTake(matches, remove: false, out message);

	/// <summary>
	/// Receives the first message of a kind, if there is one, leaving the others in their order. On the owner's thread.
	/// </summary>
	public bool TryReceive<TKind>([MaybeNullWhen(false)] out TKind message)
		where TKind : T
	{
		if (TryTake(static m => m is TKind, remove: true, out var taken))
		{
			message = (TKind)taken;
			return true;
		}

		message = default;
		return false;
	}

	/// <summary>
	/// The first message of a kind, if there is one, left in the mailbox. On the owner's thread.
	/// </summary>
	public bool TryPeek<TKind>([MaybeNullWhen(false)] out TKind message)
		where TKind : T
	{
		if (TryTake(static m => m is TKind, remove: false, out var taken))
		{
			message = (TKind)taken;
			return true;
		}

		message = default;
		return false;
	}

	private bool Holds(Func<T, bool> matches) => TryTake(matches, remove: false, out _);

	/// <summary>
	/// Sleeps until a message that <paramref name="matches"/> is in the mailbox, for <paramref name="timeout"/> at most
	/// (<see cref="Timeout.InfiniteTimeSpan"/> for no limit). Returns <see langword="false"/> if none came, or if the
	/// mailbox is completed with none. On the owner's thread.
	/// </summary>
	public bool WaitFor(Func<T, bool> matches, TimeSpan timeout)
	{
		var start = Stopwatch.GetTimestamp();
		while (!Holds(matches))
		{
			if (Interlocked.Read(ref _state) == Completed)
				return false;

			if (timeout == Timeout.InfiniteTimeSpan)
			{
				_signal.Wait();
			}
			else
			{
				var left = timeout - Stopwatch.GetElapsedTime(start);
				if (left <= TimeSpan.Zero || !_signal.Wait(left))
					return Holds(matches);
			}

			Volatile.Write(ref _isSignaled, 0);
		}

		return true;
	}

	/// <summary>
	/// Sleeps until a message that <paramref name="matches"/> is in the mailbox. Returns <see langword="false"/> if the
	/// mailbox is completed with none. On the owner's thread.
	/// </summary>
	public bool WaitFor(Func<T, bool> matches) => WaitFor(matches, Timeout.InfiniteTimeSpan);

	/// <summary>
	/// Sleeps until a message is in the mailbox. Returns <see langword="false"/> if the mailbox is completed with none.
	/// On the owner's thread.
	/// </summary>
	public bool Wait() => WaitFor(static _ => true);

	/// <summary>
	/// Awaits a message. Returns <see langword="false"/> once the mailbox is completed and each of its messages
	/// received. For the owner, which awaits one message at a time.
	/// </summary>
	public async ValueTask<bool> WaitToReceiveAsync(CancellationToken cancellationToken = default)
	{
		while (!Holds(static _ => true))
		{
			if (Interlocked.Read(ref _state) == Completed)
				return false;

			await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
			Volatile.Write(ref _isSignaled, 0);
		}

		return true;
	}

	/// <summary>
	/// Awaits the next message and receives it. Throws <see cref="InvalidOperationException"/> if the mailbox is
	/// completed and each of its messages received. For the owner.
	/// </summary>
	public async ValueTask<T> ReceiveAsync(CancellationToken cancellationToken = default)
	{
		while (true)
		{
			if (TryReceive(out var message))
				return message;

			if (!await WaitToReceiveAsync(cancellationToken).ConfigureAwait(false))
				throw new InvalidOperationException("The mailbox is completed.");
		}
	}

	/// <summary>
	/// Receives each message as it comes, until the mailbox is completed. For the owner.
	/// </summary>
	public async IAsyncEnumerable<T> ReadAllAsync(
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	)
	{
		while (await WaitToReceiveAsync(cancellationToken).ConfigureAwait(false))
		{
			while (TryReceive(out var message))
				yield return message;
		}
	}
}
