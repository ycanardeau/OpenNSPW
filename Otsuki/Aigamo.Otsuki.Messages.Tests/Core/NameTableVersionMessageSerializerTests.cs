using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class NameTableVersionMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.NameTableVersion> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.NameTableVersion>
		{
			{
				[0xc9, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
				new CoreMessage.NameTableVersion { Version = 4 }
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.NameTableVersion expected)
	{
		var message = NameTableVersionMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.Version.Should().Be(expected.Version);
		message.VersionNotUsed.Should().Be(expected.VersionNotUsed);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.NameTableVersion message)
	{
		NameTableVersionMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
