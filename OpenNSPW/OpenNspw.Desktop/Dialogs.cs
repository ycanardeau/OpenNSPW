using System.Drawing;
using Aigamo.Saruhashi;
using Control = Aigamo.Saruhashi.Control;
using Brush = Aigamo.Saruhashi.Brush;
using Graphics = Aigamo.Saruhashi.Graphics;
using Pen = Aigamo.Saruhashi.Pen;
using SolidBrush = Aigamo.Saruhashi.SolidBrush;

namespace OpenNspw.Desktop;

// Controls that Saruhashi does not have: an edit box, a combo box, a list box and a group box. They draw the state of
// the game's dialog control (HWND) and report what the user does.

internal abstract class DialogControl : Control
{
	protected static readonly Brush Window = new SolidBrush(Color.White);
	protected static readonly Brush WindowText = new SolidBrush(Color.Black);
	protected static readonly Brush Highlight = new SolidBrush(Color.FromArgb(0, 120, 215));
	protected static readonly Brush HighlightText = new SolidBrush(Color.White);
	protected static readonly Pen Border = new(Color.FromArgb(122, 122, 122));

	protected void DrawText(Graphics graphics, string text, Brush brush, int x, int y)
	{
		if (Font is { } font)
		{
			graphics.DrawString(text, font, brush, new PointF(x, y));
		}
	}
}

internal sealed class EditBox : DialogControl
{
	public event EventHandler<string>? Edited;

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		WindowManager.SetFocus(this);
	}

	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		base.OnKeyPress(e);
		if (!Enabled)
		{
			return;
		}

		if (e.KeyChar == '\b')
		{
			Text = Text.Length > 0 ? Text[..^1] : Text;
		}
		else if (!char.IsControl(e.KeyChar))
		{
			Text += e.KeyChar;
		}
		else
		{
			return;
		}

		Edited?.Invoke(this, Text);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.FillRectangle(Enabled ? Window : new SolidBrush(Color.FromArgb(240, 240, 240)), ClientRectangle);
		e.Graphics.DrawRectangle(Border, 0, 0, Width - 1, Height - 1);
		DrawText(e.Graphics, Text + (Focused ? "|" : ""), WindowText, 3, 1);
	}
}

// A drop-down list, simplified: each click selects the next item.
internal sealed class ComboBoxControl : DialogControl
{
	public IReadOnlyList<string> Items { get; set; } = [];

	public int SelectedIndex { get; set; } = -1;

	public event EventHandler<int>? Selected;

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (Enabled && Items.Count > 0)
		{
			Selected?.Invoke(this, (SelectedIndex + 1) % Items.Count);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.FillRectangle(Window, ClientRectangle);
		e.Graphics.DrawRectangle(Border, 0, 0, Width - 1, Height - 1);
		var text = SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : "";
		DrawText(e.Graphics, text + "  ▼", WindowText, 3, 1);
	}
}

internal sealed class ListBoxControl : DialogControl
{
	private const int ItemHeight = 18;

	public IReadOnlyList<string> Items { get; set; } = [];

	public int SelectedIndex { get; set; } = -1;

	public event EventHandler<int>? Selected;

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		var index = e.Location.Y / ItemHeight;
		if (Enabled && index < Items.Count)
		{
			Selected?.Invoke(this, index);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.FillRectangle(Window, ClientRectangle);
		e.Graphics.DrawRectangle(Border, 0, 0, Width - 1, Height - 1);
		for (var i = 0; i < Items.Count && (i + 1) * ItemHeight <= Height; i++)
		{
			if (i == SelectedIndex)
			{
				e.Graphics.FillRectangle(Highlight, 1, i * ItemHeight + 1, Width - 2, ItemHeight);
			}

			DrawText(e.Graphics, Items[i], i == SelectedIndex ? HighlightText : WindowText, 3, i * ItemHeight + 1);
		}
	}
}

// A check box; Saruhashi's draws only its button appearance.
internal sealed class CheckBoxControl : DialogControl
{
	private const int BoxSize = 13;

	public bool Checked { get; set; }

	public event EventHandler? Clicked;

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (Enabled)
		{
			Clicked?.Invoke(this, EventArgs.Empty);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var y = Math.Max((Height - BoxSize) / 2, 0);
		e.Graphics.FillRectangle(Window, 0, y, BoxSize, BoxSize);
		e.Graphics.DrawRectangle(Border, 0, y, BoxSize - 1, BoxSize - 1);
		if (Checked)
		{
			e.Graphics.FillRectangle(WindowText, 3, y + 3, BoxSize - 6, BoxSize - 6);
		}

		DrawText(e.Graphics, Text, new SolidBrush(Enabled ? ForeColor : Color.Gray), BoxSize + 4, 0);
	}
}

internal sealed class GroupBoxControl : DialogControl
{
	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.DrawRectangle(Border, 0, 4, Width - 1, Height - 5);
	}
}

// A static text, drawn left or right aligned.
internal sealed class StaticText(bool right) : DialogControl
{
	private readonly bool _right = right;

	protected override void OnPaint(PaintEventArgs e)
	{
		var x = _right && Font is { } font ? Width - (int)e.Graphics.MeasureString(Text, font).Width : 0;
		DrawText(e.Graphics, Text, new SolidBrush(ForeColor), Math.Max(x, 0), 0);
	}
}

// The forms that show the game's dialogs (HWND, from NSPW_NET_RC.cs), kept in step with them. What the user does is
// posted to the game's thread.
internal sealed class DialogForms(Nspw game, WindowManager windowManager)
{
	// The pixels of a dialog unit, for the dialogs' 9-point font.
	private const float UnitX = 1.5f;
	private const float UnitY = 1.625f;
	private const int CaptionHeight = 22;

	private readonly Nspw _game = game;
	private readonly WindowManager _windowManager = windowManager;
	private readonly Dictionary<HWND, (Form Form, Dictionary<HWND, Control> Controls)> _forms = [];

	private static Rectangle ToPixels(int x, int y, int cx, int cy)
	{
		return new Rectangle((int)(x * UnitX), (int)(y * UnitY) + CaptionHeight, (int)(cx * UnitX), (int)(cy * UnitY));
	}

	private Control CreateControl(HWND dialog, HWND item, DLGITEMTEMPLATE template)
	{
		Control control;
		switch (template.Kind)
		{
			case "PUSHBUTTON" or "DEFPUSHBUTTON":
				var button = new Button();
				button.Click += (_, _) => _game.PostToGame(() => _game.ClickDlgItem(dialog, item.Id));
				control = button;
				break;
			case "AUTOCHECKBOX":
				var checkBox = new CheckBoxControl();
				checkBox.Clicked += (_, _) => _game.PostToGame(() => _game.ClickDlgItem(dialog, item.Id));
				control = checkBox;
				break;
			case "EDITTEXT":
				var edit = new EditBox();
				edit.Edited += (_, text) => _game.PostToGame(() => _game.TypeDlgItemText(dialog, item.Id, text));
				control = edit;
				break;
			case "COMBOBOX":
				var combo = new ComboBoxControl();
				combo.Selected += (_, index) => _game.PostToGame(() => _game.SelectDlgItem(dialog, item.Id, index));
				control = combo;
				break;
			case "LISTBOX":
				var list = new ListBoxControl();
				list.Selected += (_, index) => _game.PostToGame(() => _game.SelectDlgItem(dialog, item.Id, index));
				control = list;
				break;
			case "GROUPBOX":
				control = new GroupBoxControl();
				break;
			default:
				control = new StaticText(template.Kind == "RTEXT");
				break;
		}

		// A drop-down list is as tall as its edit field; the template's height includes the list.
		var bounds = ToPixels(template.X, template.Y, template.Cx, template.Kind == "COMBOBOX" ? 12 : template.Cy);
		control.Location = bounds.Location;
		control.Size = bounds.Size;
		return control;
	}

	private (Form, Dictionary<HWND, Control>) CreateForm(HWND dialog)
	{
		var template = dialog.Template!;
		var size = ToPixels(0, 0, template.Cx, template.Cy);
		var form = new Form
		{
			Size = new Size(size.Width, size.Height + CaptionHeight),
			Location = new Point((1024 - size.Width) / 2, (768 - size.Height - CaptionHeight) / 2),
		};
		var caption = new StaticText(false) { Location = new Point(6, 3), Size = new Size(size.Width - 12, CaptionHeight - 4) };
		form.Controls.Add(caption);
		var controls = new Dictionary<HWND, Control> { [dialog] = caption };
		foreach (var (item, itemTemplate) in dialog.Children.Zip(template.Items))
		{
			var control = CreateControl(dialog, item, itemTemplate);
			form.Controls.Add(control);
			controls[item] = control;
		}

		_windowManager.Root.Controls.Add(form);
		form.Visible = true;
		return (form, controls);
	}

	private static void Update(HWND window, Control control)
	{
		control.Enabled = window.Enabled;
		switch (control)
		{
			case EditBox edit when edit.Focused:
				break;
			case CheckBoxControl checkBox:
				checkBox.Text = window.Text;
				checkBox.Checked = window.Checked == winuser.BST_CHECKED;
				break;
			case ComboBoxControl combo:
				combo.Items = [.. window.Items.Select(i => i.Text)];
				combo.SelectedIndex = window.CurSel;
				break;
			case ListBoxControl list:
				list.Items = [.. window.Items.Select(i => i.Text)];
				list.SelectedIndex = window.CurSel;
				break;
			default:
				control.Text = window.Text.Replace("&", "");
				break;
		}
	}

	// Shows a form for each dialog that exists, and removes those of the destroyed ones.
	public void Update()
	{
		lock (_game.WindowsLock)
		{
			var dialogs = _game.windows.Where(w => w.Kind == "DIALOG" && !w.Destroyed && w.Template is not null).ToHashSet();
			foreach (var gone in _forms.Keys.Where(d => !dialogs.Contains(d)).ToList())
			{
				_windowManager.Root.Controls.Remove(_forms[gone].Form);
				_forms.Remove(gone);
			}

			foreach (var dialog in dialogs)
			{
				if (!_forms.TryGetValue(dialog, out var entry))
				{
					entry = CreateForm(dialog);
					_forms[dialog] = entry;
				}

				foreach (var (window, control) in entry.Controls)
				{
					Update(window, control);
				}
			}
		}
	}

	// Whether a point (in game pixels) is on a dialog.
	public bool Contains(Point point)
	{
		return _forms.Values.Any(f => f.Form.Bounds.Contains(point));
	}
}
