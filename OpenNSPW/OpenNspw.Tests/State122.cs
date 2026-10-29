using System.Runtime.InteropServices;

namespace OpenNspw.Tests;

// 1.22's own state in a single-player game, which a test saves to run code twice from the same state: the emulator's
// memory, and the computer's fleets, which the game keeps and shares with it.
internal static class State122
{
	private static Span<byte> FleetBytes(Nspw game) =>
		MemoryMarshal.AsBytes((Span<ComputerFleet>)game.ComputerFleets);

	public static byte[] Save(Nspw game) => [.. game.Emulator!.Memory, .. FleetBytes(game)];

	public static void Restore(Nspw game, byte[] state)
	{
		var memory = game.Emulator!.Memory;
		state.AsSpan(0, memory.Length).CopyTo(memory);
		state.AsSpan(memory.Length).CopyTo(FleetBytes(game));
	}
}
