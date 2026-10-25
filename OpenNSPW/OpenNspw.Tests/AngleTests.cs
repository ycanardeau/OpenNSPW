using Xunit;

namespace OpenNspw.Tests;

// Angle's steps give the same bits as the statements they replace (docs/Refactoring.md, Value types).
public class AngleTests
{
	private static readonly double[] Edges =
	[
		-720,
		-360,
		-22.5,
		-1e-14,
		-0.0,
		0,
		1e-14,
		22.4999,
		22.5,
		337.5,
		359.99999999999994,
		360,
		360.0000000000001,
		720,
	];

	private static IEnumerable<double> Degrees()
	{
		var random = new Random(7);
		for (var i = 0; i < 100_000; i++)
		{
			yield return (random.NextDouble() - 0.5) * 1440;
		}

		foreach (var edge in Edges)
		{
			yield return edge;
		}
	}

	private static void AssertSameBits(double expected, Angle actual)
	{
		Assert.Equal(
			BitConverter.DoubleToInt64Bits(expected),
			BitConverter.DoubleToInt64Bits(actual.Degrees)
		);
	}

	[Fact]
	public void Wraps_are_the_original_statements()
	{
		foreach (var degrees in Degrees())
		{
			var d = degrees;
			if (d >= 360)
				d = d - 360;
			if (d < 0)
				d = 360 + d;
			AssertSameBits(d, new Angle(degrees).Wrapped());

			d = degrees;
			if (d < 0)
				d = 360 + d;
			if (d >= 360)
				d = d - 360;
			AssertSameBits(d, new Angle(degrees).WrapNegative().WrapFullTurn());

			AssertSameBits((int)degrees % 360, new Angle(degrees).WholeDegrees());
		}
	}

	[Fact]
	public void SnappedToEightDirections_is_the_original_switch()
	{
		foreach (var degrees in Degrees())
		{
			var heading = -1.0;
			switch ((int)(degrees / 22.5))
			{
				case 0:
				case 15:
					heading = 0.0;
					break;
				case 1:
				case 2:
					heading = 45.0;
					break;
				case 3:
				case 4:
					heading = 90.0;
					break;
				case 5:
				case 6:
					heading = 90.0 + 45.0;
					break;
				case 7:
				case 8:
					heading = 180.0;
					break;
				case 9:
				case 10:
					heading = 180.0 + 45.0;
					break;
				case 11:
				case 12:
					heading = 270.0;
					break;
				case 13:
				case 14:
					heading = 270.0 + 45.0;
					break;
			}

			AssertSameBits(heading, new Angle(degrees).SnappedToEightDirections(-1.0));
		}
	}
}
