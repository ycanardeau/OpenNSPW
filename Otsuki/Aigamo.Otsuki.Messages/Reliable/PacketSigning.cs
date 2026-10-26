// Comments from: https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/ac197b6f-789f-4905-bae5-362ca920e177

namespace Aigamo.Otsuki.Messages.Reliable;

/// <summary>
/// The signing mode of a connection, carried in the <b>dwSigningOpts</b> field of CONNECTED_SIGNED.
/// </summary>
[Flags]
public enum PacketSigning
{
	None = 0,

	/// <summary>
	/// PACKET_SIGNING_FAST: every frame carries the partner's secret as its signature.
	/// </summary>
	Fast = 0x00000001,

	/// <summary>
	/// PACKET_SIGNING_FULL: every frame carries a digest of its contents and the partner's secret.
	/// </summary>
	Full = 0x00000002,
}
