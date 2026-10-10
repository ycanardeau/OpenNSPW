using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the DirectPlay 8 interfaces, structs and constants that the game uses (dplay8.h, dpaddr.h), with the
// same names and values. OpenNspw.DirectPlay implements the interfaces on Otsuki.
//
// Differences from the C++ API, because C# has no raw pointers:
// - Structs that the game allocates or passes by pointer (DPN_PLAYER_INFO, DPN_SERVICE_PROVIDER_INFO) are classes.
// - Wide strings (PWSTR) are strings.
// - DPN_BUFFER_DESC.pBufferData and DPNMSG_RECEIVE.pReceiveData are unsafe pointers, as in C++, so the game builds
//   and reads its messages as in the original. The data received is pinned while the message handler runs.
// - The buffer sizes of EnumServiceProviders count items, not bytes.
// - Message buffers passed to the message handler are objects of the DPNMSG_* classes.

public delegate int PFNDPNMESSAGEHANDLER(object? pvUserContext, uint dwMessageId, object pMsgBuffer);

[StructLayout(LayoutKind.Sequential)]
public unsafe struct DPN_BUFFER_DESC
{
	public uint dwBufferSize;
	public byte* pBufferData;
}

public sealed class DPN_PLAYER_INFO
{
	public const uint SIZE = 24;

	public uint dwSize;
	public uint dwInfoFlags;
	public string? pwszName;
	public byte[]? pvData;
	public uint dwDataSize;
	public uint dwPlayerFlags;
}

public sealed class DPN_APPLICATION_DESC
{
	public const uint SIZE = 80;

	public uint dwSize;
	public uint dwFlags;
	public Guid guidInstance;
	public Guid guidApplication;
	public uint dwMaxPlayers;
	public uint dwCurrentPlayers;
	public string? pwszSessionName;
	public string? pwszPassword;
	public byte[]? pvReservedData;
	public uint dwReservedDataSize;
	public byte[]? pvApplicationReservedData;
	public uint dwApplicationReservedDataSize;
}

public sealed class DPN_SERVICE_PROVIDER_INFO
{
	public uint dwFlags;
	public Guid guid;
	public string? pwszName;
	public object? pvReserved;
	public uint dwReserved;
}

public sealed class DPN_SP_CAPS
{
	public const uint SIZE = 44;

	public uint dwSize;
	public uint dwFlags;
	public uint dwNumThreads;
	public uint dwDefaultEnumCount;
	public uint dwDefaultEnumRetryInterval;
	public uint dwDefaultEnumTimeout;
	public uint dwMaxEnumPayloadSize;
	public uint dwBuffersPerThread;
	public uint dwSystemBufferSize;
}

public sealed class DPNMSG_CREATE_PLAYER
{
	public uint dwSize;
	public uint dpnidPlayer;
	public object? pvPlayerContext;
}

public sealed class DPNMSG_DESTROY_PLAYER
{
	public uint dwSize;
	public uint dpnidPlayer;
	public object? pvPlayerContext;
	public uint dwReason;
}

public sealed class DPNMSG_TERMINATE_SESSION
{
	public uint dwSize;
	public int hResultCode;
	public byte[]? pvTerminateData;
	public uint dwTerminateDataSize;
}

public sealed unsafe class DPNMSG_RECEIVE
{
	public uint dwSize;
	public uint dpnidSender;
	public object? pvPlayerContext;
	public byte* pReceiveData;
	public uint dwReceiveDataSize;
	public uint hBufferHandle;
	public uint dwReceiveFlags;
}

public sealed class DPNMSG_CONNECT_COMPLETE
{
	public uint dwSize;
	public uint hAsyncOp;
	public object? pvUserContext;
	public int hResultCode;
	public byte[]? pvApplicationReplyData;
	public uint dwApplicationReplyDataSize;
	public uint dpnidLocal;
}

public sealed class DPNMSG_SEND_COMPLETE
{
	public uint dwSize;
	public uint hAsyncOp;
	public object? pvUserContext;
	public int hResultCode;
	public uint dwSendTime;
	public uint dwFirstFrameRTT;
	public uint dwFirstFrameRetryCount;
}

public interface IDirectPlay8Address : IUnknown
{
	int SetSP(Guid? pguidSP);

	int SetDevice(Guid? devGuid);

	// pvData is a uint for DPNA_DATATYPE_DWORD and a string for DPNA_DATATYPE_STRING.
	int AddComponent(string pwszName, object pvData, uint dwDataSize, uint dwDataType);
}

public interface IDirectPlay8ThreadPool : IUnknown
{
	int Initialize(object? pvUserContext, PFNDPNMESSAGEHANDLER pfn, uint dwFlags);

	int Close(uint dwFlags);

	int SetThreadCount(uint dwProcessorNum, uint dwNumThreads, uint dwFlags);

	int DoWork(uint dwAllowedTimeSlice, uint dwFlags);
}

public interface IDirectPlay8Peer : IUnknown
{
	int Initialize(object? pvUserContext, PFNDPNMESSAGEHANDLER pfn, uint dwFlags);

	int EnumServiceProviders(Guid? pguidServiceProvider, Guid? pguidApplication, DPN_SERVICE_PROVIDER_INFO[]? pSPInfoBuffer, ref uint pcbEnumData, ref uint pcReturned, uint dwFlags);

	int GetSPCaps(Guid? pguidSP, DPN_SP_CAPS pdpspCaps, uint dwFlags);

	int SetPeerInfo(DPN_PLAYER_INFO pdpnPlayerInfo, object? pvAsyncContext, object? phAsyncHandle, uint dwFlags);

	int GetPeerInfo(uint dpnid, DPN_PLAYER_INFO? pdpnPlayerInfo, ref uint pdwSize, uint dwFlags);

	int Host(DPN_APPLICATION_DESC pdnAppDesc, IDirectPlay8Address?[] prgpDeviceInfo, uint cDeviceInfo, object? pdnSecurity, object? pdnCredentials, object? pvPlayerContext, uint dwFlags);

	int Connect(DPN_APPLICATION_DESC pdnAppDesc, IDirectPlay8Address? pHostAddr, IDirectPlay8Address? pDeviceInfo, object? pdnSecurity, object? pdnCredentials, byte[]? pvUserConnectData, uint dwUserConnectDataSize, object? pvPlayerContext, object? pvAsyncContext, ref uint phAsyncHandle, uint dwFlags);

	int SendTo(uint dpnid, ref DPN_BUFFER_DESC prgBufferDesc, uint cBufferDesc, uint dwTimeOut, object? pvAsyncContext, ref uint phAsyncHandle, uint dwFlags);

	int Close(uint dwFlags);
}

public static class dplay8
{
	public static readonly Guid CLSID_DirectPlay8Peer = new(0x286f484d, 0x375e, 0x4458, 0xa2, 0x72, 0xb1, 0x38, 0xe2, 0xf8, 0x0a, 0x6a);
	public static readonly Guid CLSID_DirectPlay8ThreadPool = new(0xfc47060e, 0x6153, 0x4b34, 0xb9, 0x75, 0x8e, 0x41, 0x21, 0xeb, 0x7f, 0x3c);
	public static readonly Guid CLSID_DirectPlay8Address = new(0x934a9523, 0xa3ca, 0x4bc5, 0xad, 0xa0, 0xd6, 0xd9, 0x5d, 0x97, 0x94, 0x21);

	public static readonly Guid IID_IDirectPlay8Peer = new(0x5102dacf, 0x241b, 0x11d3, 0xae, 0xa7, 0x00, 0x60, 0x97, 0xb0, 0x14, 0x11);
	public static readonly Guid IID_IDirectPlay8ThreadPool = new(0x0d22ee73, 0x4a46, 0x4a0d, 0x89, 0xb2, 0x04, 0x5b, 0x4d, 0x66, 0x64, 0x25);
	public static readonly Guid IID_IDirectPlay8Address = new(0x83783300, 0x4063, 0x4c8a, 0x9d, 0xb3, 0x82, 0x83, 0x0a, 0x7f, 0xeb, 0x31);

	public static readonly Guid CLSID_DP8SP_IPX = new(0x53934290, 0x628d, 0x11d2, 0xae, 0x0f, 0x00, 0x60, 0x97, 0xb0, 0x14, 0x11);
	public static readonly Guid CLSID_DP8SP_MODEM = new(0x6d4a3650, 0x628d, 0x11d2, 0xae, 0x0f, 0x00, 0x60, 0x97, 0xb0, 0x14, 0x11);
	public static readonly Guid CLSID_DP8SP_SERIAL = new(0x743b5d60, 0x628d, 0x11d2, 0xae, 0x0f, 0x00, 0x60, 0x97, 0xb0, 0x14, 0x11);
	public static readonly Guid CLSID_DP8SP_TCPIP = new(0xebfe7ba0, 0x628d, 0x11d2, 0xae, 0x0f, 0x00, 0x60, 0x97, 0xb0, 0x14, 0x11);

	public static readonly Guid GUID_NULL = Guid.Empty;

	public const uint DPN_MSGID_OFFSET = 0xFFFF0000;
	public const uint DPN_MSGID_CONNECT_COMPLETE = DPN_MSGID_OFFSET | 0x0005;
	public const uint DPN_MSGID_CREATE_PLAYER = DPN_MSGID_OFFSET | 0x0007;
	public const uint DPN_MSGID_DESTROY_PLAYER = DPN_MSGID_OFFSET | 0x0009;
	public const uint DPN_MSGID_RECEIVE = DPN_MSGID_OFFSET | 0x0011;
	public const uint DPN_MSGID_SEND_COMPLETE = DPN_MSGID_OFFSET | 0x0014;
	public const uint DPN_MSGID_TERMINATE_SESSION = DPN_MSGID_OFFSET | 0x0016;

	public const uint DPNID_ALL_PLAYERS_GROUP = 0;

	public const uint DPNOP_SYNC = 0x80000000;
	public const uint DPNCONNECT_OKTOQUERYFORADDRESSING = 0x0001;
	public const uint DPNHOST_OKTOQUERYFORADDRESSING = 0x0001;
	public const uint DPNINFO_NAME = 0x0001;
	public const uint DPNINITIALIZE_DISABLEPARAMVAL = 0x0001;
	public const uint DPNPLAYER_LOCAL = 0x0002;
	public const uint DPNPLAYER_HOST = 0x0004;
	public const uint DPNSESSION_NODPNSVR = 0x0040;
	public const uint DPNSPCAPS_SUPPORTSALLADAPTERS = 0x0004;

	public const uint DPNSEND_NOCOMPLETE = 0x0002;
	public const uint DPNSEND_GUARANTEED = 0x0008;
	public const uint DPNSEND_NOLOOPBACK = 0x0020;
	public const uint DPNSEND_PRIORITY_HIGH = 0x0080;

	public const int DPNERR_GENERIC = winerror.E_FAIL;
	public const int DPNERR_BUFFERTOOSMALL = unchecked((int)0x80158100);
	public const int DPNERR_CONNECTING = unchecked((int)0x80158150);
	public const int DPNERR_INVALIDPLAYER = unchecked((int)0x80158420);
	public const int DPNERR_NOCONNECTION = unchecked((int)0x80158480);

	public const uint DPNA_DATATYPE_STRING = 0x00000001;
	public const uint DPNA_DATATYPE_DWORD = 0x00000002;
	public const string DPNA_KEY_HOSTNAME = "hostname";
	public const string DPNA_KEY_PORT = "port";
	public const string DPNA_KEY_PHONENUMBER = "phonenumber";

	// From all_head.h, which needs these values.
	public const uint EASY_SEND = DPNSEND_NOCOMPLETE | DPNSEND_NOLOOPBACK;
	public const uint MUST_SEND = DPNSEND_NOLOOPBACK | DPNSEND_GUARANTEED;
}
