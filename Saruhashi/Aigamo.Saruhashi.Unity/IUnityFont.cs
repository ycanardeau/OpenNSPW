using UnityEngine;

namespace Aigamo.Saruhashi.Unity;

public interface IUnityFont : IFont
{
	void Draw(string text, Vector2 position, Color color);
	Vector2 MeasureString(string? text);
}
