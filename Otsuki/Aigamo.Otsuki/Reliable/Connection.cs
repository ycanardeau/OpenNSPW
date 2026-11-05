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
/// logic lives in the transitions that each <see cref="ConnectionState"/> implements; the connection owns the state
/// and the timers, and runs the transition for each message. It runs on its <see cref="ConnectionLoop"/>, with the
/// other connections of that loop, one message at a time: its messages go to the loop's mailbox.
/// </summary>
internal sealed class Connection
{
	public const int ProtocolVersion = 0x00010006;

	/// <summary>
	/// The loop that runs the connection.
	/// </summary>
	public ConnectionLoop Loop { get; }

	private readonly IDatagramTransport _transport;
	private readonly Action<Connection> _onTerminated;
	private ConnectionState _state;
	private bool _flushRequested;

	public EndPoint RemoteEndPoint { get; }

	public bool IsOutbound { get; }

	/// <summary>
	/// The signing mode of the session, once known: inbound connections offer it, and outbound ones accept only it,
	/// so that a listener cannot talk a signed session into an unsigned connection. <see cref="PacketSigning.None"/>
	/// before then, when an outbound connection takes whatever the listener offers.
	/// </summary>
	public PacketSigning OfferedSigning { get; }

	/// <summary>
	/// Signs our frames and validates the partner's once the connection is established with signing.
	/// </summary>
	public PacketSigner? Signer { get; private set; }

	public TimeProvider TimeProvider { get; }

	public ReliableProfile Profile { get; }

	public IConnectionObserver Observer { get; }

	public LoopTimer ConnectRetryTimer { get; }

	public LoopTimer RetryTimer { get; }

	public LoopTimer DelayedAcknowledgmentTimer { get; }

	public LoopTimer DelayedSendMaskTimer { get; }

	public LoopTimer KeepAliveTimer { get; }

	public LoopTimer HardDisconnectTimer { get; }

	public LoopTimer GracefulDisconnectTimer { get; }

	/// <summary>
	/// Posts a message to the connection, from any thread. Returns <see langword="false"/> if its loop has stopped.
	/// </summary>
	public bool Post(ConnectionMessage message) => Loop.Post(this, message);

	public Connection(
		ConnectionLoop loop,
		EndPoint remoteEndPoint,
		bool isOutbound,
		PacketSigning offeredSigning,
		IDatagramTransport transport,
		TimeProvider timeProvider,
		ReliableProfile profile,
		IConnectionObserver observer,
		Action<Connection> onTerminated
	)
	{
		Loop = loop;
		RemoteEndPoint = remoteEndPoint;
		IsOutbound = isOutbound;
		OfferedSigning = offeredSigning;
		_transport = transport;
		TimeProvider = timeProvider;
		Profile = profile;
		Observer = observer;
		_onTerminated = onTerminated;
		ConnectRetryTimer = new(
			loop.Timers,
			generation => new ConnectionMessage.ConnectRetryTimerElapsed(generation),
			Handle
		);
		RetryTimer = new(
			loop.Timers,
			generation => new ConnectionMessage.RetryTimerElapsed(generation),
			Handle
		);
		DelayedAcknowledgmentTimer = new(
			loop.Timers,
			generation => new ConnectionMessage.DelayedAcknowledgmentTimerElapsed(generation),
			Handle
		);
		DelayedSendMaskTimer = new(
			loop.Timers,
			generation => new ConnectionMessage.DelayedSendMaskTimerElapsed(generation),
			Handle
		);
		KeepAliveTimer = new(
			loop.Timers,
			generation => new ConnectionMessage.KeepAliveTimerElapsed(generation),
			Handle
		);
		HardDisconnectTimer = new(
			loop.Timers,
			generation => new ConnectionMessage.HardDisconnectTimerElapsed(generation),
			Handle
		);
		GracefulDisconnectTimer = new(
			loop.Timers,
			generation => new ConnectionMessage.GracefulDisconnectTimerElapsed(generation),
			Handle
		);
		_state = isOutbound ? new ConnectionState.Idle() : new ConnectionState.Listening();
	}

	public override string ToString() =>
		$"{nameof(Connection)}({RemoteEndPoint}, {(IsOutbound ? "outbound" : "inbound")})";

	public int Timestamp =>
		unchecked(
			(int)(TimeProvider.GetTimestamp() / Math.Max(1, TimeProvider.TimestampFrequency / 1000))
		);

	/// <summary>
	/// Posts <see cref="ConnectionMessage.Flush"/> unless one is already in the mailbox, so that the messages
	/// queued before it are sent together.
	/// </summary>
	public void RequestFlush()
	{
		if (_flushRequested)
			return;

		_flushRequested = true;
		Post(new ConnectionMessage.Flush());
	}

	/// <summary>
	/// The sequence ID that selects the secret of a frame: <b>bSeq</b> of a DFRAME, <b>bNSeq</b> of a SACK.
	/// </summary>
	private static SequenceId? GetSigningSequence(ReliableMessage message) =>
		message switch
		{
			ReliableMessage.DataFrame frame => frame.SequenceId,
			ReliableMessage.Sack sack => sack.NextSend,
			_ => null,
		};

	/// <summary>
	/// Sends a frame, signed when the connection is. <paramref name="secret"/> is the secret a data frame got with its
	/// sequence ID; SACKs and HARD_DISCONNECTs take <see cref="PacketSigner.ControlSecret"/>.
	/// </summary>
	public void Transmit(ReliableMessage message, ulong? secret = null)
	{
		if (Signer is null)
		{
			_transport.Send(RemoteEndPoint, ReliableMessageSerializer.Default.Serialize(message));
			return;
		}

		var datagram = ReliableMessageSerializer.Signed.Serialize(message);
		_transport.Send(RemoteEndPoint, Signer.Sign(datagram, secret ?? Signer.ControlSecret));
	}

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
		Signer = handshake.CreateSigner(IsOutbound);
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
		LoopTimer timer,
		long generation,
		TCommand command
	) => timer.TryConsume(generation) ? Execute(command) : Unit.Default;

	private Unit SendPayload(ConnectionMessage.SendPayload command)
	{
		if (_state is IConnectionTransition<ConnectionMessage.SendPayload>)
			return Execute(command);

		if (command.SendId != 0)
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

	// Data frames and acknowledgments take a switch, which allocates nothing; the rest take Match, which keeps every
	// kind handled but makes a delegate for each kind at each call.
	private Unit Receive(ReliableMessage message) =>
		message switch
		{
			ReliableMessage.DataFrame m => Execute(m),
			ReliableMessage.Sack m => Execute(m),
			_ => ReceiveOther(message),
		};

	private Unit ReceiveOther(ReliableMessage message) =>
		message.Match(
			DataFrame: Execute,
			Connect: Execute,
			Connected: Execute,
			ConnectedSigned: Execute,
			HardDisconnect: Execute,
			Sack: Execute
		);

	/// <summary>
	/// Decodes a datagram from the partner, and drops it if the connection is signed and the signature is wrong
	/// ([MC-DPL8R] section 3.1.5.2.7).
	/// </summary>
	private Unit Receive(byte[] datagram)
	{
		var serializer = Signer is null
			? ReliableMessageSerializer.Default
			: ReliableMessageSerializer.Signed;
		if (serializer.Deserialize(datagram) is not { } message)
			return Unit.Default;

		if (Signer is not null)
		{
			var nextReceive = _state is IHasChannel established
				? established.Channel.NextReceive
				: new SequenceId(0);
			if (!Signer.Validate(datagram, nextReceive, GetSigningSequence(message) ?? nextReceive))
				return Unit.Default;
		}

		return Receive(message);
	}

	/// <summary>
	/// Handles a datagram from the partner. On the loop's thread.
	/// </summary>
	public void ReceiveDatagram(byte[] datagram)
	{
		if (_state is not ConnectionState.Closed)
			Receive(datagram);
	}

	/// <summary>
	/// Handles a message. On the loop's thread.
	/// </summary>
	public void Handle(ConnectionMessage message)
	{
		if (_state is ConnectionState.Closed)
			return;

		// The messages of every frame take a switch, which allocates nothing; the rest take Match, which keeps every
		// kind handled but makes a delegate for each kind at each call.
		switch (message)
		{
			case ConnectionMessage.DatagramReceived m:
				Receive(m.Datagram);
				return;
			case ConnectionMessage.SendPayload m:
				SendPayload(m);
				return;
			case ConnectionMessage.Flush m:
				_flushRequested = false;
				Execute(m);
				return;
			case ConnectionMessage.RetryTimerElapsed m:
				Elapse(RetryTimer, m.Generation, m);
				return;
			case ConnectionMessage.DelayedAcknowledgmentTimerElapsed m:
				Elapse(DelayedAcknowledgmentTimer, m.Generation, m);
				return;
			case ConnectionMessage.DelayedSendMaskTimerElapsed m:
				Elapse(DelayedSendMaskTimer, m.Generation, m);
				return;
			case ConnectionMessage.KeepAliveTimerElapsed m:
				Elapse(KeepAliveTimer, m.Generation, m);
				return;
		}

		message.Match(
			StartConnecting: Execute,
			DatagramArrived: m => Receive(m.Message),
			DatagramReceived: m => Receive(m.Datagram),
			SendPayload: SendPayload,
			Flush: m =>
			{
				_flushRequested = false;
				return Execute(m);
			},
			Disconnect: Execute,
			HardDisconnect: Execute,
			Abort: m => Abort(m.Result),
			EndpointShutDown: m =>
				_state is ConnectionState.HardDisconnecting ? Unit.Default : Abort(m.Result),
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
