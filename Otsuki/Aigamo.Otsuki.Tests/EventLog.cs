using System.Collections.Immutable;
using Aigamo.Otsuki.Actors;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Records the events of a peer, server or client as they stream, so that tests can wait for them. The task that reads
/// them posts the events so far after each (Latest), which the tests read from any thread.
/// </summary>
internal sealed class EventLog
{
	public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

	private readonly Latest<ImmutableList<PeerEvent>> _events = new([]);

	public EventLog(Peer peer)
		: this(peer.ReadEventsAsync()) { }

	public EventLog(IAsyncEnumerable<PeerEvent> events) =>
		_ = Task.Run(async () =>
		{
			var read = ImmutableList<PeerEvent>.Empty;
			await foreach (var peerEvent in events)
			{
				read = read.Add(peerEvent);
				_events.Post(read);
			}
		});

	public IReadOnlyList<PeerEvent> Events => _events.Value;

	public IReadOnlyList<T> OfType<T>(Func<T, bool>? predicate = null)
		where T : PeerEvent => [.. Events.OfType<T>().Where(predicate ?? (_ => true))];

	public async Task<IReadOnlyList<T>> WaitForAsync<T>(
		int count,
		Func<T, bool>? predicate = null,
		TimeSpan? timeout = null
	)
		where T : PeerEvent
	{
		var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
		while (true)
		{
			var matches = OfType(predicate);
			if (matches.Count >= count)
				return matches;

			if (DateTime.UtcNow > deadline)
				throw new TimeoutException(
					$"Expected {count} {typeof(T).Name} but got {matches.Count}. Events:\n{string.Join("\n", Events)}"
				);

			await Task.Delay(5);
		}
	}

	public async Task<T> WaitForAsync<T>(Func<T, bool>? predicate = null, TimeSpan? timeout = null)
		where T : PeerEvent => (await WaitForAsync(1, predicate, timeout))[0];
}
