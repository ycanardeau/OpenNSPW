namespace Aigamo.Otsuki.Session;

/// <summary>
/// What the caller of a command waits on, which the core cannot complete: it only names it in an answer
/// (<see cref="ISessionOutputs.Complete(Completion)"/>), given once the input is done and its snapshot published, and
/// the shell completes it.
/// </summary>
/// <param name="waiter">The shell's own, which the core does not look into.</param>
internal sealed class Completion(object waiter)
{
	public object Waiter { get; } = waiter;
}

/// <summary>
/// What the caller of a command that returns a <typeparamref name="T"/> waits on (<see cref="Completion"/>).
/// </summary>
internal sealed class Completion<T>(object waiter)
{
	public object Waiter { get; } = waiter;
}
