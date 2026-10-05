namespace Aigamo.Otsuki.Session.Transitions;

internal interface ISessionTransition
{
	/// <summary>
	/// The state that implements this transition, for transitions that stay in it.
	/// </summary>
	SessionState Current => (SessionState)this;
}

/// <summary>
/// Handles <typeparamref name="TCommand"/> in a state that allows it and returns the next state.
/// </summary>
internal interface ISessionTransition<TCommand> : ISessionTransition
{
	SessionState Execute(SessionActor session, TCommand command);
}
