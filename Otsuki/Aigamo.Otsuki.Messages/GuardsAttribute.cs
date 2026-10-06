// Code from: https://larryparkerdotnet.wordpress.com/2009/08/25/documenting-thread-safety/

namespace Aigamo.Otsuki.Messages;

/// <summary>
/// Indicates the items that this synchronization object guards.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="GuardsAttribute"/> object
/// and sets the <see cref="Items"/> property.
/// </remarks>
/// <param name="syncObjectName">The name of the sync object.</param>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class GuardsAttribute(params string[] items) : Attribute
{
	/// <summary>
	/// Gets or sets a comma-separated list of items guarded by this synchronization object.
	/// </summary>
	public string[] Items { get; set; } = items;
}
