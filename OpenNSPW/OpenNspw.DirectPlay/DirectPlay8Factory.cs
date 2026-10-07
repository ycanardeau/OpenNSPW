using Aigamo.Otsuki;

namespace OpenNspw.DirectPlay;

// Creates the DirectPlay 8 objects of one game, for INspwPlatform.CoCreateInstance. The thread pool it creates serves
// the peers it creates, so that two games in one process do not handle each other's events.
public sealed class DirectPlay8Factory(PeerOptions? options = null)
{
	private readonly PeerOptions? _options = options;
	private readonly List<DirectPlay8Peer> _peers = [];

	public IReadOnlyList<DirectPlay8Peer> Peers => _peers;

	// Returns S_OK and the object, or REGDB_E_CLASSNOTREG for a class that is not DirectPlay 8.
	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv)
	{
		if (rclsid == dplay8.CLSID_DirectPlay8Peer)
		{
			var peer = new DirectPlay8Peer(_options);
			_peers.Add(peer);
			ppv = peer;
		}
		else if (rclsid == dplay8.CLSID_DirectPlay8ThreadPool)
		{
			ppv = new DirectPlay8ThreadPool(_peers);
		}
		else if (rclsid == dplay8.CLSID_DirectPlay8Address)
		{
			ppv = new DirectPlay8Address();
		}
		else
		{
			ppv = null;
			return winerror.REGDB_E_CLASSNOTREG;
		}

		return winerror.S_OK;
	}
}
