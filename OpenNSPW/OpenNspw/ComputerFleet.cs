using System.Runtime.InteropServices;

namespace OpenNspw;

// An entry of NSPW 1.22's table of the computer's fleets (docs/ComputerPlayer.md, Fleets), as it is in the emulator's
// memory.
[StructLayout(LayoutKind.Explicit, Size = 0x498)]
public struct ComputerFleet
{
	// 1, form again; 50, move; 10 is also tested; 0, nothing to do.
	[FieldOffset(0x000)]
	public short State;

	// A unit number, or 0 for no fleet.
	[FieldOffset(0x002)]
	public short Flagship;

	// 1 if the first member becomes the flagship when the members change.
	[FieldOffset(0x004)]
	public short RenewsFlagship;

	// 0, hold; 1, attack its targets; 50, go to its second point.
	[FieldOffset(0x006)]
	public short Mode;

	// The point its flagship aims at.
	[FieldOffset(0x008)]
	public double AimX;

	[FieldOffset(0x010)]
	public double AimY;

	// Its path: the next move of its flagship is element 0.
	[FieldOffset(0x018)]
	public Array64<double> PathX;

	[FieldOffset(0x218)]
	public Array64<double> PathY;

	// Enemy unit numbers, from index 1. The members follow, so that 1.22 reads index 18 as the first member.
	[FieldOffset(0x42C)]
	public Array18<short> Targets;

	// Unit numbers, the flagship first.
	[FieldOffset(0x450)]
	public Array18<short> Members;

	// The naval base it is returning to, or 0.
	[FieldOffset(0x474)]
	public short Base;

	// Its destination; x 9601 (World.Beyond) for none.
	[FieldOffset(0x478)]
	public double DestinationX;

	[FieldOffset(0x480)]
	public double DestinationY;

	// Where mode 50 sends it.
	[FieldOffset(0x488)]
	public double SecondX;

	[FieldOffset(0x490)]
	public double SecondY;
}
