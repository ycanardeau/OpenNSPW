using Aigamo.Saruhashi;
using OpenNspw.Singleplayer;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.Desktop.Controls;

// The scenarios of NSPW 1.22, a set at a time as its scenario selection screen shows them, with its heading.
internal sealed class ScenarioBrowserForm : Form
{
	private readonly Label _heading;
	private readonly string[] _headings;
	private readonly TextButton[][] _scenarioButtons;
	private int _page;

	public event EventHandler<int>? ScenarioSelected;

	public event EventHandler? Canceled;

	private void ShowPage(int page)
	{
		_page = Math.Clamp(page, 0, _scenarioButtons.Length - 1);
		_heading.Text = _headings[_page];
		for (var i = 0; i < _scenarioButtons.Length; i++)
		{
			foreach (var button in _scenarioButtons[i])
			{
				button.Visible = i == _page;
			}
		}
	}

	private TextButton CreateScenarioButton(ScenarioTitle scenario, int index)
	{
		var button = new TextButton { Location = new DrawingPoint(120, 150 + index * 25), Text = $"{scenario.Number,3}  {scenario.Title}" };
		button.Click += (_, _) => ScenarioSelected?.Invoke(this, scenario.Number);
		Controls.Add(button);
		return button;
	}

	public ScenarioBrowserForm(IReadOnlyList<ScenarioSet> sets)
	{
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = Palette.SettingScreen;

		_heading = new Label { Bounds = new DrawingRectangle(120, 80, 784, 20), ForeColor = DrawingColor.Yellow, BackColor = DrawingColor.Transparent };
		Controls.Add(_heading);

		var previous = new TextButton { Location = new DrawingPoint(120, 110), Text = "<- Previous" };
		previous.Click += (_, _) => ShowPage(_page - 1);
		Controls.Add(previous);

		var next = new TextButton { Location = new DrawingPoint(250, 110), Text = "Next ->" };
		next.Click += (_, _) => ShowPage(_page + 1);
		Controls.Add(next);

		_headings = [.. sets.Select(set => set.Heading)];
		_scenarioButtons = [.. sets.Select(set => set.Scenarios.Select(CreateScenarioButton).ToArray())];

		var back = new Button { Bounds = new DrawingRectangle(67, 690, 140, 35), Text = "Back" };
		back.Click += (_, _) => Canceled?.Invoke(this, EventArgs.Empty);
		Controls.Add(back);

		ShowPage(0);
	}
}
