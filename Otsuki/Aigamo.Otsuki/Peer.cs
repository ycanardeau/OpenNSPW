using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Session;

namespace Aigamo.Otsuki;

/// <summary>
/// A DirectPlay 8 peer, like IDirectPlay8Peer. All protocol work happens on actors behind this facade;
/// methods only post commands to them, so they never block on network activity and need no locks.
/// <para>
/// Notifications are queued as <see cref="PeerEvent"/>s. Consume them on your own thread with
/// <see cref="DoWork"/> (like DirectPlay's DoWork mode) or asynchronously with <see cref="ReadEventsAsync"/>,
/// but not both: each event is delivered once.
/// </para>
/// </summary>
public sealed class Peer : IAsyncDisposable
{
	public const int DefaultPort = 2302;

	/// <summary>
	/// DPNID_ALL_PLAYERS_GROUP: sends to every player in the session.
	/// </summary>
	public static readonly Dpnid AllPlayers = Dpnid.Empty;

	private readonly Channel<PeerEvent> _events = Channel.CreateUnbounded<PeerEvent>(
		new UnboundedChannelOptions { SingleReader = false, SingleWriter = true }
	);

	private readonly SessionActor _session;
	private SessionSnapshot _snapshot = SessionSnapshot.Empty;
	private long _nextSendHandle;

	public Peer(PeerOptions? options = null) =>
		_session = new SessionActor(
			options ?? PeerOptions.Default,
			_events.Writer,
			snapshot => Volatile.Write(ref _snapshot, snapshot)
		);

	private SessionSnapshot Snapshot => Volatile.Read(ref _snapshot);

	public IPEndPoint? LocalEndPoint => Snapshot.LocalEndPoint;

	public Dpnid LocalPlayer => Snapshot.LocalPlayer;

	public bool IsHost => Snapshot.IsHost;

	public ApplicationDescription? ApplicationDescription => Snapshot.Application;

	/// <summary>
	/// Players for which <see cref="PeerEvent.PlayerCreated"/> was raised and not yet followed by <see cref="PeerEvent.PlayerDestroyed"/>.
	/// </summary>
	public IReadOnlyCollection<PlayerInfo> Players => [.. Snapshot.Players.Values];

	public PlayerInfo? GetPeerInfo(Dpnid player) => Snapshot.Players.GetValueOrDefault(player);

	private void PostCommand(SessionMessage command)
	{
		if (!_session.Post(command))
			throw new ObjectDisposedException(nameof(Peer));
	}

	private Task PostCommandAsync(Func<TaskCompletionSource, SessionMessage> createCommand)
	{
		var completion = new TaskCompletionSource(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		PostCommand(createCommand(completion));
		return completion.Task;
	}

	/// <summary>
	/// Sets the local player's name and data. Takes effect for the next <see cref="HostAsync"/> or <see cref="ConnectAsync(ApplicationDescription, IPEndPoint, int, byte[])"/>.
	/// </summary>
	public void SetPeerInfo(string name, byte[]? data = null) =>
		PostCommand(new SessionMessage.SetPeerInfo(name, data is null ? [] : [.. data]));

	/// <summary>
	/// Creates a session with the local player as host. <see cref="PeerEvent.PlayerCreated"/> follows for the local player.
	/// </summary>
	public Task HostAsync(ApplicationDescription application, int port = DefaultPort) =>
		PostCommandAsync(completion => new SessionMessage.Host(
			application,
			new IPEndPoint(IPAddress.Any, port),
			completion
		));

	/// <summary>
	/// Starts joining the session hosted at <paramref name="hostEndPoint"/>. The task completes once the attempt
	/// has started; its outcome is reported by <see cref="PeerEvent.ConnectCompleted"/>.
	/// </summary>
	public Task ConnectAsync(
		ApplicationDescription application,
		IPEndPoint hostEndPoint,
		int localPort = 0,
		byte[]? connectData = null
	) =>
		PostCommandAsync(completion => new SessionMessage.Connect(
			application,
			hostEndPoint,
			new IPEndPoint(IPAddress.Any, localPort),
			connectData is null ? [] : [.. connectData],
			completion
		));

	public async Task ConnectAsync(
		ApplicationDescription application,
		string hostname,
		int port = DefaultPort,
		int localPort = 0,
		byte[]? connectData = null
	)
	{
		var addresses = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);
		var address =
			addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
			?? throw new ArgumentException(
				$"No IPv4 address found for '{hostname}'.",
				nameof(hostname)
			);
		await ConnectAsync(application, new IPEndPoint(address, port), localPort, connectData)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Sends a message to a player, or to every player with <see cref="AllPlayers"/>. Returns a handle
	/// that <see cref="PeerEvent.SendCompleted"/> reports unless <see cref="SendFlags.NoComplete"/> is set.
	/// </summary>
	public long SendTo(
		Dpnid target,
		ReadOnlySpan<byte> data,
		SendFlags flags = SendFlags.Guaranteed
	)
	{
		var handle = Interlocked.Increment(ref _nextSendHandle);
		PostCommand(new SessionMessage.SendTo(target, data.ToArray(), flags, handle));
		return handle;
	}

	public bool TryGetEvent([NotNullWhen(true)] out PeerEvent? peerEvent) =>
		_events.Reader.TryRead(out peerEvent);

	/// <summary>
	/// Handles the queued events on the calling thread and returns how many were handled.
	/// </summary>
	public int DoWork(Action<PeerEvent> handler, int maxEvents = int.MaxValue)
	{
		var count = 0;
		while (count < maxEvents && _events.Reader.TryRead(out var peerEvent))
		{
			handler(peerEvent);
			count++;
		}
		return count;
	}

	/// <summary>
	/// Streams the events. The stream ends after the peer is closed and all events were read.
	/// </summary>
	public IAsyncEnumerable<PeerEvent> ReadEventsAsync(
		CancellationToken cancellationToken = default
	) => _events.Reader.ReadAllAsync(cancellationToken);

	/// <summary>
	/// Leaves the session, disconnecting gracefully from every peer, and shuts the peer down.
	/// </summary>
	public async Task CloseAsync()
	{
		var completion = new TaskCompletionSource(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		if (_session.Post(new SessionMessage.Close(completion)))
			await completion.Task.ConfigureAwait(false);
		else
			await _session.Completion.ConfigureAwait(false);
	}

	public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);
}
