using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Reliable.Transitions;
using Aigamo.Otsuki.Transport;
using Aigamo.Results;

namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// One connection of the DirectPlay 8 reliable protocol ([MC-DPL8R]) to a single remote endpoint. The protocol
/// logic lives in the transitions that each <see cref="ConnectionState"/> implements; this actor owns the state,
/// the timers and the transport, and runs the transition for each message.
/// Packet signing and coalesced sends are not implemented; coalesced payloads are understood on receipt.
/// </summary>
internal sealed class Connection : Actor<ConnectionMessage>
{
	public const int ProtocolVersion = 0x00010006;

	private readonly IDatagramTransport _transport;
	private readonly Action<Connection> _onTerminated;
	private ConnectionState _state;

	public IPEndPoint RemoteEndPoint { get; }

	public bool IsOutbound { get; }

	public TimeProvider TimeProvider { get; }

	public ReliableProfile Profile { get; }

	public IConnectionObserver Observer { get; }

	public ActorTimer<ConnectionMessage> ConnectRetryTimer { get; }

	public ActorTimer<ConnectionMessage> RetryTimer { get; }

	public ActorTimer<ConnectionMessage> DelayedAcknowledgmentTimer { get; }

	public ActorTimer<ConnectionMessage> DelayedSendMaskTimer { get; }

	public ActorTimer<ConnectionMessage> KeepAliveTimer { get; }

	public ActorTimer<ConnectionMessage> HardDisconnectTimer { get; }

	public ActorTimer<ConnectionMessage> GracefulDisconnectTimer { get; }

	public Connection(
		IPEndPoint remoteEndPoint,
		bool isOutbound,
		IDatagramTransport transport,
		TimeProvider timeProvider,
		ReliableProfile profile,
		IConnectionObserver observer,
		Action<Connection> onTerminated
	)
	{
		RemoteEndPoint = remoteEndPoint;
		IsOutbound = isOutbound;
		_transport = transport;
		TimeProvider = timeProvider;
		Profile = profile;
		Observer = observer;
		_onTerminated = onTerminated;
		ConnectRetryTimer = new(
			timeProvider,
			generation => new ConnectionMessage.ConnectRetryTimerElapsed(generation),
			Post
		);
		RetryTimer = new(
			timeProvider,
			generation => new ConnectionMessage.RetryTimerElapsed(generation),
			Post
		);
		DelayedAcknowledgmentTimer = new(
			timeProvider,
			generation => new ConnectionMessage.DelayedAcknowledgmentTimerElapsed(generation),
			Post
		);
		DelayedSendMaskTimer = new(
			timeProvider,
			generation => new ConnectionMessage.DelayedSendMaskTimerElapsed(generation),
			Post
		);
		KeepAliveTimer = new(
			timeProvider,
			generation => new ConnectionMessage.KeepAliveTimerElapsed(generation),
			Post
		);
		HardDisconnectTimer = new(
			timeProvider,
			generation => new ConnectionMessage.HardDisconnectTimerElapsed(generation),
			Post
		);
		GracefulDisconnectTimer = new(
			timeProvider,
			generation => new ConnectionMessage.GracefulDisconnectTimerElapsed(generation),
			Post
		);
		_state = isOutbound ? new ConnectionState.Idle() : new ConnectionState.Listening();
		Start();
	}

	public override string ToString() =>
		$"{nameof(Connection)}({RemoteEndPoint}, {(IsOutbound ? "outbound" : "inbound")})";

	public int Timestamp =>
		unchecked(
			(int)(TimeProvider.GetTimestamp() / Math.Max(1, TimeProvider.TimestampFrequency / 1000))
		);

	public void Transmit(ReliableMessage message) =>
		_transport.Send(RemoteEndPoint, ReliableMessageSerializer.Default.Serialize(message));

	/// <summary>
	/// Runs the transition of <paramref name="state"/> for <paramref name="command"/>, if it has one.
	/// </summary>
	public ConnectionState Apply<TCommand>(ConnectionState state, TCommand command) =>
		state is IConnectionTransition<TCommand> transition
			? transition.Execute(this, command)
			: state;

	public void CancelChannelTimers()
	{
		RetryTimer.Cancel();
		DelayedAcknowledgmentTimer.Cancel();
		DelayedSendMaskTimer.Cancel();
		KeepAliveTimer.Cancel();
		GracefulDisconnectTimer.Cancel();
	}

	public ConnectionState Terminate()
	{
		ConnectRetryTimer.Cancel();
		HardDisconnectTimer.Cancel();
		CancelChannelTimers();
		_onTerminated(this);
		Stop();
		return new ConnectionState.Closed();
	}

	public ConnectionState GiveUpConnecting()
	{
		if (IsOutbound)
			Observer.OnConnectFailed(this, ResultCode.NoResponse);
		return Terminate();
	}

	public ConnectionState Establish(Handshake handshake)
	{
		ConnectRetryTimer.Cancel();
		KeepAliveTimer.Start(Profile.KeepAliveInterval);
		var channel = new ReliableChannel(this, handshake);
		Observer.OnConnected(this);

		// Measures the round-trip time and, for the listener, confirms the connection ([MC-DPL8R] section 4.1).
		channel.SendKeepAlive();
		return new ConnectionState.Established(handshake, channel);
	}

	/// <summary>
	/// Ends an established connection and reports it to the upper layer. <paramref name="result"/> is
	/// <see cref="ResultCode.Success"/> when the partner disconnected deliberately.
	/// </summary>
	public ConnectionState Close(ReliableChannel channel, ResultCode result)
	{
		channel.FailAllSends(result == ResultCode.Success ? ResultCode.ConnectionLost : result);
		Observer.OnDisconnected(this, result);
		return Terminate();
	}

	/// <summary>
	/// Stays in <paramref name="state"/> while the channel is open, and closes the connection otherwise.
	/// </summary>
	public ConnectionState Settle(
		ConnectionState state,
		ReliableChannel channel,
		ChannelStatus status
	) =>
		status.Match(
			Open: () => state,
			Disconnected: () => Close(channel, ResultCode.Success),
			Lost: () => Close(channel, ResultCode.ConnectionLost)
		);

	private Unit Execute<TCommand>(TCommand command)
	{
		_state = Apply(_state, command);
		return Unit.Default;
	}

	private Unit Elapse<TCommand>(
		ActorTimer<ConnectionMessage> timer,
		long generation,
		TCommand command
	) => timer.TryConsume(generation) ? Execute(command) : Unit.Default;

	private Unit SendPayload(ConnectionMessage.SendPayload command)
	{
		if (_state is IConnectionTransition<ConnectionMessage.SendPayload>)
			return Execute(command);

		Observer.OnSendCompleted(this, command.SendId, ResultCode.NoConnection);
		return Unit.Default;
	}

	/// <summary>
	/// Terminates in any state without telling the partner.
	/// </summary>
	private Unit Abort(ResultCode result)
	{
		_state = _state.Match(
			Idle: _ =>
			{
				Observer.OnConnectFailed(this, result);
				return Terminate();
			},
			Listening: _ => Terminate(),
			ConnectSent: _ =>
			{
				Observer.OnConnectFailed(this, result);
				return Terminate();
			},
			ConnectReceived: _ => Terminate(),
			Established: established => Close(established.Channel, result),
			HardDisconnecting: _ =>
			{
				Observer.OnDisconnected(this, result);
				return Terminate();
			},
			Closed: closed => closed
		);
		return Unit.Default;
	}

	private Unit Receive(ReliableMessage message) =>
		message.Match(
			DataFrame: Execute,
			Connect: Execute,
			Connected: Execute,
			HardDisconnect: Execute,
			Sack: Execute
		);

	protected override void Receive(ConnectionMessage message)
	{
		if (_state is ConnectionState.Closed)
			return;

		message.Match(
			StartConnecting: Execute,
			DatagramArrived: m => Receive(m.Message),
			SendPayload: SendPayload,
			Disconnect: Execute,
			HardDisconnect: Execute,
			Abort: m => Abort(m.Result),
			ConnectRetryTimerElapsed: m => Elapse(ConnectRetryTimer, m.Generation, m),
			RetryTimerElapsed: m => Elapse(RetryTimer, m.Generation, m),
			DelayedAcknowledgmentTimerElapsed: m =>
				Elapse(DelayedAcknowledgmentTimer, m.Generation, m),
			DelayedSendMaskTimerElapsed: m => Elapse(DelayedSendMaskTimer, m.Generation, m),
			KeepAliveTimerElapsed: m => Elapse(KeepAliveTimer, m.Generation, m),
			HardDisconnectTimerElapsed: m => Elapse(HardDisconnectTimer, m.Generation, m),
			GracefulDisconnectTimerElapsed: m => Elapse(GracefulDisconnectTimer, m.Generation, m)
		);
	}
}
