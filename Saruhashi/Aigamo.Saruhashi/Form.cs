using System.Drawing;

namespace Aigamo.Saruhashi;

public class Form : Control
{
	public event EventHandler? FormClosed;
	public event EventHandler? Load;

	public Form() : base()
	{
		SetState(States.Visible, false);
	}

	public override BindingContext? BindingContext
	{
		get
		{
			var bm = base.BindingContext;
			if (bm is null)
			{
				bm = new BindingContext();
				BindingContext = bm;
			}

			return bm;
		}
		set => base.BindingContext = value;
	}

	protected override Size DefaultSize => new Size(300, 300);
	public DialogResult DialogResult { get; set; }
	public bool KeyPreview { get; set; }

	public void Close()
	{
		if (Parent is null)
			return;

		Parent.Controls.Remove(this);

		OnFormClosed(EventArgs.Empty);
	}

	protected override void OnCreateControl()
	{
		base.OnCreateControl();

		OnLoad(EventArgs.Empty);
	}

	protected virtual void OnFormClosed(EventArgs e) => FormClosed?.Invoke(this, e);
	protected virtual void OnLoad(EventArgs e) => Load?.Invoke(this, e);
}
