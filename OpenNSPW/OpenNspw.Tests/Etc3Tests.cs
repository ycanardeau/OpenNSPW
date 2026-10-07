using Xunit;

namespace OpenNspw.Tests;

// etc3.cpp
public class Etc3Tests
{
	[Fact]
	public void make_my_rnd()
	{
		FunctionTest.Run("etc3", "make_my_rnd", (game, c) =>
		{
			game.make_my_rnd();
			return null;
		});
	}

	[Fact]
	public void my_rnd()
	{
		FunctionTest.Run("etc3", "my_rnd", (game, c) => game.my_rnd(c.IntArg(0)));
	}

	[Fact]
	public void rnd()
	{
		FunctionTest.Run("etc3", "rnd", (game, c) => game.rnd(c.IntArg(0)));
	}

	[Fact]
	public void set_sprt_data()
	{
		FunctionTest.Run("etc3", "set_sprt_data", (game, c) =>
		{
			game.set_sprt_data();
			return null;
		});
	}

	[Fact]
	public void cloud_in_start()
	{
		FunctionTest.Run("etc3", "cloud_in_start", (game, c) =>
		{
			game.cloud_in_start();
			return null;
		});
	}

	[Fact]
	public void cloud_cont()
	{
		FunctionTest.Run("etc3", "cloud_cont", (game, c) =>
		{
			game.cloud_cont();
			return null;
		});
	}
}
