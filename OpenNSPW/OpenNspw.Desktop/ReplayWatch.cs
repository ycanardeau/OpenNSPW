using OpenNspw.Replay;

namespace OpenNspw.Desktop;

// A replay being watched (docs/Replays.md): its file, what it holds (ReplaySummary), and what the viewer has chosen, which
// carries over when the replay starts again to go back in it.
internal sealed class ReplayWatch(string path, ReplaySummary summary)
{
	// The speeds the viewer chooses from, in turn.
	public static readonly double[] Speeds = [0.5, 1, 2, 4, 8];

	public string Path { get; } = path;

	public ReplaySummary Summary { get; } = summary;

	public ReplayView View { get; } = new();

	public double Speed { get; set; } = 1;

	public bool IsPaused { get; set; }

	// The replay at `path`, or null if it cannot be read.
	public static ReplayWatch? Open(string path)
	{
		try
		{
			using var stream = File.OpenRead(path);
			return new ReplayWatch(path, ReplaySummary.Read(stream));
		}
		catch (Exception e)
			when (e is IOException or UnauthorizedAccessException or InvalidDataException)
		{
			Console.Error.WriteLine($"The replay cannot be read: {e.Message}");
			return null;
		}
	}

	// The speed after this one, back to the first after the last.
	public double NextSpeed()
	{
		var index = Array.IndexOf(Speeds, Speed);
		return Speeds[(index + 1) % Speeds.Length];
	}
}
