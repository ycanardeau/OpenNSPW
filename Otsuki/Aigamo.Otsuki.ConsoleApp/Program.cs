using System.Net.Sockets;
using System.Text;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.ConsoleApp;

/// <summary>
/// A chat over DirectPlay 8. One peer hosts, the others join it, and everyone talks to everyone directly.
/// </summary>
internal static class Program
{
	/// <summary>
	/// Peers can only join sessions of the same application.
	/// </summary>
	private static readonly ApplicationDescription Application = new()
	{
		GuidApplication = new("0b0f7a52-5f3d-4c42-9a51-7d2e0c3f6a11"),
		SessionName = "Otsuki chat",
	};

	private const SendFlags ChatSendFlags = SendFlags.Guaranteed | SendFlags.NoComplete;

	private static void Print(string text) => Console.WriteLine(text);

	private static void PrintUsage() =>
		Print(
			"""
			Usage:
			  Aigamo.Otsuki.ConsoleApp host [--port PORT] [--name NAME] [--do-work]
			  Aigamo.Otsuki.ConsoleApp join HOSTNAME [--port PORT] [--name NAME] [--do-work]

			  --port     The host's port (default 2302).
			  --name     Your player name (default: your user name).
			  --do-work  Poll events once per frame with Peer.DoWork instead of streaming them.
			"""
		);

	private static void PrintHelp() =>
		Print(
			"""
			* Type a message and press Enter to send it to everyone.
			*   /w NAME MESSAGE  whisper to one player
			*   /players         list the players
			*   /quit            leave the session
			"""
		);

	private static string Describe(PlayerInfo player) =>
		player.IsHost ? $"{player.Name} (host)" : player.Name;

	private static bool PlayerCreated(
		PeerEvent.PlayerCreated created,
		Dictionary<Dpnid, string> names,
		TaskCompletionSource ready
	)
	{
		var player = created.Player;
		names[player.Id] = player.Name;
		if (!player.IsLocal)
		{
			Print($"* {Describe(player)} joined");
		}
		else if (player.IsHost)
		{
			Print($"* You are {player.Name}, waiting for players to join");
			PrintHelp();
			ready.TrySetResult();
		}
		return true;
	}

	private static bool PlayerDestroyed(
		PeerEvent.PlayerDestroyed destroyed,
		Dictionary<Dpnid, string> names
	)
	{
		if (names.Remove(destroyed.Player, out var name))
			Print($"* {name} left ({destroyed.Reason})");
		return true;
	}

	private static bool DataReceived(
		PeerEvent.DataReceived received,
		Dictionary<Dpnid, string> names
	)
	{
		var sender = names.GetValueOrDefault(received.Sender, received.Sender.ToString());
		Print($"<{sender}> {Encoding.UTF8.GetString(received.Data.AsSpan())}");
		return true;
	}

	private static bool ConnectCompleted(
		PeerEvent.ConnectCompleted completed,
		TaskCompletionSource ready
	)
	{
		if (completed.Result != ResultCode.Success)
		{
			Print($"* Could not join: {completed.Result}");
			return false;
		}

		Print("* Joined the session");
		PrintHelp();
		ready.TrySetResult();
		return true;
	}

	private static bool SessionTerminated(PeerEvent.SessionTerminated terminated)
	{
		Print($"* The session ended: {terminated.Result}");
		return false;
	}

	/// <summary>
	/// Prints a peer event. Returns <see langword="false"/> once the session is over.
	/// Only ever called from one thread at a time, so <paramref name="names"/> needs no lock.
	/// </summary>
	private static bool HandleEvent(
		PeerEvent peerEvent,
		Dictionary<Dpnid, string> names,
		TaskCompletionSource ready
	) =>
		peerEvent.Match(
			PlayerCreated: created => PlayerCreated(created, names, ready),
			PlayerDestroyed: destroyed => PlayerDestroyed(destroyed, names),
			DataReceived: received => DataReceived(received, names),
			ConnectCompleted: completed => ConnectCompleted(completed, ready),
			// Chat messages are sent with SendFlags.NoComplete, so there are none.
			SendCompleted: _ => true,
			SessionTerminated: SessionTerminated
		);

	/// <summary>
	/// Consumes events as they arrive.
	/// </summary>
	private static async Task StreamEventsAsync(
		Peer peer,
		Func<PeerEvent, bool> handle,
		TaskCompletionSource ended
	)
	{
		await foreach (var peerEvent in peer.ReadEventsAsync())
		{
			if (!handle(peerEvent))
				ended.TrySetResult();
		}
		ended.TrySetResult();
	}

	/// <summary>
	/// Consumes events once per frame, like a game loop calling IDirectPlay8ThreadPool::DoWork.
	/// </summary>
	private static async Task PollEventsAsync(
		Peer peer,
		Func<PeerEvent, bool> handle,
		TaskCompletionSource ended
	)
	{
		using var frame = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
		while (!ended.Task.IsCompleted && await frame.WaitForNextTickAsync())
		{
			peer.DoWork(peerEvent =>
			{
				if (!handle(peerEvent))
					ended.TrySetResult();
			});
		}
	}

	private static void ListPlayers(Peer peer)
	{
		foreach (var player in peer.Players.OrderBy(p => p.Name))
			Print($"* {Describe(player)}{(player.IsLocal ? " (you)" : string.Empty)}");
	}

	private static void Whisper(Peer peer, string arguments)
	{
		var parts = arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2)
		{
			Print("* Usage: /w NAME MESSAGE");
			return;
		}

		var player = peer.Players.FirstOrDefault(p =>
			!p.IsLocal && string.Equals(p.Name, parts[0], StringComparison.OrdinalIgnoreCase)
		);
		if (player is null)
		{
			Print($"* There is no player called {parts[0]}");
			return;
		}

		peer.SendTo(player.Id, Encoding.UTF8.GetBytes($"(whisper) {parts[1]}"), ChatSendFlags);
	}

	/// <summary>
	/// Runs one line of input. Returns <see langword="false"/> to leave.
	/// </summary>
	private static bool Execute(Peer peer, string line)
	{
		var command = line.Split(' ', 2);
		switch (command[0])
		{
			case "/quit":
				return false;

			case "/players":
				ListPlayers(peer);
				return true;

			case "/w":
				Whisper(peer, command.Length > 1 ? command[1] : string.Empty);
				return true;

			case "/help":
				PrintHelp();
				return true;

			case var unknown when unknown.StartsWith('/'):
				Print($"* Unknown command {unknown}; type /help");
				return true;

			case "":
				return true;

			default:
				peer.SendTo(
					Peer.AllPlayers,
					Encoding.UTF8.GetBytes(line),
					ChatSendFlags | SendFlags.NoLoopback
				);
				return true;
		}
	}

	private static void ReadInput(Peer peer)
	{
		while (Console.ReadLine() is { } line && Execute(peer, line)) { }
	}

	private static async Task StartAsync(Peer peer, ChatOptions options)
	{
		if (options.IsHost)
		{
			await peer.HostAsync(Application, options.Port);
			Print($"* Hosting on port {peer.LocalEndPoint!.Port}");
		}
		else
		{
			Print($"* Joining {options.Hostname}:{options.Port}");
			await peer.ConnectAsync(Application, options.Hostname!, options.Port);
		}
	}

	public static async Task<int> Main(string[] args)
	{
		if (ChatOptions.Parse(args) is not { } options)
		{
			PrintUsage();
			return 1;
		}

		var peer = new Peer();
		var names = new Dictionary<Dpnid, string>();
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		bool Handle(PeerEvent peerEvent) => HandleEvent(peerEvent, names, ready);
		var events = options.DoWork
			? PollEventsAsync(peer, Handle, ended)
			: StreamEventsAsync(peer, Handle, ended);

		peer.SetPeerInfo(options.Name);
		try
		{
			await StartAsync(peer, options);
		}
		catch (Exception e) when (e is SocketException or ArgumentException)
		{
			Print($"* {e.Message}");
			await peer.CloseAsync();
			return 1;
		}

		if (await Task.WhenAny(ready.Task, ended.Task) == ready.Task)
		{
			// Console.ReadLine blocks, so input runs on its own thread while events keep printing.
			var input = Task.Run(() => ReadInput(peer));
			await Task.WhenAny(input, ended.Task);
		}

		await peer.CloseAsync();
		ended.TrySetResult();
		await events;
		return 0;
	}
}
