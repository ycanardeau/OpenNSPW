using System.Buffers.Binary;
using System.Diagnostics;

namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// A message of the load: the time it was sent, as <see cref="Stopwatch"/>'s timestamp, which the processes of one
/// machine share, then padding to its size.
/// </summary>
internal static class Payload
{
	public const int MinSize = sizeof(long);

	public static byte[] Create(int size)
	{
		var payload = new byte[Math.Max(MinSize, size)];
		BinaryPrimitives.WriteInt64LittleEndian(payload, Stopwatch.GetTimestamp());
		return payload;
	}

	/// <summary>
	/// Microseconds since <paramref name="payload"/> was created, or -1 if it is not a payload of the load.
	/// </summary>
	public static long Age(ReadOnlySpan<byte> payload, long now)
	{
		if (payload.Length < MinSize)
			return -1;

		var sent = BinaryPrimitives.ReadInt64LittleEndian(payload);
		return (now - sent) * 1_000_000 / Stopwatch.Frequency;
	}
}
