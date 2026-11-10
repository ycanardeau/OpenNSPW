using System.Drawing;
using Xunit;

namespace Aigamo.Saruhashi.Tests;

public sealed class TextBoxTests
{
	// The text box's left on the screen, and its text's: the default margin of 2 pixels (TextBox.TextRectangle).
	private const int Left = 10;
	private const int TextLeft = Left + 2;
	private const int Top = 10;

	private readonly FakeGraphicsFactory _factory = new();
	private readonly FakeClipboard _clipboard = new();
	private readonly WindowManager _windowManager;
	private readonly TextBox _textBox = new() { Bounds = new Rectangle(Left, Top, 100, 23) };
	private int _edits;

	public TextBoxTests()
	{
		_windowManager = new WindowManager(new Rectangle(0, 0, 640, 480), _factory, new FakeFont())
		{
			Clipboard = _clipboard,
		};
		_windowManager.Root.Controls.Add(_textBox);
		_textBox.Edited += (_, _) => _edits++;
	}

	// Where the caret goes at the position `index` of the text, on the screen, unscrolled.
	private static Point At(int index) => new(TextLeft + index * FakeGraphics.CharWidth, Top + 5);

	// Presses and releases a key, with its modifiers held while it is pressed.
	private void Press(Keys keys)
	{
		_windowManager.OnKeyDown(new KeyEventArgs(keys));
		_windowManager.OnKeyUp(new KeyEventArgs(keys & Keys.KeyCode));
	}

	private void Type(string text)
	{
		foreach (var c in text)
			_windowManager.OnKeyPress(new KeyPressEventArgs(c));
	}

	private void MouseDown(Point point, int clicks = 1) =>
		_windowManager.OnMouseDown(new MouseEventArgs(MouseButtons.Left, clicks, point, 0));

	private void MouseMove(Point point) =>
		_windowManager.OnMouseMove(new MouseEventArgs(MouseButtons.Left, 0, point, 0));

	private void MouseUp(Point point) =>
		_windowManager.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, point, 0));

	private void Click(int index)
	{
		MouseDown(At(index));
		MouseUp(At(index));
	}

	// Focuses the text box with its text, and the caret at its end.
	private void Start(string text)
	{
		_textBox.Text = text;
		_textBox.Focus();
		Press(Keys.End);
	}

	[Fact]
	public void Typing_InsertsAtTheCaret()
	{
		Start("helo");

		Press(Keys.Left);
		Type("l");

		Assert.Equal("hello", _textBox.Text);
		Assert.Equal(4, _textBox.SelectionStart);
		Assert.Equal(0, _textBox.SelectionLength);
		Assert.Equal(1, _edits);
	}

	[Fact]
	public void Focus_ByTheKeyboard_SelectsAll_AndTypingReplacesIt()
	{
		_textBox.Text = "hello";
		_textBox.Focus();

		Assert.Equal("hello", _textBox.SelectedText);

		Type("x");

		Assert.Equal("x", _textBox.Text);
	}

	[Fact]
	public void ShiftArrows_Select_AndAnArrowCollapsesTheSelection()
	{
		Start("hello");

		Press(Keys.Shift | Keys.Left);
		Press(Keys.Shift | Keys.Left);

		Assert.Equal("lo", _textBox.SelectedText);
		Assert.Equal(3, _textBox.SelectionStart);

		Press(Keys.Right);

		Assert.Equal(5, _textBox.SelectionStart);
		Assert.Equal(0, _textBox.SelectionLength);
	}

	[Fact]
	public void CtrlArrows_GoByWords()
	{
		Start("one two  three");
		Press(Keys.Home);

		Press(Keys.Control | Keys.Right);
		Assert.Equal(4, _textBox.SelectionStart);

		Press(Keys.Control | Keys.Right);
		Assert.Equal(9, _textBox.SelectionStart);

		Press(Keys.Control | Keys.Shift | Keys.Left);
		Assert.Equal("two  ", _textBox.SelectedText);
	}

	[Fact]
	public void ShiftHomeAndEnd_SelectToTheEnds()
	{
		Start("hello");
		Press(Keys.Left);
		Press(Keys.Left);

		Press(Keys.Shift | Keys.Home);
		Assert.Equal("hel", _textBox.SelectedText);

		Press(Keys.Shift | Keys.End);
		Assert.Equal("lo", _textBox.SelectedText);
	}

	[Fact]
	public void CtrlA_CtrlC_CtrlV_CopyAndPaste()
	{
		Start("hello");

		Press(Keys.Control | Keys.A);
		Press(Keys.Control | Keys.C);
		Press(Keys.End);
		Press(Keys.Control | Keys.V);

		Assert.Equal("hello", _clipboard.Text);
		Assert.Equal("hellohello", _textBox.Text);
		Assert.Equal(10, _textBox.SelectionStart);
		Assert.Equal(1, _edits);
	}

	[Fact]
	public void CtrlX_Cuts()
	{
		Start("hello");
		_textBox.Select(1, 3);

		Press(Keys.Control | Keys.X);

		Assert.Equal("ell", _clipboard.Text);
		Assert.Equal("ho", _textBox.Text);
		Assert.Equal(1, _textBox.SelectionStart);
		Assert.Equal(1, _edits);
	}

	[Fact]
	public void InsertAndDelete_CopyCutAndPaste_AsWindowsDoes()
	{
		Start("hello");
		_textBox.Select(0, 2);

		Press(Keys.Control | Keys.Insert);
		Assert.Equal("he", _clipboard.Text);

		Press(Keys.Shift | Keys.Delete);
		Assert.Equal("llo", _textBox.Text);

		Press(Keys.End);
		Press(Keys.Shift | Keys.Insert);
		Assert.Equal("llohe", _textBox.Text);
	}

	[Fact]
	public void Copy_WithNothingSelected_LeavesTheClipboard()
	{
		_clipboard.Text = "kept";
		Start("hello");

		Press(Keys.Control | Keys.C);
		Press(Keys.Control | Keys.X);

		Assert.Equal("kept", _clipboard.Text);
		Assert.Equal("hello", _textBox.Text);
		Assert.Equal(0, _edits);
	}

	[Fact]
	public void Paste_TakesTheFirstLine_AsMuchAsMaxLengthLeavesRoomFor()
	{
		_clipboard.Text = "abc\r\ndef";
		_textBox.MaxLength = 4;
		Start("xy");

		Press(Keys.Control | Keys.V);
		Assert.Equal("xyab", _textBox.Text);
		Assert.Equal(1, _edits);

		Press(Keys.Control | Keys.V);
		Type("z");
		Assert.Equal("xyab", _textBox.Text);
		Assert.Equal(1, _edits);
	}

	[Fact]
	public void Paste_DoesNotSplitASurrogatePair_AtMaxLength()
	{
		_clipboard.Text = "a😀";
		_textBox.MaxLength = 2;
		Start("");

		Press(Keys.Control | Keys.V);

		Assert.Equal("a", _textBox.Text);
	}

	[Fact]
	public void ReadOnly_Copies_ButDoesNotChange()
	{
		_clipboard.Text = "pasted";
		_textBox.ReadOnly = true;
		Start("hello");
		Press(Keys.Control | Keys.A);

		Press(Keys.Control | Keys.C);
		Press(Keys.Control | Keys.X);
		Press(Keys.Control | Keys.V);
		Press(Keys.Delete);
		Type("x\b");

		Assert.Equal("hello", _clipboard.Text);
		Assert.Equal("hello", _textBox.Text);
		Assert.Equal("hello", _textBox.SelectedText);
		Assert.Equal(0, _edits);
	}

	[Fact]
	public void BackspaceAndDelete_DeleteCharactersAndWords()
	{
		Start("hello big world");

		Type("\b");
		Assert.Equal("hello big worl", _textBox.Text);

		// Ctrl is held as Backspace types its character.
		_windowManager.OnKeyDown(new KeyEventArgs(Keys.ControlKey | Keys.Control));
		Type("\b");
		_windowManager.OnKeyUp(new KeyEventArgs(Keys.ControlKey));
		Assert.Equal("hello big ", _textBox.Text);

		Press(Keys.Home);
		Press(Keys.Delete);
		Assert.Equal("ello big ", _textBox.Text);

		Press(Keys.Control | Keys.Delete);
		Assert.Equal("big ", _textBox.Text);
		Assert.Equal(4, _edits);
	}

	[Fact]
	public void BackspaceAndDelete_DeleteTheSelection()
	{
		Start("hello");
		_textBox.Select(1, 3);

		Type("\b");

		Assert.Equal("ho", _textBox.Text);
		Assert.Equal(1, _textBox.SelectionStart);
	}

	[Fact]
	public void ControlCharacters_OtherThanBackspace_AreNotTyped()
	{
		Start("hello");

		// MonoGame's DesktopGL types these for Tab, Enter, Escape and Delete.
		Type("\t\r\u001b\u007f");

		Assert.Equal("hello", _textBox.Text);
		Assert.Equal(0, _edits);
	}

	[Fact]
	public void SettingText_PutsTheCaretAtTheStart_WithoutEdited()
	{
		var changes = 0;
		_textBox.TextChanged += (_, _) => changes++;
		Start("hello");

		_textBox.Text = "bye";

		Assert.Equal(0, _textBox.SelectionStart);
		Assert.Equal(0, _textBox.SelectionLength);
		Assert.Equal(2, changes);
		Assert.Equal(0, _edits);
	}

	[Fact]
	public void SettingSelectedText_ReplacesTheSelection_WithoutEdited()
	{
		Start("hello");
		_textBox.Select(1, 3);

		_textBox.SelectedText = "ipp";

		Assert.Equal("hippo", _textBox.Text);
		Assert.Equal(4, _textBox.SelectionStart);
		Assert.Equal(0, _edits);
	}

	[Fact]
	public void Select_CutsToTheText_AndANegativeLengthSelectsBackward()
	{
		_textBox.Text = "hello";

		_textBox.Select(3, 10);
		Assert.Equal("lo", _textBox.SelectedText);

		_textBox.Select(3, -2);
		Assert.Equal("el", _textBox.SelectedText);
		Assert.Equal(1, _textBox.SelectionStart);

		Assert.Throws<ArgumentOutOfRangeException>(() => _textBox.Select(-1, 0));
	}

	[Fact]
	public void Arrows_SkipSurrogatePairs()
	{
		Start("a😀b");

		Press(Keys.Left);
		Assert.Equal(3, _textBox.SelectionStart);

		Press(Keys.Left);
		Assert.Equal(1, _textBox.SelectionStart);

		Press(Keys.Shift | Keys.Right);
		Assert.Equal("😀", _textBox.SelectedText);

		Press(Keys.Delete);
		Assert.Equal("ab", _textBox.Text);
	}

	[Fact]
	public void Click_PutsTheCaretAtTheNearestPosition()
	{
		_textBox.Text = "hello";

		MouseDown(At(3) + new Size(4, 0));
		Assert.Equal(3, _textBox.SelectionStart);
		Assert.True(_textBox.Focused);

		// A click focuses without selecting all.
		Assert.Equal(0, _textBox.SelectionLength);

		MouseUp(At(3));
		MouseDown(At(3) + new Size(6, 0));
		Assert.Equal(4, _textBox.SelectionStart);
		MouseUp(At(3));

		// Past the end of the text.
		Click(8);
		Assert.Equal(5, _textBox.SelectionStart);
	}

	[Fact]
	public void Drag_Selects()
	{
		_textBox.Text = "hello";

		MouseDown(At(1));
		MouseMove(At(4));
		MouseUp(At(4));

		Assert.Equal("ell", _textBox.SelectedText);

		// Released, the mouse no longer selects.
		_windowManager.OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, At(5), 0));
		Assert.Equal("ell", _textBox.SelectedText);
	}

	[Fact]
	public void DragBackward_Selects_WithTheCaretWhereTheMouseIs()
	{
		_textBox.Text = "hello";

		MouseDown(At(4));
		MouseMove(At(1));
		MouseUp(At(1));
		Press(Keys.Shift | Keys.Right);

		Assert.Equal("ll", _textBox.SelectedText);
	}

	[Fact]
	public void DoubleClick_SelectsAWord_AndTheSpacesAfterIt()
	{
		_textBox.Text = "ab cd  ef";

		Click(3);
		MouseDown(At(3) + new Size(3, 0), clicks: 2);
		MouseUp(At(3));

		Assert.Equal("cd  ", _textBox.SelectedText);

		// Past the end of the text, the last word.
		Click(9);
		MouseDown(At(9) + new Size(5, 0), clicks: 2);
		MouseUp(At(9));

		Assert.Equal("ef", _textBox.SelectedText);
	}

	[Fact]
	public void ShiftClick_ExtendsTheSelection()
	{
		_textBox.Text = "hello";
		Click(1);

		_windowManager.OnKeyDown(new KeyEventArgs(Keys.ShiftKey | Keys.Shift));
		Click(4);
		_windowManager.OnKeyUp(new KeyEventArgs(Keys.ShiftKey));

		Assert.Equal("ell", _textBox.SelectedText);
	}

	[Fact]
	public void TheText_ScrollsToKeepTheCaretInView()
	{
		// 20 characters, 200 pixels, in a text rectangle 96 wide.
		Start("abcdefghijklmnopqrst");
		_windowManager.Draw();

		// The caret, 1 pixel wide, at the rectangle's right.
		Assert.Equal(2 + 96 - 1, _textBox.GetPositionFromCharIndex(20).X);

		// A click goes to where the scrolled text is.
		var scroll = 200 + 1 - 96;
		MouseDown(new Point(TextLeft + 110 - scroll, Top + 5));
		MouseUp(new Point(TextLeft + 110 - scroll, Top + 5));
		Assert.Equal(11, _textBox.SelectionStart);

		Press(Keys.Home);
		_windowManager.Draw();
		Assert.Equal(2, _textBox.GetPositionFromCharIndex(0).X);
	}

	[Fact]
	public void Paint_DrawsTheSelectionInItsColor_AndOnlyWhatIsInView()
	{
		Start("hello");
		_textBox.Select(1, 3);

		_windowManager.Draw();

		Assert.Equal(
			new (string, Color)[]
			{
				("h", _textBox.ForeColor),
				("ell", _textBox.SelectionForeColor),
				("o", _textBox.ForeColor),
			},
			_factory.Strings.Select(s => (s.Text, s.Color))
		);

		_factory.Strings.Clear();
		Start("abcdefghijklmnopqrst");
		_windowManager.Draw();

		// Scrolled 105 pixels: from the "l" at 110, to the "t", whose right is at 200.
		Assert.Equal(new[] { "lmnopqrst" }, _factory.Strings.Select(s => s.Text));
	}

	[Fact]
	public void Paint_HidesTheSelection_WithoutTheFocus()
	{
		_textBox.Text = "hello";
		_textBox.Select(1, 3);

		_windowManager.Draw();

		Assert.Equal(new[] { "hello" }, _factory.Strings.Select(s => s.Text));
	}

	[Fact]
	public void Paint_ShowsThePlaceholder_WhileEmptyAndUnfocused()
	{
		_textBox.PlaceholderText = "Name";

		_windowManager.Draw();
		Assert.Equal(new[] { "Name" }, _factory.Strings.Select(s => s.Text));

		_factory.Strings.Clear();
		_textBox.Focus();
		_windowManager.Draw();
		Assert.Empty(_factory.Strings);
	}

	private sealed class PreviewForm : Form
	{
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.Home)
				e.Handled = true;
		}
	}

	[Fact]
	public void AKey_HandledByAFormThatPreviewsIt_IsNotProcessed()
	{
		var form = new PreviewForm
		{
			Bounds = new Rectangle(0, 0, 640, 480),
			KeyPreview = true,
			Visible = true,
		};
		_windowManager.Root.Controls.Remove(_textBox);
		form.Controls.Add(_textBox);
		_windowManager.Root.Controls.Add(form);
		Start("hello");

		Press(Keys.Home);

		Assert.Equal(5, _textBox.SelectionStart);
	}
}
