using Aigamo.Otsuki.Actors;

namespace OpenNspw;

// What other threads post to the game (docs/Actors.md): the game's thread owns its state, and takes each kind of message
// where the port takes that input, so that the battle is the one the input would give it there. Not part of the
// original. A host's own kinds derive from it too, such as the frame buffers that the desktop app gives back.
public abstract record GameMessage
{
	// A window message, or a user's action (PostToGame), for the message loops: PeekMessage, GetMessage and WaitMessage.
	public sealed record Window(MSG Message) : GameMessage;

	// A virtual key went down or up, for GetKeyboardState.
	public sealed record KeyState(int VirtualKey, bool IsDown) : GameMessage;

	// The cursor moved, for GetCursorPos.
	public sealed record Cursor(POINT Position) : GameMessage;

	// A key or a mouse button of a DirectInput device (GUID_SysKeyboard or GUID_SysMouse) went down or up, for
	// GetDeviceData.
	public sealed record DeviceInput(Guid Device, uint Offset, uint Data, uint TimeStamp)
		: GameMessage;
}

public partial class Nspw
{
	// The game's mailbox, which any thread posts to, and the game's thread receives from.
	public Mailbox<GameMessage> Mailbox { get; } = new();
}
