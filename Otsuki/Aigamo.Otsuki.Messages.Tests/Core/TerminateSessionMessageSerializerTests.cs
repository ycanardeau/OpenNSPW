using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class TerminateSessionMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.TerminateSession> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.TerminateSession>
		{
			{
				[0xDF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
				new CoreMessage.TerminateSession { TerminateData = ImmutableArray<byte>.Empty }
			},
			{
				[
					0xDF,
					0x00,
					0x00,
					0x00,
					0x08,
					0x00,
					0x00,
					0x00,
					0x04,
					0x00,
					0x00,
					0x00,
					0x01,
					0x02,
					0x03,
					0x04,
				],
				new CoreMessage.TerminateSession { TerminateData = [1, 2, 3, 4] }
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.TerminateSession expected)
	{
		var message = TerminateSessionMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.TerminateData.Should().Equal(expected.TerminateData);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.TerminateSession message)
	{
		TerminateSessionMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
