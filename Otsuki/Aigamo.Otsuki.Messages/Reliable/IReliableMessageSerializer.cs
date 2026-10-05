namespace Aigamo.Otsuki.Messages.Reliable;

public interface IReliableMessageSerializer<TMessage>
	where TMessage : ReliableMessage
{
	TMessage? Deserialize(byte[] data);

	byte[] Serialize(TMessage message);
}
