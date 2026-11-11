using UnityEngine;
using DrawingRectangle = System.Drawing.Rectangle;

namespace Aigamo.Saruhashi.Unity;

public interface IUnityImage : IImage
{
	/// <summary>
	/// Draws the image with its top left at <paramref name="position"/>, in IMGUI's points: during an IMGUI repaint.
	/// </summary>
	void Draw(Vector2 position);
}

public static class UnityImage
{
	private sealed class TextureImage(Texture texture, Color color) : IUnityImage
	{
		public Texture Texture { get; } = texture;
		public Color Color { get; } = color;

		public void Draw(Vector2 position) =>
			GUI.DrawTexture(
				new Rect(position.x, position.y, Texture.width, Texture.height),
				Texture,
				ScaleMode.StretchToFill,
				alphaBlend: true,
				imageAspect: 0,
				Color,
				borderWidth: 0,
				borderRadius: 0
			);
	}

	// A part of a texture, `size` pixels large, where `texCoords` are in the texture: from 0 to 1, and from the bottom, as
	// Unity's.
	private sealed class TextureRegionImage(Texture texture, Rect texCoords, Vector2 size, Color color)
		: IUnityImage
	{
		public Texture Texture { get; } = texture;
		public Rect TexCoords { get; } = texCoords;
		public Vector2 Size { get; } = size;
		public Color Color { get; } = color;

		public void Draw(Vector2 position)
		{
			var color = GUI.color;
			GUI.color = Color;
			GUI.DrawTextureWithTexCoords(new Rect(position, Size), Texture, TexCoords, alphaBlend: true);
			GUI.color = color;
		}
	}

	public static IUnityImage Create(Texture texture, Color color) => new TextureImage(texture, color);

	/// <summary>
	/// An image of the part of <paramref name="texture"/> in <paramref name="region"/>, in the texture's pixels from its top
	/// left, as Saruhashi's.
	/// </summary>
	public static IUnityImage Create(Texture texture, DrawingRectangle region, Color color) =>
		new TextureRegionImage(
			texture,
			new Rect(
				(float)region.X / texture.width,
				1 - (float)region.Bottom / texture.height,
				(float)region.Width / texture.width,
				(float)region.Height / texture.height
			),
			new Vector2(region.Width, region.Height),
			color
		);

	/// <summary>
	/// An image of <paramref name="sprite"/>, in its texture's pixels: its <see cref="Sprite.textureRect"/>, which a sprite
	/// packed tightly in an atlas does not have.
	/// </summary>
	public static IUnityImage Create(Sprite sprite, Color color)
	{
		var texture = sprite.texture;
		var rect = sprite.textureRect;
		return new TextureRegionImage(
			texture,
			new Rect(
				rect.x / texture.width,
				rect.y / texture.height,
				rect.width / texture.width,
				rect.height / texture.height
			),
			rect.size,
			color
		);
	}
}
