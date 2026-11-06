using Microsoft.Xna.Framework;
using OpenNspw.App.Controls;
using OpenNspw.Replay;

namespace OpenNspw.App.Screens;

// The replays of the battles played (docs/Replays.md): the list, from which one is watched (GameScreen) or deleted.
// selected: the replay to select first, by its place in the list.
internal sealed class ReplaysScreen(NspwGame game, int selected = 0) : Screen(game)
{
	private readonly int _selected = selected;
	private Backdrop _backdrop = default!;
	private ReplaysForm _form = default!;

	// The replays in the user's folder, newest first, each with what it holds, or none if it cannot be read.
	private static List<ReplayRow> ReadRows(UIFonts fonts)
	{
		if (!Directory.Exists(GameData.ReplayDirectory))
			return [];

		var rows = new List<ReplayRow>();
		foreach (
			var path in Directory
				.GetFiles(GameData.ReplayDirectory, "*" + ReplayFormat.Extension)
				.OrderByDescending(File.GetLastWriteTimeUtc)
		)
		{
			ReplaySummary? summary;
			try
			{
				using var stream = File.OpenRead(path);
				summary = ReplaySummary.Read(stream);
			}
			catch (Exception e)
				when (e is IOException or UnauthorizedAccessException or InvalidDataException)
			{
				summary = null;
			}

			rows.Add(new ReplayRow(fonts, path, summary));
		}

		return rows;
	}

	// Asks before deleting a replay, then shows the list again on the one after it.
	private void AskDelete(string path)
	{
		var index = _form.Controls.OfType<ReplayRow>().ToList().FindIndex(row => row.Path == path);
		var confirm = new ConfirmForm(
			Game.Fonts,
			Texts.DeleteReplayTitle,
			Texts.DeleteReplayMessage(Path.GetFileName(path)),
			Texts.Delete
		);
		confirm.Confirmed += (_, _) =>
		{
			try
			{
				File.Delete(path);
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				Console.Error.WriteLine($"The replay cannot be deleted: {e.Message}");
			}

			Game.ShowScreen(new ReplaysScreen(Game, index));
		};
		confirm.Canceled += (_, _) =>
		{
			WindowManager.Root.Controls.Remove(confirm);
			_form.Focus();
		};
		WindowManager.Root.Controls.Add(confirm);
		confirm.BringToFront();
		confirm.Show();
		confirm.Focus();
	}

	public override void LoadContent()
	{
		base.LoadContent();
		_backdrop = new Backdrop(GraphicsDevice);

		_form = new ReplaysForm(Game.Fonts, ReadRows(Game.Fonts), _selected);
		_form.WatchChosen += (_, path) =>
		{
			if (ReplayWatch.Open(path) is { } watch)
				Game.Watch(watch);
		};
		_form.DeleteChosen += (_, path) => AskDelete(path);
		_form.OpenFolderChosen += (_, _) => Game.Host.OpenFolder(GameData.ReplayDirectory);
		_form.Canceled += (_, _) => Game.ShowScreen(new MainMenuScreen(Game));
		WindowManager.Root.Controls.Add(_form);
		_form.Show();
		_form.Focus();
	}

	public override void UnloadContent()
	{
		base.UnloadContent();
		_backdrop.Dispose();
	}

	public override void Draw(GameTime gameTime)
	{
		_backdrop.Draw(Game.SpriteBatch, Game.ViewportAdapter);
	}
}
