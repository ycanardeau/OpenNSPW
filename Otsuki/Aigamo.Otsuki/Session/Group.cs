using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session;

/// <summary>
/// A group in the name table ([MC-DPL8CS] section 1.3.7.2). Owned by the <see cref="SessionActor"/>.
/// </summary>
internal sealed class Group
{
	public required Dpnid Id { get; init; }

	public string Name { get; set; } = string.Empty;

	public ImmutableArray<byte> Data { get; set; } = [];

	public GroupFlags Flags { get; init; }

	/// <summary>
	/// The player that created the group. An <see cref="GroupFlags.AutoDestruct"/> group goes when it leaves.
	/// </summary>
	public Dpnid Owner { get; init; }

	/// <summary>
	/// The name table version at which the group was created.
	/// </summary>
	public int Version { get; init; }

	/// <summary>
	/// The members, with the name table version at which each was added.
	/// </summary>
	public Dictionary<Dpnid, int> Members { get; } = [];

	/// <summary>
	/// Whether <see cref="PeerEvent.GroupCreated"/> was raised for this group.
	/// </summary>
	public bool Created { get; set; }

	public GroupInfo ToGroupInfo() => new(Id, Name, Data, Flags, Owner, [.. Members.Keys]);

	public NameTableEntryInfo ToNameTableEntryInfo() =>
		new()
		{
			Dpnid = Id,
			DpnidOwner = Owner,
			Group = true,
			GroupAutoDestruct = Flags.HasFlag(GroupFlags.AutoDestruct),
			Version = Version,
			Data = Data,
			Name = Name,
		};

	public static Group FromNameTableEntry(NameTableEntryInfo entry) =>
		new()
		{
			Id = entry.Dpnid,
			Name = entry.Name,
			Data = [.. entry.Data],
			Flags = entry.GroupAutoDestruct ? GroupFlags.AutoDestruct : GroupFlags.None,
			Owner = entry.DpnidOwner,
			Version = entry.Version,
		};
}
