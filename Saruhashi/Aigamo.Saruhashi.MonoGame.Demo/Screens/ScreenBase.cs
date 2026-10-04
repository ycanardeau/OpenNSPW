using MonoGame.Extended.Screens;
using DrawingRectangle = System.Drawing.Rectangle;

namespace Aigamo.Saruhashi.MonoGame.Demo.Screens;

internal abstract class ScreenBase : Screen
{
	protected WindowManager WindowManager { get; }

	protected ScreenBase(WindowManager windowManager)
	{
		WindowManager = windowManager;
	}

	public override void LoadContent()
	{
		var screens = typeof(ScreenBase).Assembly.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(ScreenBase)))
			.OrderBy(t => t.ToString().Length)
			.ThenBy(t => t.ToString());
		foreach (var (type, i) in screens.Select((t, i) => (t, i)))
		{
			var button = new Button
			{
				Bounds = new DrawingRectangle(1024 - 300, 20 * i, 300, 20),
				Text = type.Name,
			};
			button.Click += (sender, e) =>
			{
				var screen = Activator.CreateInstance(type, WindowManager) as Screen ?? throw new TypeLoadException();
				ScreenManager.ReplaceScreen(screen);
			};
			WindowManager.Root.Controls.Add(button);
		}
	}

	public override void UnloadContent()
	{
		WindowManager.Root.Controls.Clear();
	}
}
