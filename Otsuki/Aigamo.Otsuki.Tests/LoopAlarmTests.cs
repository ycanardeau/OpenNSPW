using Aigamo.Otsuki.Actors;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

public class LoopAlarmTests
{
	// The runtime's timer takes whole milliseconds and drops the rest, so the alarm asks for whole milliseconds, rounded
	// up: a deadline 0.7 ms away rings after 1 ms, never before the deadline.
	[Fact]
	public void Arm_RingsAfterTheDeadline_InWholeMilliseconds()
	{
		var clock = new ManualTimeProvider();
		var rang = 0;
		var alarm = new LoopAlarm(clock, () => rang++);

		alarm.Arm(alarm.Now + TimeSpan.FromMilliseconds(0.7));

		clock.FireNext(clock.Now + TimeSpan.FromMilliseconds(0.9)).Should().BeFalse();
		clock.FireNext(clock.Now + TimeSpan.FromMilliseconds(1)).Should().BeTrue();
		rang.Should().Be(1);
	}
}
