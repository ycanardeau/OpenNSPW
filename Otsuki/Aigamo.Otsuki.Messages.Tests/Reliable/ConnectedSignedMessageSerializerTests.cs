using Aigamo.Otsuki.Messages.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Reliable;

public class ConnectedSignedMessageSerializerTests
{
	// A connector's confirmation: no poll, the listener's cookie echoed, both secrets, full signing.
	private static readonly byte[] Data = Convert.FromHexString(
		"8003" // bCommand, bExtOpCode
			+ "0100" // bMsgID, bRspId
			+ "06000100" // dwCurrentProtocolVersion
			+ "C6AEC979" // dwSessID
			+ "9D366723" // tTimestamp
			+ "0807060504030201" // ullConnectSig
			+ "1111111111111111" // ullSenderSecret
			+ "2222222222222222" // ullReceiverSecret
			+ "02000000" // dwSigningOpts
			+ "78563412" // dwEchoTimestamp
	);

	private static readonly ReliableMessage.ConnectedSigned Message = new()
	{
		MessageId = 1,
		ResponseId = 0,
		ProtocolVersion = 0x00010006,
		SessionId = new SessionId(0x79C9AEC6),
		Timestamp = 0x2367369D,
		ConnectSignature = 0x0102030405060708,
		SenderSecret = 0x1111111111111111,
		ReceiverSecret = 0x2222222222222222,
		SigningOptions = PacketSigning.Full,
		EchoTimestamp = 0x12345678,
	};

	[Fact]
	public void Deserialize()
	{
		var message = ReliableMessageSerializer.Default.Deserialize(Data);

		message.Should().BeOfType<ReliableMessage.ConnectedSigned>().Which.Should().Be(Message);
		((ReliableMessage.ConnectedSigned)message!).Poll.Should().BeFalse();
	}

	[Fact]
	public void Serialize()
	{
		ReliableMessageSerializer.Default.Serialize(Message).Should().Equal(Data);
		Data.Should().HaveCount(48);
	}
}

public class PacketSignatureTests
{
	[Theory]
	// DFRAME without masks: the signature follows the 4-byte header.
	[InlineData("3F000000" + "0000000000000000", 4)]
	// DFRAME with SACK1 and SEND1: two masks precede it.
	[InlineData("3F500000" + "00000000" + "00000000" + "0000000000000000", 12)]
	// SACK with SACK_MASK1: the 12-byte header and one mask precede it.
	[InlineData("800603000000000000000000" + "00000000" + "0000000000000000", 16)]
	// HARD_DISCONNECT: the 16-byte header precedes it.
	[InlineData("80040000060001000000000000000000" + "0000000000000000", 16)]
	public void GetSignatureOffset(string hex, int expected)
	{
		PacketSignature.GetSignatureOffset(Convert.FromHexString(hex)).Should().Be(expected);
	}

	[Theory]
	[InlineData("88010000060001000000000000000000")] // CONNECT carries no signature
	[InlineData("3F000000000000")] // too short to hold one
	public void GetSignatureOffset_WithoutSignature(string hex)
	{
		PacketSignature.GetSignatureOffset(Convert.FromHexString(hex)).Should().BeNull();
	}

	[Fact]
	public void ComputeFullSignature_IgnoresTheSignatureBytesAndDependsOnTheRest()
	{
		var frame = Convert.FromHexString("3F000000" + "AABBCCDDEEFF0011" + "41424344");
		var signature = PacketSignature.ComputeFullSignature(frame, 4, secret: 42);

		var signed = frame.ToArray();
		PacketSignature.WriteSignature(signed, 4, signature);
		PacketSignature.ReadSignature(signed, 4).Should().Be(signature);
		PacketSignature.ComputeFullSignature(signed, 4, secret: 42).Should().Be(signature);

		var tampered = signed.ToArray();
		tampered[^1] ^= 1;
		PacketSignature.ComputeFullSignature(tampered, 4, secret: 42).Should().NotBe(signature);
		PacketSignature.ComputeFullSignature(signed, 4, secret: 43).Should().NotBe(signature);
	}

	[Fact]
	public void ComputeNextSecret_IsTheFirst64BitsOfSha1OfBothValues()
	{
		var expected = BitConverter.ToUInt64(
			System.Security.Cryptography.SHA1.HashData([
				.. BitConverter.GetBytes(1UL),
				.. BitConverter.GetBytes(2UL),
			])
		);

		PacketSignature.ComputeNextSecret(1, 2).Should().Be(expected);
	}
}
