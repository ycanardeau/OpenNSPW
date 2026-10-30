using System.Net;

namespace Aigamo.Otsuki.Transport;

internal static class AnyEndPoint
{
	/// <summary>
	/// The local endpoint to bind when only the remote one is given: any address and port, for an
	/// <see cref="IPEndPoint"/>. Other kinds of endpoint have no such default, so their local endpoint must be given.
	/// </summary>
	public static EndPoint For(EndPoint remoteEndPoint) =>
		remoteEndPoint is IPEndPoint
			? new IPEndPoint(IPAddress.Any, 0)
			: throw new ArgumentException(
				$"No default local endpoint for {remoteEndPoint}. Give the local endpoint to bind.",
				nameof(remoteEndPoint)
			);
}
