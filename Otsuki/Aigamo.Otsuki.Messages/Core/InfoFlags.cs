// Comments from: https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/f2bea43f-8fe7-4a7d-86ae-4910d4aa1bb5

namespace Aigamo.Otsuki.Messages.Core;

/// <summary>
/// Which of a player's or group's fields a message carries.
/// </summary>
[Flags]
public enum InfoFlags
{
	None = 0,

	/// <summary>
	/// DPNINFO_NAME: the name is included.
	/// </summary>
	Name = 0x00000001,

	/// <summary>
	/// DPNINFO_DATA: the data is included.
	/// </summary>
	Data = 0x00000002,
}
