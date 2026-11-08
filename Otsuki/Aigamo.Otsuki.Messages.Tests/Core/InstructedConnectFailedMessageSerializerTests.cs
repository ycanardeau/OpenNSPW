using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class InstructedConnectFailedMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.InstructedConnectFailed> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.InstructedConnectFailed>
		{
			{
				[0xC7, 0x00, 0x00, 0x00, 0x05, 0x00, 0x50, 0x00],
				new CoreMessage.InstructedConnectFailed { Dpnid = new Dpnid(0x00500005) }
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.InstructedConnectFailed expected)
	{
		var message = InstructedConnectFailedMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.Dpnid.Should().Be(expected.Dpnid);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.InstructedConnectFailed message)
	{
		InstructedConnectFailedMessageSerializer
			.Default.Serialize(message)
			.Should()
			.Equal(expected);
	}
}
