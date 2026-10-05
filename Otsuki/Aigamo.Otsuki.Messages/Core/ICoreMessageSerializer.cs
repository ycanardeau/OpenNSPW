namespace Aigamo.Otsuki.Messages.Core;

public interface ICoreMessageSerializer<TMessage>
	where TMessage : CoreMessage
{
	TMessage? Deserialize(byte[] data);

	byte[] Serialize(TMessage message);
}
