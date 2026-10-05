namespace Aigamo.Otsuki.Reliable.Transitions;

internal interface IConnectionTransition
{
	/// <summary>
	/// The state that implements this transition, for transitions that stay in it.
	/// </summary>
	ConnectionState Current => (ConnectionState)this;
}

/// <summary>
/// Handles <typeparamref name="TCommand"/> in a state that allows it and returns the next state.
/// </summary>
internal interface IConnectionTransition<TCommand> : IConnectionTransition
{
	ConnectionState Execute(Connection connection, TCommand command);
}
