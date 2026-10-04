using Microsoft.Xna.Framework;
using DrawingRectangle = System.Drawing.Rectangle;

namespace Aigamo.Saruhashi.MonoGame.Demo.Screens;

internal sealed class Screen2 : ScreenBase
{
	public Screen2(WindowManager windowManager) : base(windowManager)
	{
	}

	public override void LoadContent()
	{
		base.LoadContent();

		var form = new Control { Bounds = new DrawingRectangle((1024 - 300) / 2, (768 - 300) / 2, 300, 300), Name = "Form1" };
		form.MouseClick += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseClick {e.Button} {e.Location} {form.Capture}");
		WindowManager.Root.Controls.Add(form);

		var button3 = new Button { Bounds = new DrawingRectangle(50, 50, 100, 100), Name = "Button3", Text = "Button3" };
		button3.MouseClick += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseClick {e.Button} {e.Location} {button3.Capture}");
		button3.MouseDown += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseDown {e.Button} {e.Location} {button3.Capture}");
		button3.MouseMove += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseMove {e.Button} {e.Location} {button3.Capture}");
		button3.MouseUp += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseUp {e.Button} {e.Location} {button3.Capture}");
		form.Controls.Add(button3);

		var button1 = new Button { Bounds = new DrawingRectangle(50, 50, 200, 200), Name = "Button1", Text = "Button1" };
		button1.MouseClick += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseClick {e.Button} {e.Location} {button1.Capture}");
		button1.MouseDown += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseDown {e.Button} {e.Location} {button1.Capture}");
		button1.MouseMove += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseMove {e.Button} {e.Location} {button1.Capture}");
		button1.MouseUp += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseUp {e.Button} {e.Location} {button1.Capture}");
		form.Controls.Add(button1);

		var button2 = new Button { Bounds = new DrawingRectangle(150, 187, 75, 23), Name = "Button2", Text = "Button2" };
		button2.MouseClick += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseClick {e.Button} {e.Location} {button2.Capture}");
		button2.MouseDown += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseDown {e.Button} {e.Location} {button2.Capture}");
		button2.MouseMove += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseMove {e.Button} {e.Location} {button2.Capture}");
		button2.MouseUp += (sender, e) => Console.WriteLine($"{((Control)sender!).Name} MouseUp {e.Button} {e.Location} {button2.Capture}");
		button1.Controls.Add(button2);

		var button4 = new Button { Bounds = new DrawingRectangle(-50, -50, 100, 100), Name = "Button4", Text = "Button4" };
		button3.Controls.Add(button4);

		var label1 = new Control { Bounds = new DrawingRectangle(8, 8, 100, 100), Text = "Hello World!" };
		form.Controls.Add(label1);
	}

	public override void Update(GameTime gameTime)
	{
	}

	public override void Draw(GameTime gameTime)
	{
	}
}
