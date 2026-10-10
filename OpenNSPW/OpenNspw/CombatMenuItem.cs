namespace OpenNspw;

// An entry of the combat menu of the unit information panel (cmbt_menu_slctd, NEW_MENU.menu): a mode, the supply
// action, or the weapon to arm an attacker or a bomber with. The original keeps them in the same #define group as the
// modes. The arming entries have the values of the FireKinds they arm with, and the code relies on it: it compares a
// unit's weapon with them, and stores a weapon in cmbt_menu_slctd. The underlying type is the fields'.
public enum CombatMenuItem : short
{
	None = 0,

	[Original("MOVE")]
	Move = 1,

	[Original("SLOW")]
	Slow = 2,

	[Original("RETURN")]
	Return = 4,

	// 補給と修理
	[Original("SPRY")]
	Supply = 5,

	// FireKind.Torpedo
	[Original("RDY_TPD")]
	ArmWithTorpedo = 15,

	// FireKind.Bomb
	[Original("RDY_BOM")]
	ArmWithBomb = 16,

	// FireKind.Unarmed
	[Original("NOTHING")]
	Disarm = 17,
}
