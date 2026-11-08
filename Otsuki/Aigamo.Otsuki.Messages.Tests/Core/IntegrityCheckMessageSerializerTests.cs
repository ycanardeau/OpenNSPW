using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class IntegrityCheckMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.IntegrityCheck> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.IntegrityCheck>
		{
			{
				[0xE3, 0x00, 0x00, 0x00, 0x07, 0x00, 0x20, 0x00],
				new CoreMessage.IntegrityCheck { DpnidRequesting = new Dpnid(0x00200007) }
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.IntegrityCheck expected)
	{
		var message = IntegrityCheckMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.DpnidRequesting.Should().Be(expected.DpnidRequesting);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.IntegrityCheck message)
	{
		IntegrityCheckMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
