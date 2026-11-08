using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class AckConnectInfoMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.AckConnectInfo> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.AckConnectInfo>
		{
			{ [0xC3, 0x00, 0x00, 0x00], new CoreMessage.AckConnectInfo() },
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.AckConnectInfo expected)
	{
		var message = AckConnectInfoMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.AckConnectInfo message)
	{
		AckConnectInfoMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
