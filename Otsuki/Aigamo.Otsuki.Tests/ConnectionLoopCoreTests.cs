using System.Net;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Reliable;
using Aigamo.Otsuki.Session;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// The connection loop's core on its own, with no thread, timer or socket: each test gives it inputs at times of its
/// choosing and reads what it did, in order.
/// </summary>
public class ConnectionLoopCoreTests
{
	private sealed record Sent(EndPoint RemoteEndPoint, byte[] Datagram);

	private sealed record Done;

	private sealed class Outputs : IConnectionLoopOutputs
	{
		private readonly Random _random = new(0);

		public List<object> Log { get; } = [];

		public Queue<ConnectionLoopItem> Deferred { get; } = [];

		public void Send(EndPoint remoteEndPoint, byte[] datagram) =>
			Log.Add(new Sent(remoteEndPoint, datagram));

		public void Report(SessionMessage message) => Log.Add(message);

		public void Defer(Connection connection, ConnectionMessage message) =>
			Deferred.Enqueue(new ConnectionLoopItem(connection, message, null));

		public void Done() => Log.Add(new Done());

		public void FillRandom(Span<byte> bytes) => _random.NextBytes(bytes);
	}

	private sealed class Side(IPEndPoint address)
	{
		public IPEndPoint Address { get; } = address;

		public Outputs Outputs { get; } = new();

		public ConnectionLoopCore Core { get; private set; } = null!;

		public Side Start()
		{
			Core = new ConnectionLoopCore(this, ReliableProfile.Default, Outputs);
			return this;
		}

		public void Handle(ConnectionLoopMessage message, TimeSpan now) =>
			Core.Handle(new ConnectionLoopItem(null, null, message), now);

		public void Handle(Connection connection, ConnectionMessage message, TimeSpan now) =>
			Core.Handle(new ConnectionLoopItem(connection, message, null), now);
	}

	private static readonly IPEndPoint AddressA = new(IPAddress.Parse("10.4.0.1"), 2302);

	private static readonly IPEndPoint AddressB = new(IPAddress.Parse("10.4.0.2"), 2302);

	private static bool IsHardDisconnect(object entry) =>
		entry is Sent sent
		&& ReliableMessageSerializer.Default.Deserialize(sent.Datagram)
			is ReliableMessage.HardDisconnect;

	// Hands each side's deferred messages and the datagrams it sent since to the other, until neither has more, as a
	// network that loses nothing would; a side that is cut off sends into nothing.
	private static void Exchange(Side a, Side b, TimeSpan now, bool cutOff = false)
	{
		var read = new Dictionary<Side, int> { [a] = 0, [b] = 0 };
		var progress = true;
		while (progress)
		{
			progress = false;
			foreach (var (from, to) in new[] { (a, b), (b, a) })
			{
				while (from.Outputs.Deferred.TryDequeue(out var item))
				{
					from.Core.Handle(item, now);
					progress = true;
				}

				for (; read[from] < from.Outputs.Log.Count; read[from]++)
				{
					if (from.Outputs.Log[read[from]] is not Sent sent || cutOff)
						continue;

					to.Handle(new ConnectionLoopMessage.DatagramReceived(from.Address, sent.Datagram), now);
					progress = true;
				}
			}
		}
	}

	// An outbound connection signs as the session says, whatever the loop was told for inbound ones: the session does
	// not read the loop's signing mode, which is the loop's alone.
	[Theory]
	[InlineData(PacketSigning.None)]
	[InlineData(PacketSigning.Fast)]
	[InlineData(PacketSigning.Full)]
	public void Register_SignsAsTheSessionSays(PacketSigning signing)
	{
		var a = new Side(AddressA).Start();
		a.Handle(new ConnectionLoopMessage.SetSigning(PacketSigning.Fast), TimeSpan.Zero);
		var connection = new Connection(AddressB, isOutbound: true, a);

		a.Handle(new ConnectionLoopMessage.RegisterConnection(connection, signing), TimeSpan.Zero);

		a.Core.Find(connection)!.OfferedSigning.Should().Be(signing);
	}

	// The goodbye: a connection told to hard-disconnect and then shut down sends every HARD_DISCONNECT its profile
	// asks for, spaced by its timer, and the loop is done only after the last, so that closing the transport then cuts
	// nothing off ([MC-DPL8R] section 3.1.6.4).
	[Fact]
	public void ShutDown_AfterHardDisconnect_SendsEveryGoodbyeBeforeDone()
	{
		var a = new Side(AddressA).Start();
		var b = new Side(AddressB).Start();
		var now = TimeSpan.Zero;
		var connection = new Connection(AddressB, isOutbound: true, a);
		a.Handle(new ConnectionLoopMessage.RegisterConnection(connection, PacketSigning.None), now);
		Exchange(a, b, now);
		a.Outputs.Log.Should().ContainSingle(e => e is SessionMessage.ConnectionConnected);

		a.Handle(connection, new ConnectionMessage.HardDisconnect(), now);
		a.Handle(new ConnectionLoopMessage.ShutDown(), now);
		for (var i = 0; i < 100 && !a.Outputs.Log.OfType<Done>().Any(); i++)
		{
			now += TimeSpan.FromMilliseconds(10);
			a.Handle(new ConnectionLoopMessage.TimersDue(), now);
			Exchange(a, b, now, cutOff: true);
		}

		var log = a.Outputs.Log;
		var done = log.FindIndex(e => e is Done);
		done.Should().BeGreaterThan(0, "the loop ends once its last connection has");
		log.Count(IsHardDisconnect).Should().Be(ReliableProfile.Default.HardDisconnectCount);
		log.FindLastIndex(IsHardDisconnect).Should().BeLessThan(done);
		now.Should()
			.BeGreaterOrEqualTo(
				ReliableProfile.Default.HardDisconnectInterval
					* ReliableProfile.Default.HardDisconnectCount
			);
	}
}
