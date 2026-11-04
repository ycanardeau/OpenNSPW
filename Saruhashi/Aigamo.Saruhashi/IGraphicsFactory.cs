namespace Aigamo.Saruhashi;

public interface IGraphicsFactory
{
	public Graphics Create(Control control);

	/// <summary>
	/// The graphics that a paint of a control draws with, given back with <see cref="Return"/> once the paint is done. A
	/// factory may give the same graphics to every paint, as every control paints every frame; by default, a new one.
	/// </summary>
	public Graphics Rent(Control control) => Create(control);

	/// <summary>
	/// Takes back a graphics of <see cref="Rent"/>: by default, disposes it.
	/// </summary>
	public void Return(Graphics graphics) => graphics.Dispose();
}
