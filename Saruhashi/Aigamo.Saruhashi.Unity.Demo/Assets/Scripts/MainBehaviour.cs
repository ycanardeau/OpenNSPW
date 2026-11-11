#nullable enable

using System;
using UnityEngine;
using DrawingRectangle = System.Drawing.Rectangle;

// Unity 2021.3 compiles C# 9: block namespaces, and no primary constructors.
namespace Aigamo.Saruhashi.Unity.Demo
{
	/// <summary>
	/// The demo, as Aigamo.Saruhashi.MonoGame.Demo's: screens of controls in a 1024×768 layout fit to the screen, with a
	/// button for each screen at the top right.
	/// </summary>
	/// <remarks>
	/// It needs no scene: <see cref="Create"/> adds it to whichever scene plays.
	/// </remarks>
	public sealed class MainBehaviour : MonoBehaviour
	{
		private const int Width = 1024;
		private const int Height = 768;

		// As the MonoGame demo clears its screen: cornflower blue.
		private static readonly Color BackgroundColor = new Color32(100, 149, 237, 255);

		private static readonly (string Name, Action<WindowManager> Load)[] Screens =
		{
			(nameof(Screen1), Screen1.Load),
			(nameof(Screen2), Screen2.Load),
			(nameof(Screen3), Screen3.Load),
			(nameof(Screen4), Screen4.Load),
		};

		private WindowManager? _windowManager;
		private UnityGui? _gui;

		// The screen to show at the next frame: not during a click, which would remove the menu's button from under it.
		private int? _nextScreen = 0;

		// The layout as large as the screen fits it, in the screen's middle, as MonoGame.Extended's BoxingViewportAdapter.
		private static Matrix4x4 FitToScreen()
		{
			var scale = Mathf.Min((float)UnityEngine.Screen.width / Width, (float)UnityEngine.Screen.height / Height);
			var offset = new Vector3(
				(UnityEngine.Screen.width - Width * scale) / 2,
				(UnityEngine.Screen.height - Height * scale) / 2,
				0
			);
			return Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1));
		}

		private void ShowScreen(WindowManager windowManager, int index)
		{
			windowManager.Root.Controls.Clear();

			for (var i = 0; i < Screens.Length; i++)
			{
				var screen = i;
				var button = new Button
				{
					Bounds = new DrawingRectangle(Width - 300, 20 * i, 300, 20),
					Text = Screens[i].Name,
				};
				button.Click += (_, _) => _nextScreen = screen;
				windowManager.Root.Controls.Add(button);
			}

			Screens[index].Load(windowManager);
		}

		private void Start()
		{
			var camera = Camera.main;
			if (camera == null)
				camera = new GameObject("Main Camera").AddComponent<Camera>();

			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = BackgroundColor;

			var font = new UnityFont(Font.CreateDynamicFontFromOSFont("Arial", 16), 16);
			_windowManager = new WindowManager(
				new DrawingRectangle(0, 0, Width, Height),
				new UnityGraphicsFactory(),
				font
			);
			_gui = new UnityGui(_windowManager);
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
				Application.Quit();

			if (_windowManager != null && _nextScreen is int next)
			{
				_nextScreen = null;
				ShowScreen(_windowManager, next);
			}
		}

		private void OnGUI()
		{
			if (_gui == null)
				return;

			_gui.Matrix = FitToScreen();
			_gui.OnGUI();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Create() => new GameObject("Saruhashi").AddComponent<MainBehaviour>();
	}
}
