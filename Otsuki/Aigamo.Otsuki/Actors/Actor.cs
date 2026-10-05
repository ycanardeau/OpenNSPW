using System.Diagnostics;
using System.Threading.Channels;

namespace Aigamo.Otsuki.Actors;

/// <summary>
/// An actor owns its state exclusively and processes the messages in its mailbox one at a time,
/// so the state never needs locks. Other threads interact with an actor only by posting messages.
/// </summary>
public abstract class Actor<TMessage>
	where TMessage : notnull
{
	private readonly Channel<TMessage> _mailbox = Channel.CreateUnbounded<TMessage>(
		new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
	);

	private Task _completion = Task.CompletedTask;

	/// <summary>
	/// Completes when the actor has stopped and drained its mailbox.
	/// </summary>
	public Task Completion => _completion;

	/// <summary>
	/// Handles a single message. Only ever called from the actor's own message loop.
	/// </summary>
	protected abstract void Receive(TMessage message);

	protected virtual void OnError(Exception exception) =>
		Trace.TraceError($"{GetType().Name}: {exception}");

	private async Task RunAsync()
	{
		await foreach (var message in _mailbox.Reader.ReadAllAsync().ConfigureAwait(false))
		{
			try
			{
				Receive(message);
			}
			catch (Exception exception)
			{
				OnError(exception);
			}
		}
	}

	/// <summary>
	/// Starts the message loop. Derived classes call this at the end of their constructor,
	/// once all of their fields are initialized.
	/// </summary>
	protected void Start() => _completion = Task.Run(RunAsync);

	/// <summary>
	/// Stops accepting messages. Messages that are already in the mailbox are still processed.
	/// </summary>
	protected void Stop() => _mailbox.Writer.TryComplete();

	/// <summary>
	/// Enqueues a message. Returns <see langword="false"/> if the actor has stopped.
	/// </summary>
	public bool Post(TMessage message) => _mailbox.Writer.TryWrite(message);
}
