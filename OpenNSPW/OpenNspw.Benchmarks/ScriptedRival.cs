using System.Runtime.InteropServices;
using static OpenNspw.all_head;

namespace OpenNspw.Benchmarks;

// IDirectPlay8Address of a session that has no network: it keeps nothing.
internal sealed class NullAddress : IDirectPlay8Address
{
	public int SetSP(Guid? pguidSP)
	{
		return winerror.S_OK;
	}

	public int SetDevice(Guid? devGuid)
	{
		return winerror.S_OK;
	}

	public int AddComponent(string pwszName, object pvData, uint dwDataSize, uint dwDataType)
	{
		return winerror.S_OK;
	}

	public uint Release()
	{
		return 0;
	}
}

// IDirectPlay8Peer of a game that plays alone, against a rival played here, in the same process, without a network.
//
// The rival is a guest that joins as soon as the game hosts, and whose simulation is the same as the game's: it sends
// back the game's checksums (DP_FLAG_1) and screen changes, and answers each turn's order of the game with an order of
// its own, from Order(), or with DP_NO_ORDER. Its messages are delivered by DoWork, on the game's thread, as DirectPlay
// does in DoWork mode, so the game runs the same code as in a network game, but takes the same time in every run.
internal sealed unsafe class ScriptedRival : IDirectPlay8Peer
{
	private const uint LocalPlayer = 1;
	private const uint RivalPlayer = 2;
	private const string RivalName = "Rival";

	// DPNSUCCESS_PENDING: an asynchronous operation started.
	private const int DPNSUCCESS_PENDING = 0x0015800E;

	// The game reads received messages as structs without checking their size, so they are passed in a buffer of at
	// least this size, filled with zeros after the data.
	private const int MinimumReceiveBuffer = 1024;

	private readonly Lock _lock = new();
	private readonly Queue<(uint MessageId, object? Message, byte[]? Data)> _events = [];
	private readonly Queue<byte[]> _orders = [];
	private PFNDPNMESSAGEHANDLER? _handler;
	private object? _userContext;
	private string _localName = string.Empty;

	// Whether a message of this type is what each player sends once per turn, to give an order or none (UpdateBattle).
	private static bool IsOrder(MessageType type)
	{
		return type is MessageType.NoOrder or MessageType.UnitArrived or MessageType.MoveOrder or MessageType.MoveShipsOrder
			or MessageType.MovePlanesOrder or MessageType.SelectOrder or MessageType.SelectShipsOrder
			or MessageType.SelectPlanesOrder or MessageType.SelectLandOrder or MessageType.MenuOrder
			or MessageType.GoToGameSetting or MessageType.ResumeAndGoToGameSetting;
	}

	private static byte[] ToBytes<T>(T message) where T : unmanaged
	{
		return MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in message)).ToArray();
	}

	private void Enqueue(uint messageId, object? message, byte[]? data = null)
	{
		lock (_lock)
		{
			_events.Enqueue((messageId, message, data));
		}
	}

	// The rival's message in answer to one of the game's.
	private byte[]? Answer(byte[] data)
	{
		var type = MemoryMarshal.Read<MessageType>(data);
		if (IsOrder(type))
		{
			lock (_lock)
			{
				return _orders.TryDequeue(out var order) ? order : ToBytes(new _DP_FLAG { dwType = MessageType.NoOrder });
			}
		}

		// The rival is a guest, which leaves its dialog when the host starts the game.
		return (uint)type == MSG_EXIT_WAITING ? null : data;
	}

	// The rival gives an order in its next turn, as a message of the game (such as _DP_NEW_PP_SHIP).
	public void Order<T>(T message) where T : unmanaged
	{
		lock (_lock)
		{
			_orders.Enqueue(ToBytes(message));
		}
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
			? [new DPN_SERVICE_PROVIDER_INFO { guid = dplay8.CLSID_DP8SP_TCPIP, pwszName = "DirectPlay8 TCP/IP Service Provider" }]
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
		pdpspCaps.dwFlags = dplay8.DPNSPCAPS_SUPPORTSALLADAPTERS;
		return winerror.S_OK;
	}

	public int SetPeerInfo(DPN_PLAYER_INFO pdpnPlayerInfo, object? pvAsyncContext, object? phAsyncHandle, uint dwFlags)
	{
		_localName = pdpnPlayerInfo.pwszName ?? string.Empty;
		return winerror.S_OK;
	}

	public int GetPeerInfo(uint dpnid, DPN_PLAYER_INFO? pdpnPlayerInfo, ref uint pdwSize, uint dwFlags)
	{
		var name = dpnid switch
		{
			LocalPlayer => _localName,
			RivalPlayer => RivalName,
			_ => null,
		};
		if (name is null)
		{
			return dplay8.DPNERR_INVALIDPLAYER;
		}

		var size = DPN_PLAYER_INFO.SIZE + (uint)((name.Length + 1) * sizeof(char));
		if (pdpnPlayerInfo is null || pdwSize < size)
		{
			pdwSize = size;
			return dplay8.DPNERR_BUFFERTOOSMALL;
		}

		pdpnPlayerInfo.dwInfoFlags = dplay8.DPNINFO_NAME;
		pdpnPlayerInfo.pwszName = name;
		pdpnPlayerInfo.dwPlayerFlags = dpnid == LocalPlayer ? dplay8.DPNPLAYER_LOCAL | dplay8.DPNPLAYER_HOST : 0;
		return winerror.S_OK;
	}

	// The game hosts, and the rival joins at once.
	public int Host(DPN_APPLICATION_DESC pdnAppDesc, IDirectPlay8Address?[] prgpDeviceInfo, uint cDeviceInfo, object? pdnSecurity, object? pdnCredentials, object? pvPlayerContext, uint dwFlags)
	{
		Enqueue(dplay8.DPN_MSGID_CREATE_PLAYER, new DPNMSG_CREATE_PLAYER { dpnidPlayer = LocalPlayer });
		Enqueue(dplay8.DPN_MSGID_CREATE_PLAYER, new DPNMSG_CREATE_PLAYER { dpnidPlayer = RivalPlayer });
		return winerror.S_OK;
	}

	// The game only hosts.
	public int Connect(DPN_APPLICATION_DESC pdnAppDesc, IDirectPlay8Address? pHostAddr, IDirectPlay8Address? pDeviceInfo, object? pdnSecurity, object? pdnCredentials, byte[]? pvUserConnectData, uint dwUserConnectDataSize, object? pvPlayerContext, object? pvAsyncContext, ref uint phAsyncHandle, uint dwFlags)
	{
		return dplay8.DPNERR_GENERIC;
	}

	public int SendTo(uint dpnid, ref DPN_BUFFER_DESC prgBufferDesc, uint cBufferDesc, uint dwTimeOut, object? pvAsyncContext, ref uint phAsyncHandle, uint dwFlags)
	{
		var data = new ReadOnlySpan<byte>(prgBufferDesc.pBufferData, (int)prgBufferDesc.dwBufferSize).ToArray();
		if (Answer(data) is { } answer)
		{
			Enqueue(dplay8.DPN_MSGID_RECEIVE, null, answer);
		}

		phAsyncHandle = 0;
		return (dwFlags & dplay8.DPNSEND_NOCOMPLETE) != 0 ? winerror.S_OK : DPNSUCCESS_PENDING;
	}

	public int Close(uint dwFlags)
	{
		return winerror.S_OK;
	}

	public uint Release()
	{
		return 0;
	}

	// Delivers the rival's messages so far to the game's message handler. Called by ScriptedRivalThreadPool.DoWork.
	public void DoWork()
	{
		(uint MessageId, object? Message, byte[]? Data)[] events;
		lock (_lock)
		{
			events = [.. _events];
			_events.Clear();
		}

		foreach (var (messageId, message, data) in events)
		{
			if (data is null)
			{
				_handler?.Invoke(_userContext, messageId, message!);
				continue;
			}

			var buffer = new byte[Math.Max(data.Length, MinimumReceiveBuffer)];
			data.CopyTo(buffer, 0);
			fixed (byte* pReceiveData = buffer)
			{
				_handler?.Invoke(_userContext, messageId, new DPNMSG_RECEIVE
				{
					dpnidSender = RivalPlayer,
					pReceiveData = pReceiveData,
					dwReceiveDataSize = (uint)data.Length,
				});
			}
		}
	}
}

// IDirectPlay8ThreadPool in DoWork mode, for a game against a ScriptedRival.
internal sealed class ScriptedRivalThreadPool(Func<ScriptedRival?> rival) : IDirectPlay8ThreadPool
{
	private readonly Func<ScriptedRival?> _rival = rival;

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
		_rival()?.DoWork();
		return winerror.S_OK;
	}

	public uint Release()
	{
		return 0;
	}
}
