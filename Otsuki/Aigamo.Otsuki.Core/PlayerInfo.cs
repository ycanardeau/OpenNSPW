using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki;

/// <summary>
/// A player in the session, like DPN_PLAYER_INFO.
/// </summary>
public sealed record PlayerInfo(
	Dpnid Id,
	string Name,
	ImmutableArray<byte> Data,
	bool IsLocal,
	bool IsHost
);
