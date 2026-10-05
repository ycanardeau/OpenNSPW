using System.Collections.Immutable;
using System.Text;
using Aigamo.Otsuki.Messages.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Reliable;

public class DataFrameMessageSerializerTests
{
	public static IEnumerable<object?[]> TestData()
	{
		yield return new object?[]
		{
			new byte[] { 0x3F, 0x02, 0x00, 0x00, 0xC6, 0xAE, 0xC9, 0x79 },
			new ReliableMessage.DataFrame
			{
				Reliable = true,
				Sequential = true,
				Poll = true,
				NewMessage = true,
				EndMessage = true,
				SequenceId = SequenceId.Empty,
				NextReceive = SequenceId.Empty,
				SessionId = new SessionId(0x79C9AEC6),
			},
			false,
		};

		yield return new object?[]
		{
			new byte[] { 0x3f, 0x02, 0x00, 0x00, 0x7d, 0x99, 0xc5, 0x51 },
			new ReliableMessage.DataFrame
			{
				Reliable = true,
				Sequential = true,
				Poll = true,
				NewMessage = true,
				EndMessage = true,
				SequenceId = SequenceId.Empty,
				NextReceive = SequenceId.Empty,
				SessionId = new SessionId(0x51C5997D),
			},
			false,
		};

		yield return new object?[]
		{
			new byte[]
			{
				0x7F,
				0x50,
				1,
				2,
				3,
				0,
				0,
				0,
				4,
				0,
				0,
				0,
				0x48,
				0x00,
				0x65,
				0x00,
				0x6C,
				0x00,
				0x6C,
				0x00,
				0x6F,
				0x00,
				0x20,
				0x00,
				0x57,
				0x00,
				0x6F,
				0x00,
				0x72,
				0x00,
				0x6C,
				0x00,
				0x64,
				0x00,
				0x21,
				0x00,
			},
			new ReliableMessage.DataFrame
			{
				Poll = true,
				SequenceId = new SequenceId(1),
				NextReceive = new SequenceId(2),
				SackMask = 3,
				SendMask = 4,
				Payload = Encoding.Unicode.GetBytes("Hello World!").ToImmutableArray(),
				Reliable = true,
				Sequential = true,
				NewMessage = true,
				EndMessage = true,
				User1 = true,
			},
			false,
		};
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, ReliableMessage.DataFrame expected, bool enableSigning)
	{
		var message = DataFrameMessageSerializer.Default.Deserialize(data);
		message.Command.Should().Be(expected.Command);
		message.Control.Should().Be(expected.Control);
		message.SequenceId.Should().Be(expected.SequenceId);
		message.NextReceive.Should().Be(expected.NextReceive);
		message.SackMask.Should().Be(expected.SackMask);
		message.SendMask.Should().Be(expected.SendMask);
		message.Payload.ToArray().Should().Equal(expected.Payload.ToArray());
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, ReliableMessage.DataFrame message, bool enableSigning)
	{
		DataFrameMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}

	public static IEnumerable<object[]> RoundTripData()
	{
		// PACKET_CONTROL_SACK1 set with a zero mask.
		yield return new object[]
		{
			new byte[] { 0x3F, 0x10, 0x05, 0x07, 0x00, 0x00, 0x00, 0x00, 0xAA, 0xBB },
		};

		// PACKET_CONTROL_KEEPALIVE_OR_CORRELATE set with a zero session ID.
		yield return new object[] { new byte[] { 0x27, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 } };
	}

	[Theory]
	[MemberData(nameof(RoundTripData))]
	internal void RoundTrip_PreservesFlagsFromWire(byte[] data)
	{
		var message = DataFrameMessageSerializer.Default.Deserialize(data);
		message!.Control.Should().Be((PacketControl)data[1]);
		DataFrameMessageSerializer.Default.Serialize(message).Should().Equal(data);
	}
}
