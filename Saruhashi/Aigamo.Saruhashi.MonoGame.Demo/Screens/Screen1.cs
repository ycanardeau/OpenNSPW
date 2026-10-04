using Microsoft.Xna.Framework;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;

namespace Aigamo.Saruhashi.MonoGame.Demo.Screens;

internal sealed class Screen1 : ScreenBase
{
	public Screen1(WindowManager windowManager) : base(windowManager)
	{
	}

	public override void LoadContent()
	{
		base.LoadContent();

		var form = new Control { Bounds = new DrawingRectangle((1024 - 300) / 2, (768 - 300) / 2, 300, 300), Name = "Form1" };
		form.MouseClick += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseClick {e.Button} {e.Location} {form.Capture}");
		form.MouseCaptureChanged += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseCaptureChanged {form.Capture}");
		form.MouseDown += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseDown {e.Button} {e.Location} {form.Capture}");
		form.MouseMove += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseMove {e.Button} {e.Location} {form.Capture}");
		form.MouseUp += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseUp {e.Button} {e.Location} {form.Capture}");
		form.MouseEnter += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseEnter");
		form.MouseLeave += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseLeave");
		WindowManager.Root.Controls.Add(form);

		var button = new Button { Bounds = new DrawingRectangle(250, 0, 75, 23) };
		form.Controls.Add(button);

		var form3 = new Form { Bounds = new DrawingRectangle(55, 4, 16, 16), BackColor = DrawingColor.Blue };
		button.Controls.Add(form3);
		form3.Show();

		var form2 = new Form { Bounds = new DrawingRectangle(4, 4, 16, 16), BackColor = DrawingColor.Red };
		button.Controls.Add(form2);
		form2.Show();

		var form4 = new Form { Bounds = new DrawingRectangle(4, 4, 58, 8), BackColor = DrawingColor.Yellow };
		form2.Controls.Add(form4);
		form4.Show();

		var button2 = new Button { Bounds = new DrawingRectangle(-25, 0, 75, 23) };
		form.Controls.Add(button2);

		var form6 = new Form { Bounds = new DrawingRectangle(55, 4, 16, 16), BackColor = DrawingColor.Blue };
		button2.Controls.Add(form6);
		form6.Show();

		var form5 = new Form { Bounds = new DrawingRectangle(4, 4, 16, 16), BackColor = DrawingColor.Red };
		button2.Controls.Add(form5);
		form5.Show();

		var form7 = new Form { Bounds = new DrawingRectangle(4, 4, 58, 8), BackColor = DrawingColor.Yellow };
		form5.Controls.Add(form7);
		form7.Show();
	}

	public override void UnloadContent()
	{
		WindowManager.Root.Controls.Clear();
	}

	public override void Update(GameTime gameTime)
	{
	}

	public override void Draw(GameTime gameTime)
	{
	}
}
