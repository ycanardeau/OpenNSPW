using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class IntegrityCheckMessageSerializerTests
{
	private static IEnumerable<object?[]> TestData()
	{
		yield return new object?[]
		{
			new byte[] { },
			new CoreMessage.IntegrityCheck { },
		};
	}

	[Theory(Skip = "Not implemneted")]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.IntegrityCheck expected)
	{
		var message = IntegrityCheckMessageSerializer.Default.Deserialize(data);
		message.PacketType.Should().Be(expected.PacketType);
		message.DpnidRequesting.Should().Be(expected.DpnidRequesting);
	}

	[Theory(Skip = "Not implemneted")]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.IntegrityCheck message)
	{
		IntegrityCheckMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
