using System.Net;
using System.Net.Sockets;
using Aigamo.Results;

namespace Aigamo.Otsuki.Session;

internal enum DirectPlayUrlError
{
	NotDirectPlayUrl,
	MissingHostname,
	InvalidPort,
	UnresolvableHostname,
}

/// <summary>
/// A DirectPlay address ([MC-DPL8CS] section 2.2.8), for example
/// <c>x-directplay:/provider=%7BEBFE7BA0-628D-11D2-AE0F-006097B01411%7D;hostname=192.168.0.10;port=2302</c>.
/// The text is kept as received so that keys we do not interpret survive being passed on to other peers.
/// </summary>
internal sealed record DirectPlayUrl(string Value)
{
	private const string Scheme = "x-directplay:/";

	private const string TcpIpProvider = "{EBFE7BA0-628D-11D2-AE0F-006097B01411}";

	public static DirectPlayUrl Empty { get; } = new(string.Empty);

	/// <summary>
	/// The address of a TCP/IP service provider endpoint.
	/// </summary>
	public static DirectPlayUrl FromEndPoint(IPEndPoint endPoint) =>
		new(
			$"{Scheme}provider={Uri.EscapeDataString(TcpIpProvider)};hostname={endPoint.Address};port={endPoint.Port}"
		);

	/// <summary>
	/// The "key=value" components, unescaped. Empty if this is not a DirectPlay URL.
	/// </summary>
	public IReadOnlyDictionary<string, string> Components =>
		!Value.StartsWith(Scheme, StringComparison.Ordinal)
			? new Dictionary<string, string>()
			: Value[Scheme.Length..]
				.Split(';')
				.Select(component => component.Split('=', 2))
				.Where(pair => pair.Length == 2)
				.GroupBy(pair => pair[0])
				.ToDictionary(group => group.Key, group => Uri.UnescapeDataString(group.Last()[1]));

	private static Result<IPAddress, DirectPlayUrlError> ResolveHostname(string hostname)
	{
		if (IPAddress.TryParse(hostname, out var address))
			return address;

		try
		{
			var resolved = Dns.GetHostAddresses(hostname)
				.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
			if (resolved is not null)
				return resolved;
		}
		catch (SocketException)
		{
			// Reported below like a name without IPv4 addresses.
		}

		return DirectPlayUrlError.UnresolvableHostname;
	}

	/// <summary>
	/// The endpoint of a TCP/IP service provider address.
	/// </summary>
	public Result<IPEndPoint, DirectPlayUrlError> ToEndPoint()
	{
		if (!Value.StartsWith(Scheme, StringComparison.Ordinal))
			return DirectPlayUrlError.NotDirectPlayUrl;

		var components = Components;
		if (!components.TryGetValue("hostname", out var hostname))
			return DirectPlayUrlError.MissingHostname;

		if (
			!components.TryGetValue("port", out var portText)
			|| !int.TryParse(portText, out var port)
			|| port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort
		)
			return DirectPlayUrlError.InvalidPort;

		return ResolveHostname(hostname).Map(address => new IPEndPoint(address, port));
	}

	public override string ToString() => Value;
}
