#nullable enable

using UnityEngine;
using DrawingRectangle = System.Drawing.Rectangle;

namespace Aigamo.Saruhashi.Unity.Demo
{
	/// <summary>
	/// Aigamo.Saruhashi.MonoGame.Demo's Screen3, logging to Unity's console.
	/// </summary>
	internal static class Screen3
	{
		public static void Load(WindowManager windowManager)
		{
			var button1 = new Button
			{
				Bounds = new DrawingRectangle(10, 10, 64, 24),
				Text = "Hello World!",
			};
			windowManager.Root.Controls.Add(button1);

			var button2 = new Button { Bounds = new DrawingRectangle(10, 100, 64, 24), Text = "Focus" };
			windowManager.Root.Controls.Add(button2);
		}
	}
}
