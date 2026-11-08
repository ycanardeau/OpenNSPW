using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class HostMigrateMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.HostMigrate> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.HostMigrate>
		{
			{
				[0xCD, 0x00, 0x00, 0x00, 0x01, 0x00, 0x10, 0x00, 0x03, 0x00, 0x30, 0x00],
				new CoreMessage.HostMigrate
				{
					DpnidOldHost = new Dpnid(0x00100001),
					DpnidNewHost = new Dpnid(0x00300003),
				}
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.HostMigrate expected)
	{
		var message = HostMigrateMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.DpnidOldHost.Should().Be(expected.DpnidOldHost);
		message.DpnidNewHost.Should().Be(expected.DpnidNewHost);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.HostMigrate message)
	{
		HostMigrateMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
