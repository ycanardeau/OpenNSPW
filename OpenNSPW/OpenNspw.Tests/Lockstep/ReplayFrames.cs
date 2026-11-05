using OpenNspw.Replay;

namespace OpenNspw.Tests.Lockstep;

// What a game that plays a replay shows at each of the replay's frames, as the frame ends: its speed and its camera; and
// a change made to the game at one of them. A replay that plays faster than the game draws shows few of its frames
// (ReplayPlayer.ShowsFrame), so that a test that steps the game by the frames shown (SingleplayerRun.Frames) reads it
// here, by the replay's own.
internal sealed class ReplayFrames(
	ReplayPlayer player,
	int changeAt = -1,
	Action<Nspw>? change = null
) : IGameExtension
{
	private readonly ReplayPlayer _player = player;
	private readonly int _changeAt = changeAt;
	private readonly Action<Nspw>? _change = change;

	public Dictionary<int, (short Speed, WorldPosition Camera)> ByFrame { get; } = [];

	// Steps the game until the replay has played `frames` frames, or has ended.
	public void PlayTo(SingleplayerRun run, int frames = int.MaxValue)
	{
		while (!_player.HasEnded && _player.Frame < frames)
			run.Frames(1);
	}

	public void FrameEnded(Nspw game)
	{
		if (!_player.HoldsInput)
			return;

		ByFrame[_player.Frame] = (game.GameSpeed, game.CameraPosition);
		if (_player.Frame == _changeAt)
			_change?.Invoke(game);
	}
}
