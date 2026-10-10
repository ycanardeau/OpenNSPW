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
			game.MakeSharedRandomTable();
			return null;
		});
	}

	[Fact]
	public void my_rnd()
	{
		FunctionTest.Run("etc3", "my_rnd", (game, c) => game.SharedRandom(c.IntArg(0)));
	}

	[Fact]
	public void rnd()
	{
		FunctionTest.Run("etc3", "rnd", (game, c) => game.Random(c.IntArg(0)));
	}

	[Fact]
	public void set_sprt_data()
	{
		FunctionTest.Run("etc3", "set_sprt_data", (game, c) =>
		{
			game.InitializeSprites();
			return null;
		});
	}

	[Fact]
	public void cloud_in_start()
	{
		FunctionTest.Run("etc3", "cloud_in_start", (game, c) =>
		{
			game.InitializeClouds();
			return null;
		});
	}

	[Fact]
	public void cloud_cont()
	{
		FunctionTest.Run("etc3", "cloud_cont", (game, c) =>
		{
			game.UpdateClouds();
			return null;
		});
	}
}
