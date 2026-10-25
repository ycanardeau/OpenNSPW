using Xunit;

namespace OpenNspw.Tests;

// WorldPosition and WorldVector give the same bits as the expressions they replace (docs/Refactoring.md, Value types).
public class VectorTests
{
	private static void AssertSameBits(double expected, double actual)
	{
		Assert.Equal(
			BitConverter.DoubleToInt64Bits(expected),
			BitConverter.DoubleToInt64Bits(actual)
		);
	}

	private static double Coordinate(Random random)
	{
		return (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-2, 6));
	}

	// FromDirection(direction) * factor is `x+=CosDegrees(direction)*factor; y+=SinDegrees(direction)*factor;`.
	[Fact]
	public void FromDirection_times_a_factor_is_the_original_move()
	{
		var random = new Random(5);
		for (var i = 0; i < 100_000; i++)
		{
			var direction = (random.NextDouble() - 0.25) * 720;
			var factor = Coordinate(random);
			var position = new WorldPosition(Coordinate(random), Coordinate(random));
			var moved = position + WorldVector.FromDirection(direction) * factor;
			AssertSameBits(position.X + nspw_math.CosDegrees(direction) * factor, moved.X);
			AssertSameBits(position.Y + nspw_math.SinDegrees(direction) * factor, moved.Y);
		}
	}

	// (to - from).Length and .Direction are Distance and Direction of `to.X-from.X` and `to.Y-from.Y`, and NonZero is
	// `if(wrk_x==0) wrk_x=1; if(wrk_y==0) wrk_y=1;`.
	[Fact]
	public void Difference_is_the_original_computation()
	{
		var random = new Random(6);
		for (var i = 0; i < 100_000; i++)
		{
			var from = new WorldPosition(Coordinate(random), Coordinate(random));
			var to =
				i % 10 == 0
					? new WorldPosition(from.X, Coordinate(random))
					: new WorldPosition(
						Coordinate(random),
						i % 7 == 0 ? from.Y : Coordinate(random)
					);
			var dx = to.X - from.X;
			var dy = to.Y - from.Y;
			var difference = to - from;
			AssertSameBits(nspw_math.Distance(dx, dy), difference.Length);
			AssertSameBits(nspw_math.Direction(dx, dy), difference.Direction.Degrees);

			if (dx == 0)
				dx = 1;
			if (dy == 0)
				dy = 1;
			AssertSameBits(dx, difference.NonZero().X);
			AssertSameBits(dy, difference.NonZero().Y);
			AssertSameBits(nspw_math.Direction(dx, dy), difference.NonZero().Direction.Degrees);
		}
	}
}
