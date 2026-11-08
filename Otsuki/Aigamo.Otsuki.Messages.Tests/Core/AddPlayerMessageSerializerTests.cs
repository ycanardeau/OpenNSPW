using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

public class AddPlayerMessageSerializerTests
{
	public static TheoryData<byte[], CoreMessage.AddPlayer> TestData()
	{
		var data = new TheoryData<byte[], CoreMessage.AddPlayer>
		{
			{
				[
					0xD0,
					0x00,
					0x00,
					0x00,
					0x05,
					0x00,
					0x50,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x01,
					0x00,
					0x00,
					0x05,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x07,
					0x00,
					0x00,
					0x00,
					0x30,
					0x00,
					0x00,
					0x00,
					0x04,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x41,
					0x00,
					0x00,
					0x00,
				],
				new CoreMessage.AddPlayer
				{
					Dpnid = new Dpnid(0x00500005),
					DpnidOwner = Dpnid.Empty,
					Peer = true,
					Version = 5,
					DnetClientVersion = DnetVersion.DirectX90,
					Name = "A",
				}
			},
			{
				[
					0xD0,
					0x00,
					0x00,
					0x00,
					0x05,
					0x00,
					0x50,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x01,
					0x00,
					0x00,
					0x05,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x07,
					0x00,
					0x00,
					0x00,
					0x32,
					0x00,
					0x00,
					0x00,
					0x04,
					0x00,
					0x00,
					0x00,
					0x30,
					0x00,
					0x00,
					0x00,
					0x02,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0x00,
					0xAA,
					0xBB,
					0x41,
					0x00,
					0x00,
					0x00,
				],
				new CoreMessage.AddPlayer
				{
					Dpnid = new Dpnid(0x00500005),
					DpnidOwner = Dpnid.Empty,
					Peer = true,
					Version = 5,
					DnetClientVersion = DnetVersion.DirectX90,
					Data = [0xAA, 0xBB],
					Name = "A",
				}
			},
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, CoreMessage.AddPlayer expected)
	{
		var message = AddPlayerMessageSerializer.Default.Deserialize(data);
		Assert.NotNull(message);
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

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Serialize(byte[] expected, CoreMessage.AddPlayer message)
	{
		AddPlayerMessageSerializer.Default.Serialize(message).Should().Equal(expected);
	}
}
