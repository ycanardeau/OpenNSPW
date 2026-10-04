using System.ComponentModel;

namespace Aigamo.Saruhashi;

public class RadioButton : ButtonBase
{
	private bool _autoCheck = true;
	private bool _checked;

	public RadioButton() : base()
	{
		IsChecked = () => Checked;

		SetStyle(ControlStyles.StandardClick, false);
	}

	public Appearance Appearance { get; set; }

	public bool AutoCheck
	{
		get => _autoCheck;
		set
		{
			if (_autoCheck == value)
				return;

			_autoCheck = value;
			PerformAutoUpdates();
		}
	}

	public bool Checked
	{
		get => _checked;
		set
		{
			if (_checked == value)
				return;

			_checked = value;
			PerformAutoUpdates();
			OnCheckedChanged(EventArgs.Empty);
		}
	}

	protected Func<bool> IsChecked { get; set; }

	public event EventHandler? CheckedChanged;

	private CheckBoxState DetermineState(bool up)
	{
		var isChecked = IsChecked();

		if (isChecked)
		{
			if (!up)
				return CheckBoxState.CheckedPressed;

			if (MouseIsOver)
				return CheckBoxState.CheckedHot;

			if (!IsEnabled())
				return CheckBoxState.CheckedDisabled;

			return CheckBoxState.CheckedNormal;
		}
		else
		{
			if (!up)
				return CheckBoxState.UncheckedPressed;

			if (MouseIsOver)
				return CheckBoxState.UncheckedHot;

			if (!IsEnabled())
				return CheckBoxState.UncheckedDisabled;

			return CheckBoxState.UncheckedNormal;
		}
	}

	protected virtual void OnCheckedChanged(EventArgs e) => CheckedChanged?.Invoke(this, e);

	protected override void OnClick(EventArgs e)
	{
		if (AutoCheck)
			Checked = true;

		base.OnClick(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (Capture && WindowManager.WindowFromPoint(PointToScreen(e.Location)) == this)
			{
				OnClick(EventArgs.Empty);
				OnMouseClick(new MouseEventArgs(e.Button, e.Clicks, PointToClient(e.Location), e.Delta));
			}
		}

		base.OnMouseUp(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		switch (Appearance)
		{
			case Appearance.Normal:
				throw new NotImplementedException();

			case Appearance.Button:
				PushButtonState pushButtonState = DetermineState(!MouseIsDown) switch
				{
					CheckBoxState.UncheckedNormal => PushButtonState.Normal,
					CheckBoxState.UncheckedHot => PushButtonState.Hot,
					CheckBoxState.UncheckedPressed => PushButtonState.Pressed,
					CheckBoxState.UncheckedDisabled => PushButtonState.Disabled,
					CheckBoxState.CheckedNormal => PushButtonState.Pressed,
					CheckBoxState.CheckedHot => PushButtonState.Pressed,
					CheckBoxState.CheckedPressed => PushButtonState.Pressed,
					CheckBoxState.CheckedDisabled => PushButtonState.Disabled,
					_ => 0,
				};
				ButtonRenderer.DrawButton(e.Graphics, ClientRectangle, GetText(), Font, focused: false, pushButtonState);
				break;
		}

		base.OnPaint(e);
	}

	private void PerformAutoUpdates()
	{
		if (!_autoCheck)
			return;

		if (Checked && Parent != null)
		{
			var radioButtons = Parent.Controls
				.Where(c => c != this && c is RadioButton)
				.Select(c => c as RadioButton)
				.Where(b => b.AutoCheck && b.Checked);
			foreach (var b in radioButtons)
			{
				var propDesc = TypeDescriptor.GetProperties(this)[nameof(Checked)];
				propDesc.SetValue(b, false);
			}
		}
	}
}
