using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class RequestIntegrityCheckMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.RequestIntegrityCheck> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.RequestIntegrityCheck>
		{
			{
				[0xE2, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x03, 0x00, 0x30, 0x00],
				new CoreMessage.RequestIntegrityCheck
				{
					RequestContext = 1,
					DpnidTarget = new Dpnid(0x00300003),
				}
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.RequestIntegrityCheck expected)
	{
		var message = RequestIntegrityCheckMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.RequestContext.Should().Be(expected.RequestContext);
		message.DpnidTarget.Should().Be(expected.DpnidTarget);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.RequestIntegrityCheck message)
	{
		RequestIntegrityCheckMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
