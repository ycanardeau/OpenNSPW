# Bluetooth
A proposal for letting two copies of OpenNSPW play over Bluetooth, as a DirectPlay service provider next to TCP/IP.

This is a proposal, not a final design. [Open questions](#open-questions) lists what still needs to be decided.

## Summary
- Bluetooth play is **OpenNSPW against OpenNSPW only**. Microsoft's DirectPlay 8 Bluetooth service provider only ever shipped for Pocket PC 2002, never for desktop Windows, so NSPW 1.22 and NSPW NET never had one to play over.
- The **ported game code does not change**. The connection dialog already handles a service provider it does not know (see [The game side](#the-game-side)).
- **Otsuki** has general endpoints instead of IPv4 ones, and a transport that carries its datagrams over any reliable stream.
- A new **Bluetooth Classic RFCOMM** connector supplies those streams, with code of its own for each OS: macOS and Windows so far.
- **On macOS 26, a Mac can host but not join.** Its IOBluetooth delivers callbacks on the main thread, never completes SDP queries, and doesn't open channels to a computer (see [macOS 26](#macos-26)).
- **The desktop app doesn't offer Bluetooth yet.** Its choice of Bluetooth in the multiplayer form is still in progress (see [The desktop app](#the-desktop-app)).
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
- `BluetoothEndPoint` (in `Aigamo.Otsuki.Bluetooth`) holds only the device address (`BD_ADDR`), with value equality for the connection table. A device runs an application's service once, and the connector looks up the RFCOMM channel of the service with SDP whenever it opens a stream, as Apple recommends, because channel numbers can change.

`DirectPlayUrl` maps each endpoint type to its provider:

- TCP/IP stays as it is.
- Other endpoints get an empty URL for now.
- Bluetooth will use `provider={CLSID_DP8SP_BLUETOOTH};hostname=<BD_ADDR>`. No documented DirectPlay URL format for Bluetooth exists, and only OpenNSPW peers will read these, so the keys are our choice.

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
**Bluetooth Classic RFCOMM**, registered in SDP under a UUID of the application's own. The host listens on it, and a guest connects to that service on the chosen device.

On macOS, `Aigamo.Otsuki.Bluetooth` does this through a small Objective-C library, `Native/OtsukiBluetooth.m`. The project builds it with `clang` on macOS only, for Apple silicon and Intel, and copies `libOtsukiBluetooth.dylib` next to the assembly.

- The library runs a thread with a run loop of its own, because IOBluetooth delivers callbacks on the run loop of the thread that asked for them. Every IOBluetooth call goes through that thread. On macOS 26, callbacks come on the main thread instead, and the library hands them on to its thread.
- Its C interface creates and destroys the host, publishes the service and accepts channels, opens a channel to a device's service, and writes and closes channels. Results and data come back through four callbacks.
- `MacBluetoothConnector` turns that into an `IStreamConnector`, and `BluetoothTransport.CreateFactory` into a `PeerOptions.TransportFactory`.
- The project isn't packable: a package built on Linux in CI would lack the library.
- With `OTSUKI_BLUETOOTH_LOG=1`, the library logs each step to standard error: publishing, SDP queries, channels opening, data received, writes that fail, and channels closing.

**macOS kills a process that touches Bluetooth without permission.** The app responsible for the process must declare `NSBluetoothAlwaysUsageDescription` in its `Info.plist`, or the first IOBluetooth call ends the process, and no exception can catch it. So:

- The console chat (`Aigamo.Otsuki.ConsoleApp host --bluetooth`, `join <address> --bluetooth`) only works from a terminal app that may use Bluetooth. Run from an app without the key, such as the Claude app, it was killed when it first asked IOBluetooth for the computer's controller. The crash report confirmed that the library and its callbacks had been called correctly up to that point.
- The game must not touch IOBluetooth unless its app declares the key. The service provider must not even look for an adapter otherwise, and `dotnet run` builds of the desktop app depend on the terminal they run from.

### macOS 26
On macOS 26, IOBluetooth is a shim over CoreBluetooth's classic manager. Its log says "Loading Shim Coordinator", and requests go to `bluetoothd` as CoreBluetooth messages. Checked on macOS 26.6.2 with a MacBook (BCM4387) and the Windows 11 computer below, paired. Both sides ran the console chat, the Mac's from an app bundle that declares `NSBluetoothAlwaysUsageDescription`, launched with `open`.

- **Callbacks come on the main thread.** The shim calls the delegates and notifications on the main thread's run loop, whichever thread asked. A .NET app's main thread runs none, so no channel opened, accepted or received anything at first. `bluetoothd` logs "No sessions found saving incoming connection info" for every incoming channel. The shim claims the channel only once its main-thread callback runs. Other projects report the same ([webbluetooth](https://github.com/eugenehp/webbluetooth/releases/tag/v0.0.4), [Special_audio](https://github.com/PushkarX-Code/Special_audio/pull/14)).
  - So the library hands callbacks on to its own thread, and `BluetoothTransport.RunMainLoopUntil` turns the main thread's run loop, for apps whose main thread has none. The console chat runs on the thread pool and calls it.
  - With that and a random channel (below), a Windows guest joined the chat on the Mac, both sides chatted, and the guest left cleanly, with two hosts in a row.
- **SDP queries never complete.** `performSDPQuery:` returns success, but the shim logs "**** This currently won't trigger SDP delegate", and `sdpQueryComplete:status:` is never called, even with the main run loop turning. [Others see it on macOS 15.5](https://developer.apple.com/forums/thread/785107). The device's cached SDP records do hold the host's service and channel, from `bluetoothd`'s own queries.
- **Channels to a computer aren't opened.** Opening the cached record's channel with the main run loop turning, `openRFCOMMChannelAsync:` returns success, and the shim first asks CoreBluetooth to connect the device ("Baseband connecting"). `bluetoothd` brings the link up and tries audio profiles such as AVRCP and A2DP, which a computer refuses. The shim never counts the device as connected, never opens the channel, and logs "Trying to close a channel that isn't open" after about 25 seconds. `openConnection` first times out the same way. When the computer had connected to the Mac first, the shim opened a channel to it at once.
- **The last incoming channel stays behind.** `bluetoothd` keeps the last incoming channel of each channel number after it closes, and hands it to the next registration on that number. That registration fails to accept it ("RFCOMM_Accept failed with status 916") and then refuses the channels that remote devices open ("STATUS 121"). The record is used up by that failure, so the registration after it works. A second registration on the same number doesn't help: `bluetoothd` then accepts no channel on that number at all, and unregistering doesn't undo that. So the library publishes its service on a random channel, which avoids the last one used 29 times in 30.

So on macOS 26 a Mac can host, but not join. `BluetoothTransport.CanConnect` is false there, and the desktop app is to offer no Bluetooth then, since its form offers joining too. The console chat still tries joining, to check later versions. Only 26.6.2 was checked.

On Windows, `WindowsBluetoothConnector` uses Winsock's RFCOMM sockets (`AF_BTH`, `BTHPROTO_RFCOMM`) through .NET's `Socket`, with a `SOCKADDR_BTH` endpoint of its own:

- **Hosting:** it listens on a channel that Windows chooses (`BT_PORT_ANY`), and publishes it with `WSASetService` under the service's UUID and name. Windows builds the SDP record from them. Disposing removes the record.
- **Connecting:** it gives Windows the device and the service's UUID without a channel, and Windows looks the channel up with SDP, as on macOS. `Socket.ConnectAsync` needs the socket bound first, which .NET does only for IP sockets, so the connector binds it to channel 0, which takes no channel.
- **The controller and paired devices** come from `BluetoothFindFirstRadio` and `BluetoothFindFirstDevice`. With no controller, or with it off, `GetLocalEndPoint` throws `IOException` and there are no paired devices.
- **Permission:** a desktop app needs none, so `BluetoothTransport.IsAllowed` is always true on Windows. On macOS it is `IsUsageDescribed`.
- **Checked** on a Windows 11 computer with an Intel adapter: reading its address, listening and publishing the service, publishing again after disposing, and opening a channel to a device that isn't there, which fails after about 5 seconds, or at once when cancelled or disposed. Accepting stays pending until a device connects, and disposing ends it. No other device was at hand, so no channel was opened or accepted.

| OS | API | Notes |
| --- | --- | --- |
| macOS | IOBluetooth: `IOBluetoothRFCOMMChannel`, `IOBluetoothSDPServiceRecord`, `IOBluetoothDevice` | Through the Objective-C library above. The app needs `NSBluetoothAlwaysUsageDescription` in its `Info.plist`. On macOS 26, it can only host, with the main run loop turning (see [macOS 26](#macos-26)). |
| Windows | Winsock `AF_BTH` sockets, `WSASetService`, and the Bluetooth APIs (`bluetoothapis.dll`) | Through P/Invoke, with no native code or package of our own. Desktop apps need no declaration. |
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
Done:

- **The GUID:** `DirectPlay8Peer.CLSID_DP8SP_BLUETOOTH` is {995513AF-3027-4B9A-956E-C772B3F78006}, as Wine's `dplay8.h` defines it. It lives in `OpenNspw.DirectPlay`, not in the port's [dplay8.cs](../OpenNSPW/OpenNspw/dplay8.cs), which the game uses as is.
- **Turning it on:** the platform gives `DirectPlay8Factory` an `IBluetoothProvider`, which creates the transport of a session and asks the player for the host's device. Without one, nothing changes: the dialog lists only TCP/IP. A platform must give one only where the app may use Bluetooth (see [The Bluetooth connector](#the-bluetooth-connector)). The desktop app doesn't give one yet.
- **Listing it:** with a provider, `EnumServiceProviders` lists "DirectPlay8 Bluetooth Service Provider" after TCP/IP, and `GetSPCaps` answers for it as it does for TCP/IP: no adapters of its own, and all adapters supported.
- **Hosting:** `Host` with the Bluetooth provider hosts from `BluetoothEndPoint.Any`. The peer's transport factory sends sessions from Bluetooth endpoints to the provider, and all others to UDP.
- **Joining:** `Connect` with the Bluetooth provider takes the host from the address's host name. The game gives none, so with `DPNCONNECT_OKTOQUERYFORADDRESSING` it calls `IBluetoothProvider.ChooseHost`, which shows the device picker.
  - If the player cancels, `Connect` returns `DPNERR_USERCANCEL`, and the game says "Failed to launch game." and stays in the dialog.
  - Without the flag, it returns `DPNERR_ADDRESSING`.
  - `Connect` already blocks the game's thread, so a modal picker fits.
- **`LocalEndPoint`:** `DirectPlay8Peer.LocalEndPoint` is an `EndPoint` now, which is an `IPEndPoint` over TCP/IP.
- **Tests:** `NetworkTests` play through the connection dialogs over `LoopbackBluetooth`, a provider on the in-memory streams of Otsuki's tests, from the host's click on OK to the start of the game. They also check that only TCP/IP is listed without a provider, and that a guest who chooses no device stays in the dialog.

## The desktop app
In progress, written and checked with a fake Bluetooth:

- **When it offers Bluetooth:** `DesktopBluetooth` is the app's `IBluetoothProvider`. The app creates one only when it can join over Bluetooth (`BluetoothTransport.CanConnect`, false from macOS 26 on) and may use it (`BluetoothTransport.IsAllowed`): always on Windows, and on macOS when its `Info.plist` declares `NSBluetoothAlwaysUsageDescription`, which `tools/package-macos-app.sh` is to write into OpenNSPW.app. Checking reads the bundle and touches no Bluetooth. `OPENNSPW_BLUETOOTH=1` turns it on anyway: for a build run from a terminal that may use Bluetooth, or to try a later macOS.
- **The service:** hosts publish the game's application GUID (`g_guidApp`) as their service's UUID, under the name "OpenNSPW".
- **The form:** where Bluetooth is offered, the multiplayer form chooses between the Internet and Bluetooth, the dialog's two service providers, below the choice between hosting and joining.
  - Over Bluetooth, a host sees the computer's own address, to tell the guest.
  - A guest types the host's address, or picks one of up to four paired devices. The last one joined is remembered in `ui.json`.
  - The form gives the device to `DesktopBluetooth` before clicking OK, so `ChooseHost` answers with it instead of asking. That is the device picker.
  - An address that isn't one is reported under the form, and nothing is sent to the game.
  - The form touches Bluetooth only once Bluetooth is chosen, since the first call may ask the user for permission.
- **The dialog remembers the provider:** the game saves the provider used last ("Preferred Provider"), as the original does, so the form opens on Bluetooth after a game over Bluetooth. The form reads it from the dialog once the dialog has handled `WM_INITDIALOG`.
- **Long names:** an `ActionButton` cuts text too wide for it with an ellipsis, for long device names.
- **Checked:** with a fake Bluetooth and scripted clicks, the form was checked through switching between the Internet and Bluetooth, hosting and joining, a wrong address, a paired device, Back while connecting, a second visit, and hosting into the waiting dialog. Nothing went over real Bluetooth.

## Steps
Each step builds and is tested on its own:

1. **Generalize Otsuki's endpoints.** Done. All Otsuki and OpenNSPW network tests pass, and `NamedEndPointTests` run a session over endpoints that are not IP addresses.
2. **`StreamDatagramTransport`.** Done. `StreamDatagramTransportTests` cover framing, opening a stream on the first send, replies over an accepted stream, an endpoint that refuses, and a peer that comes back, over in-memory streams, and run a two-peer session over it.
3. **The macOS RFCOMM connector**, an `IStreamConnector`. Built and unit-tested, except for the Bluetooth itself. On macOS 26.6.2, a Windows guest chatted with a Mac host, but the Mac can't join (see [macOS 26](#macos-26)). Still to do: chat on a macOS before 26, and a desktop-app host on macOS 26. Also found there:
   - The connector publishes the service even for a guest. So two chats on one Mac both register the same channel, and when one goes, the other may stop receiving channels.
   - After a failed join, the console chat hung on exit and had to be killed.
4. **The Bluetooth service provider** in `OpenNspw.DirectPlay`. Done, with a loopback provider in `NetworkTests`.
5. **The desktop app's provider**: the Bluetooth choice and the device picker in the multiplayer form, `NSBluetoothAlwaysUsageDescription` in the app's `Info.plist`, and a provider given only when the app has that key. In progress: written and checked with a fake Bluetooth.
6. **A manual two-machine game**, run through the whole lifecycle: game end, Back, play again, close.
7. **Windows, then Linux connectors.** Test each against the macOS one. The Windows connector is built and checked on one computer (see [The Bluetooth connector](#the-bluetooth-connector)). Windows joined a chat hosted on a Mac on macOS 26.6.2: joining, chatting both ways and leaving all worked. Still to do: play between two computers, Windows with Windows and Windows with a Mac.
8. **Document** pairing and Bluetooth PAN for players.

## Open questions
- **Port or evolved game.** These changes are in Otsuki and `OpenNspw.DirectPlay`, not in the ported game, and the evolved desktop app links the DirectPlay files. Doing it on `main` gives both games Bluetooth. The other option is to treat it as evolved-only.
- **macOS interop.** Decided: an Objective-C library, built by the project on macOS. C# interop would need no native build, but would have to create Objective-C delegate classes and blocks at runtime. Releases already package the macOS app on a macOS runner.
- **Pairing.** The form lists only paired devices and takes any address typed. Pairing is left to the OS's Bluetooth settings. Should the form also search for devices nearby?
- **Is it worth it?** Bluetooth PAN already gives the same result with no code. The native provider mainly saves players from setting up PAN, which some OS versions make awkward.
