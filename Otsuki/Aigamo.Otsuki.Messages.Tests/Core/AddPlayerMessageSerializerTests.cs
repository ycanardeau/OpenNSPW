using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class AddPlayerMessageSerializerTests
{
	private static IEnumerable<object?[]> TestData()
	{
		yield return new object?[]
		{
			new byte[] { },
			new CoreMessage.AddPlayer { },
		};
	}

	[Theory(Skip = "Not implemneted")]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.AddPlayer expected)
	{
		var message = AddPlayerMessageSerializer.Default.Deserialize(data);
		message.PacketType.Should().Be(expected.PacketType);
		message.Dpnid.Should().Be(expected.Dpnid);
		message.DpnidOwner.Should().Be(expected.DpnidOwner);
		message.Flags.Should().Be(expected.Flags);
		message.Version.Should().Be(expected.Version);
		message.VersionNotUsed.Should().Be(expected.VersionNotUsed);
		message.DnetClientVersion.Should().Be(expected.DnetClientVersion);
		message.Url.Should().Be(expected.Url);
		message.Data.Should().Equal(expected.Data);
		message.Name.Should().Be(expected.Name);
	}

	[Theory(Skip = "Not implemneted")]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.AddPlayer message)
	{
		AddPlayerMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
