using System.Runtime.CompilerServices;
using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// The tests that count the native memory of the stand-ins (NativeAllocations), which is the whole process's: xUnit runs
// them alone, after the others, so that no other test's game holds any of it as they count.
[CollectionDefinition(nameof(NativeMemory), DisableParallelization = true)]
public sealed class NativeMemory;

// The native memory of a game (docs/Performance.md, Native memory): a game let go of gives it all back once it is
// collected, so that a process that plays many games, as the tests do, holds no more of it than the games it keeps.
// A game that played on would hold about a dozen megabytes more for each, which the collector sees none of.
[Collection(nameof(NativeMemory))]
public sealed class NativeMemoryTests
{
	// The native memory held once every game let go of is collected, and its objects' finalizers have run.
	private static long BytesHeld()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		return NativeAllocations.Bytes;
	}

	// Plays some frames of a battle, and lets the game go where it is, as one that stops before its end does, with no
	// surface or sound released: the native memory held as it plays. Not inlined, so that nothing of the game stays on
	// the caller's stack.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static long PlayABattle()
	{
		using var run = new SingleplayerRun(1, recordsOutput: false);
		run.Frames(50);
		return NativeAllocations.Bytes;
	}

	[Fact]
	public void AGameLetGoOfGivesBackItsNativeMemory()
	{
		var before = BytesHeld();
		var playing = PlayABattle();
		var after = BytesHeld();

		Assert.True(
			playing - before > 1_000_000,
			$"A battle held {playing - before} bytes of native memory as it played: its surfaces' and sounds' are not counted."
		);
		Assert.Equal(before, after);
	}
}
