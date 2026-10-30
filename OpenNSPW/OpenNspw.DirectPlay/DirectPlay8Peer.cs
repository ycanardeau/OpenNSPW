using System.Collections.Immutable;
using System.Net;
using Aigamo.Otsuki;
using Aigamo.Otsuki.Bluetooth;
using Aigamo.Otsuki.Messages.Core;

namespace OpenNspw.DirectPlay;

// IDirectPlay8Peer on an Otsuki peer. The service providers are TCP/IP, and Bluetooth when the platform gives a
// Bluetooth provider. The peer's events are delivered to the message handler by DirectPlay8ThreadPool.DoWork, on the
// game's thread, as DirectPlay does in DoWork mode.
public sealed unsafe class DirectPlay8Peer(PeerOptions? options, IBluetoothProvider? bluetooth)
	: IDirectPlay8Peer
{
	public const string TcpIPName = "DirectPlay8 TCP/IP Service Provider";

	public const string BluetoothName = "DirectPlay8 Bluetooth Service Provider";

	// DPNERR_ADDRESSING: an address is missing something it needs.
	private const int DPNERR_ADDRESSING = unchecked((int)0x80158040);

	// DPNERR_USERCANCEL: the player cancelled a dialog.
	private const int DPNERR_USERCANCEL = unchecked((int)0x80158650);

	// DPNSUCCESS_PENDING: an asynchronous operation started.
	private const int DPNSUCCESS_PENDING = 0x0015800E;

	// The game reads received messages as structs without checking their size, so they are passed in a buffer of at
	// least this size, filled with zeros after the data.
	private const int MinimumReceiveBuffer = 1024;

	// As Wine's dplay8.h defines it. Microsoft shipped this provider for the Pocket PC 2002 only, so the originals on
	// Windows never had it.
	public static readonly Guid CLSID_DP8SP_BLUETOOTH = new(
		0x995513af,
		0x3027,
		0x4b9a,
		0x95,
		0x6e,
		0xc7,
		0x72,
		0xb3,
		0xf7,
		0x80,
		0x06
	);

	// Sends a session over Bluetooth when it is hosted or joined from a Bluetooth endpoint, and over UDP otherwise.
	private static PeerOptions WithBluetooth(PeerOptions? options, IBluetoothProvider? bluetooth)
	{
		var udpOptions = options ?? PeerOptions.Default;
		if (bluetooth is null)
		{
			return udpOptions;
		}

		var udp = udpOptions.TransportFactory;
		return udpOptions with
		{
			TransportFactory = localEndPoint =>
				localEndPoint is BluetoothEndPoint bluetoothEndPoint
					? bluetooth.CreateTransport(bluetoothEndPoint)
					: udp(localEndPoint),
		};
	}

	private readonly IBluetoothProvider? _bluetooth = bluetooth;
	private readonly Peer _peer = new(WithBluetooth(options, bluetooth));
	private PFNDPNMESSAGEHANDLER? _handler;
	private object? _userContext;

	// The local endpoint of the session, once hosting or connected: an IPEndPoint over TCP/IP, and a BluetoothEndPoint
	// over Bluetooth.
	public EndPoint? LocalEndPoint => _peer.LocalEndPoint;

	private static ApplicationDescription ToApplication(DPN_APPLICATION_DESC desc)
	{
		return new ApplicationDescription
		{
			GuidApplication = desc.guidApplication,
			GuidInstance = desc.guidInstance,
			Flags = (SessionFlags)desc.dwFlags,
			MaxPlayers = (int)desc.dwMaxPlayers,
			SessionName = desc.pwszSessionName ?? string.Empty,
			Password = desc.pwszPassword ?? string.Empty,
		};
	}

	private bool IsBluetooth(IDirectPlay8Address? address) =>
		_bluetooth is not null
		&& (address as DirectPlay8Address)?.ServiceProvider == CLSID_DP8SP_BLUETOOTH;

	// The host of a Bluetooth session: the address's host name, or else the device the player chooses, if Connect may
	// ask (DPNCONNECT_OKTOQUERYFORADDRESSING). The game gives Bluetooth no host name, so the player chooses.
	private int GetBluetoothHost(
		IDirectPlay8Address? address,
		uint flags,
		out BluetoothEndPoint? host
	)
	{
		host = null;
		if ((address as DirectPlay8Address)?.Hostname is { } hostname)
		{
			return BluetoothEndPoint.TryParse(hostname, out host)
				? winerror.S_OK
				: DPNERR_ADDRESSING;
		}

		if ((flags & dplay8.DPNCONNECT_OKTOQUERYFORADDRESSING) == 0)
		{
			return DPNERR_ADDRESSING;
		}

		host = _bluetooth!.ChooseHost();
		return host is null ? DPNERR_USERCANCEL : winerror.S_OK;
	}

	private static SendFlags ToSendFlags(uint flags)
	{
		return (SendFlags)flags
			& (SendFlags.NoComplete | SendFlags.Guaranteed | SendFlags.NoLoopback);
	}

	public int Initialize(object? pvUserContext, PFNDPNMESSAGEHANDLER pfn, uint dwFlags)
	{
		_userContext = pvUserContext;
		_handler = pfn;
		return winerror.S_OK;
	}

	public int EnumServiceProviders(
		Guid? pguidServiceProvider,
		Guid? pguidApplication,
		DPN_SERVICE_PROVIDER_INFO[]? pSPInfoBuffer,
		ref uint pcbEnumData,
		ref uint pcReturned,
		uint dwFlags
	)
	{
		// The service providers, or the adapters of one. Neither lists adapters of its own, and both support all
		// adapters.
		DPN_SERVICE_PROVIDER_INFO[] items = pguidServiceProvider is null
			?
			[
				new DPN_SERVICE_PROVIDER_INFO
				{
					guid = dplay8.CLSID_DP8SP_TCPIP,
					pwszName = TcpIPName,
				},
				.. _bluetooth is null
					? (DPN_SERVICE_PROVIDER_INFO[])[]
					:
					[
						new DPN_SERVICE_PROVIDER_INFO
						{
							guid = CLSID_DP8SP_BLUETOOTH,
							pwszName = BluetoothName,
						},
					],
			]
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
		if (
			pguidSP != dplay8.CLSID_DP8SP_TCPIP
			&& !(pguidSP == CLSID_DP8SP_BLUETOOTH && _bluetooth is not null)
		)
		{
			return winerror.E_FAIL;
		}

		pdpspCaps.dwFlags = dplay8.DPNSPCAPS_SUPPORTSALLADAPTERS;
		return winerror.S_OK;
	}

	public int SetPeerInfo(
		DPN_PLAYER_INFO pdpnPlayerInfo,
		object? pvAsyncContext,
		object? phAsyncHandle,
		uint dwFlags
	)
	{
		_peer.SetPeerInfo(pdpnPlayerInfo.pwszName ?? string.Empty);
		return winerror.S_OK;
	}

	public int GetPeerInfo(
		uint dpnid,
		DPN_PLAYER_INFO? pdpnPlayerInfo,
		ref uint pdwSize,
		uint dwFlags
	)
	{
		if (_peer.GetPeerInfo(new Dpnid((int)dpnid)) is not { } player)
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
		pdpnPlayerInfo.dwPlayerFlags =
			(player.IsLocal ? dplay8.DPNPLAYER_LOCAL : 0)
			| (player.IsHost ? dplay8.DPNPLAYER_HOST : 0);
		return winerror.S_OK;
	}

	public int Host(
		DPN_APPLICATION_DESC pdnAppDesc,
		IDirectPlay8Address?[] prgpDeviceInfo,
		uint cDeviceInfo,
		object? pdnSecurity,
		object? pdnCredentials,
		object? pvPlayerContext,
		uint dwFlags
	)
	{
		var device = prgpDeviceInfo.FirstOrDefault();
		try
		{
			if (IsBluetooth(device))
			{
				_peer
					.HostAsync(ToApplication(pdnAppDesc), BluetoothEndPoint.Any)
					.GetAwaiter()
					.GetResult();
			}
			else
			{
				var port = (device as DirectPlay8Address)?.Port ?? Peer.DefaultPort;
				_peer.HostAsync(ToApplication(pdnAppDesc), port).GetAwaiter().GetResult();
			}

			return winerror.S_OK;
		}
		catch (Exception)
		{
			return dplay8.DPNERR_GENERIC;
		}
	}

	public int Connect(
		DPN_APPLICATION_DESC pdnAppDesc,
		IDirectPlay8Address? pHostAddr,
		IDirectPlay8Address? pDeviceInfo,
		object? pdnSecurity,
		object? pdnCredentials,
		byte[]? pvUserConnectData,
		uint dwUserConnectDataSize,
		object? pvPlayerContext,
		object? pvAsyncContext,
		ref uint phAsyncHandle,
		uint dwFlags
	)
	{
		BluetoothEndPoint? bluetoothHost = null;
		if (IsBluetooth(pHostAddr))
		{
			var hr = GetBluetoothHost(pHostAddr, dwFlags, out bluetoothHost);
			if (hr != winerror.S_OK)
			{
				return hr;
			}
		}

		var application = ToApplication(pdnAppDesc);
		var host = pHostAddr as DirectPlay8Address;
		var localPort = (pDeviceInfo as DirectPlay8Address)?.Port ?? 0;
		try
		{
			var connecting = bluetoothHost is not null
				? _peer.ConnectAsync(application, bluetoothHost, BluetoothEndPoint.Any)
				: _peer.ConnectAsync(
					application,
					host?.Hostname ?? "localhost",
					host?.Port ?? Peer.DefaultPort,
					localPort
				);
			connecting.GetAwaiter().GetResult();
		}
		catch (Exception)
		{
			return dplay8.DPNERR_GENERIC;
		}

		phAsyncHandle = 1;
		return DPNSUCCESS_PENDING;
	}

	public int SendTo(
		uint dpnid,
		ref DPN_BUFFER_DESC prgBufferDesc,
		uint cBufferDesc,
		uint dwTimeOut,
		object? pvAsyncContext,
		ref uint phAsyncHandle,
		uint dwFlags
	)
	{
		var data = new ReadOnlySpan<byte>(
			prgBufferDesc.pBufferData,
			(int)prgBufferDesc.dwBufferSize
		);
		phAsyncHandle = unchecked(
			(uint)_peer.SendTo(new Dpnid((int)dpnid), data, ToSendFlags(dwFlags))
		);
		return (dwFlags & dplay8.DPNSEND_NOCOMPLETE) != 0 ? winerror.S_OK : DPNSUCCESS_PENDING;
	}

	public int Close(uint dwFlags)
	{
		_peer.CloseAsync().GetAwaiter().GetResult();
		return winerror.S_OK;
	}

	public uint Release()
	{
		_peer.DisposeAsync().AsTask().GetAwaiter().GetResult();
		return 0;
	}

	private int Handle(uint messageId, object message)
	{
		return _handler?.Invoke(_userContext, messageId, message) ?? winerror.S_OK;
	}

	private int Receive(Dpnid sender, ImmutableArray<byte> data)
	{
		var buffer = new byte[Math.Max(data.Length, MinimumReceiveBuffer)];
		data.CopyTo(buffer);
		fixed (byte* pReceiveData = buffer)
		{
			return Handle(
				dplay8.DPN_MSGID_RECEIVE,
				new DPNMSG_RECEIVE
				{
					dpnidSender = (uint)sender.Value,
					pReceiveData = pReceiveData,
					dwReceiveDataSize = (uint)data.Length,
				}
			);
		}
	}

	private void Dispatch(PeerEvent e)
	{
		e.Match<int>(
			PlayerCreated: created =>
				Handle(
					dplay8.DPN_MSGID_CREATE_PLAYER,
					new DPNMSG_CREATE_PLAYER { dpnidPlayer = (uint)created.Player.Id.Value }
				),
			PlayerDestroyed: destroyed =>
				Handle(
					dplay8.DPN_MSGID_DESTROY_PLAYER,
					new DPNMSG_DESTROY_PLAYER
					{
						dpnidPlayer = (uint)destroyed.Player.Value,
						dwReason = (uint)destroyed.Reason,
					}
				),
			DataReceived: received => Receive(received.Sender, received.Data),
			ConnectCompleted: completed =>
				Handle(
					dplay8.DPN_MSGID_CONNECT_COMPLETE,
					new DPNMSG_CONNECT_COMPLETE { hResultCode = (int)completed.Result }
				),
			SendCompleted: completed =>
				Handle(
					dplay8.DPN_MSGID_SEND_COMPLETE,
					new DPNMSG_SEND_COMPLETE
					{
						hAsyncOp = unchecked((uint)completed.Handle),
						hResultCode = (int)completed.Result,
					}
				),
			SessionTerminated: terminated =>
				Handle(
					dplay8.DPN_MSGID_TERMINATE_SESSION,
					new DPNMSG_TERMINATE_SESSION { hResultCode = (int)terminated.Result }
				),
			// The game neither changes player information after joining nor uses groups, and has no handlers for
			// their messages.
			PeerInfoUpdated: _ => winerror.S_OK,
			// The game does not host sessions with DPNSESSION_MIGRATE_HOST.
			HostMigrated: _ => winerror.S_OK,
			GroupCreated: _ => winerror.S_OK,
			GroupDestroyed: _ => winerror.S_OK,
			PlayerAddedToGroup: _ => winerror.S_OK,
			PlayerRemovedFromGroup: _ => winerror.S_OK
		);
	}

	// Delivers the queued events to the message handler. Called by DirectPlay8ThreadPool.DoWork.
	public int DoWork()
	{
		return _peer.DoWork(Dispatch);
	}
}
