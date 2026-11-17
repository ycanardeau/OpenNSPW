using System.Drawing;
using Xunit;

namespace Aigamo.Saruhashi.Tests;

public sealed class FocusTests
{
	private readonly WindowManager _windowManager;
	private readonly Form _form = new() { KeyPreview = true };
	private readonly Control _row = new() { Bounds = new Rectangle(10, 10, 100, 20) };
	private readonly Control _other = new() { Bounds = new Rectangle(10, 40, 100, 20) };
	private int _formKeys;

	public FocusTests()
	{
		_windowManager = new WindowManager(
			new Rectangle(0, 0, 640, 480),
			new FakeGraphicsFactory(),
			new FakeFont()
		);
		_windowManager.Root.Controls.Add(_form);
		_form.Controls.Add(_row);
		_form.Controls.Add(_other);
		_form.Show();
		_form.KeyDown += (_, _) => _formKeys++;
	}

	private void Press(Keys keys) => _windowManager.OnKeyDown(new KeyEventArgs(keys));

	[Fact]
	public void HidingTheFocusedControl_GivesTheFocusToItsForm_WhichKeepsTakingKeys()
	{
		_row.Focus();
		Press(Keys.Down);

		_row.Hide();
		Press(Keys.Down);

		Assert.True(_form.Focused);
		Assert.Equal(2, _formKeys);
	}

	[Fact]
	public void DisablingTheFocusedControl_GivesTheFocusToItsForm()
	{
		_row.Focus();

		_row.Enabled = false;
		Press(Keys.Down);

		Assert.True(_form.Focused);
		Assert.Equal(1, _formKeys);
	}

	[Fact]
	public void HidingTheFormOfTheFocusedControl_GivesTheFocusToTheRoot()
	{
		_row.Focus();

		_form.Hide();

		Assert.True(_windowManager.Root.Focused);
	}

	[Fact]
	public void HidingAnotherControl_KeepsTheFocus()
	{
		_row.Focus();

		_other.Hide();

		Assert.True(_row.Focused);
	}

	[Fact]
	public void LeavingTheFocus_RaisesLostFocusAndGotFocus()
	{
		var events = new List<string>();
		_row.LostFocus += (_, _) => events.Add("row lost");
		_form.GotFocus += (_, _) => events.Add("form got");
		_row.Focus();

		_row.Hide();

		Assert.Equal(["row lost", "form got"], events);
	}
}
