using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Core;

/// <summary>
/// The group and info update messages of [MC-DPL8CS] sections 2.2.4 and 2.2.5, byte for byte. Offsets count from
/// the end of dwPacketType, and the data comes before the name.
/// </summary>
public class NameTableOperationSerializerTests
{
	private static byte[] Hex(string hex) => Convert.FromHexString(hex.Replace(" ", ""));

	private static void RoundTrip<T>(T message, byte[] data)
		where T : CoreMessage
	{
		CoreMessageSerializer.Default.Serialize(message).Should().Equal(data);
		CoreMessageSerializer
			.Default.Deserialize(data)
			.Should()
			.BeEquivalentTo(message, o => o.RespectingRuntimeTypes());
	}

	[Fact]
	public void RequestUpdateInfo()
	{
		RoundTrip(
			new CoreMessage.RequestUpdateInfo
			{
				PacketContext = 5,
				Dpnid = new Dpnid(0x11223344),
				InfoFlags = InfoFlags.Name | InfoFlags.Data,
				Name = "A",
				Data = ImmutableArray.Create<byte>(0xEE),
			},
			Hex(
				"D6000000" // dwPacketType
					+ "05000000" // dwPacketContext
					+ "44332211" // dpnid
					+ "03000000" // dwInfoFlags
					+ "1D000000 04000000" // dwNameOffset (28 + 1), dwNameSize
					+ "1C000000 01000000" // dwDataOffset (28), dwDataSize
					+ "EE" // data
					+ "41000000" // name
			)
		);
	}

	[Fact]
	public void UpdateInfo()
	{
		RoundTrip(
			new CoreMessage.UpdateInfo
			{
				PacketContext = 5,
				Dpnid = new Dpnid(0x11223344),
				Version = 9,
				InfoFlags = InfoFlags.Name,
				Name = "A",
				DpnidRequesting = new Dpnid(0x55667788),
			},
			Hex(
				"DB000000" // dwPacketType
					+ "05000000" // dwPacketContext
					+ "44332211" // dpnid
					+ "09000000 00000000" // dwVersion, dwVersionNotUsed
					+ "01000000" // dwInfoFlags
					+ "28000000 04000000" // dwNameOffset (40), dwNameSize
					+ "00000000 00000000" // no data
					+ "88776655" // dpnidRequesting
					+ "41000000" // name
			)
		);
	}

	[Fact]
	public void RequestCreateGroup()
	{
		RoundTrip(
			new CoreMessage.RequestCreateGroup
			{
				PacketContext = 1,
				GroupFlags = GroupFlags.AutoDestruct,
				InfoFlags = InfoFlags.Name | InfoFlags.Data,
				Name = "G",
				Data = ImmutableArray.Create<byte>(1, 2),
			},
			Hex(
				"D2000000"
					+ "01000000" // dwPacketContext
					+ "01000000" // dwGroupFlags: DPNGROUP_AUTODESTRUCT
					+ "03000000" // dwInfoFlags
					+ "1E000000 04000000" // dwNameOffset (28 + 2), dwNameSize
					+ "1C000000 02000000" // dwDataOffset (28), dwDataSize
					+ "0102"
					+ "47000000"
			)
		);
	}

	[Fact]
	public void CreateGroup_CarriesTheGroupsNameTableEntry()
	{
		var message = new CoreMessage.CreateGroup
		{
			DpnidRequesting = new Dpnid(0x01010101),
			PacketContext = 2,
			Group = new NameTableEntryInfo
			{
				Dpnid = new Dpnid(0x02020202),
				DpnidOwner = new Dpnid(0x01010101),
				Group = true,
				GroupAutoDestruct = true,
				Version = 7,
				Name = "G",
			},
		};
		var data = Hex(
			"D7000000"
				+ "01010101" // dpnidRequesting
				+ "02000000" // dwPacketContext
				+ "02020202" // dpnid
				+ "01010101" // dpnidOwner
				+ "50000000" // dwFlags: GROUP | GROUP_AUTODESTRUCT
				+ "07000000 00000000" // dwVersion, dwVersionNotUsed
				+ "00000000" // dwDNETVersion
				+ "38000000 04000000" // dwNameOffset (56), dwNameSize
				+ "00000000 00000000" // no data
				+ "00000000 00000000" // no URL
				+ "47000000"
		);

		CoreMessageSerializer.Default.Serialize(message).Should().Equal(data);
		var read = (CoreMessage.CreateGroup)CoreMessageSerializer.Default.Deserialize(data)!;
		read.DpnidRequesting.Should().Be(message.DpnidRequesting);
		read.PacketContext.Should().Be(2);
		read.Group.Dpnid.Should().Be(message.Group.Dpnid);
		read.Group.DpnidOwner.Should().Be(message.Group.DpnidOwner);
		read.Group.GroupAutoDestruct.Should().BeTrue();
		read.Group.Version.Should().Be(7);
		read.Group.Name.Should().Be("G");
	}

	[Fact]
	public void FixedLayoutMessages()
	{
		RoundTrip(
			new CoreMessage.RequestAddPlayerToGroup
			{
				PacketContext = 1,
				DpnidGroup = new Dpnid(2),
				DpnidPlayer = new Dpnid(3),
			},
			Hex("D3000000 01000000 02000000 03000000")
		);
		RoundTrip(
			new CoreMessage.AddPlayerToGroup
			{
				DpnidGroup = new Dpnid(2),
				DpnidPlayer = new Dpnid(3),
				Version = 4,
				DpnidRequesting = new Dpnid(5),
				PacketContext = 6,
			},
			Hex("D9000000 02000000 03000000 04000000 00000000 05000000 06000000")
		);
		RoundTrip(
			new CoreMessage.RequestDeletePlayerFromGroup
			{
				PacketContext = 1,
				DpnidGroup = new Dpnid(2),
				DpnidPlayer = new Dpnid(3),
			},
			Hex("D4000000 01000000 02000000 03000000")
		);
		RoundTrip(
			new CoreMessage.DeletePlayerFromGroup
			{
				DpnidGroup = new Dpnid(2),
				DpnidPlayer = new Dpnid(3),
				Version = 4,
				DpnidRequesting = new Dpnid(5),
				PacketContext = 6,
			},
			Hex("DA000000 02000000 03000000 04000000 00000000 05000000 06000000")
		);
		RoundTrip(
			new CoreMessage.RequestDestroyGroup { PacketContext = 1, DpnidGroup = new Dpnid(2) },
			Hex("D5000000 01000000 02000000 00000000")
		);
		RoundTrip(
			new CoreMessage.DestroyGroup
			{
				DpnidGroup = new Dpnid(2),
				Version = 4,
				DpnidRequesting = new Dpnid(5),
				PacketContext = 6,
			},
			Hex("D8000000 02000000 04000000 00000000 05000000 06000000")
		);
	}

	[Fact]
	public void Deserialize_RejectsOffsetsPastTheEnd()
	{
		CoreMessageSerializer
			.Default.Deserialize(
				Hex("D6000000 05000000 44332211 01000000 FF000000 04000000 00000000 00000000")
			)
			.Should()
			.BeNull();
	}
}
