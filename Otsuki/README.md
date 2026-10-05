# Otsuki

## References
* [[MC-DPL8R]: DirectPlay 8 Protocol: Reliable](https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/7a35d96c-daca-4311-bc2b-bd6a2f50bf14)
* [[MC-DPL8CS]: DirectPlay 8 Protocol: Core and Service Providers](https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/2968b3eb-a248-4281-b718-8a7d55fd9b36)

## Projects

* [Aigamo.Otsuki.Messages](Aigamo.Otsuki.Messages) – the messages of both protocols and their serializers.
* [Aigamo.Otsuki](Aigamo.Otsuki) – a DirectPlay 8 peer built on those messages and on the actor model. See [its README](Aigamo.Otsuki/README.md) for usage and the design.
* [Aigamo.Otsuki.ConsoleApp](Aigamo.Otsuki.ConsoleApp) – a chat that demonstrates the peer.
