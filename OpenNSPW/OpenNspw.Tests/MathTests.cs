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

	// SinDegrees and CosDegrees give the same bits as the expressions they replace, for directions in and out of the
	// range the game keeps them in.
	[Fact]
	public void Degrees_are_the_original_expressions()
	{
		var random = new Random(2);
		for (var i = 0; i < 100_000; i++)
		{
			var degrees = (random.NextDouble() - 0.25) * 720;
			Assert.Equal(BitConverter.DoubleToInt64Bits(nspw_math.sin(degrees * all_head.a_PI)), BitConverter.DoubleToInt64Bits(nspw_math.SinDegrees(degrees)));
			Assert.Equal(BitConverter.DoubleToInt64Bits(nspw_math.cos(degrees * all_head.a_PI)), BitConverter.DoubleToInt64Bits(nspw_math.CosDegrees(degrees)));
		}
	}

	// The original's computation of a distance, written out as at its about 70 sites.
	private static double InlineDistance(double wrk_x, double wrk_y)
	{
		double drctn;
		if (wrk_x == 0) wrk_x = 1;
		if (wrk_y == 0) wrk_y = 1;
		drctn = nspw_math.atan2(wrk_y, wrk_x) * all_head.RAD_to;
		if (drctn < 0)
			drctn = 360 + drctn;
		if (wrk_x < 0)
			wrk_x = 0 - wrk_x;
		if (wrk_y < 0)
			wrk_y = 0 - wrk_y;
		if (drctn >= 180)
			drctn = drctn - 180;
		if (drctn >= 90)
			drctn = 90 - (drctn - 90);
		return (wrk_x) / (nspw_math.CosDegrees(drctn));
	}

	// Distance gives the same bits as the computation it replaces, for vectors of every direction and size, and with
	// components of 0.
	[Fact]
	public void Distance_is_the_original_computation()
	{
		var random = new Random(3);
		for (var i = 0; i < 100_000; i++)
		{
			var dx = i % 10 == 0 ? 0 : (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-2, 6));
			var dy = i % 7 == 0 ? 0 : (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-2, 6));
			Assert.Equal(BitConverter.DoubleToInt64Bits(InlineDistance(dx, dy)), BitConverter.DoubleToInt64Bits(nspw_math.Distance(dx, dy)));
		}
	}
}
