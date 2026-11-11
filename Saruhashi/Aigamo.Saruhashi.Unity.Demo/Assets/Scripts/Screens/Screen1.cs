#nullable enable

using UnityEngine;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;

namespace Aigamo.Saruhashi.Unity.Demo
{
	/// <summary>
	/// Aigamo.Saruhashi.MonoGame.Demo's Screen1, logging to Unity's console.
	/// </summary>
	internal static class Screen1
	{
		public static void Load(WindowManager windowManager)
		{
			var form = new Control
			{
				Bounds = new DrawingRectangle((1024 - 300) / 2, (768 - 300) / 2, 300, 300),
				Name = "Form1",
			};
			form.MouseClick += (sender, e) =>
				Debug.Log(
					$"{((Control)sender!).Name} MouseClick {e.Button} {e.Location} {form.Capture}"
				);
			form.MouseCaptureChanged += (sender, e) =>
				Debug.Log($"{((Control)sender!).Name} MouseCaptureChanged {form.Capture}");
			form.MouseDown += (sender, e) =>
				Debug.Log(
					$"{((Control)sender!).Name} MouseDown {e.Button} {e.Location} {form.Capture}"
				);
			form.MouseMove += (sender, e) =>
				Debug.Log(
					$"{((Control)sender!).Name} MouseMove {e.Button} {e.Location} {form.Capture}"
				);
			form.MouseUp += (sender, e) =>
				Debug.Log(
					$"{((Control)sender!).Name} MouseUp {e.Button} {e.Location} {form.Capture}"
				);
			form.MouseEnter += (sender, e) =>
				Debug.Log($"{((Control)sender!).Name} MouseEnter");
			form.MouseLeave += (sender, e) =>
				Debug.Log($"{((Control)sender!).Name} MouseLeave");
			windowManager.Root.Controls.Add(form);

			var button = new Button { Bounds = new DrawingRectangle(250, 0, 75, 23) };
			form.Controls.Add(button);

			var form3 = new Form
			{
				Bounds = new DrawingRectangle(55, 4, 16, 16),
				BackColor = DrawingColor.Blue,
			};
			button.Controls.Add(form3);
			form3.Show();

			var form2 = new Form
			{
				Bounds = new DrawingRectangle(4, 4, 16, 16),
				BackColor = DrawingColor.Red,
			};
			button.Controls.Add(form2);
			form2.Show();

			var form4 = new Form
			{
				Bounds = new DrawingRectangle(4, 4, 58, 8),
				BackColor = DrawingColor.Yellow,
			};
			form2.Controls.Add(form4);
			form4.Show();

			var button2 = new Button { Bounds = new DrawingRectangle(-25, 0, 75, 23) };
			form.Controls.Add(button2);

			var form6 = new Form
			{
				Bounds = new DrawingRectangle(55, 4, 16, 16),
				BackColor = DrawingColor.Blue,
			};
			button2.Controls.Add(form6);
			form6.Show();

			var form5 = new Form
			{
				Bounds = new DrawingRectangle(4, 4, 16, 16),
				BackColor = DrawingColor.Red,
			};
			button2.Controls.Add(form5);
			form5.Show();

			var form7 = new Form
			{
				Bounds = new DrawingRectangle(4, 4, 58, 8),
				BackColor = DrawingColor.Yellow,
			};
			form5.Controls.Add(form7);
			form7.Show();
		}
	}
}
