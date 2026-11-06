using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aigamo.Otsuki.Actors;
using Microsoft.Xna.Framework.Audio;
using OpenNspw.DirectPlay;

namespace OpenNspw.App;

// A sound of the game, as a MonoGame sound effect. DirectSound's volume is in hundredths of a decibel.
internal sealed class Sound(SoundEffect effect) : ISound, IDisposable
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

	public void Dispose()
	{
		_instance.Dispose();
		_effect.Dispose();
	}
}

// A frame that the game showed: its pixels, and the game's state that goes with them (GameSnapshot). The game's while it
// fills it, the UI's once posted, until the UI gives it back (docs/Actors.md).
internal sealed class FrameBuffer
{
	public ushort[] Pixels { get; set; } = [];

	public int Width { get; set; }

	public int Height { get; set; }

	public GameSnapshot Snapshot { get; } = new();
}

// What the game posts to the UI's thread (GameSession), which takes it at the start of each update.
internal abstract record UIMessage
{
	private UIMessage() { }

	// A frame to show, the UI's until it gives it back (BufferFree).
	public sealed record FrameReady(FrameBuffer Buffer) : UIMessage;

	// The windows, as they are since one has changed.
	public sealed record WindowsChanged(ImmutableArray<WindowView> Windows) : UIMessage;

	// A message box the game showed, or what the game threw.
	public sealed record MessageShown(string Text) : UIMessage;

	// A user's action on a dialog has run (DialogFilled), after which the form reads the dialog's fields again.
	public sealed record DialogFilled : UIMessage;

	// WinMain has returned.
	public sealed record Ended : UIMessage;
}

// A frame buffer that the UI gives back to the game, for another frame.
internal sealed record BufferFree(FrameBuffer Buffer) : GameMessage;

// The platform of the app: the game's files in their layers (GameData), its sprites from the app's, DirectPlay 8
// on Otsuki, MonoGame sound, text rasterized with a TrueType font, and settings in a JSON file. It runs on the game's
// thread, and posts what the UI shows to the UI's mailbox (ui).
internal sealed partial class AppPlatform(
	GameFileLayers files,
	TextRasterizer text,
	Mailbox<UIMessage> ui,
	Mailbox<GameMessage> game
) : INspwPlatform
{
	// How the settings are read and written, generated, so that trimming keeps what it needs.
	[JsonSerializable(typeof(Dictionary<string, string>))]
	private sealed partial class SettingsJson : JsonSerializerContext;

	// How many texts RasterizeText keeps before it starts again.
	private const int MaxTexts = 1024;

	private readonly GameFileLayers _files = files;
	private readonly DirectPlay8Factory _directPlay = new();
	private readonly TextRasterizer _text = text;
	private readonly Mailbox<UIMessage> _ui = ui;
	private readonly Mailbox<GameMessage> _game = game;

	// The texts rasterized, by text and height: the game writes most of its texts again every frame. On the game's thread;
	// emptied once it has many, as the texts with numbers in them keep changing.
	private readonly Dictionary<(string Text, int Height), TextBitmap?> _texts = [];
	private readonly List<Sound> _sounds = [];
	private readonly Stopwatch _clock = Stopwatch.StartNew();

	// The frame buffers the game holds, to fill, and the one it filled but keeps, while it holds no other to fill next.
	private readonly Stack<FrameBuffer> _free = new([new(), new(), new()]);
	private FrameBuffer? _kept;
	private DirectDrawSurface7? _primary;
	private readonly string _settingsPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"OpenNSPW",
		"settings.json"
	);
	private Dictionary<string, string>? _settings;

	// Copies what goes with a frame, such as the game's state for the UI, into its snapshot: called on the game's thread as
	// it shows each frame.
	public Action<GameSnapshot>? CaptureState { get; set; }

	public Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
	{
		return _files.Open(path, mode, access, share);
	}

	public IReadOnlyList<string> FindFiles(string directory, string pattern)
	{
		return _files.Find(directory, pattern);
	}

	public SpriteBitmap? LoadSprite(string path)
	{
		return SpriteFiles.LoadBitmap(path);
	}

	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv)
	{
		return _directPlay.CoCreateInstance(rclsid, riid, out ppv);
	}

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		_ui.Post(new UIMessage.MessageShown($"{lpCaption}: {lpText}"));
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

	// Takes the buffers the UI gave back, and posts the frame kept, now that the game holds another to fill.
	private void TakeFreed()
	{
		while (_game.TryReceive<BufferFree>(out var free))
		{
			_free.Push(free.Buffer);
		}

		if (_kept is not null && _free.Count > 0)
		{
			_ui.Post(new UIMessage.FrameReady(_kept));
			_kept = null;
		}
	}

	// Fills a buffer with the primary surface's pixels and the game's state, and posts it to the UI if the game holds
	// another to fill next; or keeps it, to post once the UI gives one back. The game never waits for the UI.
	private void Show(DirectDrawSurface7 primary)
	{
		TakeFreed();
		var buffer = _kept ?? _free.Pop();
		_kept = null;
		if (buffer.Pixels.Length != primary.Pixels.Length)
		{
			buffer.Pixels = new ushort[primary.Pixels.Length];
		}

		primary.Pixels.CopyTo(buffer.Pixels);
		buffer.Width = primary.Width;
		buffer.Height = primary.Height;
		CaptureState?.Invoke(buffer.Snapshot);
		if (_free.Count > 0)
		{
			_ui.Post(new UIMessage.FrameReady(buffer));
		}
		else
		{
			_kept = buffer;
		}
	}

	public void Present(DirectDrawSurface7 primary)
	{
		_primary = primary;
		Show(primary);
	}

	// Shows the frame shown last again, with the game's state copied again (CaptureState), such as when the view of a
	// replay moves while it is paused: on the game's thread. The primary surface still holds the last frame.
	public void CaptureAgain()
	{
		if (_primary is { } primary)
		{
			Show(primary);
		}
	}

	public void ShowWindows(ImmutableArray<WindowView> windows)
	{
		_ui.Post(new UIMessage.WindowsChanged(windows));
	}

	// Sleeps until a window message comes; a buffer the UI gives back meanwhile wakes it to post the frame it kept, so
	// that the UI does not show an old frame while a dialog is up.
	public void WaitForMessage(Mailbox<GameMessage> mailbox)
	{
		do
		{
			mailbox.WaitFor(static message => message is GameMessage.Window or BufferFree);
			TakeFreed();
		} while (!mailbox.TryPeek<GameMessage.Window>(out _));
	}

	// Whether nothing of the game's frame is shown: on the screens that the UI draws in the game's place, and while the game
	// goes ahead as fast as it can, such as a replay to a point. Its text is not rasterized then, which only draws. Asked
	// on the game's thread.
	public Func<bool>? HidesFrame { get; set; }

	public TextBitmap? RasterizeText(string text, int height, string faceName)
	{
		if (HidesFrame?.Invoke() == true)
		{
			return null;
		}

		if (!_texts.TryGetValue((text, height), out var bitmap))
		{
			if (_texts.Count >= MaxTexts)
			{
				_texts.Clear();
			}

			bitmap = _text.Rasterize(text, height);
			_texts[(text, height)] = bitmap;
		}

		return bitmap;
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

		Sound sound;
		try
		{
			sound = new Sound(
				new SoundEffect(
					pcm,
					(int)format.nSamplesPerSec,
					format.nChannels == 2 ? AudioChannels.Stereo : AudioChannels.Mono
				)
			);
		}
		catch (Exception)
		{
			return null;
		}

		_sounds.Add(sound);
		return sound;
	}

	// Stops and frees the game's sounds, once it has ended, on its thread.
	public void StopSounds()
	{
		foreach (var sound in _sounds)
		{
			sound.Dispose();
		}

		_sounds.Clear();
	}

	private Dictionary<string, string> Settings()
	{
		if (_settings is null)
		{
			try
			{
				_settings = JsonSerializer.Deserialize(
					File.ReadAllText(_settingsPath),
					SettingsJson.Default.DictionaryStringString
				);
			}
			catch (Exception) { }

			_settings ??= [];
		}

		return _settings;
	}

	public string? ReadSetting(string key)
	{
		return Settings().GetValueOrDefault(key);
	}

	public void WriteSetting(string key, string value)
	{
		Settings()[key] = value;
		Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
		File.WriteAllText(
			_settingsPath,
			JsonSerializer.Serialize(Settings(), SettingsJson.Default.DictionaryStringString)
		);
	}
}
