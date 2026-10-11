namespace OpenNspw;

// The kind of a unit (UNIT.kind). The underlying type is the field's. The code compares ranges of kinds (AP to GF3 are
// the facilities, GF1 to GF3 the fortifications), so the order is part of the type.
public enum UnitKind
{
	// An unused unit.
	None = 0,

	[Original("BB1")]
	Battleship = 1,

	[Original("CA1")]
	Cruiser = 2,

	[Original("DD1")]
	Destroyer = 3,

	[Original("SS1")]
	Submarine = 4,

	[Original("CV1")]
	Carrier = 5,

	[Original("CVL1")]
	LightCarrier = 6,

	[Original("FT1")]
	Fighter = 7,

	// A carrier's bomber.
	[Original("AT1")]
	Attacker = 8,

	// A strategic bomber.
	[Original("BM1")]
	Bomber = 9,

	[Original("TR1")]
	Transport = 10,

	[Original("AP")]
	AirBase = 11,

	// 軍港
	[Original("SP")]
	NavalBase = 12,

	[Original("CT1")]
	City = 13,

	[Original("MN1")]
	Mine = 14,

	// 歩兵基地
	[Original("GF1")]
	InfantryBase = 15,

	// トーチカ群
	[Original("GF2")]
	Pillboxes = 16,

	// 要塞
	[Original("GF3")]
	Fortress = 17,
}
