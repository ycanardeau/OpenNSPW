using System.Security.Cryptography;

namespace Aigamo.Otsuki.Actors;

/// <summary>
/// The randomness that an actor gives the core it runs: the system's secure source, or, for a test that must repeat, a
/// <see cref="Random"/> of the actor's own from a seed. Only used on the actor's thread.
/// </summary>
internal sealed class RandomSource(int? seed)
{
	private readonly Random? _random = seed is int value ? new Random(value) : null;

	public void Fill(Span<byte> bytes)
	{
		if (_random is null)
			RandomNumberGenerator.Fill(bytes);
		else
			_random.NextBytes(bytes);
	}
}
