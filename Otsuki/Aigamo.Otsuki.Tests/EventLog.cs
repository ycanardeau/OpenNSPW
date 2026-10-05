namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Records the events of a peer through <see cref="Peer.ReadEventsAsync"/> so that tests can wait for them.
/// </summary>
internal sealed class EventLog
{
	public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

	private readonly List<PeerEvent> _events = [];
	private readonly Lock _lock = new();

	public EventLog(Peer peer) =>
		_ = Task.Run(async () =>
		{
			await foreach (var peerEvent in peer.ReadEventsAsync())
			{
				lock (_lock)
					_events.Add(peerEvent);
			}
		});

	public IReadOnlyList<PeerEvent> Events
	{
		get
		{
			lock (_lock)
				return [.. _events];
		}
	}

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
