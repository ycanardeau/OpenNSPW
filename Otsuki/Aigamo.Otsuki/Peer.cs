using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Aigamo.Otsuki.Enumeration;
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

	private readonly PeerOptions _options;
	private readonly SessionActor _session;
	private SessionSnapshot _snapshot = SessionSnapshot.Empty;
	private long _nextSendHandle;

	public Peer(PeerOptions? options = null)
	{
		_options = options ?? PeerOptions.Default;
		_session = new SessionActor(
			_options,
			_events.Writer,
			snapshot => Volatile.Write(ref _snapshot, snapshot)
		);
	}

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

	/// <summary>
	/// Groups for which <see cref="PeerEvent.GroupCreated"/> was raised and not yet followed by <see cref="PeerEvent.GroupDestroyed"/>.
	/// </summary>
	public IReadOnlyCollection<GroupInfo> Groups => [.. Snapshot.Groups.Values];

	/// <summary>
	/// A group with its members, like IDirectPlay8Peer::GetGroupInfo and EnumGroupMembers.
	/// </summary>
	public GroupInfo? GetGroupInfo(Dpnid group) => Snapshot.Groups.GetValueOrDefault(group);

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

	private Task<T> PostCommandAsync<T>(Func<TaskCompletionSource<T>, SessionMessage> createCommand)
	{
		var completion = new TaskCompletionSource<T>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		PostCommand(createCommand(completion));
		return completion.Task;
	}

	/// <summary>
	/// Sets the local player's name and data, like IDirectPlay8Peer::SetPeerInfo. Outside a session, they take effect
	/// for the next <see cref="HostAsync"/> or <see cref="ConnectAsync(ApplicationDescription, IPEndPoint, int, byte[])"/>.
	/// In a session, the host updates them for everyone and <see cref="PeerEvent.PeerInfoUpdated"/> follows on every
	/// peer; the task completes once the update is in force, and fails with <see cref="DirectPlayException"/> if the
	/// host does not answer.
	/// </summary>
	public Task SetPeerInfo(string name, byte[]? data = null) =>
		PostCommandAsync(completion => new SessionMessage.SetPeerInfo(
			name,
			data is null ? [] : [.. data],
			completion
		));

	/// <summary>
	/// Removes another player from the session as the host, like IDirectPlay8Peer::DestroyPeer. The player is told
	/// with <paramref name="data"/>, and <see cref="PeerEvent.PlayerDestroyed"/> follows with
	/// <see cref="DestroyPlayerFlags.HostDestroyedPlayer"/>.
	/// </summary>
	public Task DestroyPeerAsync(Dpnid player, byte[]? data = null) =>
		PostCommandAsync(completion => new SessionMessage.DestroyPlayer(
			player,
			data is null ? [] : [.. data],
			completion
		));

	/// <summary>
	/// Creates a group, like IDirectPlay8Peer::CreateGroup, and returns its DPNID. Only the host creates groups, so a
	/// non-host peer asks it to. <see cref="PeerEvent.GroupCreated"/> follows on every peer.
	/// </summary>
	public Task<Dpnid> CreateGroupAsync(
		string name,
		byte[]? data = null,
		GroupFlags flags = GroupFlags.None
	) =>
		PostCommandAsync<Dpnid>(completion => new SessionMessage.CreateGroup(
			name,
			data is null ? [] : [.. data],
			flags,
			completion
		));

	/// <summary>
	/// Destroys a group, like IDirectPlay8Peer::DestroyGroup. <see cref="PeerEvent.GroupDestroyed"/> follows.
	/// </summary>
	public Task DestroyGroupAsync(Dpnid group) =>
		PostCommandAsync(completion => new SessionMessage.DestroyGroup(group, completion));

	/// <summary>
	/// Adds a player to a group, like IDirectPlay8Peer::AddPlayerToGroup. <see cref="PeerEvent.PlayerAddedToGroup"/>
	/// follows.
	/// </summary>
	public Task AddPlayerToGroupAsync(Dpnid group, Dpnid player) =>
		PostCommandAsync(completion => new SessionMessage.AddPlayerToGroup(
			group,
			player,
			completion
		));

	/// <summary>
	/// Removes a player from a group, like IDirectPlay8Peer::RemovePlayerFromGroup.
	/// <see cref="PeerEvent.PlayerRemovedFromGroup"/> follows.
	/// </summary>
	public Task RemovePlayerFromGroupAsync(Dpnid group, Dpnid player) =>
		PostCommandAsync(completion => new SessionMessage.RemovePlayerFromGroup(
			group,
			player,
			completion
		));

	/// <summary>
	/// Creates a session with the local player as host. <see cref="PeerEvent.PlayerCreated"/> follows for the local player.
	/// </summary>
	public Task HostAsync(ApplicationDescription application, int port = DefaultPort)
	{
		const SessionFlags signing = SessionFlags.FastSigned | SessionFlags.FullSigned;
		if ((application.Flags & signing) == signing)
			throw new ArgumentException(
				"A session is either fast signed or full signed, not both.",
				nameof(application)
			);

		return PostCommandAsync(completion => new SessionMessage.Host(
			application,
			new IPEndPoint(IPAddress.Any, port),
			completion
		));
	}

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
	/// Sends a message to a player, to the members of a group, or to every player with <see cref="AllPlayers"/>. Returns a handle
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

	/// <summary>
	/// Sets the game-specific data that the host returns to <see cref="EnumHostsAsync"/>, such as the state of the
	/// game.
	/// </summary>
	public void SetEnumResponseData(ReadOnlySpan<byte> data) =>
		PostCommand(new SessionMessage.SetEnumResponseData([.. data]));

	/// <summary>
	/// Finds hosts of sessions, like IDirectPlay8Peer::EnumHosts ([MC-DPLHP]). Sends queries to
	/// <paramref name="target"/>, a host or a broadcast address such as 255.255.255.255:2302, and yields every
	/// response, so a host appears once per query it answered. Stops once <see cref="EnumHostsOptions.Timeout"/> has
	/// passed after the last query.
	/// </summary>
	/// <param name="guidApplication">Only hosts of this application answer; <see langword="null"/> asks every host.</param>
	/// <param name="payload">Application-specific data sent with each query.</param>
	public IAsyncEnumerable<EnumeratedHost> EnumHostsAsync(
		IPEndPoint target,
		Guid? guidApplication = null,
		byte[]? payload = null,
		EnumHostsOptions? options = null,
		CancellationToken cancellationToken = default
	) =>
		HostEnumerator.EnumerateAsync(
			_options,
			target,
			guidApplication,
			payload is null ? [] : [.. payload],
			options ?? EnumHostsOptions.Default,
			cancellationToken
		);

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
