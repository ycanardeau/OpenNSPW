using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable;

internal static class SequenceIdExtensions
{
	/// <summary>
	/// Number of steps from <paramref name="from"/> forward to <paramref name="to"/> in the 8-bit sequence space.
	/// </summary>
	public static int DistanceTo(this SequenceId from, SequenceId to) =>
		(byte)(to.Value - from.Value);
}
