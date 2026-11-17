using System.Net;
using System.Reflection;
using Aigamo.Otsuki.Session;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// The host name that a peer's address may give is looked up away from the session, which goes on meanwhile.
/// </summary>
public class HostnameLookupTests
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	private static SessionActor SessionOf(Peer peer) =>
		(SessionActor)
			typeof(Peer)
				.GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance)!
				.GetValue(peer)!;

	[Fact]
	public async Task ASlowLookupDoesNotHoldTheSession()
	{
		var lookup = new TaskCompletionSource<IPAddress[]>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		await using var peer = new Peer(new PeerOptions { LookUpHostname = _ => lookup.Task });
		var resolved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		IPAddress? found = null;

		((ISessionOutputs)SessionOf(peer)).ResolveHostname(
			"rival.example",
			address =>
			{
				found = address;
				return new SessionMessage.SetPeerInfo("", [], new Completion(resolved));
			}
		);

		// The session answers a command while the name is still being looked up.
		await peer.SetPeerInfo("Alice").WaitAsync(Timeout);
		resolved.Task.IsCompleted.Should().BeFalse();

		lookup.SetResult([IPAddress.IPv6Loopback, IPAddress.Parse("192.0.2.7")]);
		await resolved.Task.WaitAsync(Timeout);
		found.Should().Be(IPAddress.Parse("192.0.2.7"));
	}

	[Fact]
	public async Task AFailedLookupGivesNoAddress()
	{
		await using var peer = new Peer(
			new PeerOptions
			{
				LookUpHostname = _ =>
					Task.FromException<IPAddress[]>(
						new System.Net.Sockets.SocketException(
							(int)System.Net.Sockets.SocketError.HostNotFound
						)
					),
			}
		);
		var resolved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		IPAddress? found = IPAddress.None;

		((ISessionOutputs)SessionOf(peer)).ResolveHostname(
			"nowhere.example",
			address =>
			{
				found = address;
				return new SessionMessage.SetPeerInfo("", [], new Completion(resolved));
			}
		);

		await resolved.Task.WaitAsync(Timeout);
		found.Should().BeNull();
	}
}
