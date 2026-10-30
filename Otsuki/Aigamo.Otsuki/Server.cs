using System.Diagnostics.CodeAnalysis;
using System.Net;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki;

/// <summary>
/// The server of a client/server session, like IDirectPlay8Server ([MC-DPL8CS] section 1.3.1). It knows every
/// client and runs the session; clients talk only to it and never learn of each other. Notifications arrive as
/// <see cref="PeerEvent"/>s, as with <see cref="Peer"/>: a <see cref="PeerEvent.PlayerCreated"/> for the server
/// player and for each client, <see cref="PeerEvent.PeerInfoUpdated"/> when a client changes its information
/// (DPN_MSGID_CLIENT_INFO), and so on.
/// </summary>
public sealed class Server(PeerOptions? options = null) : IAsyncDisposable
{
	private readonly Peer _peer = new(options);

	public EndPoint? LocalEndPoint => _peer.LocalEndPoint;

	/// <summary>
	/// The server's own player.
	/// </summary>
	public Dpnid LocalPlayer => _peer.LocalPlayer;

	public ApplicationDescription? ApplicationDescription => _peer.ApplicationDescription;

	/// <summary>
	/// The connected clients, like IDirectPlay8Server::EnumPlayersAndGroups.
	/// </summary>
	public IReadOnlyCollection<PlayerInfo> Clients => [.. _peer.Players.Where(p => !p.IsLocal)];

	public IReadOnlyCollection<GroupInfo> Groups => _peer.Groups;

	public PlayerInfo? GetClientInfo(Dpnid client) =>
		_peer.GetPeerInfo(client) is { IsLocal: false } info ? info : null;

	public GroupInfo? GetGroupInfo(Dpnid group) => _peer.GetGroupInfo(group);

	/// <summary>
	/// Sets the server's name and data, like IDirectPlay8Server::SetServerInfo. Every client hears of a change in a
	/// session as <see cref="PeerEvent.PeerInfoUpdated"/> (DPN_MSGID_SERVER_INFO).
	/// </summary>
	public Task SetServerInfo(string name, byte[]? data = null) => _peer.SetPeerInfo(name, data);

	/// <summary>
	/// Creates the session, listening on <paramref name="localEndPoint"/>. <see cref="SessionFlags.ClientServer"/> is
	/// added to the flags, and host migration does not apply.
	/// </summary>
	public Task HostAsync(ApplicationDescription application, EndPoint localEndPoint) =>
		_peer.HostAsync(
			application with
			{
				Flags = application.Flags | SessionFlags.ClientServer,
			},
			localEndPoint
		);

	/// <summary>
	/// Creates the session, listening on <paramref name="port"/> of every IPv4 address.
	/// <see cref="SessionFlags.ClientServer"/> is added to the flags, and host migration does not apply.
	/// </summary>
	public Task HostAsync(ApplicationDescription application, int port = Peer.DefaultPort) =>
		_peer.HostAsync(
			application with
			{
				Flags = application.Flags | SessionFlags.ClientServer,
			},
			port
		);

	/// <summary>
	/// Sets the game-specific data returned to clients that enumerate hosts.
	/// </summary>
	public void SetEnumResponseData(ReadOnlySpan<byte> data) => _peer.SetEnumResponseData(data);

	/// <summary>
	/// Sends a message to a client, to the members of a group, or to every client with <see cref="Peer.AllPlayers"/>,
	/// which loops back to the server unless <see cref="SendFlags.NoLoopback"/> is set.
	/// </summary>
	public long SendTo(
		Dpnid target,
		ReadOnlySpan<byte> data,
		SendFlags flags = SendFlags.Guaranteed
	) => _peer.SendTo(target, data, flags);

	/// <summary>
	/// Disconnects a client, like IDirectPlay8Server::DestroyClient. The client receives
	/// <see cref="PeerEvent.SessionTerminated"/>.
	/// </summary>
	public Task DestroyClientAsync(Dpnid client, byte[]? data = null) =>
		_peer.DestroyPeerAsync(client, data);

	public Task<Dpnid> CreateGroupAsync(
		string name,
		byte[]? data = null,
		GroupFlags flags = GroupFlags.None
	) => _peer.CreateGroupAsync(name, data, flags);

	public Task DestroyGroupAsync(Dpnid group) => _peer.DestroyGroupAsync(group);

	public Task AddPlayerToGroupAsync(Dpnid group, Dpnid client) =>
		_peer.AddPlayerToGroupAsync(group, client);

	public Task RemovePlayerFromGroupAsync(Dpnid group, Dpnid client) =>
		_peer.RemovePlayerFromGroupAsync(group, client);

	public bool TryGetEvent([NotNullWhen(true)] out PeerEvent? peerEvent) =>
		_peer.TryGetEvent(out peerEvent);

	public int DoWork(Action<PeerEvent> handler, int maxEvents = int.MaxValue) =>
		_peer.DoWork(handler, maxEvents);

	public IAsyncEnumerable<PeerEvent> ReadEventsAsync(
		CancellationToken cancellationToken = default
	) => _peer.ReadEventsAsync(cancellationToken);

	/// <summary>
	/// Ends the session for every client and shuts the server down.
	/// </summary>
	public Task CloseAsync() => _peer.CloseAsync();

	public ValueTask DisposeAsync() => _peer.DisposeAsync();
}
