using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using OpenNspw.Desktop.Controls;

namespace OpenNspw.Desktop.Screens;

// The title's picture, fading into the navy of the menu below it.
internal sealed class MainMenuScreen(NspwGame game) : Screen(game)
{
	// The title's picture in the sprite sheet (set_sprt_data).
	private static readonly Rectangle TitleRegion = new(0, 3940, 699, 384);

	private Texture2D _title = default!;
	private Texture2D _fadeDown = default!;
	private Texture2D _fadeRight = default!;

	// A strip that goes from transparent to the navy of the menus along its length.
	private Texture2D CreateFade(int width, int height)
	{
		var colors = new Color[width * height];
		var ink = Theme.Ink.ToXnaColor();
		for (var i = 0; i < colors.Length; i++)
			colors[i] = ink * (i / (float)(colors.Length - 1));

		var texture = new Texture2D(GraphicsDevice, width, height);
		texture.SetData(colors);
		return texture;
	}

	public override void LoadContent()
	{
		base.LoadContent();
		_title = SpriteSheet.LoadRegion(GraphicsDevice, GameFiles.Resolve(Game.DataDirectory, "t3.bmp"), TitleRegion);
		_fadeDown = CreateFade(1, 256);
		_fadeRight = CreateFade(256, 1);

		var form = new MainMenuForm(Game.Fonts, Game.HasSingleplayerExecutable);
		form.SingleplayerChosen += (_, _) => Game.ShowScreen(new SingleplayerScreen(Game));
		form.MultiplayerChosen += (_, _) => Game.ShowScreen(new MultiplayerScreen(Game));
		form.QuitChosen += (_, _) => Game.Exit();
		WindowManager.Root.Controls.Add(form);
		form.Show();
		form.Focus();
	}

	public override void UnloadContent()
	{
		base.UnloadContent();
		_title.Dispose();
		_fadeDown.Dispose();
		_fadeRight.Dispose();
	}

	public override void Draw(GameTime gameTime)
	{
		var spriteBatch = Game.SpriteBatch;
		spriteBatch.Begin(transformMatrix: Game.ViewportAdapter.GetScaleMatrix(), samplerState: SamplerState.LinearClamp);
		spriteBatch.FillRectangle(0, 0, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT, Theme.Ink.ToXnaColor());

		// The picture fills the width from the top.
		var scale = all_head.SCRN_WIDTH / (float)TitleRegion.Width;
		spriteBatch.Draw(_title, new Vector2(all_head.SCRN_WIDTH / 2f, 0), null, Color.White, 0, new Vector2(TitleRegion.Width / 2f, 0), scale, SpriteEffects.None, 0);

		// It fades into the navy before its bottom edge, under the menu.
		spriteBatch.Draw(_fadeDown, new Rectangle(0, 300, all_head.SCRN_WIDTH, 250), Color.White);
		spriteBatch.FillRectangle(0, 549, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT - 549, Theme.Ink.ToXnaColor());
		spriteBatch.Draw(_fadeRight, new Rectangle(0, 0, 640, all_head.SCRN_HEIGHT), null, Color.White * 0.6f, 0, Vector2.Zero, SpriteEffects.FlipHorizontally, 0);
		spriteBatch.End();
	}
}
