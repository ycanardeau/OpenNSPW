using Aigamo.Otsuki.Messages.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Reliable;

public class SequenceIdTests
{
	private static readonly SequenceId _testSequenceId = new(255);

	[Fact]
	public void Empty()
	{
		SequenceId.Empty.Should().Be(new SequenceId(0));
	}

	public static TheoryData<SequenceId, object?, int> CompareTo_TestData()
	{
		var data = new TheoryData<SequenceId, object?, int>
		{
			{ _testSequenceId, _testSequenceId, 0 },
			{
				new SequenceId(0),
				new SequenceId(128),
				-1 /* REVIEW */
			},
			{ new SequenceId(128), new SequenceId(0), -1 },
			{ new SequenceId(0), new SequenceId(255), 1 },
			{ new SequenceId(255), new SequenceId(0), -1 },
			{ new SequenceId(1), new SequenceId(255), 1 },
			{ new SequenceId(255), new SequenceId(1), -1 },
			{ new SequenceId(1), new SequenceId(0), 1 },
			{ new SequenceId(0), new SequenceId(1), -1 },
			{ new SequenceId(127), new SequenceId(0), 1 },
			{ new SequenceId(0), new SequenceId(127), -1 },
			{ new SequenceId(127), new SequenceId(1), 1 },
			{ new SequenceId(1), new SequenceId(127), -1 },
			{ new SequenceId(128), new SequenceId(1), 1 },
			{ new SequenceId(1), new SequenceId(128), -1 },
			{ new SequenceId(128), new SequenceId(127), 1 },
			{ new SequenceId(127), new SequenceId(128), -1 },
			{
				new SequenceId(255),
				new SequenceId(127),
				-1 /* REVIEW */
			},
			{ new SequenceId(127), new SequenceId(255), -1 },
			{ new SequenceId(255), new SequenceId(128), 1 },
			{ new SequenceId(128), new SequenceId(255), -1 },
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(CompareTo_TestData))]
	internal void CompareTo(SequenceId sequenceId1, object obj, int expected)
	{
		if (obj is SequenceId sequenceId2)
			Math.Sign(sequenceId1.CompareTo(sequenceId2)).Should().Be(expected);
	}

	public static TheoryData<SequenceId, object?, bool> Equals_TestData()
	{
		var data = new TheoryData<SequenceId, object?, bool>
		{
			{ _testSequenceId, _testSequenceId, true },
			{ _testSequenceId, new SequenceId(255), true },
			{ _testSequenceId, SequenceId.Empty, false },
			{ new SequenceId(1), new SequenceId(1), true },
			{ new SequenceId(1), new SequenceId(0), false },
			{ _testSequenceId, new(), false },
			{ _testSequenceId, null, false },
		};

		return data;
	}

	[Theory]
	[MemberData(nameof(Equals_TestData))]
	internal void EqualsTest(SequenceId sequenceId1, object obj, bool expected)
	{
		if (obj is SequenceId sequenceId2)
		{
			sequenceId1.Equals(sequenceId2).Should().Be(expected);
			(sequenceId1 == sequenceId2).Should().Be(expected);
			(sequenceId1 != sequenceId2).Should().Be(!expected);
			sequenceId1.GetHashCode().Equals(sequenceId2.GetHashCode()).Should().Be(expected);
		}
		sequenceId1.Equals(obj).Should().Be(expected);
	}

	[Fact]
	public void Increment()
	{
		var sequenceId1 = new SequenceId(byte.MaxValue);
		(++sequenceId1).Should().Be(SequenceId.Empty);

		var sequenceId2 = SequenceId.Empty;
		(++sequenceId2).Should().Be(new SequenceId(1));
	}

	[Fact]
	public void ToStringTest()
	{
		_testSequenceId.ToString().Should().Be(_testSequenceId.Value.ToString());
		$"{_testSequenceId:X}".Should().Be($"{_testSequenceId.Value:X}");
	}
}
