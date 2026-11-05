using System.Collections.Immutable;
using System.Diagnostics.Tracing;
using Aigamo.Otsuki.Actors;

namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// The runtime's counters (the System.Runtime event source), each the newest value it reported, by name: the lock
/// contentions, the thread pool's queue and threads, the time in garbage collection. They come on the runtime's thread,
/// which posts each newest set for the reporter to read.
/// </summary>
internal sealed class RuntimeCounters : EventListener
{
	private readonly Latest<ImmutableDictionary<string, double>> _values = new(
		ImmutableDictionary<string, double>.Empty
	);

	// Only the runtime's thread that reports the counters writes it.
	private ImmutableDictionary<string, double> _pending = ImmutableDictionary<
		string,
		double
	>.Empty;

	public ImmutableDictionary<string, double> Values => _values.Value;

	protected override void OnEventSourceCreated(EventSource eventSource)
	{
		if (eventSource.Name == "System.Runtime")
			EnableEvents(
				eventSource,
				EventLevel.Verbose,
				EventKeywords.All,
				new Dictionary<string, string?> { ["EventCounterIntervalSec"] = "1" }
			);
	}

	protected override void OnEventWritten(EventWrittenEventArgs eventData)
	{
		if (eventData.EventName != "EventCounters" || eventData.Payload is not { Count: > 0 })
			return;

		if (eventData.Payload[0] is not IDictionary<string, object?> payload)
			return;

		if (payload.TryGetValue("Name", out var name) && name is string counter)
		{
			var value =
				payload.TryGetValue("Increment", out var increment) && increment is double delta
					? delta
				: payload.TryGetValue("Mean", out var mean) && mean is double average ? average
				: 0;
			_pending = _pending.SetItem(counter, value);
			_values.Post(_pending);
		}
	}

	public double Get(string name) => Values.TryGetValue(name, out var value) ? value : 0;
}
