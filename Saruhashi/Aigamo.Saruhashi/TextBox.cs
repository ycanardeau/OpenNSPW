using System.Drawing;

namespace Aigamo.Saruhashi;

/// <summary>
/// A line of text that the user types, selects with the mouse or the keyboard, and cuts, copies and pastes through the
/// window manager's clipboard (<see cref="WindowManager.Clipboard"/>), and undoes and redoes, with the keys of Windows' edit
/// control and of a Mac's text field: the arrows, Home and End, with Shift to select, Ctrl or Option (Alt) to go by words,
/// and Command to the ends; Ctrl or Command with A, C, X, V and Z, and with Y or Shift+Z to redo; Ctrl+Insert, Shift+Insert
/// and Shift+Delete. A double-click selects a word, and Shift+click extends the selection.
/// </summary>
/// <remarks>
/// Backspace is taken as the character it types (<see cref="Control.KeyPress"/>), as Windows' edit control takes it, and
/// Delete as a key (<see cref="Control.KeyDown"/>). The text scrolls within <see cref="TextRectangle"/> to keep the caret in
/// view.
/// </remarks>
public class TextBox : Control
{
	// How long the caret is shown, then hidden: Windows' default (GetCaretBlinkTime).
	private const int CaretBlinkTime = 530;

	// How many edits can be undone.
	private const int UndoLimit = 100;

	// What an edit is, for joining it to the last one in one step to undo: a run of typing, or of deleting, is one step.
	private enum EditKind
	{
		Other,
		Typing,
		DeletingBackward,
		DeletingForward,
	}

	// The text, and the selection from the anchor to the caret, as an edit leaves them, to undo and redo.
	private readonly record struct TextState(string Text, int Anchor, int Caret);

	// The selection runs from the anchor, where it was started, to the caret, which moves as it is extended.
	private int _anchor;
	private int _caret;

	// When the caret last moved: it is shown for a blink from then, as it is while the user types.
	private long _caretMovedAt = Environment.TickCount64;

	private bool _dragging;
	private bool _focusingByMouse;
	private int _maxLength = 32767;

	// The x of each position of the text, 0 to its length, from the start of the text, as last measured, and the text and
	// font measured: measured again only when either changes (Measure).
	private float[] _offsets = [0];
	private string? _measuredText;
	private IFont? _measuredFont;

	// How far the text is scrolled to the left, in pixels, to keep the caret in view (KeepCaretInView).
	private float _scroll;

	// The states before the edits that can be undone, the last last, and before those undone, to redo.
	private readonly List<TextState> _undo = [];
	private readonly List<TextState> _redo = [];

	// The kind of the last edit and the state it left, which an edit of the same kind from there joins (Record).
	private EditKind _lastEditKind;
	private TextState _afterLastEdit;

	/// <summary>
	/// Occurs when the user changes the text, by typing, deleting, cutting or pasting; not when <see cref="Text"/> or
	/// <see cref="SelectedText"/> is set.
	/// </summary>
	public event EventHandler? Edited;

	/// <summary>
	/// The color of the caret, or <see cref="Color.Empty"/> for <see cref="Control.ForeColor"/>.
	/// </summary>
	public Color CaretColor { get; set; } = Color.Empty;

	public int CaretWidth { get; set; } = 1;

	protected override Size DefaultSize => new Size(100, 23);

	/// <summary>
	/// Whether the selection is hidden while the text box does not have the focus.
	/// </summary>
	public bool HideSelection { get; set; } = true;

	/// <summary>
	/// How many characters the user can type or paste; 0 for no limit. <see cref="Text"/> can be set longer.
	/// </summary>
	public int MaxLength
	{
		get => _maxLength;
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			_maxLength = value;
		}
	}

	private int Limit => _maxLength == 0 ? int.MaxValue : _maxLength;

	public Color PlaceholderForeColor { get; set; } = Color.FromArgb(128, 128, 128);

	/// <summary>
	/// The text shown while the text box is empty and does not have the focus.
	/// </summary>
	public string PlaceholderText { get; set; } = string.Empty;

	/// <summary>
	/// Whether the user can only select and copy the text, not change it.
	/// </summary>
	public bool ReadOnly { get; set; }

	public Color SelectionBackColor { get; set; } = Color.FromArgb(38, 79, 120);

	public Color SelectionForeColor { get; set; } = Color.FromArgb(241, 241, 241);

	/// <summary>
	/// Where the text is drawn and scrolled, in the control's client area, with the line centered in its height: the
	/// client area less a margin of 2 pixels on the left and the right.
	/// </summary>
	protected virtual Rectangle TextRectangle => new Rectangle(2, 0, Width - 4, Height);

	public bool CanUndo => _undo.Count > 0;

	public bool CanRedo => _redo.Count > 0;

	/// <summary>
	/// Forgets the edits, which can no longer be undone or redone.
	/// </summary>
	public void ClearUndo()
	{
		_undo.Clear();
		_redo.Clear();
		_lastEditKind = EditKind.Other;
	}

	/// <summary>
	/// The text. Setting it puts the caret at its start, with nothing selected, and forgets the edits (<see cref="ClearUndo"/>).
	/// </summary>
	public override string Text
	{
		get => base.Text;
		set
		{
			if (value == base.Text)
				return;

			_anchor = 0;
			_caret = 0;
			_scroll = 0;
			ClearUndo();
			base.Text = value;
		}
	}

	public int TextLength => Text.Length;

	/// <summary>
	/// Selects <paramref name="length"/> characters from <paramref name="start"/>, both cut to the text, with the caret at
	/// the end; before the start if <paramref name="length"/> is negative.
	/// </summary>
	public void Select(int start, int length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(start);

		var textLength = Text.Length;
		_anchor = Math.Min(start, textLength);
		_caret = (int)Math.Clamp((long)_anchor + length, 0, textLength);
		_caretMovedAt = Environment.TickCount64;
	}

	public int SelectionStart
	{
		get => Math.Min(_anchor, _caret);
		set => Select(value, Math.Abs(_caret - _anchor));
	}

	public int SelectionLength
	{
		get => Math.Abs(_caret - _anchor);
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			Select(Math.Min(_anchor, _caret), value);
		}
	}

	private int SelectionEnd => Math.Max(_anchor, _caret);

	// The kind of a character for words: a space, a word's (a letter, a digit or an underscore), or another, such as
	// punctuation. A run of characters of one kind is a word, as Windows' edit control breaks them.
	private static int CharKind(char c) =>
		char.IsWhiteSpace(c) ? 0
		: char.IsLetterOrDigit(c) || c == '_' ? 1
		: 2;

	// The position after the character at `index`, keeping a surrogate pair together.
	private int NextPosition(int index) =>
		index >= Text.Length ? Text.Length
		: char.IsSurrogatePair(Text, index) ? index + 2
		: index + 1;

	// The position before the character before `index`, keeping a surrogate pair together.
	private int PreviousPosition(int index) =>
		index <= 0 ? 0
		: index >= 2 && char.IsSurrogatePair(Text[index - 2], Text[index - 1]) ? index - 2
		: index - 1;

	// The start of the next word: past the rest of the word at `index`, and the spaces after it (Ctrl+Right).
	private int NextWordPosition(int index)
	{
		var text = Text;
		if (index < text.Length && !char.IsWhiteSpace(text[index]))
		{
			var kind = CharKind(text[index]);
			while (index < text.Length && CharKind(text[index]) == kind)
				index++;
		}

		while (index < text.Length && char.IsWhiteSpace(text[index]))
			index++;

		return index;
	}

	// The start of the word before `index`, past the spaces before it (Ctrl+Left).
	private int PreviousWordPosition(int index)
	{
		var text = Text;
		while (index > 0 && char.IsWhiteSpace(text[index - 1]))
			index--;

		if (index > 0)
		{
			var kind = CharKind(text[index - 1]);
			while (index > 0 && CharKind(text[index - 1]) == kind)
				index--;
		}

		return index;
	}

	// The word that a double-click at `index` selects: the run of characters of the kind of the one there, or of the last
	// one at the end of the text, and the spaces after a word, as Windows selects.
	private (int Start, int End) WordAt(int index)
	{
		var text = Text;
		if (text.Length == 0)
			return (0, 0);

		var at = Math.Min(index, text.Length - 1);
		var kind = CharKind(text[at]);
		var start = at;
		while (start > 0 && CharKind(text[start - 1]) == kind)
			start--;

		var end = at;
		while (end < text.Length && CharKind(text[end]) == kind)
			end++;

		if (kind != 0)
		{
			while (end < text.Length && char.IsWhiteSpace(text[end]))
				end++;
		}

		return (start, end);
	}

	// Moves the caret to `position`, extending the selection from the anchor, or with nothing selected.
	private void MoveCaret(int position, bool extend)
	{
		_caret = Math.Clamp(position, 0, Text.Length);
		if (!extend)
			_anchor = _caret;

		_caretMovedAt = Environment.TickCount64;
	}

	// The x of each position of `text` in `font`: the width of the text before it, measured whole, so that the pairs of
	// characters that a font draws closer together are measured as drawn.
	private static float[] MeasureOffsets(Graphics graphics, string text, IFont font)
	{
		var offsets = new float[text.Length + 1];
		for (var i = 1; i <= text.Length; i++)
			offsets[i] = graphics.MeasureString(text[..i], font).Width;

		return offsets;
	}

	private void Measure(Graphics graphics)
	{
		var text = Text;
		var font = Font;
		if (text == _measuredText && font == _measuredFont)
			return;

		_offsets = font is null ? new float[text.Length + 1] : MeasureOffsets(graphics, text, font);
		_measuredText = text;
		_measuredFont = font;
	}

	// Measures the text with a graphics of the text box's own where it changed since it was last painted, as when it is
	// clicked in the frame in which it was typed.
	private void EnsureMeasured()
	{
		if (Text == _measuredText && Font == _measuredFont)
			return;

		using var graphics = CreateGraphics();
		Measure(graphics);
	}

	/// <summary>
	/// The position of the text nearest to <paramref name="pt"/>, in the control's client area: where a click there puts
	/// the caret, from 0 to <see cref="TextLength"/>.
	/// </summary>
	public int GetCharIndexFromPosition(Point pt)
	{
		EnsureMeasured();

		var x = pt.X - TextRectangle.X + _scroll;
		var index = 0;
		while (index < Text.Length)
		{
			var next = NextPosition(index);
			if (x < (_offsets[index] + _offsets[next]) / 2)
				break;

			index = next;
		}

		return index;
	}

	/// <summary>
	/// Where the character at <paramref name="index"/> is drawn, in the control's client area: its left, at the top of
	/// <see cref="TextRectangle"/>, as the text is scrolled.
	/// </summary>
	public Point GetPositionFromCharIndex(int index)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Text.Length);
		EnsureMeasured();

		var bounds = TextRectangle;
		return new Point((int)MathF.Round(bounds.X + _offsets[index] - _scroll), bounds.Y);
	}

	// Sets the text, with the caret at `caret` and nothing selected, as an edit leaves them.
	private void SetText(string text, int caret)
	{
		_anchor = caret;
		_caret = caret;
		_caretMovedAt = Environment.TickCount64;
		base.Text = text;
	}

	// Puts `text` in place of the selection, as much of it as MaxLength leaves room for, with the caret after it.
	private void ReplaceSelection(string text)
	{
		var start = SelectionStart;
		var length = SelectionLength;
		var room = Math.Max(0, Limit - (Text.Length - length));
		if (text.Length > room)
			text = text[..(room > 0 && char.IsHighSurrogate(text[room - 1]) ? room - 1 : room)];

		SetText(
			string.Concat(Text.Substring(0, start), text, Text.Substring(start + length)),
			start + text.Length
		);
	}

	private TextState State => new(Text, _anchor, _caret);

	// Remembers `before`, the state before an edit of `kind`, to undo the edit to, which forgets the edits undone: an edit
	// of the kind of the last one, from where the last left the text and the caret, joins its step instead.
	private void Record(EditKind kind, TextState before)
	{
		if (kind == EditKind.Other || kind != _lastEditKind || before != _afterLastEdit)
		{
			if (_undo.Count == UndoLimit)
				_undo.RemoveAt(0);

			_undo.Add(before);
		}

		_redo.Clear();
		_lastEditKind = kind;
		_afterLastEdit = State;
	}

	/// <summary>
	/// The selected text. Setting it puts the text in place of the selection, with the caret after it, as a step that
	/// <see cref="Undo"/> undoes.
	/// </summary>
	public string SelectedText
	{
		get => Text.Substring(SelectionStart, SelectionLength);
		set
		{
			var before = State;
			ReplaceSelection(value);
			if (Text != before.Text)
				Record(EditKind.Other, before);
		}
	}

	protected virtual void OnEdited(EventArgs e) => Edited?.Invoke(this, e);

	// Puts `text` in place of the selection as the user does, by typing, deleting, cutting or pasting, from `before`, the
	// state before the edit, which Undo takes back: not in a read-only text box, and raising Edited if the text changes.
	private void Edit(string text, EditKind kind, TextState before)
	{
		if (ReadOnly)
			return;

		ReplaceSelection(text);
		if (Text == before.Text)
			return;

		Record(kind, before);
		OnEdited(EventArgs.Empty);
	}

	private void Edit(string text, EditKind kind = EditKind.Other) => Edit(text, kind, State);

	// Takes back the last state of `from`, keeping the current one in `to`, as the user undoes or redoes: not in a
	// read-only text box, and raising Edited if the text changes.
	private void Step(List<TextState> from, List<TextState> to)
	{
		if (ReadOnly || from.Count == 0)
			return;

		var before = Text;
		var state = from[^1];
		from.RemoveAt(from.Count - 1);
		to.Add(State);
		_anchor = state.Anchor;
		_caret = state.Caret;
		_caretMovedAt = Environment.TickCount64;
		_lastEditKind = EditKind.Other;
		base.Text = state.Text;
		if (Text != before)
			OnEdited(EventArgs.Empty);
	}

	/// <summary>
	/// Takes back the last edit, with the selection as it was before it, if the text box is not read-only.
	/// </summary>
	public void Undo() => Step(_undo, _redo);

	/// <summary>
	/// Makes the last edit undone again, if the text box is not read-only.
	/// </summary>
	public void Redo() => Step(_redo, _undo);

	public void SelectAll() => Select(0, Text.Length);

	/// <summary>
	/// Selects nothing, leaving the caret at the start of the selection.
	/// </summary>
	public void DeselectAll() => Select(SelectionStart, 0);

	/// <summary>
	/// Puts the selected text on the clipboard, if any is selected.
	/// </summary>
	public void Copy()
	{
		if (SelectionLength > 0)
			WindowManager.Clipboard.SetText(SelectedText);
	}

	/// <summary>
	/// Puts the selected text on the clipboard and deletes it, if any is selected and the text box is not read-only.
	/// </summary>
	public void Cut()
	{
		if (ReadOnly || SelectionLength == 0)
			return;

		Copy();
		Edit(string.Empty);
	}

	// The text up to its first line break: what a line takes of a pasted text, as Windows' single-line edit control takes.
	private static string FirstLine(string text)
	{
		var end = text.IndexOfAny(['\r', '\n']);
		return end < 0 ? text : text[..end];
	}

	/// <summary>
	/// Puts the clipboard's text in place of the selection, up to its first line break and as much as
	/// <see cref="MaxLength"/> leaves room for, if the text box is not read-only.
	/// </summary>
	public void Paste()
	{
		if (!ReadOnly)
			Edit(FirstLine(WindowManager.Clipboard.GetText()));
	}

	public void Clear() => Text = string.Empty;

	// Backspace: deletes the selection, or the text from `start` to the caret.
	private void DeleteBackward(int start)
	{
		if (ReadOnly)
			return;

		var before = State;
		if (SelectionLength == 0)
			MoveCaret(start, extend: true);

		Edit(string.Empty, EditKind.DeletingBackward, before);
	}

	// Delete: deletes the selection, or the text from the caret to `end`.
	private void DeleteForward(int end)
	{
		if (ReadOnly)
			return;

		var before = State;
		if (SelectionLength == 0)
			MoveCaret(end, extend: true);

		Edit(string.Empty, EditKind.DeletingForward, before);
	}

	// Whether the modifiers take a key by words: Ctrl, as Windows does, or Option (Alt), as a Mac does, but not both, which
	// is AltGr on many keyboards.
	private static bool IsByWords(Keys modifiers) =>
		modifiers.HasFlag(Keys.Control) != modifiers.HasFlag(Keys.Alt);

	// Does what a key does in Windows' edit control, or in a Mac's text field, and whether it does anything.
	private bool ProcessKey(KeyEventArgs e)
	{
		var shift = e.Shift;

		// Ctrl, or Command as on a Mac, for a shortcut, but not with Alt: Ctrl+Alt is AltGr on many keyboards, which types
		// characters.
		var shortcut = (e.Control || e.Command) && !e.Alt;
		var byWords = IsByWords(e.Modifiers);

		// Command goes to the ends, as on a Mac.
		var toEnds = e.Command;
		var moves = byWords || toEnds;
		switch (e.KeyCode)
		{
			case Keys.Left when !shift && !moves && SelectionLength > 0:
				MoveCaret(SelectionStart, extend: false);
				return true;
			case Keys.Left:
				MoveCaret(
					toEnds ? 0
					: byWords ? PreviousWordPosition(_caret)
					: PreviousPosition(_caret),
					shift
				);
				return true;
			case Keys.Right when !shift && !moves && SelectionLength > 0:
				MoveCaret(SelectionEnd, extend: false);
				return true;
			case Keys.Right:
				MoveCaret(
					toEnds ? Text.Length
					: byWords ? NextWordPosition(_caret)
					: NextPosition(_caret),
					shift
				);
				return true;
			case Keys.Home:
			case Keys.Up when toEnds:
				MoveCaret(0, shift);
				return true;
			case Keys.End:
			case Keys.Down when toEnds:
				MoveCaret(Text.Length, shift);
				return true;
			case Keys.Delete when shift:
				Cut();
				return true;
			case Keys.Delete:
				DeleteForward(
					toEnds ? Text.Length
					: byWords ? NextWordPosition(_caret)
					: NextPosition(_caret)
				);
				return true;
			case Keys.Insert when shortcut:
				Copy();
				return true;
			case Keys.Insert when shift:
				Paste();
				return true;
			case Keys.A when shortcut:
				SelectAll();
				return true;
			case Keys.C when shortcut:
				Copy();
				return true;
			case Keys.X when shortcut:
				Cut();
				return true;
			case Keys.V when shortcut:
				Paste();
				return true;
			case Keys.Z when shortcut && shift:
			case Keys.Y when shortcut:
				Redo();
				return true;
			case Keys.Z when shortcut:
				Undo();
				return true;
			default:
				return false;
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		// A form that previews the keys (Form.KeyPreview) takes them first, and may handle them.
		base.OnKeyDown(e);
		if (e.Handled)
			return;

		e.Handled = ProcessKey(e);
	}

	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		base.OnKeyPress(e);
		if (e.Handled)
			return;

		// Ctrl+Backspace, or Option+Backspace as on a Mac, deletes a word, and Command+Backspace, as on a Mac, up to the
		// start: the modifier was pressed before the Backspace that types the character.
		if (e.KeyChar == '\b')
		{
			var modifiers = WindowManager.ModifierKeys;
			DeleteBackward(
				modifiers.HasFlag(Keys.Command) ? 0
				: IsByWords(modifiers) ? PreviousWordPosition(_caret)
				: PreviousPosition(_caret)
			);
			e.Handled = true;
		}
		else if (!char.IsControl(e.KeyChar))
		{
			Edit(e.KeyChar.ToString(), EditKind.Typing);
			e.Handled = true;
		}
	}

	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);

		// Focused by the keyboard, as with Tab, or by the app, the whole text is selected, ready to be typed over; a click
		// puts the caret where it is.
		if (!_focusingByMouse)
			SelectAll();
	}

	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);
		_dragging = false;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		_focusingByMouse = true;
		try
		{
			base.OnMouseDown(e);
		}
		finally
		{
			_focusingByMouse = false;
		}

		if (e.Button != MouseButtons.Left)
			return;

		var index = GetCharIndexFromPosition(e.Location);
		if (e.Clicks == 2)
		{
			var (start, end) = WordAt(index);
			MoveCaret(start, extend: false);
			MoveCaret(end, extend: true);
			_dragging = false;
			return;
		}

		MoveCaret(index, extend: WindowManager.ModifierKeys.HasFlag(Keys.Shift));
		_dragging = true;
	}

	// Dragged, the selection follows the mouse, and the text scrolls as the caret goes past its edges.
	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_dragging && Capture)
			MoveCaret(GetCharIndexFromPosition(e.Location), extend: true);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		_dragging = false;
	}

	protected override void OnMouseCaptureChanged(EventArgs e)
	{
		base.OnMouseCaptureChanged(e);
		_dragging = false;
	}

	/// <summary>
	/// The height of a line of text in <paramref name="font"/>, which the caret and the selection are as high as: the
	/// height of "Ag", for a line as high whatever is typed. A font of other scripts may measure it with their characters.
	/// </summary>
	protected virtual float MeasureLineHeight(Graphics graphics, IFont font) =>
		graphics.MeasureString("Ag", font).Height;

	// Scrolls the text to have the caret in view in `width` pixels, and no further than the end of the text.
	private void KeepCaretInView(float width)
	{
		var end = Math.Max(0, _offsets[^1] + CaretWidth - width);
		_scroll = Math.Clamp(_scroll, 0, end);

		var caret = _offsets[_caret];
		if (caret < _scroll)
			_scroll = caret;
		else if (caret + CaretWidth > _scroll + width)
			_scroll = caret + CaretWidth - width;
	}

	// The positions from the first to the last whose characters between them are drawn whole in `width` pixels: a
	// character partly scrolled out of view is not drawn, as the text is not clipped to its rectangle.
	private (int First, int Last) VisibleRange(float width)
	{
		var first = 0;
		while (first < Text.Length && _offsets[first] < _scroll)
			first = NextPosition(first);

		var last = first;
		while (last < Text.Length && _offsets[NextPosition(last)] <= _scroll + width)
			last = NextPosition(last);

		return (first, last);
	}

	private bool IsCaretShown =>
		Focused && Enabled && (Environment.TickCount64 - _caretMovedAt) / CaretBlinkTime % 2 == 0;

	private void DrawSegment(
		Graphics graphics,
		IFont font,
		Color color,
		int start,
		int end,
		float x,
		float y
	)
	{
		if (start < end)
			graphics.DrawString(Text[start..end], font, SolidBrush.Of(color), new PointF(x, y));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var graphics = e.Graphics;
		if (Font is { } font)
		{
			Measure(graphics);

			var bounds = TextRectangle;
			KeepCaretInView(bounds.Width);

			var lineHeight = MeasureLineHeight(graphics, font);
			var y = bounds.Y + (bounds.Height - lineHeight) / 2;
			float X(int index) => bounds.X + _offsets[index] - _scroll;

			if (Text.Length == 0)
			{
				if (!Focused && PlaceholderText.Length > 0)
				{
					graphics.DrawString(
						PlaceholderText,
						font,
						SolidBrush.Of(PlaceholderForeColor),
						new PointF(bounds.X, y)
					);
				}
			}
			else
			{
				var (first, last) = VisibleRange(bounds.Width);
				var (start, end) =
					SelectionLength > 0 && (Focused || !HideSelection)
						? (
							Math.Clamp(SelectionStart, first, last),
							Math.Clamp(SelectionEnd, first, last)
						)
						: (last, last);
				if (start < end)
				{
					graphics.FillRectangle(
						SolidBrush.Of(SelectionBackColor),
						X(start),
						y,
						X(end) - X(start),
						lineHeight
					);
				}

				DrawSegment(graphics, font, ForeColor, first, start, X(first), y);
				DrawSegment(graphics, font, SelectionForeColor, start, end, X(start), y);
				DrawSegment(graphics, font, ForeColor, end, last, X(end), y);
			}

			if (IsCaretShown)
			{
				graphics.FillRectangle(
					SolidBrush.Of(CaretColor.IsEmpty ? ForeColor : CaretColor),
					X(_caret),
					y,
					CaretWidth,
					lineHeight
				);
			}
		}

		base.OnPaint(e);
	}
}
