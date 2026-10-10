namespace OpenNspw;

// A side of the battle (UNIT.used, EFFECT.used, your_side), or None for an unused unit or effect. The underlying type
// is the fields'.
public enum Side : short
{
	None = 0,

	// 日本海軍
	[Original("JPN")]
	Japan = 1,

	// 合衆国海軍
	[Original("USA")]
	UnitedStates = 2,
}
