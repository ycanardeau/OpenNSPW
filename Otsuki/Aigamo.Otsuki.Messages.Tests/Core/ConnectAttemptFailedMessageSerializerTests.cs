using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class ConnectAttemptFailedMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.ConnectAttemptFailed> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.ConnectAttemptFailed>
		{
			{
				[0xC8, 0x00, 0x00, 0x00, 0x05, 0x00, 0x50, 0x00],
				new CoreMessage.ConnectAttemptFailed { Dpnid = new Dpnid(0x00500005) }
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.ConnectAttemptFailed expected)
	{
		var message = ConnectAttemptFailedMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.Dpnid.Should().Be(expected.Dpnid);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.ConnectAttemptFailed message)
	{
		ConnectAttemptFailedMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
