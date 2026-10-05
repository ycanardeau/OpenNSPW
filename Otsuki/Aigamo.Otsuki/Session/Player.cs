using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;

namespace Aigamo.Otsuki.Session;

/// <summary>
/// A name table entry ([MC-DPL8CS] section 2.2.6). Owned by the <see cref="SessionActor"/>.
/// </summary>
internal sealed class Player
{
	public required Dpnid Id { get; init; }

	public required string Name { get; init; }

	public ImmutableArray<byte> Data { get; init; } = [];

	/// <summary>
	/// The DirectPlay address that other peers connect to.
	/// </summary>
	public DirectPlayUrl Url { get; init; } = DirectPlayUrl.Empty;

	public bool IsLocal { get; init; }

	public bool IsHost { get; init; }

	/// <summary>
	/// The name table version at which the player was added.
	/// </summary>
	public int Version { get; init; }

	public DnetVersion DnetVersion { get; init; } = DnetVersion.DirectX90;

	public Connection? Connection { get; set; }

	/// <summary>
	/// Whether <see cref="PeerEvent.PlayerCreated"/> was raised for this player.
	/// </summary>
	public bool Created { get; set; }

	/// <summary>
	/// Messages that arrived before <see cref="PeerEvent.PlayerCreated"/> was raised.
	/// </summary>
	public List<byte[]> PendingData { get; } = [];

	/// <summary>
	/// The name table version last reported through DN_NAMETABLE_VERSION. Tracked by the host.
	/// </summary>
	public int ReportedVersion { get; set; }

	/// <summary>
	/// Limits how long the host waits for DN_ACK_CONNECT_INFO.
	/// </summary>
	public ITimer? JoinTimer { get; set; }

	public PlayerInfo ToPlayerInfo() => new(Id, Name, Data, IsLocal, IsHost);

	public NameTableEntryInfo ToNameTableEntryInfo() =>
		new()
		{
			Dpnid = Id,
			Host = IsHost,
			Peer = true,
			Version = Version,
			DnetVersion = DnetVersion,
			Url = Url.Value,
			Data = Data,
			Name = Name,
		};

	public CoreMessage.AddPlayer ToAddPlayerMessage() =>
		new()
		{
			Dpnid = Id,
			Host = IsHost,
			Peer = true,
			Version = Version,
			DnetClientVersion = DnetVersion,
			Url = Url.Value,
			Data = Data,
			Name = Name,
		};
}
