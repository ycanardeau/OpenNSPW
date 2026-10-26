namespace Aigamo.Otsuki.Messages.Core;

/// <summary>
/// A core message that changes the name table, which a new host collects with DN_REQ_NAMETABLE_OP after a migration.
/// </summary>
public interface INameTableOperation
{
	/// <summary>
	/// The name table version that the operation brings the table to.
	/// </summary>
	int Version { get; }
}
