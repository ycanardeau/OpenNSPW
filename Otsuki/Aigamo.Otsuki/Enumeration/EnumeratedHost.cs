using System.Collections.Immutable;
using System.Net;

namespace Aigamo.Otsuki;

/// <summary>
/// A host that answered <see cref="Peer.EnumHostsAsync"/>, like DPNMSG_ENUM_HOSTS_RESPONSE.
/// </summary>
/// <param name="HostEndPoint">The address to connect to, which is where the response came from.</param>
/// <param name="Application">The session. <see cref="ApplicationDescription.Password"/> is never sent.</param>
/// <param name="CurrentPlayers">The number of players when the host answered.</param>
/// <param name="ResponseData">The data that the host set with <see cref="Peer.SetEnumResponseData"/>.</param>
/// <param name="RoundTripTime">The time from the query to this response.</param>
public sealed record EnumeratedHost(
	IPEndPoint HostEndPoint,
	ApplicationDescription Application,
	int CurrentPlayers,
	ImmutableArray<byte> ResponseData,
	TimeSpan RoundTripTime
);

/// <summary>
/// How <see cref="Peer.EnumHostsAsync"/> queries, like the dwEnumCount, dwRetryInterval and dwTimeOut of
/// IDirectPlay8Peer::EnumHosts. [MC-DPLHP] leaves them to the implementation.
/// </summary>
public sealed record EnumHostsOptions
{
	public static EnumHostsOptions Default { get; } = new();

	/// <summary>
	/// How many queries to send. Datagrams can be lost, so more than one improves the odds of hearing every host.
	/// </summary>
	public int Count { get; init; } = 5;

	/// <summary>
	/// The time between queries.
	/// </summary>
	public TimeSpan RetryInterval { get; init; } = TimeSpan.FromMilliseconds(1500);

	/// <summary>
	/// How long to keep listening after the last query.
	/// </summary>
	public TimeSpan Timeout { get; init; } = TimeSpan.FromMilliseconds(1500);
}
