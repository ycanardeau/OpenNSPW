using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.Desktop.Screens;

// The title's picture, and the choice of single player, multiplayer or quitting.
internal sealed class MainMenuScreen(NspwGame game) : Screen(game)
{
	// The title's picture in the sprite sheet, and where the title screen draws it (set_sprt_data, demo.cpp).
	private static readonly Rectangle TitleRegion = new(0, 3940, 699, 384);
	private static readonly DrawingPoint TitleLocation = new(212 - 50, 130);

	private Texture2D? _title;

	private static Button CreateButton(int x, string text, EventHandler onClick)
	{
		var button = new Button { Bounds = new DrawingRectangle(x, 690, 140, 35), Text = text };
		button.Click += onClick;
		return button;
	}

	public override void LoadContent()
	{
		base.LoadContent();
		_title = SpriteSheet.LoadRegion(GraphicsDevice, GameFiles.Resolve(Game.DataDirectory, "t3.bmp"), TitleRegion);

		var form = new Form { Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT), BackColor = DrawingColor.Black };
		form.Paint += (_, e) => e.Graphics.DrawImage(MonoGameImage.Create(_title, Color.White), TitleLocation);

		var singleplayer = CreateButton(67, "Single Player", (_, _) => Game.ShowScreen(new SingleplayerScreen(Game)));
		singleplayer.Enabled = Game.HasSingleplayerExecutable;
		form.Controls.Add(singleplayer);
		form.Controls.Add(CreateButton(217, "Multiplayer", (_, _) => Game.ShowScreen(new MultiplayerScreen(Game))));
		form.Controls.Add(CreateButton(817, "Quit", (_, _) => Game.Exit()));

		WindowManager.Root.Controls.Add(form);
		form.Show();
		form.Focus();
	}

	public override void UnloadContent()
	{
		base.UnloadContent();
		_title?.Dispose();
	}
}
