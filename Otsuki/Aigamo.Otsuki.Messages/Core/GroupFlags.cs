// Comments from: https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/ae589cbb-48b2-4980-80b4-70b520dc0fd0

namespace Aigamo.Otsuki.Messages.Core;

[Flags]
public enum GroupFlags
{
	None = 0,

	/// <summary>
	/// DPNGROUP_AUTODESTRUCT: the group is deleted once all players have been removed.
	/// </summary>
	AutoDestruct = 0x00000001,
}
