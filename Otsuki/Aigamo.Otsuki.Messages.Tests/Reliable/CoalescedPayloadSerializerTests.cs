using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Reliable;

public class CoalescedPayloadSerializerTests
{
	private static byte[] Bytes(int count, byte value) => Enumerable.Repeat(value, count).ToArray();

	public static IEnumerable<object[]> TestData()
	{
		// One header: two bytes of padding after it, no padding after the last payload.
		yield return new object[]
		{
			new byte[] { 0x03, 0x03, 0x00, 0x00, 0xAA, 0xBB, 0xCC },
			new[]
			{
				new CoalescedPayload
				{
					Reliable = true,
					Payload = ImmutableArray.Create<byte>(0xAA, 0xBB, 0xCC),
				},
			},
		};

		// Two headers: no padding after them, the first payload is padded to a 32-bit boundary.
		yield return new object[]
		{
			new byte[]
			{
				0x05,
				0x04,
				0x02,
				0x41,
				0x01,
				0x02,
				0x03,
				0x04,
				0x05,
				0x00,
				0x00,
				0x00,
				0x06,
				0x07,
			},
			new[]
			{
				new CoalescedPayload
				{
					Sequential = true,
					Payload = ImmutableArray.Create<byte>(0x01, 0x02, 0x03, 0x04, 0x05),
				},
				new CoalescedPayload
				{
					User1 = true,
					Payload = ImmutableArray.Create<byte>(0x06, 0x07),
				},
			},
		};

		// 0x123 bytes: the high bits of the size go into PACKET_COMMAND_COALESCE_BIG_1.
		yield return new object[]
		{
			new byte[] { 0x23, 0x09, 0x00, 0x00 }
				.Concat(Bytes(0x123, 0x5A))
				.ToArray(),
			new[] { new CoalescedPayload { Payload = Bytes(0x123, 0x5A).ToImmutableArray() } },
		};

		// The largest payload size (0x7FF) uses all three PACKET_COMMAND_COALESCE_BIG flags.
		yield return new object[]
		{
			new byte[] { 0xFF, 0xB9, 0x00, 0x00 }
				.Concat(Bytes(0x7FF, 0xA5))
				.ToArray(),
			new[]
			{
				new CoalescedPayload
				{
					User2 = true,
					Payload = Bytes(0x7FF, 0xA5).ToImmutableArray(),
				},
			},
		};
	}

	[Theory]
	[MemberData(nameof(TestData))]
	public void Deserialize(byte[] data, CoalescedPayload[] expected)
	{
		var payloads = CoalescedPayloadSerializer.Default.Deserialize(data);
		payloads.Should().NotBeNull();
		payloads!.Count.Should().Be(expected.Length);
		for (var i = 0; i < expected.Length; i++)
		{
			payloads[i].Command.Should().Be(expected[i].Command);
			payloads[i].Payload.ToArray().Should().Equal(expected[i].Payload.ToArray());
		}
	}

	[Theory]
	[MemberData(nameof(TestData))]
	public void Serialize(byte[] expected, CoalescedPayload[] payloads)
	{
		CoalescedPayloadSerializer.Default.Serialize(payloads).Should().Equal(expected);
	}

	public static IEnumerable<object[]> MalformedData()
	{
		// No PACKET_COMMAND_END_COALESCE within 32 headers.
		yield return new object[] { Bytes(64, 0x00) };

		// The payload is shorter than its header says.
		yield return new object[] { new byte[] { 0x03, 0x01, 0x00, 0x00, 0xAA } };

		// The headers are cut off.
		yield return new object[] { new byte[] { 0x03, 0x00 } };
	}

	[Theory]
	[MemberData(nameof(MalformedData))]
	public void Deserialize_Malformed_ReturnsNull(byte[] data)
	{
		CoalescedPayloadSerializer.Default.Deserialize(data).Should().BeNull();
	}

	public static IEnumerable<object[]> InvalidPayloads()
	{
		yield return new object[] { Array.Empty<CoalescedPayload>() };

		yield return new object[] { Enumerable.Repeat(new CoalescedPayload(), 33).ToArray() };

		yield return new object[]
		{
			new[] { new CoalescedPayload { Payload = Bytes(0x800, 0x00).ToImmutableArray() } },
		};
	}

	[Theory]
	[MemberData(nameof(InvalidPayloads))]
	public void Serialize_Invalid_Throws(CoalescedPayload[] payloads)
	{
		var act = () => CoalescedPayloadSerializer.Default.Serialize(payloads);
		act.Should().Throw<ArgumentException>();
	}
}
