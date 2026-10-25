using Xunit;

namespace OpenNspw.Tests;

// etc1.cpp
public class Etc1Tests
{
	[Fact(Skip = FunctionTest.NotRecorded)]
	public void set_pos_of_attack_TR1()
	{
		FunctionTest.Run(
			"etc1",
			"set_pos_of_attack_TR1",
			(game, c) =>
			{
				game.SetTransportLandingDestination(c.IntArg(0));
				return null;
			}
		);
	}

	// The transports' branch of fire_now, which launches the landing craft.
	[Fact(Skip = FunctionTest.NotRecorded)]
	public void fire_now_TR()
	{
		FunctionTest.Run(
			"etc1",
			"fire_now_TR",
			(game, c) =>
			{
				game.FireWeapons(c.IntArg(0), c.IntArg(1), (FireKind)c.IntArg(2));
				return null;
			}
		);
	}
}
