namespace OpenNspw;

// The keys and buttons held down (key_cndtn): the arrow keys, which scroll the battle area, and Space. The other
// flags are defined by the original but not used.
[Flags]
public enum InputButtons
{
	None = 0,

	[Original("FRONT_BTN")]
	Front = 1 << 0,

	[Original("BACK_BTN")]
	Back = 1 << 1,

	[Original("RIGHT_BTN")]
	Right = 1 << 2,

	[Original("LEFT_BTN")]
	Left = 1 << 3,

	[Original("UP_BTN")]
	Up = 1 << 4,

	[Original("DOWN_BTN")]
	Down = 1 << 5,

	[Original("R_TURN_BTN")]
	TurnRight = 1 << 6,

	[Original("L_TURN_BTN")]
	TurnLeft = 1 << 7,

	[Original("MS_R_BTN")]
	MouseRight = 1 << 8,

	[Original("MS_L_BTN")]
	MouseLeft = 1 << 9,

	[Original("MS_C_BTN")]
	MouseCenter = 1 << 10,

	[Original("MS_R_BTN2")]
	MouseRight2 = 1 << 11,

	[Original("MS_L_BTN2")]
	MouseLeft2 = 1 << 12,

	[Original("MS_C_BTN2")]
	MouseCenter2 = 1 << 13,

	[Original("FRONT_BTN2")]
	Front2 = 1 << 14,

	[Original("BACK_BTN2")]
	Back2 = 1 << 15,

	[Original("SPACE")]
	Space = 1 << 16,

	[Original("V_KEY")]
	V = 1 << 17,

	[Original("TOP_VIEW_BTN")]
	TopView = 1 << 18,

	[Original("TOP_VIEW_BTN2")]
	TopView2 = 1 << 19,
}
