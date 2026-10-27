using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Enumeration;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Enumeration;

public class EnumerationMessageSerializerTests
{
	private static readonly Guid GuidInstance = new("00112233-4455-6677-8899-aabbccddeeff");

	private static readonly Guid GuidApplication = new("6dd5f9d4-2b3c-4d39-9f1e-6a0d4a6c4c3e");

	private static string Hex(Guid guid) => Convert.ToHexString(guid.ToByteArray());

	[Fact]
	public void Query_WithApplicationGuid()
	{
		var data = Convert.FromHexString(
			"00" // LeadByte
				+ "02" // CommandByte
				+ "3412" // EnumPayload
				+ "01" // QueryType: ApplicationGUID follows
				+ Hex(GuidApplication)
				+ "AABB" // ApplicationPayload
		);
		var message = new EnumerationMessage.Query
		{
			EnumPayload = 0x1234,
			GuidApplication = GuidApplication,
			ApplicationPayload = ImmutableArray.Create<byte>(0xAA, 0xBB),
		};

		var read = (EnumerationMessage.Query)
			EnumerationMessageSerializer.Default.Deserialize(data)!;
		read.EnumPayload.Should().Be(0x1234);
		read.GuidApplication.Should().Be(GuidApplication);
		read.ApplicationPayload.Should().Equal(0xAA, 0xBB);
		EnumerationMessageSerializer.Default.Serialize(message).Should().Equal(data);
	}

	[Fact]
	public void Query_WithoutApplicationGuid()
	{
		var data = Convert.FromHexString("0002010002");

		var read = (EnumerationMessage.Query)
			EnumerationMessageSerializer.Default.Deserialize(data)!;
		read.EnumPayload.Should().Be(1);
		read.GuidApplication.Should().BeNull();
		read.ApplicationPayload.Should().BeEmpty();
		EnumerationMessageSerializer
			.Default.Serialize(new EnumerationMessage.Query { EnumPayload = 1 })
			.Should()
			.Equal(data);
	}

	[Fact]
	public void Response()
	{
		// "AB" in UTF-16 with its terminator is 6 bytes. Offsets count from ReplyOffset, 4 bytes into the message,
		// and the variable fields follow the 88 fixed bytes from there.
		var data = Convert.FromHexString(
			"00" // LeadByte
				+ "03" // CommandByte
				+ "3412" // EnumPayload
				+ "60000000" // ReplyOffset: 88 + 6 + 2
				+ "01000000" // ResponseSize
				+ "50000000" // ApplicationDescSize
				+ "84000000" // ApplicationDescFlags: REQUIREPASSWORD | MIGRATE_HOST
				+ "08000000" // MaxPlayers
				+ "03000000" // CurrentPlayers
				+ "58000000" // SessionNameOffset
				+ "06000000" // SessionNameSize
				+ "00000000" // PasswordOffset
				+ "00000000" // PasswordSize
				+ "00000000" // ReservedDataOffset
				+ "00000000" // ReservedDataSize
				+ "5E000000" // ApplicationReservedDataOffset
				+ "02000000" // ApplicationReservedDataSize
				+ Hex(GuidInstance)
				+ Hex(GuidApplication)
				+ "410042000000" // SessionName
				+ "0102" // ApplicationReservedData
				+ "FF" // ApplicationData
		);
		var message = new EnumerationMessage.Response
		{
			EnumPayload = 0x1234,
			Flags = SessionFlags.RequirePassword | SessionFlags.MigrateHost,
			MaxPlayers = 8,
			CurrentPlayers = 3,
			GuidInstance = GuidInstance,
			GuidApplication = GuidApplication,
			SessionName = "AB",
			ApplicationReservedData = ImmutableArray.Create<byte>(1, 2),
			ApplicationData = ImmutableArray.Create<byte>(0xFF),
		};

		var read = (EnumerationMessage.Response)
			EnumerationMessageSerializer.Default.Deserialize(data)!;
		read.Should()
			.BeEquivalentTo(
				message,
				o => o.Excluding(m => m.ApplicationReservedData).Excluding(m => m.ApplicationData)
			);
		read.ApplicationReservedData.Should().Equal(1, 2);
		read.ApplicationData.Should().Equal(0xFF);
		EnumerationMessageSerializer.Default.Serialize(message).Should().Equal(data);
	}

	[Fact]
	public void Response_NeverAdvertisesNoEnumerations()
	{
		var data = EnumerationMessageSerializer.Default.Serialize(
			new EnumerationMessage.Response { Flags = SessionFlags.NoEnumerations }
		);

		((EnumerationMessage.Response)EnumerationMessageSerializer.Default.Deserialize(data)!)
			.Flags.Should()
			.Be(0);
	}

	[Theory]
	[InlineData("88010000")] // a CONNECT frame of [MC-DPL8R]
	[InlineData("0004")] // an unknown command
	[InlineData("000201")] // truncated
	public void Deserialize_RejectsOtherDatagrams(string hex)
	{
		EnumerationMessageSerializer
			.Default.Deserialize(Convert.FromHexString(hex))
			.Should()
			.BeNull();
	}
}
