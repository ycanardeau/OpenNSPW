using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class HostMigrateMessageSerializerTests
{
	private static IEnumerable<object?[]> TestData()
	{
		yield return new object?[]
		{
			new byte[] { },
			new CoreMessage.HostMigrate { },
		};
	}

	[Theory(Skip = "Not implemneted")]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.HostMigrate expected)
	{
		var message = HostMigrateMessageSerializer.Default.Deserialize(data);
		message.PacketType.Should().Be(expected.PacketType);
		message.DpnidOldHost.Should().Be(expected.DpnidOldHost);
		message.DpnidNewHost.Should().Be(expected.DpnidNewHost);
	}

	[Theory(Skip = "Not implemneted")]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.HostMigrate message)
	{
		HostMigrateMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
