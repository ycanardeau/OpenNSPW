namespace OpenNspw.DirectPlay;

// IDirectPlay8ThreadPool in DoWork mode: DoWork delivers the events of the game's peers to their message handlers, on
// the calling thread.
public sealed class DirectPlay8ThreadPool(IReadOnlyList<DirectPlay8Peer> peers) : IDirectPlay8ThreadPool
{
	private readonly IReadOnlyList<DirectPlay8Peer> _peers = peers;

	public int Initialize(object? pvUserContext, PFNDPNMESSAGEHANDLER pfn, uint dwFlags)
	{
		return winerror.S_OK;
	}

	public int Close(uint dwFlags)
	{
		return winerror.S_OK;
	}

	public int SetThreadCount(uint dwProcessorNum, uint dwNumThreads, uint dwFlags)
	{
		return winerror.S_OK;
	}

	public int DoWork(uint dwAllowedTimeSlice, uint dwFlags)
	{
		foreach (var peer in _peers.ToArray())
		{
			peer.DoWork();
		}

		return winerror.S_OK;
	}

	public uint Release()
	{
		return 0;
	}
}
