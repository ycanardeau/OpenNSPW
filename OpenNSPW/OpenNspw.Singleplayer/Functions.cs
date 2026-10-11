namespace OpenNspw.Singleplayer;

// The functions of the single-player executable that the port calls, or implements natively (docs/Singleplayer.md).
public static class Functions
{
	// cpu_strtgy(): the computer's orders for this frame.
	public const uint CpuStrategy = 0x404E30;

	// plane_in_cv(int m): the planes of carrier or air base m.
	public const uint CountPlanesIn = 0x416F00;

	// decision(): the scenario's events and victory conditions.
	public const uint Decision = 0x41C3C0;

	// set_unit_data(int m): the data of unit m by its kind and type (speed, HP, weapons, fuel).
	public const uint SetUnitData = 0x41E150;

	// set_new_unit_plane(int side, int kind, int type, int no, int planes, int arm): a new plane at carrier or air base no.
	public const uint AddPlane = 0x41F060;

	// set_cpu_route(int m, double): the route of the computer's ship m to its destination.
	public const uint SetCpuRoute = 0x42DA80;

	// set_cpu_route2(int m): the route of ship m to its destination, which a ship that follows a leader takes.
	public const uint SetCpuRoute2 = 0x42ED30;

	// load_it2(char* file_name): reads a map into cmbt_map.
	public const uint LoadMap = 0x432A30;

	// rand(): the C runtime's.
	public const uint Rand = 0x436056;

	// __ftol(): the C runtime's conversion of ST(0) to a 64-bit integer, in EDX:EAX.
	public const uint Ftol = 0x4360C4;

	// The scenarios of the single-player executable, by number.
	public static IReadOnlyList<int> Scenarios { get; } =
	[
		1,
		2,
		3,
		4,
		5,
		6,
		7,
		8,
		9,
		10,
		100,
		101,
		102,
		103,
		104,
		105,
		106,
		107,
		108,
		109,
		200,
		201,
		202,
		203,
		204,
		205,
		206,
		207,
		208,
		209,
		300,
		301,
		302,
		303,
		304,
		305,
		306,
	];

	// scenario_N(): the units and the state of scenario N, as game_init dispatches them by sinario.
	public static uint SetUpScenario(int scenario) =>
		scenario switch
		{
			1 => 0x42A2E0,
			2 => 0x42A6C0,
			3 => 0x42AA60,
			4 => 0x42AC90,
			5 => 0x42AF40,
			6 => 0x42B3A0,
			7 => 0x42B8A0,
			8 => 0x42C480,
			9 => 0x42CED0,
			10 => 0x42D3C0,
			100 => 0x41F570,
			101 => 0x41F830,
			102 => 0x41FD60,
			103 => 0x420260,
			104 => 0x4207C0,
			105 => 0x420DE0,
			106 => 0x421310,
			107 => 0x421830,
			108 => 0x423940,
			109 => 0x424DD0,
			200 => 0x421DB0,
			201 => 0x422820,
			202 => 0x422D10,
			203 => 0x422F40,
			204 => 0x4241B0,
			205 => 0x424760,
			206 => 0x421A80,
			207 => 0x425D60,
			208 => 0x425510,
			209 => 0x426270,
			300 => 0x426DE0,
			301 => 0x427030,
			302 => 0x4278B0,
			303 => 0x428020,
			304 => 0x4287E0,
			305 => 0x428C60,
			306 => 0x429310,
			_ => throw new ArgumentOutOfRangeException(
				nameof(scenario),
				scenario,
				"Not a scenario of the single-player executable."
			),
		};
}
