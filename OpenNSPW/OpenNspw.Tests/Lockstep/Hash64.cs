using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenNspw.Tests.Lockstep;

// A 64-bit hash of bytes, the same on every platform and in every process, unlike HashCode. Not cryptographic: it only
// tells whether bytes changed.
internal struct Hash64()
{
	private const ulong Prime1 = 0x9E3779B185EBCA87;
	private const ulong Prime2 = 0xC2B2AE3D27D4EB4F;

	private ulong _value = 0x27D4EB2F165667C5;

	public readonly ulong Value => _value;

	private void AddWord(ulong word)
	{
		_value = BitOperations.RotateLeft(_value ^ (word * Prime2), 31) * Prime1;
	}

	public void Add(ReadOnlySpan<byte> bytes)
	{
		var words = MemoryMarshal.Cast<byte, ulong>(bytes);
		foreach (var word in words)
		{
			AddWord(word);
		}

		ulong tail = 0;
		for (var i = words.Length * sizeof(ulong); i < bytes.Length; i++)
		{
			tail = (tail << 8) | bytes[i];
		}

		AddWord(tail ^ ((ulong)bytes.Length << 56));
	}

	public void Add(ulong value)
	{
		AddWord(value);
	}

	public void Add(string text)
	{
		Add(Encoding.UTF8.GetBytes(text));
	}

	public static ulong Of(ReadOnlySpan<byte> bytes)
	{
		var hash = new Hash64();
		hash.Add(bytes);
		return hash.Value;
	}
}
