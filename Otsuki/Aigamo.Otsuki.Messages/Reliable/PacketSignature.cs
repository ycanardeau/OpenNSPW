// Comments from: https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/8a305be6-cd80-4df7-83a6-b52de3c5a958

using System.Security.Cryptography;

namespace Aigamo.Otsuki.Messages.Reliable;

/// <summary>
/// The computations of packet signing ([MC-DPL8R] section 3.1.5.2.7). All 64-bit values are little-endian.
/// </summary>
public static class PacketSignature
{
	private const int SignatureSize = 8;

	private static int CountBits(int value)
	{
		var count = 0;
		for (; value != 0; value &= value - 1)
			count++;
		return count;
	}

	private static byte[] Sha1(byte[] data)
	{
		using var sha1 = SHA1.Create();
		return sha1.ComputeHash(data);
	}

	private static ulong First64Bits(byte[] digest) => BitConverter.ToUInt64(digest, 0);

	/// <summary>
	/// The offset of <b>ullSignature</b> in a frame of a signed connection, or <see langword="null"/> for frames that
	/// carry none (CONNECT, CONNECTED and CONNECTED_SIGNED) or that are too short.
	/// </summary>
	public static int? GetSignatureOffset(byte[] datagram)
	{
		if (datagram.Length < 2)
			return null;

		const int frameMasks = (int)(
			PacketControl.Sack1 | PacketControl.Sack2 | PacketControl.Send1 | PacketControl.Send2
		);
		const int sackMasks = (int)(
			SackFlags.Sack1 | SackFlags.Sack2 | SackFlags.Send1 | SackFlags.Send2
		);
		int? offset = ((PacketCommand)datagram[0]).HasFlag(PacketCommand.Data)
			? 4 + 4 * CountBits(datagram[1] & frameMasks)
			: (ExtendedOpcode)datagram[1] switch
			{
				ExtendedOpcode.Sack when datagram.Length > 2 => 12
					+ 4 * CountBits(datagram[2] & sackMasks),
				ExtendedOpcode.HardDisconnect => 16,
				_ => null,
			};

		return offset is int o && o + SignatureSize <= datagram.Length ? o : null;
	}

	public static ulong ReadSignature(byte[] datagram, int signatureOffset) =>
		BitConverter.ToUInt64(datagram, signatureOffset);

	/// <summary>
	/// Writes <paramref name="signature"/> into the <b>ullSignature</b> field of a serialized frame.
	/// </summary>
	public static void WriteSignature(byte[] datagram, int signatureOffset, ulong signature) =>
		Array.Copy(BitConverter.GetBytes(signature), 0, datagram, signatureOffset, SignatureSize);

	/// <summary>
	/// The full signature of a frame: the first 64 bits of a SHA-1 digest of the whole frame, with the
	/// <b>ullSignature</b> bytes set to 0, followed by the secret.
	/// </summary>
	public static ulong ComputeFullSignature(byte[] datagram, int signatureOffset, ulong secret)
	{
		var buffer = new byte[datagram.Length + 8];
		Array.Copy(datagram, buffer, datagram.Length);
		Array.Clear(buffer, signatureOffset, SignatureSize);
		Array.Copy(BitConverter.GetBytes(secret), 0, buffer, datagram.Length, 8);
		return First64Bits(Sha1(buffer));
	}

	/// <summary>
	/// The secret modifier taken from a reliable payload.
	/// </summary>
	/// <remarks>
	/// [MC-DPL8R] only says that the modifier is "derived from" the payload. This takes the first 64 bits of a
	/// SHA-1 digest of the payload, which is not confirmed against real DirectPlay.
	/// </remarks>
	public static ulong ComputeModifier(byte[] payload) => First64Bits(Sha1(payload));

	/// <summary>
	/// The next secret: the first 64 bits of a SHA-1 digest of the previous secret followed by the modifier.
	/// </summary>
	public static ulong ComputeNextSecret(ulong previous, ulong modifier) =>
		First64Bits(Sha1([.. BitConverter.GetBytes(previous), .. BitConverter.GetBytes(modifier)]));
}
