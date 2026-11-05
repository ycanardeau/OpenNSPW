using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aigamo.Otsuki.Actors;

namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// The settings of one run of the load.
/// </summary>
internal sealed record LoadSettings
{
	public int Clients { get; init; } = 100;

	public int Processes { get; init; } = 1;

	public int ClientRate { get; init; } = 30;

	public int ServerRate { get; init; } = 20;

	public int JoinRate { get; init; } = 500;

	public int Size { get; init; } = 32;

	public int Seconds { get; init; } = 30;

	public int Warmup { get; init; } = 3;

	public int Port { get; init; } = 47_624;

	public bool Guaranteed { get; init; } = true;

	/// <summary>
	/// The heaps of the server's garbage collector.
	/// </summary>
	public int ServerHeaps { get; init; } = 4;

	public string? Label { get; init; }

	public string? Out { get; init; }
}

/// <summary>
/// Runs the server and the processes of clients as processes of their own, and prints a line a second and a summary of
/// the steady state: the seconds once every client has joined and <see cref="LoadSettings.Warmup"/> more have passed.
/// </summary>
internal sealed class Driver(LoadSettings settings)
{
	private sealed record Line(int Process, Report Report);

	private readonly LoadSettings _settings = settings;

	// The lines of every process, in the order they come, from the threads that read their outputs.
	private readonly Mailbox<Line> _lines = new();
	private readonly Dictionary<int, Report> _latestClients = [];
	private readonly List<(Report Server, Report Clients)> _rows = [];

	private static (string FileName, string Arguments) Self()
	{
		var path = Environment.ProcessPath!;
		var assembly = typeof(Driver).Assembly.Location;
		return Path.GetFileNameWithoutExtension(path) == "dotnet"
			? (path, $"\"{assembly}\" ")
			: (path, "");
	}

	// The server collects garbage on its own heaps, fewer than the cores, which it shares with the clients; the clients
	// on one heap each, so that their collections do not take every core from the server.
	private Process Start(int index, string arguments)
	{
		var (fileName, prefix) = Self();
		var process = new Process
		{
			StartInfo = new ProcessStartInfo(fileName, prefix + arguments)
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			},
		};
		if (index == 0)
			process.StartInfo.Environment["DOTNET_GCHeapCount"] = _settings.ServerHeaps.ToString("x", CultureInfo.InvariantCulture);
		else
			process.StartInfo.Environment["DOTNET_gcServer"] = "0";
		process.OutputDataReceived += (_, e) =>
		{
			if (e.Data is { } data && LoadTest.Report.FromJson(data) is { } report)
				_lines.Post(new Line(index, report));
		};
		process.ErrorDataReceived += (_, e) =>
		{
			if (e.Data is { } data)
				Console.Error.WriteLine($"[{index}] {data}");
		};
		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		return process;
	}

	private static Report Sum(IEnumerable<Report> reports) =>
		reports.Aggregate(
			new Report { Role = "clients" },
			(sum, r) =>
				sum with
				{
					Clients = sum.Clients + r.Clients,
					Failed = sum.Failed + r.Failed,
					Terminated = sum.Terminated + r.Terminated,
					MessagesIn = sum.MessagesIn + r.MessagesIn,
					MessagesOut = sum.MessagesOut + r.MessagesOut,
					DatagramsIn = sum.DatagramsIn + r.DatagramsIn,
					DatagramsOut = sum.DatagramsOut + r.DatagramsOut,
					P50 = Math.Max(sum.P50, r.P50),
					P99 = Math.Max(sum.P99, r.P99),
					P999 = Math.Max(sum.P999, r.P999),
					Max = Math.Max(sum.Max, r.Max),
					Cores = sum.Cores + r.Cores,
					AllocatedMBps = sum.AllocatedMBps + r.AllocatedMBps,
					GcPauseMs = sum.GcPauseMs + r.GcPauseMs,
					LockContentions = sum.LockContentions + r.LockContentions,
					WorkingSetMB = sum.WorkingSetMB + r.WorkingSetMB,
				}
		);

	private static string Ms(long microseconds) =>
		(microseconds / 1000.0).ToString("0.0", CultureInfo.InvariantCulture);

	private void Print(Report server, Report clients)
	{
		Console.WriteLine(
			string.Create(
				CultureInfo.InvariantCulture,
				$"{server.Seconds,6:0} s  clients {server.Clients,6} ({clients.Clients} connected, {clients.Failed} failed, {clients.Terminated} ended)  sent {clients.MessagesOut,8}/s  in {server.MessagesIn,8}/s  out {server.MessagesOut,8}/s  got {clients.MessagesIn,8}/s  dgrams {server.DatagramsIn,8}/{server.DatagramsOut,-8}  p50 {Ms(server.P50),7} p99 {Ms(server.P99),7} ms  updates p99 {Ms(clients.P99),7} ms  cpu {server.Cores,5:0.00}+{clients.Cores,5:0.00}  alloc {server.AllocatedMBps,7:0.0} MB/s  gc {server.Gen0}/{server.Gen1}/{server.Gen2} {server.GcPauseMs,5:0.0} ms  locks {server.LockContentions,4:0}  pool {server.PoolThreads:0}/{server.PoolQueue:0}  ws {server.WorkingSetMB,5:0} MB"
			)
		);
	}

	private static double Median(IEnumerable<double> values)
	{
		var sorted = values.OrderBy(v => v).ToList();
		return sorted.Count == 0 ? 0 : sorted[sorted.Count / 2];
	}

	private string Summarize()
	{
		var joined = _rows.FindIndex(r => r.Server.Clients >= _settings.Clients);
		var steady = joined < 0 ? [] : _rows.Skip(joined + _settings.Warmup).ToList();
		var label = _settings.Label ?? "";
		if (steady.Count == 0)
		{
			var most = _rows.Count == 0 ? 0 : _rows.Max(r => r.Server.Clients);
			return $"| {label} | {_settings.Clients} | did not join: {most} at most | | | | | | | | |";
		}

		var p99 = steady.Max(r => r.Server.P99);
		var updatesP99 = steady.Max(r => r.Clients.P99);
		return string.Create(
			CultureInfo.InvariantCulture,
			$"| {label} | {_settings.Clients} | {Median(steady.Select(r => (double)r.Server.MessagesIn)):0} | {Median(steady.Select(r => (double)r.Server.MessagesOut)):0} | {Median(steady.Select(r => (double)r.Server.DatagramsIn)):0} / {Median(steady.Select(r => (double)r.Server.DatagramsOut)):0} | {Ms(p99)} | {Ms(updatesP99)} | {Median(steady.Select(r => r.Server.Cores)):0.00} | {Median(steady.Select(r => r.Server.AllocatedMBps)):0} | {steady.Sum(r => r.Server.GcPauseMs) / steady.Count:0.0} | {steady.Sum(r => r.Server.LockContentions) / steady.Count:0.0} |"
		);
	}

	public const string Header =
		"| Run | Clients | Messages in/s | Messages out/s | Datagrams in/out per s | p99 in (ms) | p99 updates (ms) | Server CPU (cores) | Allocated (MB/s) | GC pause (ms/s) | Lock contentions/s |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |";

	public async Task<int> RunAsync()
	{
		var s = _settings;
		var common = string.Create(
			CultureInfo.InvariantCulture,
			$"--size {s.Size} --seconds {s.Seconds + 5}{(s.Guaranteed ? "" : " --unreliable")}"
		);
		var server = Start(
			0,
			string.Create(
				CultureInfo.InvariantCulture,
				$"server --port {s.Port} --server-rate {s.ServerRate} {common}"
			)
		);
		await Task.Delay(1000);

		var processes = new List<Process> { server };
		for (var i = 0; i < s.Processes; i++)
		{
			var count = s.Clients / s.Processes + (i < s.Clients % s.Processes ? 1 : 0);
			var joins = Math.Max(1, s.JoinRate / s.Processes);
			processes.Add(
				Start(
					i + 1,
					string.Create(
						CultureInfo.InvariantCulture,
						$"clients --id {i + 1} --server 127.0.0.1:{s.Port} --clients {count} --join-rate {joins} --client-rate {s.ClientRate} {common}"
					)
				)
			);
		}

		var end = Stopwatch.GetTimestamp() + (long)(s.Seconds * Stopwatch.Frequency);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(s.Seconds));
		try
		{
			await foreach (var line in _lines.ReadAllAsync(timeout.Token))
			{
				if (line.Process != 0)
				{
					_latestClients[line.Process] = line.Report;
					continue;
				}

				var clients = Sum(_latestClients.Values);
				_rows.Add((line.Report, clients));
				Print(line.Report, clients);
				if (Stopwatch.GetTimestamp() >= end)
					break;
			}
		}
		catch (OperationCanceledException) { }

		foreach (var process in processes)
		{
			if (!process.HasExited)
				process.Kill(entireProcessTree: true);
			await process.WaitForExitAsync();
		}

		var summary = Summarize();
		Console.WriteLine();
		Console.WriteLine(Header);
		Console.WriteLine(summary);
		if (s.Out is { } path)
		{
			var text = new StringBuilder();
			if (!File.Exists(path))
				text.AppendLine(Header);
			text.AppendLine(summary);
			await File.AppendAllTextAsync(path, text.ToString());
		}

		return 0;
	}
}
