using OpenNspw.DirectPlay;

namespace OpenNspw.Tests.Lockstep;

// DirectPlay 8 between the games of one test, in memory. Events are queued for a peer when they happen, and delivered
// to its message handler by its game's DoWork, in the order they were queued, as Otsuki's peer does. Nothing happens
// on other threads, so that a game that runs alone (see LockstepScheduler) sees the same events at the same points
// every time.
internal sealed class LoopbackNetwork
{
	private uint _nextPlayerId = 0x00100001;

	public LoopbackPeer? Host { get; set; }

	public uint NextPlayerId()
	{
		var id = _nextPlayerId;
		_nextPlayerId += 0x00100001;
		return id;
	}
}

// A sink for the bytes a game sends, so that they can be part of its trace.
internal delegate void SentHandler(uint dpnid, ReadOnlySpan<byte> data);

internal sealed unsafe class LoopbackPeer(LoopbackNetwork network, SentHandler sent) : IDirectPlay8Peer
{
	private const string TcpIpName = "DirectPlay8 TCP/IP Service Provider";

	// DPNSUCCESS_PENDING: an asynchronous operation started.
	private const int DPNSUCCESS_PENDING = 0x0015800E;

	// The game reads received messages as structs without checking their size (see DirectPlay8Peer).
	private const int MinimumReceiveBuffer = 1024;

	private readonly LoopbackNetwork _network = network;
	private readonly SentHandler _sent = sent;
	private readonly Queue<Action> _events = new();
	private PFNDPNMESSAGEHANDLER? _handler;
	private object? _userContext;
	private uint _nextHandle;

	public uint Id { get; private set; }

	public string Name { get; private set; } = string.Empty;

	public bool IsHost { get; private set; }

	public LoopbackPeer? Rival { get; private set; }

	private int Handle(uint messageId, object message)
	{
		return _handler?.Invoke(_userContext, messageId, message) ?? winerror.S_OK;
	}

	private void Enqueue(Action e)
	{
		_events.Enqueue(e);
	}

	private void EnqueueCreatePlayer(uint dpnid)
	{
		Enqueue(() => Handle(dplay8.DPN_MSGID_CREATE_PLAYER, new DPNMSG_CREATE_PLAYER { dpnidPlayer = dpnid }));
	}

	private void EnqueueReceive(uint sender, byte[] data)
	{
		Enqueue(() =>
		{
			var buffer = new byte[Math.Max(data.Length, MinimumReceiveBuffer)];
			data.CopyTo(buffer, 0);
			fixed (byte* pReceiveData = buffer)
			{
				Handle(dplay8.DPN_MSGID_RECEIVE, new DPNMSG_RECEIVE
				{
					dpnidSender = sender,
					pReceiveData = pReceiveData,
					dwReceiveDataSize = (uint)data.Length,
				});
			}
		});
	}

	private LoopbackPeer? Find(uint dpnid)
	{
		return dpnid == Id ? this : Rival is { } rival && rival.Id == dpnid ? rival : null;
	}

	public int Initialize(object? pvUserContext, PFNDPNMESSAGEHANDLER pfn, uint dwFlags)
	{
		_userContext = pvUserContext;
		_handler = pfn;
		return winerror.S_OK;
	}

	public int EnumServiceProviders(Guid? pguidServiceProvider, Guid? pguidApplication, DPN_SERVICE_PROVIDER_INFO[]? pSPInfoBuffer, ref uint pcbEnumData, ref uint pcReturned, uint dwFlags)
	{
		DPN_SERVICE_PROVIDER_INFO[] items = pguidServiceProvider is null
			? [new DPN_SERVICE_PROVIDER_INFO { guid = dplay8.CLSID_DP8SP_TCPIP, pwszName = TcpIpName }]
			: [];
		if (pSPInfoBuffer is null || pcbEnumData < items.Length)
		{
			pcbEnumData = (uint)items.Length;
			pcReturned = (uint)items.Length;
			return dplay8.DPNERR_BUFFERTOOSMALL;
		}

		items.CopyTo(pSPInfoBuffer, 0);
		pcReturned = (uint)items.Length;
		return winerror.S_OK;
	}

	public int GetSPCaps(Guid? pguidSP, DPN_SP_CAPS pdpspCaps, uint dwFlags)
	{
		if (pguidSP != dplay8.CLSID_DP8SP_TCPIP)
		{
			return winerror.E_FAIL;
		}

		pdpspCaps.dwFlags = dplay8.DPNSPCAPS_SUPPORTSALLADAPTERS;
		return winerror.S_OK;
	}

	public int SetPeerInfo(DPN_PLAYER_INFO pdpnPlayerInfo, object? pvAsyncContext, object? phAsyncHandle, uint dwFlags)
	{
		Name = pdpnPlayerInfo.pwszName ?? string.Empty;
		return winerror.S_OK;
	}

	public int GetPeerInfo(uint dpnid, DPN_PLAYER_INFO? pdpnPlayerInfo, ref uint pdwSize, uint dwFlags)
	{
		if (Find(dpnid) is not { } player)
		{
			return dplay8.DPNERR_INVALIDPLAYER;
		}

		var size = DPN_PLAYER_INFO.SIZE + (uint)((player.Name.Length + 1) * sizeof(char));
		if (pdpnPlayerInfo is null || pdwSize < size)
		{
			pdwSize = size;
			return dplay8.DPNERR_BUFFERTOOSMALL;
		}

		pdpnPlayerInfo.dwInfoFlags = dplay8.DPNINFO_NAME;
		pdpnPlayerInfo.pwszName = player.Name;
		pdpnPlayerInfo.dwPlayerFlags = (player == this ? dplay8.DPNPLAYER_LOCAL : 0) | (player.IsHost ? dplay8.DPNPLAYER_HOST : 0);
		return winerror.S_OK;
	}

	public int Host(DPN_APPLICATION_DESC pdnAppDesc, IDirectPlay8Address?[] prgpDeviceInfo, uint cDeviceInfo, object? pdnSecurity, object? pdnCredentials, object? pvPlayerContext, uint dwFlags)
	{
		if (_network.Host is not null)
		{
			return dplay8.DPNERR_GENERIC;
		}

		_network.Host = this;
		IsHost = true;
		Id = _network.NextPlayerId();
		EnqueueCreatePlayer(Id);
		return winerror.S_OK;
	}

	// Joins the host at once: the guest gets its own player, the host's and the completion, in Otsuki's order, and the
	// host gets the guest's player.
	public int Connect(DPN_APPLICATION_DESC pdnAppDesc, IDirectPlay8Address? pHostAddr, IDirectPlay8Address? pDeviceInfo, object? pdnSecurity, object? pdnCredentials, byte[]? pvUserConnectData, uint dwUserConnectDataSize, object? pvPlayerContext, object? pvAsyncContext, ref uint phAsyncHandle, uint dwFlags)
	{
		phAsyncHandle = 1;
		if (_network.Host is not { Rival: null } host || host == this)
		{
			Enqueue(() => Handle(dplay8.DPN_MSGID_CONNECT_COMPLETE, new DPNMSG_CONNECT_COMPLETE { hResultCode = dplay8.DPNERR_NOCONNECTION }));
			return DPNSUCCESS_PENDING;
		}

		Id = _network.NextPlayerId();
		Rival = host;
		host.Rival = this;
		EnqueueCreatePlayer(Id);
		EnqueueCreatePlayer(host.Id);
		Enqueue(() => Handle(dplay8.DPN_MSGID_CONNECT_COMPLETE, new DPNMSG_CONNECT_COMPLETE { hResultCode = winerror.S_OK }));
		host.EnqueueCreatePlayer(Id);
		return DPNSUCCESS_PENDING;
	}

	public int SendTo(uint dpnid, ref DPN_BUFFER_DESC prgBufferDesc, uint cBufferDesc, uint dwTimeOut, object? pvAsyncContext, ref uint phAsyncHandle, uint dwFlags)
	{
		var data = new ReadOnlySpan<byte>(prgBufferDesc.pBufferData, (int)prgBufferDesc.dwBufferSize).ToArray();
		_sent(dpnid, data);

		var result = winerror.S_OK;
		if (dpnid == dplay8.DPNID_ALL_PLAYERS_GROUP)
		{
			Rival?.EnqueueReceive(Id, data);
			if ((dwFlags & dplay8.DPNSEND_NOLOOPBACK) == 0)
			{
				EnqueueReceive(Id, data);
			}
		}
		else if (Find(dpnid) is { } target)
		{
			target.EnqueueReceive(Id, data);
		}
		else
		{
			result = dplay8.DPNERR_INVALIDPLAYER;
		}

		var handle = phAsyncHandle = ++_nextHandle;
		if ((dwFlags & dplay8.DPNSEND_NOCOMPLETE) != 0)
		{
			return winerror.S_OK;
		}

		Enqueue(() => Handle(dplay8.DPN_MSGID_SEND_COMPLETE, new DPNMSG_SEND_COMPLETE { hAsyncOp = handle, hResultCode = result }));
		return DPNSUCCESS_PENDING;
	}

	public int Close(uint dwFlags)
	{
		if (Rival is { } rival)
		{
			if (IsHost)
			{
				rival.Enqueue(() => rival.Handle(dplay8.DPN_MSGID_TERMINATE_SESSION, new DPNMSG_TERMINATE_SESSION { hResultCode = winerror.S_OK }));
			}

			rival.Enqueue(() => rival.Handle(dplay8.DPN_MSGID_DESTROY_PLAYER, new DPNMSG_DESTROY_PLAYER { dpnidPlayer = Id }));
			rival.Rival = null;
			Rival = null;
		}

		if (_network.Host == this)
		{
			_network.Host = null;
		}

		return winerror.S_OK;
	}

	public uint Release()
	{
		return 0;
	}

	// Delivers the events queued before the call. Events that the handlers queue wait for the next call.
	public void DoWork()
	{
		for (var count = _events.Count; count > 0; count--)
		{
			_events.Dequeue()();
		}
	}
}

internal sealed class LoopbackThreadPool(IReadOnlyList<LoopbackPeer> peers) : IDirectPlay8ThreadPool
{
	private readonly IReadOnlyList<LoopbackPeer> _peers = peers;

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

// Creates the DirectPlay 8 objects of one game on a LoopbackNetwork, for INspwPlatform.CoCreateInstance.
internal sealed class LoopbackDirectPlayFactory(LoopbackNetwork network, SentHandler sent)
{
	private readonly LoopbackNetwork _network = network;
	private readonly SentHandler _sent = sent;
	private readonly List<LoopbackPeer> _peers = [];

	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv)
	{
		if (rclsid == dplay8.CLSID_DirectPlay8Peer)
		{
			var peer = new LoopbackPeer(_network, _sent);
			_peers.Add(peer);
			ppv = peer;
		}
		else if (rclsid == dplay8.CLSID_DirectPlay8ThreadPool)
		{
			ppv = new LoopbackThreadPool(_peers);
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
