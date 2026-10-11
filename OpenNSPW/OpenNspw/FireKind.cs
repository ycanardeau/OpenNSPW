namespace OpenNspw;

// The kind of a fire, a projectile (FIRE.kind), and of the weapon a unit is armed with (UNIT.Weapon, arm[0]), which is
// the kind of fire it shoots. A plane's weapon can also be Maintenance or Unarmed, and a transport's its cargo. The
// underlying type is the fields'.
public enum FireKind
{
	None = 0,

	// 弾丸
	[Original("BLT")]
	Bullet = 11,

	// 対地砲
	[Original("GUN")]
	Gun = 12,

	// 対空炸裂弾
	[Original("SHL")]
	AntiAircraftShell = 13,

	// 対空炸裂ＶＴ信管弾
	[Original("VTH")]
	ProximityFuzedShell = 14,

	// 魚雷
	[Original("TPD")]
	Torpedo = 15,

	// 爆弾
	[Original("BOM")]
	Bomb = 16,

	// A plane with no weapon loaded.
	[Original("NTG")]
	Unarmed = 17,

	// 発進最低整備: a plane in maintenance before it can take off.
	[Original("TUN")]
	Maintenance = 18,

	// 対潜爆弾
	[Original("ASB")]
	AntiSubmarineBomb = 19,

	// 対空機関砲弾　Rapid anti Air Shell
	[Original("RAS")]
	RapidAntiAircraftShell = 20,

	// 対地砲
	[Original("SP_GUN")]
	NavalBaseGun = 21,

	// A transport's cargo: what it builds where it lands.
	[Original("TR_SP")]
	CargoNavalBase = 30,

	[Original("TR_AP")]
	CargoAirBase = 31,

	[Original("TR_GF1")]
	CargoInfantryBase = 32,

	[Original("TR_GF2")]
	CargoPillboxes = 33,

	[Original("TR_GF3")]
	CargoFortress = 34,
}
