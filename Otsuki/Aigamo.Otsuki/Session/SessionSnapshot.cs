using System.Collections.Immutable;
using System.Net;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session;

/// <summary>
/// An immutable view of the session that the <see cref="SessionActor"/> publishes for other threads to read.
/// </summary>
internal sealed record SessionSnapshot(
	ImmutableDictionary<Dpnid, PlayerInfo> Players,
	ApplicationDescription? Application,
	IPEndPoint? LocalEndPoint,
	Dpnid LocalPlayer,
	bool IsHost
)
{
	public static SessionSnapshot Empty { get; } =
		new(ImmutableDictionary<Dpnid, PlayerInfo>.Empty, null, null, Dpnid.Empty, false);
}
