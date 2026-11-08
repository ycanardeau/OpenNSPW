using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class SendPlayerDpnidMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.SendPlayerDpnid> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.SendPlayerDpnid>
		{
			{
				[0xC4, 0x00, 0x00, 0x00, 0x05, 0x00, 0x50, 0x00],
				new CoreMessage.SendPlayerDpnid { Dpnid = new Dpnid(0x00500005) }
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.SendPlayerDpnid expected)
	{
		var message = SendPlayerDpnidMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.Dpnid.Should().Be(expected.Dpnid);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.SendPlayerDpnid message)
	{
		SendPlayerDpnidMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
