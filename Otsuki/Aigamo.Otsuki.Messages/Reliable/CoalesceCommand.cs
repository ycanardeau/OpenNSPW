// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/0ce3a800-f861-4556-9078-2004c2662ab3

namespace Aigamo.Otsuki.Messages.Reliable;

[Flags]
public enum CoalesceCommand : byte
{
	/// <summary>
	/// this is the final coalesced payload in the frame
	/// </summary>
	EndCoalesce = 0x01,

	/// <summary>
	/// payload is delivered reliably
	/// </summary>
	Reliable = 0x02,

	/// <summary>
	/// payload is indicated sequentially
	/// </summary>
	Sequential = 0x04,

	/// <summary>
	/// bit 9 of the coalesced payload size
	/// </summary>
	CoalesceBig1 = 0x08,

	/// <summary>
	/// bit 10 of the coalesced payload size
	/// </summary>
	CoalesceBig2 = 0x10,

	/// <summary>
	/// bit 11 of the coalesced payload size, the most significant bit
	/// </summary>
	CoalesceBig3 = 0x20,

	/// <summary>
	/// first consumer-controlled flag
	/// </summary>
	User1 = 0x40,

	/// <summary>
	/// second consumer-controlled flag
	/// </summary>
	User2 = 0x80,
}
