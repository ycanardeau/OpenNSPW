using System.Drawing;
using Aigamo.Saruhashi;
using OpenNspw.Singleplayer;

namespace OpenNspw.Desktop;

// Before the game starts: whether to play a scenario of NSPW 1.22 against the computer (docs/Singleplayer.md), and
// which, or NSPW NET over the network, whose connection dialog follows.
internal sealed class MainMenu(WindowManager windowManager, bool canPlaySingleplayer)
{
	private const int CaptionHeight = 22;
	private const int ButtonHeight = 30;
	private const int Margin = 12;

	private readonly WindowManager _windowManager = windowManager;
	private readonly bool _canPlaySingleplayer = canPlaySingleplayer;
	private Form? _form;

	// The scenario of NSPW 1.22 to play, or null for a network game.
	public event EventHandler<int?>? Chosen;

	public event EventHandler? Quit;

	private static Form CreateForm(string title, int width, int height)
	{
		var form = new Form
		{
			Size = new Size(width, height),
			Location = new Point((all_head.SCRN_WIDTH - width) / 2, (all_head.SCRN_HEIGHT - height) / 2),
		};
		form.Controls.Add(new StaticText(false) { Text = title, Location = new Point(6, 3), Size = new Size(width - 12, CaptionHeight - 4) });
		return form;
	}

	private static Button AddButton(Form form, string text, Rectangle bounds, Action click)
	{
		var button = new Button { Text = text, Bounds = bounds };
		button.Click += (_, _) => click();
		form.Controls.Add(button);
		return button;
	}

	private void Close()
	{
		if (_form is { } form)
		{
			_windowManager.Root.Controls.Remove(form);
			_form = null;
		}
	}

	private void Show(Form form)
	{
		Close();
		_windowManager.Root.Controls.Add(form);
		form.Visible = true;
		_form = form;
	}

	private void Choose(int? scenario)
	{
		Close();
		Chosen?.Invoke(this, scenario);
	}

	// The scenarios of a collection, chosen with its button, and the one to play.
	private void ShowScenarios()
	{
		const int Width = 480;
		const int ListHeight = 10 * 18 + 2;
		var form = CreateForm("シナリオ選択", Width, CaptionHeight + 2 * ButtonHeight + ListHeight + 4 * Margin);
		var top = CaptionHeight + Margin;
		var collections = ScenarioCatalog.Collections;
		var current = collections[0];
		var list = new ListBoxControl
		{
			Location = new Point(Margin, top + ButtonHeight + Margin),
			Size = new Size(Width - 2 * Margin, ListHeight),
		};
		var bottom = list.Y + ListHeight + Margin;
		var start = AddButton(
			form,
			"開始",
			new Rectangle(Width - 2 * (100 + Margin), bottom, 100, ButtonHeight),
			() => Choose(current.Scenarios[list.SelectedIndex].Number)
		);
		AddButton(form, "戻る", new Rectangle(Width - 100 - Margin, bottom, 100, ButtonHeight), ShowModes);

		void Select(ScenarioCatalog.Collection collection)
		{
			current = collection;
			list.Items = [.. collection.Scenarios.Select(s => $"{s.Number,3}  {s.Title}")];
			list.SelectedIndex = -1;
			start.Enabled = false;
		}

		var buttonWidth = (Width - (collections.Count + 1) * Margin) / collections.Count;
		for (var i = 0; i < collections.Count; i++)
		{
			var collection = collections[i];
			AddButton(form, collection.Title.Split('（')[0], new Rectangle(Margin + i * (buttonWidth + Margin), top, buttonWidth, ButtonHeight), () => Select(collection));
		}

		list.Selected += (_, index) =>
		{
			list.SelectedIndex = index;
			start.Enabled = true;
		};
		form.Controls.Add(list);
		Select(collections[0]);
		Show(form);
	}

	public void ShowModes()
	{
		const int Width = 320;
		var form = CreateForm(all_head.CAPTION, Width, CaptionHeight + 4 * (ButtonHeight + Margin) + Margin);
		var bounds = new Rectangle(Margin, CaptionHeight + Margin, Width - 2 * Margin, ButtonHeight);
		var singleplayer = AddButton(form, "一人で遊ぶ（NSPW 1.22 のシナリオ）", bounds, ShowScenarios);
		singleplayer.Enabled = _canPlaySingleplayer;
		bounds.Offset(0, ButtonHeight + Margin);
		AddButton(form, "通信対戦（NSPW NET）", bounds, () => Choose(null));
		bounds.Offset(0, 2 * (ButtonHeight + Margin));
		AddButton(
			form,
			"終了",
			bounds,
			() =>
			{
				Close();
				Quit?.Invoke(this, EventArgs.Empty);
			}
		);
		Show(form);
	}
}
