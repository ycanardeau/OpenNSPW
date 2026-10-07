using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the DirectSound 8 interfaces, structs and constants that the game uses (dsound.h). Buffers keep their
// data in native memory, so that Lock can return pointers to it; the platform plays them.

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct WAVEFORMATEX
{
	public ushort wFormatTag;
	public ushort nChannels;
	public uint nSamplesPerSec;
	public uint nAvgBytesPerSec;
	public ushort nBlockAlign;
	public ushort wBitsPerSample;
	public ushort cbSize;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct DSBUFFERDESC
{
	public uint dwSize;
	public uint dwFlags;
	public uint dwBufferBytes;
	public uint dwReserved;
	public WAVEFORMATEX* lpwfxFormat;
	public Guid guid3DAlgorithm;
}

// A sound that the platform plays: the data of one sound buffer.
public interface ISound
{
	void Play(bool loop);

	void Stop();

	// DirectSound's volume, in hundredths of a decibel: 0 is full volume, -10000 silence.
	void SetVolume(int volume);

	// Restarts the sound from the beginning on the next Play.
	void Rewind();
}

public interface IDirectSoundBuffer : IUnknown
{
	int Play(uint dwReserved1, uint dwPriority, uint dwFlags);

	int Stop();

	int SetVolume(int lVolume);

	int SetCurrentPosition(uint dwNewPosition);

	unsafe int Lock(uint dwOffset, uint dwBytes, void** ppvAudioPtr1, uint* pdwAudioBytes1, void** ppvAudioPtr2, uint* pdwAudioBytes2, uint dwFlags);

	unsafe int Unlock(void* pvAudioPtr1, uint dwAudioBytes1, void* pvAudioPtr2, uint dwAudioBytes2);
}

public unsafe interface IDirectSound8 : IUnknown
{
	int SetCooperativeLevel(HWND? hwnd, uint dwLevel);

	int CreateSoundBuffer(DSBUFFERDESC* pcDSBufferDesc, out IDirectSoundBuffer? ppDSBuffer, object? pUnkOuter);

	int DuplicateSoundBuffer(IDirectSoundBuffer pDSBufferOriginal, out IDirectSoundBuffer? ppDSBufferDuplicate);
}

public static class dsound
{
	public const int DS_OK = 0;
	public const int DSERR_INVALIDPARAM = unchecked((int)0x80070057);

	public const uint DSSCL_NORMAL = 0x00000001;
	public const uint DSSCL_PRIORITY = 0x00000002;

	public const uint DSBCAPS_PRIMARYBUFFER = 0x00000001;
	public const uint DSBCAPS_STATIC = 0x00000002;
	public const uint DSBCAPS_CTRLFREQUENCY = 0x00000020;
	public const uint DSBCAPS_CTRLPAN = 0x00000040;
	public const uint DSBCAPS_CTRLVOLUME = 0x00000080;

	public const uint DSBPLAY_LOOPING = 0x00000001;
	public const int DSBVOLUME_MAX = 0;
	public const int DSBVOLUME_MIN = -10000;

	public const ushort WAVE_FORMAT_PCM = 1;
}

// A buffer's data, shared by the buffer and its duplicates.
internal sealed unsafe class SoundData(WAVEFORMATEX format, uint size)
{
	public WAVEFORMATEX Format { get; } = format;

	public uint Size { get; } = size;

	public byte* Memory { get; } = (byte*)NativeMemory.AllocZeroed(size);
}

public sealed unsafe class DirectSoundBuffer : IDirectSoundBuffer
{
	private readonly INspwPlatform _platform;
	private readonly SoundData? _data;
	private ISound? _sound;

	internal DirectSoundBuffer(INspwPlatform platform, SoundData? data)
	{
		_platform = platform;
		_data = data;
	}

	// The platform's sound, created once the data is written.
	private ISound? Sound => _sound ??= _data is null ? null : _platform.CreateSound(_data.Format, new ReadOnlySpan<byte>(_data.Memory, (int)_data.Size));

	internal DirectSoundBuffer Duplicate()
	{
		return new DirectSoundBuffer(_platform, _data);
	}

	public int Play(uint dwReserved1, uint dwPriority, uint dwFlags)
	{
		Sound?.Play((dwFlags & dsound.DSBPLAY_LOOPING) != 0);
		return dsound.DS_OK;
	}

	public int Stop()
	{
		Sound?.Stop();
		return dsound.DS_OK;
	}

	public int SetVolume(int lVolume)
	{
		Sound?.SetVolume(lVolume);
		return dsound.DS_OK;
	}

	public int SetCurrentPosition(uint dwNewPosition)
	{
		Sound?.Rewind();
		return dsound.DS_OK;
	}

	public int Lock(uint dwOffset, uint dwBytes, void** ppvAudioPtr1, uint* pdwAudioBytes1, void** ppvAudioPtr2, uint* pdwAudioBytes2, uint dwFlags)
	{
		if (_data is null || dwOffset + dwBytes > _data.Size)
		{
			return dsound.DSERR_INVALIDPARAM;
		}

		*ppvAudioPtr1 = _data.Memory + dwOffset;
		*pdwAudioBytes1 = dwBytes;
		if (ppvAudioPtr2 is not null)
		{
			*ppvAudioPtr2 = null;
			*pdwAudioBytes2 = 0;
		}

		return dsound.DS_OK;
	}

	public int Unlock(void* pvAudioPtr1, uint dwAudioBytes1, void* pvAudioPtr2, uint dwAudioBytes2)
	{
		_sound = null;
		return dsound.DS_OK;
	}

	public uint Release()
	{
		_sound?.Stop();
		return 0;
	}
}

public sealed unsafe class DirectSound8(INspwPlatform platform) : IDirectSound8
{
	private readonly INspwPlatform _platform = platform;

	public int SetCooperativeLevel(HWND? hwnd, uint dwLevel)
	{
		return dsound.DS_OK;
	}

	public int CreateSoundBuffer(DSBUFFERDESC* pcDSBufferDesc, out IDirectSoundBuffer? ppDSBuffer, object? pUnkOuter)
	{
		if ((pcDSBufferDesc->dwFlags & dsound.DSBCAPS_PRIMARYBUFFER) != 0)
		{
			ppDSBuffer = new DirectSoundBuffer(_platform, null);
			return dsound.DS_OK;
		}

		if (pcDSBufferDesc->lpwfxFormat is null)
		{
			ppDSBuffer = null;
			return dsound.DSERR_INVALIDPARAM;
		}

		ppDSBuffer = new DirectSoundBuffer(_platform, new SoundData(*pcDSBufferDesc->lpwfxFormat, pcDSBufferDesc->dwBufferBytes));
		return dsound.DS_OK;
	}

	public int DuplicateSoundBuffer(IDirectSoundBuffer pDSBufferOriginal, out IDirectSoundBuffer? ppDSBufferDuplicate)
	{
		ppDSBufferDuplicate = (pDSBufferOriginal as DirectSoundBuffer)?.Duplicate();
		return ppDSBufferDuplicate is null ? dsound.DSERR_INVALIDPARAM : dsound.DS_OK;
	}

	public uint Release()
	{
		return 0;
	}
}

public partial class Nspw
{
	public int DirectSoundCreate8(object? pcGuidDevice, out IDirectSound8? ppDS8, object? pUnkOuter)
	{
		ppDS8 = new DirectSound8(_platform);
		return dsound.DS_OK;
	}
}
