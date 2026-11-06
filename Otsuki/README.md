# Otsuki

**DirectPlay 8 for modern .NET.** Otsuki speaks the wire protocol of DirectX 8 and 9 DirectPlay over UDP: host a session, find it, join it and message every peer, with `async`/`await`, on any OS, without Windows or COM.

[![NuGet](https://img.shields.io/nuget/dt/Aigamo.Otsuki?label=NuGet&logo=nuget)](https://www.nuget.org/packages/Aigamo.Otsuki)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

Otsuki is the network of [OpenNSPW](../OpenNSPW), whose DirectPlay calls ([OpenNspw.DirectPlay](../OpenNSPW/OpenNspw.DirectPlay)) it carries.

```csharp
await using var peer = new Peer();
await peer.SetPeerInfo("Bob");
await peer.ConnectAsync(application, "192.168.0.10");

await foreach (var e in peer.ReadEventsAsync())
{
    if (e is PeerEvent.DataReceived received)
        Console.WriteLine($"{peer.GetPeerInfo(received.Sender)?.Name}: {Encoding.UTF8.GetString(received.Data.AsSpan())}");
}
```

## Features

- **The whole protocol.** Peer-to-peer meshes and client/server sessions, host enumeration, groups, player information, guaranteed and unreliable messages, messages larger than a frame, host migration, and fast or full packet signing.
- **The familiar API.** `Peer`, `Server` and `Client` stand for `IDirectPlay8Peer`, `IDirectPlay8Server` and `IDirectPlay8Client`. Read events once a frame with `DoWork`, as in a game loop, or stream them with `await foreach`.
- **No locks, by construction.** Every piece of protocol state belongs to one actor, which handles one message at a time. An analyzer reports a lock as a build error.
- **A sans-I/O core.** The protocol itself is state machines with no threads, timers, clocks or sockets, which the compiler keeps so: a race inside the core is code that does not build.
- **Tested hard.** Lossy, reordering and partitioned networks; replays of the specification's sample packets byte for byte; and whole sessions in a hundred seeded orders each, where a failing seed repeats.
- **Scales.** One server held 10,000 clients on 0.7 of a core, at a message and an update a second each ([measurements](../docs/OtsukiThreading.md#measurements)).
- **Any transport.** UDP by default, or any reliable stream, such as Bluetooth RFCOMM channels.

## Installation

```xml
<PackageReference Include="Aigamo.Otsuki" Version="0.0.1-alpha.*" />
```

## Quick start

A host greets each player who joins:

```csharp
using System.Text;
using Aigamo.Otsuki;
using Aigamo.Otsuki.Messages.Core;

var application = new ApplicationDescription { GuidApplication = guidApp, SessionName = "Lobby", MaxPlayers = 4 };

await using var host = new Peer();
await host.SetPeerInfo("Alice");
await host.HostAsync(application);

await foreach (var e in host.ReadEventsAsync())
{
    if (e is PeerEvent.PlayerCreated { Player: { IsLocal: false } player })
        host.SendTo(player.Id, Encoding.UTF8.GetBytes($"Welcome, {player.Name}!"));
}
```

The peer that connects, as above, prints `Alice: Welcome, Bob!`. To find hosts on the network first, use `EnumHostsAsync`. For groups, host migration, client/server sessions, signing and the design, see [Aigamo.Otsuki's README](Aigamo.Otsuki/README.md).

[Aigamo.Otsuki.ConsoleApp](Aigamo.Otsuki.ConsoleApp) is a complete chat: one peer hosts, the others join, and the oldest peer takes over when the host leaves.

```bash
dotnet run --project Otsuki/Aigamo.Otsuki.ConsoleApp -- host --name Alice
```

```bash
dotnet run --project Otsuki/Aigamo.Otsuki.ConsoleApp -- join 127.0.0.1 --name Bob
```

## Projects

| Project | What it is |
| --- | --- |
| [Aigamo.Otsuki](Aigamo.Otsuki) | The peer, server and client, which run the core on actors. See [its README](Aigamo.Otsuki/README.md) for usage and the design. |
| [Aigamo.Otsuki.Core](Aigamo.Otsuki.Core) | The protocol as state machines with no threads, timers or I/O, which an analyzer keeps so. |
| [Aigamo.Otsuki.Messages](Aigamo.Otsuki.Messages) | The messages of the protocols and their serializers. |
| [Aigamo.Otsuki.Actors](Aigamo.Otsuki.Actors) | Actors and their mailboxes: the only concurrent code of Otsuki and of what is built on it. |
| [Aigamo.Otsuki.Analyzers](Aigamo.Otsuki.Analyzers) | The analyzers that report locks, shared state and, in the core, threads, clocks and I/O as errors. |
| [Aigamo.Otsuki.Bluetooth](Aigamo.Otsuki.Bluetooth) | Sessions over Bluetooth RFCOMM channels, on macOS and Windows ([docs/Bluetooth.md](../docs/Bluetooth.md)). |
| [Aigamo.Otsuki.ConsoleApp](Aigamo.Otsuki.ConsoleApp) | A chat that demonstrates the peer. |
| [Aigamo.Otsuki.LoadTest](Aigamo.Otsuki.LoadTest) | A server and processes of clients over UDP, which report throughput, latency, CPU and garbage each second. |

## Specifications

- [[MC-DPL8R]: DirectPlay 8 Protocol: Reliable](https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/7a35d96c-daca-4311-bc2b-bd6a2f50bf14): connections, sequencing, acknowledgments and retries on top of UDP.
- [[MC-DPL8CS]: DirectPlay 8 Protocol: Core and Service Providers](https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/2968b3eb-a248-4281-b718-8a7d55fd9b36): sessions, players and the name table.
- [[MC-DPLHP]: DirectPlay 8 Protocol: Host and Port Enumeration](https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-dplhp/ad5042e7-f46f-4ead-ba45-54b833b1a279): finding hosts.

Otsuki is written from these specifications and has not been tested against Microsoft's DirectPlay yet. [Limitations](Aigamo.Otsuki/README.md#limitations) lists where it interprets them.

## Further reading

- [docs/OtsukiThreading.md](../docs/OtsukiThreading.md): why actors still race, how the core was split out, and the measurements.
- [docs/Actors.md](../docs/Actors.md): the actors and the rules that keep them sound.

## License

[MIT](LICENSE)
