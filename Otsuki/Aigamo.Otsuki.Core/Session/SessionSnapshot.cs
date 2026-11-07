using System.Collections.Immutable;
using System.Net;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session;

/// <summary>
/// An immutable view of the session that the <see cref="SessionCore"/> publishes for other threads to read.
/// </summary>
internal sealed record SessionSnapshot(
	ImmutableDictionary<Dpnid, PlayerInfo> Players,
	ApplicationDescription? Application,
	EndPoint? LocalEndPoint,
	Dpnid LocalPlayer,
	bool IsHost,
	ImmutableDictionary<Dpnid, GroupInfo> Groups
)
{
	public static SessionSnapshot Empty { get; } = new([], null, null, Dpnid.Empty, false, []);
}
