// Code from: https://larryparkerdotnet.wordpress.com/2009/08/25/documenting-thread-safety/

namespace Aigamo.Otsuki.Messages;

/// <summary>
/// Indicates the synchronization object that guards this item.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="GuardedByAttribute"/> object
/// and sets the <see cref="SyncObjectNames"/> property.
/// </remarks>
/// <param name="syncObjectNames">The name of the sync object.</param>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class GuardedByAttribute(params string[] syncObjectNames) : Attribute
{
	/// <summary>
	/// Gets or sets the name of the synchronization object that guards this item.
	/// </summary>
	public string[] SyncObjectNames { get; set; } = syncObjectNames;
}
