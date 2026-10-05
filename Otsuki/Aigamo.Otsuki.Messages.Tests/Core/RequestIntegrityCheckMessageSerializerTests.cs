using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class RequestIntegrityCheckMessageSerializerTests
{
	private static IEnumerable<object?[]> TestData()
	{
		yield return new object?[]
		{
			new byte[] { },
			new CoreMessage.RequestIntegrityCheck { },
		};
	}

	[Theory(Skip = "Not implemneted")]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.RequestIntegrityCheck expected)
	{
		var message = RequestIntegrityCheckMessageSerializer.Default.Deserialize(data);
		message.PacketType.Should().Be(expected.PacketType);
		message.RequestContext.Should().Be(expected.RequestContext);
		message.DpnidTarget.Should().Be(expected.DpnidTarget);
	}

	[Theory(Skip = "Not implemneted")]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.RequestIntegrityCheck message)
	{
		RequestIntegrityCheckMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
