using System.Net;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Enumeration;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

public class ReliableEndpointTests
{
	private sealed class NoObserver : IConnectionObserver
	{
		public void OnConnected(Connection connection) { }

		public void OnConnectFailed(Connection connection, ResultCode result) { }

		public void OnReceived(Connection connection, byte[] payload, bool user1) { }

		public void OnSendCompleted(Connection connection, long sendId, ResultCode result) { }

		public void OnDisconnected(Connection connection, ResultCode result) { }

		public void OnEnumQuery(EndPoint remoteEndPoint, EnumerationMessage.Query query) { }
	}

	// An outbound connection signs as the caller, the session, says, whatever the endpoint was told on its own thread:
	// the session does not read the endpoint's signing mode, which another thread writes.
	[Theory]
	[InlineData(PacketSigning.None)]
	[InlineData(PacketSigning.Fast)]
	[InlineData(PacketSigning.Full)]
	public void Connect_SignsAsTheCallerSays(PacketSigning signing)
	{
		var network = new InMemoryNetwork();
		var endpoint = new ReliableEndpoint(
			network.Bind(IPAddress.Parse("10.4.0.1")),
			TimeProvider.System,
			new ReliableProfile(),
			new NoObserver()
		);
		endpoint.Post(new ReliableEndpointMessage.SetSigning(PacketSigning.Fast));

		var connection = endpoint.Connect(new IPEndPoint(IPAddress.Parse("10.4.0.2"), 2302), signing);

		connection.OfferedSigning.Should().Be(signing);
		endpoint.Post(new ReliableEndpointMessage.ShutDown());
	}
}
