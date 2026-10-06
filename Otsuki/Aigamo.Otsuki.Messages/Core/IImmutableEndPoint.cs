using System.Net.Sockets;

namespace Aigamo.Otsuki.Messages.Core;

[Immutable]
internal interface IImmutableEndPoint
{
	AddressFamily AddressFamily { get; }
}
