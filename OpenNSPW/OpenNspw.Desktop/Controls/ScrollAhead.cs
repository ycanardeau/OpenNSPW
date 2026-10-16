using static OpenNspw.all_head;

namespace OpenNspw.Desktop.Controls;

// The battlefield's moves by the UI (ScrollBattlefield), shown as they are given: the game takes them in its next frame,
// so the snapshot's camera is moved by those it has not taken yet, within the map as the game keeps it (MoveCamera).
internal sealed class ScrollAhead(GameSession session)
{
	// How long a move is shown ahead at most: one the game has dropped, such as at the battle's end, is not taken.
	private const float Expiry = 0.5f;

	private readonly GameSession _session = session;
	private readonly List<(int Id, float At, int Dx, int Dy)> _moves = [];
	private int _lastId;

	public void Scroll(int dx, int dy)
	{
		_lastId++;
		_session.Game.Commands.ScrollBattlefield(dx, dy, _lastId);
		_moves.Add((_lastId, Painter.Now, dx, dy));
	}

	// Shows the battlefield where the game has it, moved by the moves it has not taken yet.
	public void Apply(GameSnapshot snapshot)
	{
		_moves.RemoveAll(m => m.Id <= snapshot.LastCameraMove || Painter.Now - m.At > Expiry);
		var camera = snapshot.GameCameraPosition;
		snapshot.CameraPosition = new WorldPosition(
			Math.Clamp(camera.X + _moves.Sum(m => m.Dx), MAP_LEFT, MAP_RIGHT - CMBT_WIDTH),
			Math.Clamp(camera.Y - _moves.Sum(m => m.Dy), MAP_BOTTOM + CMBT_HEIGHT, MAP_TOP));
	}
}
