using System.Drawing;
using Aigamo.Saruhashi;
using OpenNspw.Desktop;
using Xunit;

namespace OpenNspw.Tests;

// The desktop app's main menu, clicked through as the user does, without drawing it.
public class MainMenuTests
{
	private sealed class NoGraphics : IGraphicsFactory
	{
		public Aigamo.Saruhashi.Graphics Create(Control control) => throw new NotSupportedException();
	}

	private readonly WindowManager _windowManager = new(new Rectangle(0, 0, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT), new NoGraphics());

	private Form Form => _windowManager.Root.Controls.OfType<Form>().Single();

	private Button ButtonOf(string text) => Form.Controls.OfType<Button>().Single(b => b.Text.StartsWith(text, StringComparison.Ordinal));

	private void ClickAt(Point point)
	{
		_windowManager.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, point, 0));
		_windowManager.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, point, 0));
	}

	private void Click(Control control, int x = 5, int y = 5)
	{
		ClickAt(new Point(Form.X + control.X + x, Form.Y + control.Y + y));
	}

	private (MainMenu Menu, List<int?> Chosen) Show(bool canPlaySingleplayer)
	{
		var menu = new MainMenu(_windowManager, canPlaySingleplayer);
		var chosen = new List<int?>();
		menu.Chosen += (_, scenario) => chosen.Add(scenario);
		menu.ShowModes();
		return (menu, chosen);
	}

	[Fact]
	public void ChoosesAScenarioOfACollection()
	{
		var (_, chosen) = Show(canPlaySingleplayer: true);

		Click(ButtonOf("一人で遊ぶ"));
		Assert.False(ButtonOf("開始").Enabled);
		Click(ButtonOf("第三集"));
		// The third item of the list: 202, トーキョーボミング.
		Click(Form.Controls.OfType<ListBoxControl>().Single(), y: 2 * 18 + 5);
		Click(ButtonOf("開始"));

		Assert.Equal([202], chosen);
		Assert.Empty(_windowManager.Root.Controls.OfType<Form>());
	}

	[Fact]
	public void GoesBackToTheModes()
	{
		var (_, chosen) = Show(canPlaySingleplayer: true);

		Click(ButtonOf("一人で遊ぶ"));
		Click(ButtonOf("戻る"));

		Assert.NotNull(ButtonOf("通信対戦"));
		Assert.Empty(chosen);
	}

	[Fact]
	public void ChoosesANetworkGame()
	{
		var (_, chosen) = Show(canPlaySingleplayer: true);

		Click(ButtonOf("通信対戦"));

		Assert.Equal([null], chosen);
	}

	[Fact]
	public void OffersNoSingleplayerWithoutTheExecutable()
	{
		Show(canPlaySingleplayer: false);

		Assert.False(ButtonOf("一人で遊ぶ").Enabled);
	}
}
