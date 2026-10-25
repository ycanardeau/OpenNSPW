using static OpenNspw.Nspw;

namespace OpenNspw;

// The sound: the DirectSound and DirectMusic objects, the buffers of the sound effects, and the WAVE files they are read
// from (Audio.cs and LoadWave.cs), with the globals of win_main.cpp and LoadWave.cpp that they use.
[Globals]
public sealed unsafe partial class Sound(Nspw game)
{
	private readonly Nspw _game = game;

	// DirectSoundの変数
	public IDirectSound8? lpDS = null;
	public IDirectSoundBuffer? lpDSP = null;

	[Original("lpDSB_")]
	public Array35<Array6<IDirectSoundBuffer?>> SoundBuffers;

	[Original("snd_")]
	public Array35<short> NextSoundBuffers;

	// DirectMusicの変数
	public IDirectMusicLoader8? lpDML = null;
	public IDirectMusicPerformance8? lpDMP = null;
}
