namespace OpenNspw.ComputerPlayers;

// NSPW 1.22's cpu_strtgy (0x404E30), rewritten from its machine code (docs/ComputerPlayer.md, cpu_strtgy): the
// computer's order of the frame, for one of its fleets, in the game it is given, as its parts give it in 1.22's order.
// It is tested against the emulated original (ComputerPlayerTests). Its parts are structs, as it is, so that a frame's
// strategy allocates nothing.
internal readonly struct Strategy122(Nspw game)
{
	private readonly Nspw _game = game;

	// cpu_strtgy: the computer's order of this frame, in new_pp[0] and slct_unit[0], for its fleet Renderer.FrameCount%10+1
	// (docs/ComputerPlayer.md, cpu_strtgy).
	public void Plan()
	{
		var f = _game.Renderer.FrameCount % 10 + 1;
		_game.Selections[0] = default;
		_game.MoveOrders[0].Unit = 0;
		new ScenarioEvents(_game).ApplyComputerEvents();

		ref var fleet = ref _game.FleetAt(f);
		var upkeep = new FleetUpkeep(_game);
		var near = upkeep.FindComputersTarget(ref fleet);
		var flow = upkeep.KeepUpFleet(ref fleet);
		if (flow == StrategyFlow.NoFleet)
		{
			if (_game.ScenarioNumber != 304)
				return;
			flow = StrategyFlow.Order;
		}

		if (flow == StrategyFlow.Next)
		{
			var planes = new FleetPlanes(_game);
			planes.ArmComputersPlanes(ref fleet, near);
			planes.FlyComputersPlanes();
			var returning = new ReturnAndSupply(_game);
			if (fleet.Flagship != 0)
			{
				var targets = new FleetTargets(_game);
				for (var i = 1; i <= _game.MaxUnitId; i++)
					targets.ChooseTargetOf(i, f, ref fleet, near);
				returning.DecideToReturn(ref fleet);
			}

			returning.SupplyAtBase(ref fleet);
			returning.StopAtTheEdge(ref fleet);
			flow = new FleetMovement(_game).MoveFleet(ref fleet, near);
			var strikes = new FleetStrikes(_game);
			if (flow == StrategyFlow.Next)
				flow = strikes.StrikeFromFlagship(ref fleet, near);
			if (flow == StrategyFlow.Next)
				flow = strikes.BombFromAirBase(ref fleet, near);
			if (flow == StrategyFlow.Next)
				flow = strikes.StrikeFromOtherCarriers(ref fleet, near);
			if (flow == StrategyFlow.Next)
				strikes.DefendAirBase(ref fleet);
			if (flow == StrategyFlow.Return)
				return;
		}

		new Scenario304Reinforcements(_game).ReinforceScenario304(f, ref fleet);
		new FleetFormation(_game).OrderFleet(f, ref fleet);
	}
}
