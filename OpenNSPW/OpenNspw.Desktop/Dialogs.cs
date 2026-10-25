using System.Drawing;
using Aigamo.Saruhashi;
using OpenNspw.Desktop.Controls;
using Brush = Aigamo.Saruhashi.Brush;
using Control = Aigamo.Saruhashi.Control;
using Graphics = Aigamo.Saruhashi.Graphics;
using Pen = Aigamo.Saruhashi.Pen;
using SolidBrush = Aigamo.Saruhashi.SolidBrush;

namespace OpenNspw.Desktop;

// Controls that Saruhashi does not have: an edit box, a combo box, a list box and a group box. They draw the state of
// the game's dialog control (HWND) and report what the user does.

internal abstract class DialogControl : Control
{
	protected static readonly Brush Window = new SolidBrush(Theme.Alpha(Theme.Ink, 0.75f));
	protected static readonly Brush WindowText = new SolidBrush(Theme.Text);
	protected static readonly Brush Highlight = new SolidBrush(Theme.Brass);
	protected static readonly Brush HighlightText = new SolidBrush(Theme.Ink);
	protected static readonly Pen Border = new(Theme.Line);

	// Transparent: each control draws what it fills, over the dialog's panel.
	protected DialogControl()
	{
		BackColor = Color.Transparent;
	}

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
		e.Graphics.FillRectangle(
			Enabled ? Window : new SolidBrush(Theme.Alpha(Theme.Line, 0.12f)),
			ClientRectangle
		);
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

			DrawText(
				e.Graphics,
				Items[i],
				i == SelectedIndex ? HighlightText : WindowText,
				3,
				i * ItemHeight + 1
			);
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
			e.Graphics.FillRectangle(Highlight, 3, y + 3, BoxSize - 6, BoxSize - 6);
		}

		DrawText(
			e.Graphics,
			Text,
			new SolidBrush(Enabled ? ForeColor : Color.Gray),
			BoxSize + 4,
			0
		);
	}
}

// Transparent, as every dialog control is: it comes before the controls it groups, so it is drawn over them.
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
		var x =
			_right && Font is { } font
				? Width - (int)e.Graphics.MeasureString(Text, font).Width
				: 0;
		DrawText(e.Graphics, Text, new SolidBrush(ForeColor), Math.Max(x, 0), 0);
	}
}

// The forms that show the game's dialogs (HWND, from NSPW_NET_RC.cs), kept in step with them. What the user does is
// posted to the game's thread.
internal sealed class DialogForms(Nspw game, WindowManager windowManager, UIFonts fonts)
{
	// The pixels of a dialog unit, for the dialogs' 9-point font.
	private const float UnitX = 1.5f;
	private const float UnitY = 1.625f;
	private const int CaptionHeight = 22;

	private readonly Nspw _game = game;
	private readonly WindowManager _windowManager = windowManager;
	private readonly UIFonts _fonts = fonts;
	private readonly Dictionary<HWND, (Form Form, Dictionary<HWND, Control> Controls)> _forms = [];

	private static Rectangle ToPixels(int x, int y, int cx, int cy)
	{
		return new Rectangle(
			(int)(x * UnitX),
			(int)(y * UnitY) + CaptionHeight,
			(int)(cx * UnitX),
			(int)(cy * UnitY)
		);
	}

	private Control CreateControl(HWND dialog, HWND item, DLGITEMTEMPLATE template)
	{
		Control control;
		switch (template.Kind)
		{
			case "PUSHBUTTON" or "DEFPUSHBUTTON":
				var button = new ActionButton(_fonts)
				{
					IsPrimary = template.Kind == "DEFPUSHBUTTON",
					FontSize = 13,
				};
				button.Click += (_, _) =>
					_game.PostToGame(() => _game.ClickDlgItem(dialog, item.Id));
				control = button;
				break;
			case "AUTOCHECKBOX":
				var checkBox = new CheckBoxControl();
				checkBox.Clicked += (_, _) =>
					_game.PostToGame(() => _game.ClickDlgItem(dialog, item.Id));
				control = checkBox;
				break;
			case "EDITTEXT":
				var edit = new EditBox();
				edit.Edited += (_, text) =>
					_game.PostToGame(() => _game.TypeDlgItemText(dialog, item.Id, text));
				control = edit;
				break;
			case "COMBOBOX":
				var combo = new ComboBoxControl();
				combo.Selected += (_, index) =>
					_game.PostToGame(() => _game.SelectDlgItem(dialog, item.Id, index));
				control = combo;
				break;
			case "LISTBOX":
				var list = new ListBoxControl();
				list.Selected += (_, index) =>
					_game.PostToGame(() => _game.SelectDlgItem(dialog, item.Id, index));
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
		var bounds = ToPixels(
			template.X,
			template.Y,
			template.Cx,
			template.Kind == "COMBOBOX" ? 12 : template.Cy
		);
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
			BackColor = Theme.Panel,
		};
		form.Paint += (_, e) =>
		{
			Painter.Frame(e.Graphics, form.ClientRectangle, Theme.Line);
			Painter.Brackets(e.Graphics, form.ClientRectangle, 10, Theme.Brass);
		};
		var caption = new StaticText(false)
		{
			Location = new Point(8, 4),
			Size = new Size(size.Width - 16, CaptionHeight - 4),
			ForeColor = Theme.Brass,
		};
		form.Controls.Add(caption);
		var controls = new Dictionary<HWND, Control> { [dialog] = caption };
		foreach (var (item, itemTemplate) in dialog.Children.Zip(template.Items))
		{
			var control = CreateControl(dialog, item, itemTemplate);

			// A left-aligned text may run to the dialog's edge: the font is wider than the original's.
			if (itemTemplate.Kind == "LTEXT")
			{
				control.Width = Math.Max(control.Width, size.Width - control.X - 8);
			}

			form.Controls.Add(control);
			controls[item] = control;
		}

		// Over the screen's forms, which were added before: the first of the root's controls is the front.
		_windowManager.Root.Controls.Add(form);
		form.BringToFront();
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
			var dialogs = _game
				.windows.Where(w => w.Kind == "DIALOG" && !w.Destroyed && w.Template is not null)
				.ToHashSet();
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
