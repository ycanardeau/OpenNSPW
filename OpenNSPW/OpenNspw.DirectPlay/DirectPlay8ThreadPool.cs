namespace OpenNspw.DirectPlay;

// IDirectPlay8ThreadPool in DoWork mode: DoWork delivers the events of the game's peers to their message handlers, on
// the calling thread.
public sealed class DirectPlay8ThreadPool(IReadOnlyList<DirectPlay8Peer> peers)
	: IDirectPlay8ThreadPool
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
		// By index, without a copy, as the game calls this each time round its message loop: a peer that a handler creates
		// is served too.
		for (var i = 0; i < _peers.Count; i++)
		{
			_peers[i].DoWork();
		}

		return winerror.S_OK;
	}

	public uint Release()
	{
		return 0;
	}
}
