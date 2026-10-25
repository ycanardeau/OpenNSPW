using System.Buffers.Binary;
using OpenNspw.Singleplayer;
using Xunit;

namespace OpenNspw.Tests;

// The emulator on the single-player executable alone, without the port (docs/Singleplayer.md).
public class EmulatorTests
{
	private const uint UnitAddress = 0x4916A0;
	private const int UnitSize = 0x598;

	private static readonly byte[] Executable = File.ReadAllBytes(
		Path.Combine(GameData.FindSingleplayer(), "NSPW_122.exe")
	);

	public static TheoryData<int> Scenarios()
	{
		var data = new TheoryData<int>();
		foreach (var scenario in Functions.Scenarios)
		{
			data.Add(scenario);
		}

		return data;
	}

	// An emulator whose rand() is the MSVC CRT's from seed 1, and whose load_it2 reads nothing.
	private static Emulator CreateEmulator()
	{
		var emulator = new Emulator(Executable);
		var seed = 1u;
		emulator.SetNative(
			Functions.Rand,
			() =>
			{
				seed = seed * 214013 + 2531011;
				emulator.Cpu.Eax = new((seed >> 16) & 0x7FFF);
			}
		);
		emulator.SetNative(Functions.LoadMap, () => { });
		return emulator;
	}

	// __ftol truncates ST(0) toward zero into EDX:EAX, and gives the integer indefinite, 0x8000000000000000, for a NaN or
	// a value out of range, as the CRT's fistp does.
	[Theory]
	[InlineData(2.9, 2UL)]
	[InlineData(-2.9, 0xFFFFFFFFFFFFFFFEUL)]
	[InlineData(double.NaN, 0x8000000000000000UL)]
	[InlineData(1e30, 0x8000000000000000UL)]
	[InlineData(-1e30, 0x8000000000000000UL)]
	public void FtolTruncates(double value, ulong result)
	{
		var emulator = new Emulator(Executable);
		emulator.Cpu.Fpu.Stack.Push(Aigamo.Enzan.Register64.FromDouble(value));

		var low = emulator.Run(Functions.Ftol);

		Assert.Equal(result, ((ulong)emulator.Cpu.Edx.Value << 32) | low);
	}

	private static short Side(Emulator emulator, int unit)
	{
		return BinaryPrimitives.ReadInt16LittleEndian(
			emulator.At(UnitAddress + (uint)(unit * UnitSize), 2)
		);
	}

	[Theory]
	[MemberData(nameof(Scenarios))]
	public void ScenarioSetUpPlacesUnitsOfBothSides(int scenario)
	{
		var emulator = CreateEmulator();

		emulator.Run(Functions.SetUpScenario(scenario));

		var sides = Enumerable
			.Range(1, 100)
			.Select(m => Side(emulator, m))
			.Where(side => side != 0)
			.ToList();
		Assert.Contains((short)1, sides);
		Assert.Contains((short)2, sides);
		Assert.All(sides, side => Assert.InRange(side, 1, 2));
	}
}
