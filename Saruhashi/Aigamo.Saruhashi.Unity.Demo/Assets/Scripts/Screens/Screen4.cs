#nullable enable

using System;
using UnityEngine;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace Aigamo.Saruhashi.Unity.Demo
{
	/// <summary>
	/// What the other screens leave out of the Unity backend: labels, check boxes, radio buttons, images, a control drawn
	/// larger, and the keyboard.
	/// </summary>
	internal static class Screen4
	{
		private const int SquareSize = 8;
		private const int CheckerboardSize = 64;

		private static readonly DrawingColor FocusedColor = DrawingColor.FromArgb(0, 122, 204);
		private static readonly DrawingColor UnfocusedColor = DrawingColor.Black;

		// Black and white squares, unfiltered to show where each pixel lands.
		private static Texture2D CreateCheckerboard()
		{
			var texture = new Texture2D(CheckerboardSize, CheckerboardSize) { filterMode = FilterMode.Point };
			for (var y = 0; y < CheckerboardSize; y++)
			{
				for (var x = 0; x < CheckerboardSize; x++)
				{
					var white = (x / SquareSize + y / SquareSize) % 2 == 0;
					texture.SetPixel(x, y, white ? Color.white : Color.black);
				}
			}

			// A red square at the top left, in Saruhashi's pixels: Unity's go up from the bottom left.
			for (var y = CheckerboardSize - SquareSize; y < CheckerboardSize; y++)
			{
				for (var x = 0; x < SquareSize; x++)
					texture.SetPixel(x, y, Color.red);
			}

			texture.Apply();
			return texture;
		}

		private static void AddLabel(Control parent, int x, int y, string text) =>
			parent.Controls.Add(new Label { Bounds = new DrawingRectangle(x, y, 400, 20), Text = text });

		private static void AddCheckBoxes(Control parent)
		{
			AddLabel(parent, 10, 10, "Check boxes");

			for (var i = 1; i <= 2; i++)
			{
				var checkBox = new CheckBox
				{
					Bounds = new DrawingRectangle(10, 10 + 24 * i, 200, 20),
					Name = $"CheckBox{i}",
					Text = $"CheckBox{i}",
				};
				checkBox.CheckedChanged += (_, _) =>
					Debug.Log($"{checkBox.Name} CheckedChanged {checkBox.Checked}");
				parent.Controls.Add(checkBox);
			}
		}

		// In a control of their own, which makes them one group.
		private static void AddRadioButtons(Control parent)
		{
			AddLabel(parent, 10, 100, "Radio buttons");

			var group = new Control { Bounds = new DrawingRectangle(10, 124, 200, 72) };
			parent.Controls.Add(group);

			for (var i = 0; i < 3; i++)
			{
				var radioButton = new RadioButton
				{
					Bounds = new DrawingRectangle(0, 24 * i, 200, 20),
					Name = $"RadioButton{i + 1}",
					Text = $"RadioButton{i + 1}",
					Checked = i == 0,
				};
				radioButton.CheckedChanged += (_, _) =>
					Debug.Log($"{radioButton.Name} CheckedChanged {radioButton.Checked}");
				group.Controls.Add(radioButton);
			}
		}

		// The whole checkerboard, then its top left quarter tinted yellow.
		private static void AddImages(Control parent)
		{
			AddLabel(parent, 10, 210, "Images: a texture, and a part of it tinted");

			var texture = CreateCheckerboard();
			var whole = UnityImage.Create(texture, Color.white);
			var part = UnityImage.Create(
				texture,
				new DrawingRectangle(0, 0, CheckerboardSize / 2, CheckerboardSize / 2),
				Color.yellow
			);

			var images = new Control { Bounds = new DrawingRectangle(10, 234, 200, CheckerboardSize) };
			images.Paint += (_, e) =>
			{
				e.Graphics.DrawImage(whole, new DrawingPoint(0, 0));
				e.Graphics.DrawImage(part, new DrawingPoint(CheckerboardSize + 10, 0));
			};
			parent.Controls.Add(images);
		}

		private static void AddScaledButton(Control parent)
		{
			AddLabel(parent, 10, 310, "A button drawn at twice its size");

			var button = new Button
			{
				Bounds = new DrawingRectangle(10, 334, 75, 23),
				Text = "Scale 2",
				Scale = 2,
			};
			button.Click += (_, _) => Debug.Log("Scale 2 Click");
			parent.Controls.Add(button);
		}

		// Takes the focus when clicked, then shows what is typed into it: Backspace erases.
		private static void AddKeyboard(Control parent)
		{
			AddLabel(parent, 10, 400, "Click the box, then type");

			var box = new Control
			{
				Bounds = new DrawingRectangle(10, 424, 400, 24),
				BackColor = UnfocusedColor,
			};
			box.MouseDown += (_, _) => box.Focus();
			box.GotFocus += (_, _) => box.BackColor = FocusedColor;
			box.LostFocus += (_, _) => box.BackColor = UnfocusedColor;
			box.KeyDown += (_, e) => Debug.Log($"KeyDown {e.KeyData}");
			box.KeyUp += (_, e) => Debug.Log($"KeyUp {e.KeyData}");
			box.KeyPress += (_, e) =>
			{
				Debug.Log($"KeyPress {(int)e.KeyChar:X4}");
				box.Text = e.KeyChar == '\b'
					? box.Text.Substring(0, Math.Max(0, box.Text.Length - 1))
					: char.IsControl(e.KeyChar)
						? box.Text
						: box.Text + e.KeyChar;
			};
			parent.Controls.Add(box);
		}

		public static void Load(WindowManager windowManager)
		{
			var root = windowManager.Root;
			AddCheckBoxes(root);
			AddRadioButtons(root);
			AddImages(root);
			AddScaledButton(root);
			AddKeyboard(root);
		}
	}
}
