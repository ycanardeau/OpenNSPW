using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class IntegrityCheckResponseMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.IntegrityCheckResponse> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.IntegrityCheckResponse>
		{
			{
				[0xE4, 0x00, 0x00, 0x00, 0x07, 0x00, 0x20, 0x00],
				new CoreMessage.IntegrityCheckResponse { DpnidRequesting = new Dpnid(0x00200007) }
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.IntegrityCheckResponse expected)
	{
		var message = IntegrityCheckResponseMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.DpnidRequesting.Should().Be(expected.DpnidRequesting);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.IntegrityCheckResponse message)
	{
		IntegrityCheckResponseMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
