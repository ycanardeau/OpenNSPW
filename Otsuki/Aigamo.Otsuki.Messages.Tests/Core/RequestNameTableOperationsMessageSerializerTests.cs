using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class RequestNameTableOperationsMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.RequestNameTableOperations> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.RequestNameTableOperations>
		{
			{
				[0xCB, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
				new CoreMessage.RequestNameTableOperations { Version = 2 }
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.RequestNameTableOperations expected)
	{
		var message = RequestNameTableOperationsMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
		message.PacketType.Should().Be(expected.PacketType);
		message.Version.Should().Be(expected.Version);
		message.VersionNotUsed.Should().Be(expected.VersionNotUsed);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.RequestNameTableOperations message)
	{
		RequestNameTableOperationsMessageSerializer
			.Default.Serialize(message)
			.Should()
			.Equal(expected);
	}
}
