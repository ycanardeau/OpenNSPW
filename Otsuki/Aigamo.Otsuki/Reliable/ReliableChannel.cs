using System.Collections.Immutable;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable;

[GenerateMatch]
internal enum ChannelStatus
{
	Open,

	/// <summary>
	/// Both partners sent END_STREAM, or the partner hung up after sending it.
	/// </summary>
	Disconnected,

	/// <summary>
	/// A reliable frame ran out of retries or the partner broke the protocol.
	/// </summary>
	Lost,
}

/// <summary>
/// The data exchange of an established connection: sequencing, acknowledgments, retries, keep-alives,
/// reassembly and graceful disconnects ([MC-DPL8R] sections 3.1.4.3, 3.1.4.4, 3.1.5.2 and 3.1.6).
/// Owned by <see cref="ConnectionState.Established"/> and only used from the connection's message loop.
/// </summary>
internal sealed class ReliableChannel(Connection connection, Handshake handshake)
{
	/// <summary>
	/// From this version on, PACKET_CONTROL_KEEPALIVE_OR_CORRELATE marks a KeepAlive carrying dwSessID.
	/// </summary>
	private const int KeepAliveProtocolVersion = 0x00010005;

	private const int SequenceWindow = 64;

	private static readonly TimeSpan SelectiveAcknowledgmentRetryTimeout =
		TimeSpan.FromMilliseconds(10);

	private sealed class OutgoingFrame
	{
		public required ImmutableArray<byte> Payload { get; init; }
		public bool Reliable { get; init; }
		public bool NewMessage { get; init; }
		public bool EndMessage { get; init; }
		public bool User1 { get; init; }
		public bool KeepAlive { get; init; }
		public bool EndStream { get; init; }
		public long? SendId { get; init; }
		public SequenceId Sequence { get; set; }
		public bool Poll { get; set; }
		public long SentTimestamp { get; set; }
		public int Retries { get; set; }
		public bool SelectivelyAcknowledged { get; set; }
		public bool Dropped { get; set; }
	}

	private readonly Connection _connection = connection;
	private readonly Handshake _handshake = handshake;

	/// <summary>
	/// Sent data frames that are not yet acknowledged, in sequence order.
	/// </summary>
	private readonly List<OutgoingFrame> _unacknowledged = [];

	/// <summary>
	/// Data frames waiting for room in the send window.
	/// </summary>
	private readonly Queue<OutgoingFrame> _pending = new();

	/// <summary>
	/// Received data frames that are not yet in sequence. A <see langword="null"/> value is a frame
	/// that the partner reported as dropped through a send mask.
	/// </summary>
	private readonly Dictionary<SequenceId, ReliableMessage.DataFrame?> _received = [];

	/// <summary>
	/// Sequence IDs of our unreliable frames that will never be retried, reported through send masks.
	/// </summary>
	private readonly HashSet<SequenceId> _droppedUnreliable = [];

	private readonly List<byte[]> _reassembly = [];

	private ChannelStatus _status;
	private SequenceId _nextSend;
	private SequenceId _nextReceive;
	private bool _lastReceivedWasRetry;
	private TimeSpan _roundTripTime = connection.Profile.InitialRoundTripTime;
	private bool _reassembling;
	private bool _reassemblyUser1;
	private int _reassemblySize;
	private bool _endStreamQueued;
	private bool _endStreamAcknowledged;
	private bool _endStreamReceived;

	/// <summary>
	/// Bit i acknowledges the frame with sequence ID bNRcv + 1 + i.
	/// </summary>
	private ulong BuildSelectiveAcknowledgmentMask()
	{
		var mask = 0UL;
		for (var i = 0; i < SequenceWindow; i++)
		{
			if (
				_received.TryGetValue(_nextReceive + (byte)(1 + i), out var frame)
				&& frame is not null
			)
				mask |= 1UL << i;
		}
		return mask;
	}

	/// <summary>
	/// Bit i reports that the frame with sequence ID <paramref name="reference"/> - 1 - i was unreliable and will not be retried.
	/// </summary>
	private ulong BuildSendMask(SequenceId reference)
	{
		var mask = 0UL;
		for (var i = 0; i < SequenceWindow; i++)
		{
			if (_droppedUnreliable.Contains(reference - (byte)(1 + i)))
				mask |= 1UL << i;
		}
		return mask;
	}

	public void SendSelectiveAcknowledgment()
	{
		_connection.Transmit(
			new ReliableMessage.Sack
			{
				Response = true,
				Retry = (byte)(_lastReceivedWasRetry ? 1 : 0),
				NextSend = _nextSend,
				NextReceive = _nextReceive,
				Timestamp = _connection.Timestamp,
				SackMask = BuildSelectiveAcknowledgmentMask(),
				SendMask = BuildSendMask(_nextSend),
			}
		);
		_connection.DelayedAcknowledgmentTimer.Cancel();
		_connection.DelayedSendMaskTimer.Cancel();
	}

	private void TransmitFrame(OutgoingFrame frame, bool retry)
	{
		var sendMask = BuildSendMask(frame.Sequence);
		_connection.Transmit(
			new ReliableMessage.DataFrame
			{
				Reliable = frame.Reliable,
				Sequential = true,
				Poll = frame.Poll || retry,
				NewMessage = frame.NewMessage,
				EndMessage = frame.EndMessage,
				User1 = frame.User1,
				Retry = retry,
				EndStream = frame.EndStream,
				SequenceId = frame.Sequence,
				NextReceive = _nextReceive,
				SackMask = BuildSelectiveAcknowledgmentMask(),
				SendMask = sendMask,
				SessionId =
					frame.KeepAlive && _handshake.RemoteProtocolVersion >= KeepAliveProtocolVersion
						? _handshake.SessionId
						: SessionId.Empty,
				Payload = frame.Payload,
			}
		);

		// The frame carries our acknowledgment state, so a dedicated SACK is no longer needed.
		_connection.DelayedAcknowledgmentTimer.Cancel();
		if (sendMask != 0)
			_connection.DelayedSendMaskTimer.Cancel();
	}

	/// <summary>
	/// 2.5 RTT plus the delayed acknowledgment time-out, with linear backoff for the second and third retries
	/// and exponential backoff afterwards ([MC-DPL8R] section 3.1.2.5).
	/// </summary>
	private TimeSpan GetRetryTimeout(int retries)
	{
		var profile = _connection.Profile;
		var timeout = _roundTripTime * 2.5 + profile.DelayedAcknowledgmentTimeout;
		var factor = retries switch
		{
			0 => 1.0,
			<= 2 => 1.0 + retries,
			_ => 3.0 * Math.Pow(2, Math.Min(retries, 8) - 2),
		};
		var result = timeout * factor;
		return result < profile.MaxRetryInterval ? result : profile.MaxRetryInterval;
	}

	private void ScheduleRetryTimer()
	{
		var frame = _unacknowledged.FirstOrDefault(f => !f.SelectivelyAcknowledged && !f.Dropped);
		if (frame is null)
			_connection.RetryTimer.Cancel();
		else
			_connection.RetryTimer.Start(GetRetryTimeout(frame.Retries));
	}

	private void CompleteSend(OutgoingFrame frame, ResultCode result)
	{
		if (frame.SendId is long sendId)
			_connection.Observer.OnSendCompleted(_connection, sendId, result);
	}

	private void Pump()
	{
		var sent = false;
		while (_pending.Count > 0 && _unacknowledged.Count < _connection.Profile.MaxWindowSize)
		{
			var frame = _pending.Dequeue();
			frame.Sequence = _nextSend++;
			frame.Poll = _pending.Count == 0;
			frame.SentTimestamp = _connection.TimeProvider.GetTimestamp();
			_unacknowledged.Add(frame);
			TransmitFrame(frame, retry: false);
			if (!frame.Reliable)
				CompleteSend(frame, ResultCode.Success);
			sent = true;
		}

		if (sent && !_connection.RetryTimer.IsRunning)
			ScheduleRetryTimer();
	}

	private void Enqueue(byte[] payload, bool reliable, bool user1, long? sendId)
	{
		var size = _connection.Profile.MaxFramePayloadSize;
		var count = Math.Max(1, (payload.Length + size - 1) / size);
		for (var i = 0; i < count; i++)
		{
			var offset = i * size;
			_pending.Enqueue(
				new OutgoingFrame
				{
					Payload = payload
						.AsSpan(offset, Math.Min(size, payload.Length - offset))
						.ToImmutableArray(),
					// A message split across frames is always sent reliably so that it can be reassembled.
					Reliable = reliable || count > 1,
					NewMessage = i == 0,
					EndMessage = i == count - 1,
					User1 = user1,
					SendId = i == count - 1 ? sendId : null,
				}
			);
		}
		Pump();
	}

	/// <summary>
	/// Queues an upper-layer message. Returns <see langword="false"/> once the channel is disconnecting.
	/// </summary>
	public bool TrySend(ConnectionMessage.SendPayload message)
	{
		if (_endStreamQueued)
			return false;

		Enqueue(message.Payload, message.Reliable, message.User1, message.SendId);
		return true;
	}

	public void SendKeepAlive()
	{
		if (_endStreamQueued)
			return;

		_pending.Enqueue(
			new OutgoingFrame
			{
				Payload = [],
				Reliable = true,
				NewMessage = true,
				EndMessage = true,
				KeepAlive = true,
			}
		);
		Pump();
	}

	/// <summary>
	/// Sends END_STREAM after everything already queued ([MC-DPL8R] section 3.1.4.3).
	/// </summary>
	public void Disconnect()
	{
		if (_endStreamQueued)
			return;

		_endStreamQueued = true;
		_pending.Enqueue(
			new OutgoingFrame
			{
				Payload = [],
				Reliable = true,
				NewMessage = true,
				EndMessage = true,
				EndStream = true,
			}
		);
		Pump();
	}

	public void FailAllSends(ResultCode result)
	{
		foreach (var frame in _unacknowledged.Concat(_pending))
		{
			if (frame.Reliable)
				CompleteSend(frame, result);
		}
		_unacknowledged.Clear();
		_pending.Clear();
	}

	private void Lose() =>
		// Once the partner's END_STREAM has arrived, only the final acknowledgments are outstanding.
		_status = _endStreamReceived ? ChannelStatus.Disconnected : ChannelStatus.Lost;

	private void CheckGracefulDisconnect()
	{
		if (_status != ChannelStatus.Open)
			return;

		if (_endStreamQueued && _endStreamAcknowledged && _endStreamReceived)
		{
			SendSelectiveAcknowledgment();
			_status = ChannelStatus.Disconnected;
		}
		else if (_endStreamAcknowledged && !_connection.GracefulDisconnectTimer.IsRunning)
		{
			_connection.GracefulDisconnectTimer.Start(
				_connection.Profile.GracefulDisconnectTimeout
			);
		}
	}

	private void UpdateRoundTripTime(TimeSpan sample) =>
		_roundTripTime = _roundTripTime * 7 / 8 + sample / 8;

	/// <summary>
	/// Processes bNRcv and the SACK mask ([MC-DPL8R] sections 3.1.5.2.2 and 3.1.5.2.3).
	/// </summary>
	private void Acknowledge(SequenceId nextReceive, ulong selectiveAcknowledgmentMask)
	{
		var acknowledged = 0;
		if (_unacknowledged.Count > 0)
		{
			var count = _unacknowledged[0].Sequence.DistanceTo(nextReceive);
			if (count <= _unacknowledged.Count)
				acknowledged = count;
		}

		for (var i = 0; i < acknowledged; i++)
		{
			var frame = _unacknowledged[i];
			if (frame.Retries == 0 && !frame.Dropped)
				UpdateRoundTripTime(_connection.TimeProvider.GetElapsedTime(frame.SentTimestamp));

			if (frame.Reliable)
				CompleteSend(frame, ResultCode.Success);

			if (frame.EndStream)
				_endStreamAcknowledged = true;
		}
		_unacknowledged.RemoveRange(0, acknowledged);
		_droppedUnreliable.RemoveWhere(sequence =>
			sequence.DistanceTo(nextReceive) is > 0 and <= 128
		);

		for (var i = 0; i < SequenceWindow; i++)
		{
			if ((selectiveAcknowledgmentMask & (1UL << i)) == 0)
				continue;

			var sequence = nextReceive + (byte)(1 + i);
			var frame = _unacknowledged.Find(f => f.Sequence == sequence);
			if (frame is not null)
				frame.SelectivelyAcknowledged = true;
		}

		if (
			selectiveAcknowledgmentMask != 0
			&& _unacknowledged.Any(f => !f.SelectivelyAcknowledged && !f.Dropped)
		)
			_connection.RetryTimer.Start(SelectiveAcknowledgmentRetryTimeout);
		else if (acknowledged > 0)
			ScheduleRetryTimer();

		if (acknowledged > 0)
			Pump();
	}

	/// <summary>
	/// Processes a send mask ([MC-DPL8R] section 3.1.5.2.4).
	/// </summary>
	private void ProcessSendMask(SequenceId reference, ulong sendMask)
	{
		for (var i = 0; i < SequenceWindow; i++)
		{
			if ((sendMask & (1UL << i)) == 0)
				continue;

			var sequence = reference - (byte)(1 + i);
			if (_nextReceive.DistanceTo(sequence) < SequenceWindow)
				_received.TryAdd(sequence, null);
		}
	}

	private void Deliver(byte[] payload, bool user1)
	{
		if (payload.Length > 0)
			_connection.Observer.OnReceived(_connection, payload, user1);
	}

	private void ResetReassembly()
	{
		_reassembly.Clear();
		_reassembling = false;
		_reassemblySize = 0;
	}

	/// <summary>
	/// Handles a frame whose turn has come in the sequence ([MC-DPL8R] sections 3.1.5.2.5 and 3.1.5.2.6).
	/// </summary>
	private void ProcessInSequence(ReliableMessage.DataFrame frame)
	{
		if (frame.Coalesce)
		{
			ResetReassembly();
			foreach (
				var payload in CoalescedPayloadSerializer.Default.Deserialize(
					frame.Payload.ToArray()
				) ?? []
			)
				Deliver(payload.Payload.ToArray(), payload.User1);
		}
		else
		{
			var payload = frame.Payload.ToArray();
			var startsMessage = frame.NewMessage || !_reassembling;
			if (startsMessage)
			{
				ResetReassembly();
				_reassemblyUser1 = frame.User1;
			}

			if (startsMessage && frame.EndMessage)
			{
				Deliver(payload, frame.User1);
			}
			else
			{
				_reassembling = true;
				_reassembly.Add(payload);
				_reassemblySize += payload.Length;
				if (_reassemblySize > _connection.Profile.MaxMessageSize)
				{
					Lose();
					return;
				}

				if (frame.EndMessage)
				{
					Deliver(_reassembly.SelectMany(p => p).ToArray(), _reassemblyUser1);
					ResetReassembly();
				}
			}
		}

		if (frame.EndStream)
		{
			_endStreamReceived = true;
			Disconnect();
		}
	}

	private void DeliverInSequence()
	{
		while (_status == ChannelStatus.Open && _received.Remove(_nextReceive, out var frame))
		{
			_nextReceive++;
			if (frame is null)
				ResetReassembly();
			else
				ProcessInSequence(frame);
		}
	}

	private void ScheduleAcknowledgment(bool immediately, bool shortDelay)
	{
		var profile = _connection.Profile;
		if (immediately)
			SendSelectiveAcknowledgment();
		else if (shortDelay)
			_connection.DelayedAcknowledgmentTimer.Start(profile.ShortDelayedAcknowledgmentTimeout);
		else if (!_connection.DelayedAcknowledgmentTimer.IsRunning)
			_connection.DelayedAcknowledgmentTimer.Start(profile.DelayedAcknowledgmentTimeout);
	}

	public ChannelStatus Receive(ReliableMessage.DataFrame message)
	{
		if (
			message.KeepAliveOrCorrelate
			&& _handshake.RemoteProtocolVersion >= KeepAliveProtocolVersion
			&& message.SessionId != _handshake.SessionId
		)
			return _status;

		_connection.KeepAliveTimer.Start(_connection.Profile.KeepAliveInterval);
		Acknowledge(message.NextReceive, message.SackMask);
		_lastReceivedWasRetry = message.Retry;

		var distance = _nextReceive.DistanceTo(message.SequenceId);
		if (distance >= SequenceWindow || _received.ContainsKey(message.SequenceId))
		{
			// Duplicate or outside the window: tell the partner where we are.
			ScheduleAcknowledgment(message.Poll, shortDelay: true);
			return _status;
		}

		_received[message.SequenceId] = message;
		ProcessSendMask(message.SequenceId, message.SendMask);
		DeliverInSequence();
		if (_status != ChannelStatus.Open)
			return _status;

		ScheduleAcknowledgment(message.Poll, shortDelay: distance != 0);
		CheckGracefulDisconnect();
		return _status;
	}

	public ChannelStatus Receive(ReliableMessage.Sack message)
	{
		_connection.KeepAliveTimer.Start(_connection.Profile.KeepAliveInterval);
		Acknowledge(message.NextReceive, message.SackMask);
		ProcessSendMask(message.NextSend, message.SendMask);
		DeliverInSequence();
		CheckGracefulDisconnect();
		return _status;
	}

	/// <summary>
	/// Resends unacknowledged reliable frames and gives up on unreliable ones ([MC-DPL8R] section 3.1.6.5).
	/// </summary>
	public ChannelStatus Retransmit()
	{
		var dropped = false;
		foreach (var frame in _unacknowledged.Where(f => !f.SelectivelyAcknowledged && !f.Dropped))
		{
			if (!frame.Reliable)
			{
				frame.Dropped = true;
				_droppedUnreliable.Add(frame.Sequence);
				dropped = true;
				continue;
			}

			if (frame.Retries >= _connection.Profile.MaxRetries)
			{
				Lose();
				return _status;
			}

			frame.Retries++;
			TransmitFrame(frame, retry: true);
		}

		if (dropped && !_connection.DelayedSendMaskTimer.IsRunning)
			_connection.DelayedSendMaskTimer.Start(_connection.Profile.DelayedSendMaskTimeout);

		ScheduleRetryTimer();
		return _status;
	}
}
