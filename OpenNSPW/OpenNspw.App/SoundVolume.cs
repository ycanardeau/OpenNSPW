using Microsoft.Xna.Framework.Audio;

namespace OpenNspw.App;

// How loud the game's sounds are, from 0, silent, to 1, as loud as the game plays them: as the player chose last, heard at
// once, also over a battle, through the master volume, which a replay mutes while it goes ahead to a point (GameScreen);
// saved as it changes (UISettings).
internal sealed class SoundVolume(float value)
{
	public float Value { get; private set; } = value;

	public void Change(float value)
	{
		Value = value;
		SoundEffect.MasterVolume = value;
		UISettings.SaveVolume(value);
	}
}
