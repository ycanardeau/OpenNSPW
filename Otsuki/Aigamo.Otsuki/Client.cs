using System.Diagnostics.CodeAnalysis;
using System.Net;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki;

/// <summary>
/// A client of a client/server session, like IDirectPlay8Client ([MC-DPL8CS] section 1.3.1). It knows only the
/// server and itself, and sends only to the server. Notifications arrive as <see cref="PeerEvent"/>s: a
/// <see cref="PeerEvent.PlayerCreated"/> for itself and the server once connected, then
/// <see cref="PeerEvent.DataReceived"/> from the server, <see cref="PeerEvent.PeerInfoUpdated"/> when the server
/// changes its information (DPN_MSGID_SERVER_INFO), and <see cref="PeerEvent.SessionTerminated"/> when the server
/// goes or disconnects it.
/// </summary>
public sealed class Client(PeerOptions? options = null) : IAsyncDisposable
{
	private readonly Peer _peer = new(options);

	public EndPoint? LocalEndPoint => _peer.LocalEndPoint;

	public Dpnid LocalPlayer => _peer.LocalPlayer;

	public ApplicationDescription? ApplicationDescription => _peer.ApplicationDescription;

	/// <summary>
	/// The server's player, like IDirectPlay8Client::GetServerInfo, once connected.
	/// </summary>
	public PlayerInfo? GetServerInfo() => _peer.Players.FirstOrDefault(p => p.IsHost);

	/// <summary>
	/// Sets the client's name and data, like IDirectPlay8Client::SetClientInfo. In a session, the server hears of it
	/// as <see cref="PeerEvent.PeerInfoUpdated"/> (DPN_MSGID_CLIENT_INFO), and the task completes once it confirms.
	/// </summary>
	public Task SetClientInfo(string name, byte[]? data = null) => _peer.SetPeerInfo(name, data);

	/// <summary>
	/// Finds servers, like IDirectPlay8Client::EnumHosts.
	/// </summary>
	public IAsyncEnumerable<EnumeratedHost> EnumHostsAsync(
		EndPoint target,
		Guid? guidApplication = null,
		byte[]? payload = null,
		EnumHostsOptions? options = null,
		CancellationToken cancellationToken = default
	) => _peer.EnumHostsAsync(target, guidApplication, payload, options, cancellationToken);

	/// <summary>
	/// Starts connecting to the server, from <paramref name="localEndPoint"/>, which may be left out for an
	/// <see cref="IPEndPoint"/>. <see cref="SessionFlags.ClientServer"/> is added to the flags; the outcome is
	/// reported by <see cref="PeerEvent.ConnectCompleted"/>.
	/// </summary>
	public Task ConnectAsync(
		ApplicationDescription application,
		EndPoint serverEndPoint,
		EndPoint? localEndPoint = null,
		byte[]? connectData = null
	) =>
		_peer.ConnectAsync(
			application with
			{
				Flags = application.Flags | SessionFlags.ClientServer,
			},
			serverEndPoint,
			localEndPoint,
			connectData
		);

	/// <summary>
	/// Starts connecting to the server, from <paramref name="localPort"/> of every IPv4 address.
	/// <see cref="SessionFlags.ClientServer"/> is added to the flags; the outcome is reported by
	/// <see cref="PeerEvent.ConnectCompleted"/>.
	/// </summary>
	public Task ConnectAsync(
		ApplicationDescription application,
		IPEndPoint serverEndPoint,
		int localPort = 0,
		byte[]? connectData = null
	) =>
		_peer.ConnectAsync(
			application with
			{
				Flags = application.Flags | SessionFlags.ClientServer,
			},
			serverEndPoint,
			localPort,
			connectData
		);

	public Task ConnectAsync(
		ApplicationDescription application,
		string hostname,
		int port = Peer.DefaultPort,
		int localPort = 0,
		byte[]? connectData = null
	) =>
		_peer.ConnectAsync(
			application with
			{
				Flags = application.Flags | SessionFlags.ClientServer,
			},
			hostname,
			port,
			localPort,
			connectData
		);

	/// <summary>
	/// Sends a message to the server, like IDirectPlay8Client::Send.
	/// </summary>
	public long Send(ReadOnlySpan<byte> data, SendFlags flags = SendFlags.Guaranteed) =>
		_peer.SendTo(GetServerInfo()?.Id ?? Peer.AllPlayers, data, flags | SendFlags.NoLoopback);

	public bool TryGetEvent([NotNullWhen(true)] out PeerEvent? peerEvent) =>
		_peer.TryGetEvent(out peerEvent);

	public int DoWork(Action<PeerEvent> handler, int maxEvents = int.MaxValue) =>
		_peer.DoWork(handler, maxEvents);

	public IAsyncEnumerable<PeerEvent> ReadEventsAsync(
		CancellationToken cancellationToken = default
	) => _peer.ReadEventsAsync(cancellationToken);

	public Task CloseAsync() => _peer.CloseAsync();

	public ValueTask DisposeAsync() => _peer.DisposeAsync();
}
