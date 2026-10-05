namespace Aigamo.Otsuki.ConsoleApp;

/// <summary>
/// The command line: <c>host [options]</c> or <c>join HOSTNAME [options]</c>.
/// </summary>
internal sealed record ChatOptions(string? Hostname, int Port, string Name, bool DoWork)
{
	public bool IsHost => Hostname is null;

	public static ChatOptions? Parse(string[] args)
	{
		if (args.Length == 0)
			return null;

		var (hostname, rest) = args[0] switch
		{
			"host" => (null, args[1..]),
			"join" when args.Length >= 2 => (args[1], args[2..]),
			_ => ((string?)null, (string[]?)null),
		};
		if (rest is null)
			return null;

		var options = new ChatOptions(
			hostname,
			Peer.DefaultPort,
			Environment.UserName,
			DoWork: false
		);
		for (var i = 0; i < rest.Length; i++)
		{
			switch (rest[i])
			{
				case "--port" when i + 1 < rest.Length && int.TryParse(rest[i + 1], out var port):
					options = options with { Port = port };
					i++;
					break;

				case "--name" when i + 1 < rest.Length:
					options = options with { Name = rest[i + 1] };
					i++;
					break;

				case "--do-work":
					options = options with { DoWork = true };
					break;

				default:
					return null;
			}
		}
		return options;
	}
}
