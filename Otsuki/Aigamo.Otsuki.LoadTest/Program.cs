using System.Globalization;
using System.Net;
using Aigamo.Otsuki.LoadTest;

// Usage:
//   run [--clients N] [--processes K] [--client-rate M] [--server-rate R] [--join-rate J] [--size B] [--seconds S]
//       [--warmup W] [--port P] [--unreliable] [--server-heaps H] [--server-loops N] [--label L] [--out results.md]
//   server --port P --server-loops N --server-rate R --size B --seconds S [--unreliable]
//   clients --id I --server HOST:PORT --clients N --join-rate J --client-rate M --size B --seconds S [--unreliable]

static Dictionary<string, string> Parse(string[] args)
{
	var options = new Dictionary<string, string>();
	for (var i = 1; i < args.Length; i++)
	{
		if (!args[i].StartsWith("--", StringComparison.Ordinal))
			continue;

		var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
		options[args[i][2..]] = hasValue ? args[++i] : "true";
	}

	return options;
}

static int Int(Dictionary<string, string> options, string name, int fallback) =>
	options.TryGetValue(name, out var value) ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;

if (args.Length == 0)
{
	Console.Error.WriteLine("Usage: run | server | clients, with options (see Program.cs).");
	return 1;
}

var options = Parse(args);
var guaranteed = !options.ContainsKey("unreliable");
var seconds = TimeSpan.FromSeconds(Int(options, "seconds", 30));
switch (args[0])
{
	case "server":
		await new ServerRole(
			Int(options, "port", 47_624),
			Int(options, "server-loops", 4),
			Int(options, "server-rate", 20),
			Int(options, "size", 32),
			seconds,
			guaranteed
		).RunAsync();
		return 0;
	case "clients":
		await new ClientsRole(
			Int(options, "id", 1),
			IPEndPoint.Parse(options["server"]),
			Int(options, "clients", 100),
			Int(options, "join-rate", 500),
			Int(options, "client-rate", 30),
			Int(options, "size", 32),
			seconds,
			guaranteed
		).RunAsync();
		return 0;
	case "run":
		var defaults = new LoadSettings();
		return await new Driver(
			new LoadSettings
			{
				Clients = Int(options, "clients", defaults.Clients),
				Processes = Int(options, "processes", defaults.Processes),
				ClientRate = Int(options, "client-rate", defaults.ClientRate),
				ServerRate = Int(options, "server-rate", defaults.ServerRate),
				JoinRate = Int(options, "join-rate", defaults.JoinRate),
				Size = Int(options, "size", defaults.Size),
				Seconds = Int(options, "seconds", defaults.Seconds),
				Warmup = Int(options, "warmup", defaults.Warmup),
				Port = Int(options, "port", defaults.Port),
				Guaranteed = guaranteed,
				ServerHeaps = Int(options, "server-heaps", defaults.ServerHeaps),
				ServerLoops = Int(options, "server-loops", defaults.ServerLoops),
				Label = options.GetValueOrDefault("label"),
				Out = options.GetValueOrDefault("out"),
			}
		).RunAsync();
	default:
		Console.Error.WriteLine($"Unknown command {args[0]}.");
		return 1;
}
