using System.Diagnostics;
using System.Text.Json;

namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// One second of a process of the load test, as one line of JSON on its standard output.
/// </summary>
internal sealed record Report
{
	public required string Role { get; init; }

	public int Id { get; init; }

	public double Seconds { get; init; }

	/// <summary>
	/// The clients in the session (the server), or connected (a process of clients).
	/// </summary>
	public int Clients { get; init; }

	public int Failed { get; init; }

	/// <summary>
	/// The sessions that ended for a client after it had joined.
	/// </summary>
	public int Terminated { get; init; }

	/// <summary>
	/// The messages waiting in the session's mailbox, the fullest connection loop's, and the events', as the second
	/// ends (the server).
	/// </summary>
	public int SessionQueue { get; init; }

	public int LoopQueue { get; init; }

	public int EventQueue { get; init; }

	public long MessagesIn { get; init; }

	public long MessagesOut { get; init; }

	public long DatagramsIn { get; init; }

	public long DatagramsOut { get; init; }

	/// <summary>
	/// The sends the system refused.
	/// </summary>
	public long SendErrors { get; init; }

	/// <summary>
	/// The latency of the messages received, from their send, in microseconds.
	/// </summary>
	public long P50 { get; init; }

	public long P99 { get; init; }

	public long P999 { get; init; }

	public long Max { get; init; }

	/// <summary>
	/// The CPU the process used, in cores.
	/// </summary>
	public double Cores { get; init; }

	public double AllocatedMBps { get; init; }

	public int Gen0 { get; init; }

	public int Gen1 { get; init; }

	public int Gen2 { get; init; }

	public double GcPauseMs { get; init; }

	/// <summary>
	/// The contended locks of the process, as the runtime counts them.
	/// </summary>
	public double LockContentions { get; init; }

	public double PoolQueue { get; init; }

	public double PoolThreads { get; init; }

	public double WorkingSetMB { get; init; }

	private static readonly JsonSerializerOptions s_options = new() { WriteIndented = false };

	public string ToJson() => JsonSerializer.Serialize(this, s_options);

	public static Report? FromJson(string line)
	{
		try
		{
			return JsonSerializer.Deserialize<Report>(line, s_options);
		}
		catch (JsonException)
		{
			return null;
		}
	}
}

/// <summary>
/// What the process used since the last sample: CPU, allocations, collections and their pauses, and the runtime's
/// counters. Owned by the reporting thread.
/// </summary>
internal sealed class ProcessStats
{
	private readonly Process _process = Process.GetCurrentProcess();
	private readonly RuntimeCounters _counters = new();
	private readonly long _start = Stopwatch.GetTimestamp();
	private long _lastTimestamp = Stopwatch.GetTimestamp();
	private TimeSpan _lastCpu = Process.GetCurrentProcess().TotalProcessorTime;
	private long _lastAllocated = GC.GetTotalAllocatedBytes();
	private int _lastGen0 = GC.CollectionCount(0);
	private int _lastGen1 = GC.CollectionCount(1);
	private int _lastGen2 = GC.CollectionCount(2);
	private TimeSpan _lastPause = GC.GetTotalPauseDuration();

	/// <summary>
	/// Fills the process's part of <paramref name="report"/>.
	/// </summary>
	public Report Sample(Report report)
	{
		var now = Stopwatch.GetTimestamp();
		var elapsed = Stopwatch.GetElapsedTime(_lastTimestamp, now).TotalSeconds;
		_process.Refresh();
		var cpu = _process.TotalProcessorTime;
		var allocated = GC.GetTotalAllocatedBytes();
		var gen0 = GC.CollectionCount(0);
		var gen1 = GC.CollectionCount(1);
		var gen2 = GC.CollectionCount(2);
		var pause = GC.GetTotalPauseDuration();
		var sampled = report with
		{
			Seconds = Math.Round(Stopwatch.GetElapsedTime(_start, now).TotalSeconds, 1),
			Cores = Math.Round((cpu - _lastCpu).TotalSeconds / elapsed, 2),
			AllocatedMBps = Math.Round((allocated - _lastAllocated) / elapsed / 1e6, 1),
			Gen0 = gen0 - _lastGen0,
			Gen1 = gen1 - _lastGen1,
			Gen2 = gen2 - _lastGen2,
			GcPauseMs = Math.Round((pause - _lastPause).TotalMilliseconds, 1),
			LockContentions = _counters.Get("monitor-lock-contention-count"),
			PoolQueue = _counters.Get("threadpool-queue-length"),
			PoolThreads = _counters.Get("threadpool-thread-count"),
			WorkingSetMB = Math.Round(_process.WorkingSet64 / 1e6, 0),
		};
		_lastTimestamp = now;
		_lastCpu = cpu;
		_lastAllocated = allocated;
		_lastGen0 = gen0;
		_lastGen1 = gen1;
		_lastGen2 = gen2;
		_lastPause = pause;
		return sampled;
	}
}
