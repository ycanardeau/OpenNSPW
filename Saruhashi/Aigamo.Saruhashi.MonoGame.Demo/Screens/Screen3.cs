using Microsoft.Xna.Framework;
using DrawingRectangle = System.Drawing.Rectangle;

namespace Aigamo.Saruhashi.MonoGame.Demo.Screens;

internal sealed class Screen3(WindowManager windowManager) : ScreenBase(windowManager)
{
	public override void LoadContent()
	{
		base.LoadContent();

		var button1 = new Button
		{
			Bounds = new DrawingRectangle(10, 10, 64, 24),
			Text = "Hello World!",
		};
		WindowManager.Root.Controls.Add(button1);

		var textBox1 = new TextBox
		{
			Bounds = new DrawingRectangle(10, 40, 300, 24),
			PlaceholderText = "Type, select, cut, copy and paste",
		};
		textBox1.GotFocus += (sender, e) => Console.WriteLine("GotFocus");
		textBox1.LostFocus += (sender, e) => Console.WriteLine("LostFocus");
		textBox1.Edited += (sender, e) => Console.WriteLine($"Edited: {textBox1.Text}");
		WindowManager.Root.Controls.Add(textBox1);
		var textBox2 = new TextBox
		{
			Bounds = new DrawingRectangle(10, 70, 300, 24),
			Text = "Read-only: select and copy",
			ReadOnly = true,
		};
		WindowManager.Root.Controls.Add(textBox2);

		var button2 = new Button { Bounds = new DrawingRectangle(10, 100, 64, 24), Text = "Focus" };
		button2.MouseClick += (sender, e) => textBox1.Focus();
		WindowManager.Root.Controls.Add(button2);
	}

	public override void Update(GameTime gameTime) { }

	public override void Draw(GameTime gameTime) { }
}
