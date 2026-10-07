namespace OpenNspw;

// Stand-ins for the DirectSound interfaces that the game uses (dsound.h). The desktop app plays the sounds; tests
// record the calls.

public interface IDirectSoundBuffer
{
	int Play(uint dwReserved1, uint dwPriority, uint dwFlags);

	int Stop();

	int SetVolume(int lVolume);

	int SetCurrentPosition(uint dwNewPosition);

	uint Release();
}

public static class dsound
{
	public const uint DSBPLAY_LOOPING = 0x00000001;
	public const int DSBVOLUME_MAX = 0;
	public const int DSBVOLUME_MIN = -10000;
}
