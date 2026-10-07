using System.Diagnostics;
using System.Text.Json;
using Microsoft.Xna.Framework.Audio;
using OpenNspw.DirectPlay;

namespace OpenNspw.Desktop;

// A sound of the game, as a MonoGame sound effect. DirectSound's volume is in hundredths of a decibel.
internal sealed class Sound(SoundEffect effect) : ISound
{
	private readonly SoundEffect _effect = effect;
	private readonly SoundEffectInstance _instance = effect.CreateInstance();

	public void Play(bool loop)
	{
		_instance.IsLooped = loop;
		if (_instance.State != SoundState.Playing)
		{
			_instance.Play();
		}
	}

	public void Stop()
	{
		_instance.Stop();
	}

	public void SetVolume(int volume)
	{
		_instance.Volume = (float)Math.Clamp(Math.Pow(10, volume / 2000.0), 0, 1);
	}

	public void Rewind()
	{
		_instance.Stop();
	}
}

// The platform of the desktop app: the game's files in its data directory, DirectPlay 8 on Otsuki, MonoGame sound,
// text rasterized with a TrueType font, and settings in a JSON file.
internal sealed class DesktopPlatform(string dataDirectory) : INspwPlatform
{
	private readonly string _dataDirectory = dataDirectory;
	private readonly DirectPlay8Factory _directPlay = new();
	private readonly TextRasterizer _text = new();
	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private readonly Lock _frameLock = new();
	private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenNSPW", "settings.json");
	private Dictionary<string, string>? _settings;
	private ushort[] _frame = [];
	private int _frameWidth;
	private int _frameHeight;

	// Incremented for each frame the game shows.
	public int FrameNumber { get; private set; }

	public byte[]? FontData => _text.FontData;

	public List<string> Messages { get; } = [];

	// Copies the last frame, if it changed since `known`. Returns its number.
	public int CopyFrame(int known, ref ushort[] pixels, out int width, out int height)
	{
		lock (_frameLock)
		{
			width = _frameWidth;
			height = _frameHeight;
			if (known != FrameNumber)
			{
				if (pixels.Length != _frame.Length)
				{
					pixels = new ushort[_frame.Length];
				}

				_frame.CopyTo(pixels, 0);
			}

			return FrameNumber;
		}
	}

	public Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
	{
		try
		{
			var full = GameFiles.Resolve(_dataDirectory, path);
			if (mode is FileMode.Create or FileMode.CreateNew or FileMode.OpenOrCreate)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(full)!);
			}

			return new FileStream(full, mode, access, share);
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
	}

	public IReadOnlyList<string> FindFiles(string directory, string pattern)
	{
		return GameFiles.Find(_dataDirectory, directory, pattern);
	}

	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv)
	{
		return _directPlay.CoCreateInstance(rclsid, riid, out ppv);
	}

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		lock (Messages)
		{
			Messages.Add($"{lpCaption}: {lpText}");
		}

		return windef.IDOK;
	}

	public uint timeGetTime()
	{
		return (uint)_clock.ElapsedMilliseconds;
	}

	public int time()
	{
		return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	}

	public void Present(DirectDrawSurface7 primary)
	{
		lock (_frameLock)
		{
			if (_frame.Length != primary.Pixels.Length)
			{
				_frame = new ushort[primary.Pixels.Length];
			}

			primary.Pixels.CopyTo(_frame);
			_frameWidth = primary.Width;
			_frameHeight = primary.Height;
			FrameNumber++;
		}
	}

	public TextBitmap? RasterizeText(string text, int height, string faceName)
	{
		return _text.Rasterize(text, height);
	}

	// MonoGame plays 16-bit PCM, so 8-bit sounds are converted.
	public ISound? CreateSound(WAVEFORMATEX format, ReadOnlySpan<byte> data)
	{
		if (format.wFormatTag != dsound.WAVE_FORMAT_PCM || data.Length == 0)
		{
			return null;
		}

		byte[] pcm;
		if (format.wBitsPerSample == 8)
		{
			pcm = new byte[data.Length * 2];
			for (var i = 0; i < data.Length; i++)
			{
				var sample = (short)((data[i] - 128) << 8);
				pcm[i * 2] = (byte)sample;
				pcm[i * 2 + 1] = (byte)(sample >> 8);
			}
		}
		else
		{
			pcm = data[..(data.Length & ~1)].ToArray();
		}

		try
		{
			return new Sound(new SoundEffect(pcm, (int)format.nSamplesPerSec, format.nChannels == 2 ? AudioChannels.Stereo : AudioChannels.Mono));
		}
		catch (Exception)
		{
			return null;
		}
	}

	private Dictionary<string, string> Settings()
	{
		if (_settings is null)
		{
			try
			{
				_settings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_settingsPath));
			}
			catch (Exception)
			{
			}

			_settings ??= [];
		}

		return _settings;
	}

	public string? ReadSetting(string key)
	{
		lock (_frameLock)
		{
			return Settings().GetValueOrDefault(key);
		}
	}

	public void WriteSetting(string key, string value)
	{
		lock (_frameLock)
		{
			Settings()[key] = value;
			Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
			File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Settings()));
		}
	}
}
