using Xunit;

namespace OpenNspw.Tests;

// nspw_math.cs, against the reference's nspw_math.cpp.
public class MathTests
{
	[Fact]
	public void sin()
	{
		FunctionTest.Run("math", "sin", (game, c) => nspw_math.sin(c.DoubleArg(0)));
	}

	[Fact]
	public void cos()
	{
		FunctionTest.Run("math", "cos", (game, c) => nspw_math.cos(c.DoubleArg(0)));
	}

	[Fact]
	public void atan2()
	{
		FunctionTest.Run("math", "atan2", (game, c) => nspw_math.atan2(c.DoubleArg(0), c.DoubleArg(1)));
	}

	private static long UlpDistance(double a, double b)
	{
		return Math.Abs(BitConverter.DoubleToInt64Bits(a) - BitConverter.DoubleToInt64Bits(b));
	}

	// The portable functions must also be correct: within one ulp of the platform's, which are correctly rounded or
	// nearly so.
	[Fact]
	public void Accuracy()
	{
		var random = new Random(1);
		for (var i = 0; i < 100_000; i++)
		{
			var x = (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-8, 9));
			var y = (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-8, 9));
			Assert.True(UlpDistance(nspw_math.sin(x), Math.Sin(x)) <= 1, $"sin({x:R})");
			Assert.True(UlpDistance(nspw_math.cos(x), Math.Cos(x)) <= 1, $"cos({x:R})");
			Assert.True(UlpDistance(nspw_math.atan2(y, x), Math.Atan2(y, x)) <= 1, $"atan2({y:R}, {x:R})");
		}
	}
}
