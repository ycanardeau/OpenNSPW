namespace OpenNspw;

// Whether an effect is drawn above or below the units (EFFECT.layer), or unused. The underlying type is the field's.
public enum EffectLayer : short
{
	// An unused effect.
	None = 0,

	[Original("UPPER")]
	Upper = 1,

	[Original("LOWER")]
	Lower = 2,
}
