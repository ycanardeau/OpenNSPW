using System.Diagnostics;

namespace Aigamo.Otsuki.Actors;

/// <summary>
/// An actor owns its state exclusively and processes the messages in its mailbox one at a time, on the thread pool,
/// so the state never needs locks. Other threads interact with an actor only by posting messages.
/// </summary>
public abstract class Actor<TMessage>
	where TMessage : notnull
{
	private readonly Mailbox<TMessage> _mailbox;

	protected Actor(int capacity = int.MaxValue, MailboxScheduler? scheduler = null)
	{
		_mailbox = new Mailbox<TMessage>(Receive, OnError, capacity, scheduler);
	}

	/// <summary>
	/// How many messages wait in the mailbox, from any thread: a reading for diagnostics.
	/// </summary>
	public int QueueLength => _mailbox.Count;

	/// <summary>
	/// Completes when the actor has stopped and drained its mailbox.
	/// </summary>
	public Task Completion => _mailbox.Completion;

	/// <summary>
	/// Handles a single message. Only ever called from the actor's own message loop.
	/// </summary>
	protected abstract void Receive(TMessage message);

	protected virtual void OnError(Exception exception) =>
		Trace.TraceError($"{GetType().Name}: {exception}");

	/// <summary>
	/// Starts the message loop. Derived classes call this at the end of their constructor,
	/// once all of their fields are initialized.
	/// </summary>
	protected void Start() => _mailbox.Start();

	/// <summary>
	/// Stops accepting messages. Messages that are already in the mailbox are still processed.
	/// </summary>
	protected void Stop() => _mailbox.Complete();

	/// <summary>
	/// Enqueues a message. Returns <see langword="false"/> if the actor has stopped, or its mailbox is full.
	/// </summary>
	public bool Post(TMessage message) => _mailbox.Post(message);
}
