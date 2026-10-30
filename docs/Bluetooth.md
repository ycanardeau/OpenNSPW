# Bluetooth
A proposal for letting two copies of OpenNSPW play over Bluetooth, as a DirectPlay service provider next to TCP/IP.

This is a proposal, not a final design. [Open questions](#open-questions) lists what still needs to be decided.

## Summary
- Bluetooth play is **OpenNSPW against OpenNSPW only**. Microsoft's DirectPlay 8 Bluetooth service provider only ever shipped for Pocket PC 2002, never for desktop Windows, so NSPW 1.22 and NSPW NET never had one to play over.
- The **ported game code does not change**. The connection dialog already handles a service provider it does not know (see [The game side](#the-game-side)).
- **Otsuki** has general endpoints instead of IPv4 ones, and a transport that carries its datagrams over any reliable stream.
- A new **Bluetooth Classic RFCOMM** connector supplies those streams, with code of its own for each OS, starting with macOS.
- **Without any of this**, players can already use a Bluetooth PAN (IP over Bluetooth, set up in the OS) with the TCP/IP service provider.

## The game side
[dplay.cs](../OpenNSPW/OpenNspw/dplay.cs) is the DirectX SDK's connection wizard. It lists whatever `EnumServiceProviders` returns. For a provider other than TCP/IP, IPX and modem, it:

1. disables both address fields;
2. sets `DPNHOST_OKTOQUERYFORADDRESSING` or `DPNCONNECT_OKTOQUERYFORADDRESSING`, which lets the provider show its own dialog to ask for an address;
3. calls `Connect` directly, without enumerating hosts first.

So a Bluetooth provider only has to be listed by `DirectPlay8Peer.EnumServiceProviders`, and its `Host` and `Connect` have to get the address themselves.

The provider list is sorted, so "DirectPlay8 Bluetooth Service Provider" would come before TCP/IP. TCP/IP still stays selected by default, because the saved "Preferred Provider" defaults to "DirectPlay8 TCP/IP Service Provider" ([win_main.cs](../OpenNSPW/OpenNspw/win_main.cs)).

## Otsuki
Otsuki assumed IPv4 in these places:

| Where | What |
| --- | --- |
| `IDatagramTransport`, `PeerOptions.TransportFactory` | Endpoints are `IPEndPoint` |
| `ReliableEndpoint`, `Connection` | Connections are keyed by `IPEndPoint` |
| `SessionMessage`, `SessionSnapshot`, `SessionActor`, `Peer`, `Client`, `Server` | Local, host and remote endpoints are `IPEndPoint` |
| `DirectPlayUrl` | Only builds and reads TCP/IP addresses (`provider`, `hostname`, `port`) |
| `HostEnumerator`, `EnumeratedHost` | Enumeration is a UDP query, possibly to a broadcast address |

### Endpoints
Done: Otsuki uses `System.Net.EndPoint` in all of the places above.

- `Peer`, `Client` and `Server` take a local endpoint of any kind. Their `int` port overloads still bind to every IPv4 address, and connecting or enumerating without a local endpoint does so too when the remote one is an `IPEndPoint`. For other kinds, the local endpoint must be given.
- `PeerOptions.TransportFactory` receives the local endpoint. The default UDP transport accepts only an `IPEndPoint`.
- A Bluetooth endpoint will be a new `EndPoint` subclass holding the device address (`BD_ADDR`) and the RFCOMM channel, with value equality for the connection table. It can't be a record, because `EndPoint` is a class.

`DirectPlayUrl` maps each endpoint type to its provider:

- TCP/IP stays as it is.
- Other endpoints get an empty URL for now.
- Bluetooth will use `provider={CLSID_DP8SP_BLUETOOTH};hostname=<BD_ADDR>;port=<channel>`. No documented DirectPlay URL format for Bluetooth exists, and only OpenNSPW peers will read these, so the keys are our choice.

Peers send these URLs to each other while joining (`ICanIntroduceSelf`, `ICanAdmitPlayers`, `ICanReceiveConnectInfo`), and a peer connects to another by its URL only when a third peer joins a peer-to-peer session. So an empty URL works for two peers, as in NSPW, or for a server and its clients.

UDP behaviour stays exactly as it was.

### Datagrams over a stream
Done: RFCOMM gives a reliable byte stream, not datagrams, so `StreamDatagramTransport` adapts streams to `IDatagramTransport`:

- It frames each datagram with its length, as a big-endian 16-bit number. Longer datagrams are dropped.
- It keeps one stream per remote endpoint. The first `Send` to an endpoint opens one, and streams that remote endpoints open are accepted. Replies go back over the accepted stream.
- Datagrams sent while a stream is opening wait for it, up to 256. When a stream fails to open, fails or closes, the datagrams still waiting are lost. The reliable protocol resends them, as after packet loss, and the next `Send` opens a new stream.
- The transport doesn't know about Bluetooth. It gets its streams from an `IStreamConnector`, which opens them, accepts them, and is the Bluetooth part of each OS.

Otsuki's reliable protocol then runs on top of a stream that is already reliable. That is wasteful but harmless. It never sees loss, so it never resends. Its acknowledgements and keep-alives still detect a peer that is gone.

Enumeration needs no Bluetooth support. The game never enumerates, and Bluetooth finds peers in a different way (see the device picker under [The DirectPlay layer](#the-directplay-layer)).

## The Bluetooth connector
**Bluetooth Classic RFCOMM**, registered in SDP under a fixed OpenNSPW service UUID. The host listens on it, and a guest connects to that service on the chosen device.

| OS | API | Notes |
| --- | --- | --- |
| macOS | IOBluetooth: `IOBluetoothRFCOMMChannel`, `IOBluetoothSDPServiceRecord`, `IOBluetoothDeviceInquiry` | No .NET wrapper. Use Objective-C runtime interop from C#, or a small native shim. Its callbacks arrive on the run loop of the thread that opened the channel. The app needs `NSBluetoothAlwaysUsageDescription` in its `Info.plist`. |
| Windows | [32feet.NET](https://github.com/inthehand/32feet) (`InTheHand.Net.Bluetooth`): `BluetoothListener`, `BluetoothClient` | Winsock `AF_BTH` underneath. Registers the SDP record itself. |
| Linux | BlueZ RFCOMM sockets, or 32feet's Linux provider | 32feet has no SDP on Linux, so it connects only to a known channel. Either use a fixed channel, or register the service through BlueZ's D-Bus `ProfileManager1`. |

Why Classic rather than Bluetooth Low Energy (BLE):

- **Classic:**
  - Supported by every desktop Bluetooth adapter.
  - Both roles, listening and connecting, work on all three OSes.
  - Message size is not an issue.
  - Most OSes ask the two machines to pair first.
- **BLE:** GATT messages map more naturally to datagrams, and advertising would make finding hosts easy, but:
  - Hosting needs the peripheral role. On Windows that depends on the adapter and needs a Windows-specific target framework. On Linux it goes through BlueZ D-Bus.
  - The small ATT MTU means lowering `ReliableProfile.MaxFramePayloadSize` to fit it.

## The DirectPlay layer
- Add `CLSID_DP8SP_BLUETOOTH`, copied from the DirectX 9 SDK's `dpaddr.h`. Define it in `OpenNspw.DirectPlay` rather than the port's [dplay8.cs](../OpenNSPW/OpenNspw/dplay8.cs), which the game uses as is.
- `EnumServiceProviders` lists "DirectPlay8 Bluetooth Service Provider" only when the platform reports a Bluetooth adapter. `GetSPCaps` answers for it too.
- `Host` with the Bluetooth provider listens on RFCOMM and publishes the SDP record. It needs no prompt.
- `Connect` with the Bluetooth provider asks the platform for the host's device, through an interface given to `DirectPlay8Factory`. That is what `OKTOQUERYFORADDRESSING` lets DirectPlay's own dialogs do.
  - The desktop app implements the interface with a **device picker**: paired devices first, then devices found by a search, then the last device used.
  - `Connect` already blocks the game's thread, so a modal picker fits.

## Steps
Each step builds and is tested on its own:

1. **Generalize Otsuki's endpoints.** Done. All Otsuki and OpenNSPW network tests pass, and `NamedEndPointTests` run a session over endpoints that are not IP addresses.
2. **`StreamDatagramTransport`.** Done. `StreamDatagramTransportTests` cover framing, opening a stream on the first send, replies over an accepted stream, an endpoint that refuses, and a peer that comes back, over in-memory streams, and run a two-peer session over it.
3. **The macOS RFCOMM connector**, an `IStreamConnector`. Try it first with a small console app between two Macs.
4. **The Bluetooth service provider** in `OpenNspw.DirectPlay`, with a fake connector in tests, so that `NetworkTests` can play a game over it.
5. **The device picker** in the desktop app, and `NSBluetoothAlwaysUsageDescription`.
6. **A manual two-machine game**, run through the whole lifecycle: game end, Back, play again, close.
7. **Windows, then Linux connectors.** Test each against the macOS one.
8. **Document** pairing and Bluetooth PAN for players.

## Open questions
- **Port or evolved game.** These changes are in Otsuki and `OpenNspw.DirectPlay`, not in the ported game, and the evolved desktop app links the DirectPlay files. Doing it on `main` gives both games Bluetooth. The other option is to treat it as evolved-only.
- **macOS interop.** Pure C# Objective-C interop means no native build step but fiddly delegate classes. A Swift or Objective-C shim is simpler to write but has to be built on macOS in CI and when packaging.
- **Pairing.** Should the picker offer to pair, or leave that to the OS's Bluetooth settings?
- **Is it worth it?** Bluetooth PAN already gives the same result with no code. The native provider mainly saves players from setting up PAN, which some OS versions make awkward.
