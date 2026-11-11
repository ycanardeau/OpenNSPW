namespace Aigamo.Saruhashi;

// A base class rather than an interface with default methods, which netstandard2.0's runtimes do not support.
public abstract class GraphicsFactory
{
	public abstract Graphics Create(Control control);

	/// <summary>
	/// The graphics that a paint of a control draws with, given back with <see cref="Return"/> once the paint is done. A
	/// factory may give the same graphics to every paint, as every control paints every frame; by default, a new one.
	/// </summary>
	public virtual Graphics Rent(Control control) => Create(control);

	/// <summary>
	/// Takes back a graphics of <see cref="Rent"/>: by default, disposes it.
	/// </summary>
	public virtual void Return(Graphics graphics) => graphics.Dispose();
}
