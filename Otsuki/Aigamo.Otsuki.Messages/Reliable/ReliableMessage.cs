using System.Collections.Immutable;
using Aigamo.Extensions.Primitives;
using Aigamo.MatchGenerator;

namespace Aigamo.Otsuki.Messages.Reliable;

[GenerateMatch]
public abstract record ReliableMessage
{
	/// <summary>
	/// A command-code bitmask that contains values that are combined by using the bitwise OR operation.
	/// </summary>
	public PacketCommand Command { get; internal init; }

	private ReliableMessage(PacketCommand command)
	{
		Command = command;
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/97f93510-ca87-4df6-9af1-af5d930e42fa
	/// <summary>
	/// Data frames exist in the standard connection sequence space and typically carry application <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_ba5b8e95-9bae-4562-af24-dca6e860bc38">payload</see> data. They all are identified by having the <b>PACKET_COMMAND_DATA</b> flag (0x01) set in their <b>bCommand</b> field. The total size of the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2d23f9e2-e672-4706-a346-5ca133d47473">data frame (DFRAME)</see> header and the application <b>payload</b> data SHOULD be less than the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_03aae42f-32fd-47ab-b413-d5ec92d29d45">maximum transmission unit (MTU)</see> of the underlying protocols and network. If larger messages are to be transmitted, the implementation MUST break the application <b>payload</b> data into multiple DFRAME packets, send the portions sequentially, and set the <b>PACKET_COMMAND_NEW_MSG</b> flag on the first DFRAME and the <b>PACKET_COMMAND_END_MSG</b> flag on the final DFRAME. Otherwise, the single DFRAME MUST have both the <b>PACKET_COMMAND_NEW_MSG</b> and <b>PACKET_COMMAND_END_MSG</b> flags. Application payload data that is split into multiple DFRAMEs MUST NOT be <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2e830c5b-5096-4360-a2e6-f88ec8962dce">coalesced</see> with other payloads.
	/// </summary>
	/// <remarks>
	/// <b>bCommand</b>: Command field. The <b>PACKET_COMMAND_DATA</b> flag MUST be set. If the packet is a <b>KeepAlive</b>, the <b>PACKET_COMMAND_RELIABLE</b>, <b>PACKET_COMMAND_SEQUENTIAL</b>, and <b>PACKET_COMMAND_END_MSG</b> flags MUST be set. If the packet contains coalesced payloads, the <b>PACKET_COMMAND_NEW_MSG</b> and <b>PACKET_COMMAND_END_MSG</b> flags MUST be set. All other flags are optional.
	/// </remarks>
	[Immutable]
	public sealed record DataFrame() : ReliableMessage(PacketCommand.Data)
	{
		private readonly SessionId _sessionId;
		private readonly uint _sackMask1;
		private readonly uint _sackMask2;
		private readonly uint _sendMask1;
		private readonly uint _sendMask2;

		/// <summary>
		/// Control field. The following flags can be specified.
		/// </summary>
		public PacketControl Control { get; internal init; }

		public bool Retry
		{
			get => Control.HasFlag(PacketControl.Retry);
			init =>
				Control = value
					? (Control | PacketControl.Retry)
					: (Control & ~PacketControl.Retry);
		}

		public bool KeepAliveOrCorrelate
		{
			get => Control.HasFlag(PacketControl.KeepAliveOrCorrelate);
			private init =>
				Control = value
					? (Control | PacketControl.KeepAliveOrCorrelate)
					: (Control & ~PacketControl.KeepAliveOrCorrelate);
		}

		public bool Coalesce
		{
			get => Control.HasFlag(PacketControl.Coalesce);
			init =>
				Control = value
					? (Control | PacketControl.Coalesce)
					: (Control & ~PacketControl.Coalesce);
		}

		public bool EndStream
		{
			get => Control.HasFlag(PacketControl.EndStream);
			init =>
				Control = value
					? (Control | PacketControl.EndStream)
					: (Control & ~PacketControl.EndStream);
		}

		public bool Sack1
		{
			get => Control.HasFlag(PacketControl.Sack1);
			private init =>
				Control = value
					? (Control | PacketControl.Sack1)
					: (Control & ~PacketControl.Sack1);
		}

		public bool Sack2
		{
			get => Control.HasFlag(PacketControl.Sack2);
			private init =>
				Control = value
					? (Control | PacketControl.Sack2)
					: (Control & ~PacketControl.Sack2);
		}

		public bool Send1
		{
			get => Control.HasFlag(PacketControl.Send1);
			private init =>
				Control = value
					? (Control | PacketControl.Send1)
					: (Control & ~PacketControl.Send1);
		}

		public bool Send2
		{
			get => Control.HasFlag(PacketControl.Send2);
			private init =>
				Control = value
					? (Control | PacketControl.Send2)
					: (Control & ~PacketControl.Send2);
		}

		/// <summary>
		/// The sequence number of the packet.
		/// </summary>
		public SequenceId SequenceId { get; init; }

		/// <summary>
		/// The expected sequence number of the next packet received.
		/// </summary>
		public SequenceId NextReceive { get; init; }

		/// <summary>
		/// Optional low 32 bits of the SACK mask, in <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_079478cb-f4c5-4ce5-b72b-2144da5d2ce7">little-endian</see> byte order. The existence of this field in the packet is dependent upon the <b>bControl</b> field having <b>PACKET_CONTROL_SACK1</b> set.
		/// </summary>
		public uint SackMask1
		{
			get => _sackMask1;
			internal init
			{
				_sackMask1 = value;
				Sack1 = value != 0;
			}
		}

		/// <summary>
		/// Optional high 32 bits of the SACK mask, in little-endian byte order. The existence of this field in the packet is dependent upon the <b>bControl</b> field having <b>PACKET_CONTROL_SACK2</b> set.
		/// </summary>
		public uint SackMask2
		{
			get => _sackMask2;
			internal init
			{
				_sackMask2 = value;
				Sack2 = value != 0;
			}
		}

		/// <summary>
		/// Optional low 32 bits of the send mask, in little-endian byte order. The existence of this field in the packet is dependent upon the <b>bControl</b> field having <b>PACKET_CONTROL_SEND1</b> set.
		/// </summary>
		public uint SendMask1
		{
			get => _sendMask1;
			internal init
			{
				_sendMask1 = value;
				Send1 = value != 0;
			}
		}

		/// <summary>
		/// Optional high 32 bits of the send mask, in little-endian byte order. The existence of this field in the packet is dependent upon the <b>bControl</b> field having <b>PACKET_CONTROL_SEND2</b> set.
		/// </summary>
		public uint SendMask2
		{
			get => _sendMask2;
			internal init
			{
				_sendMask2 = value;
				Send2 = value != 0;
			}
		}

		/// <summary>
		/// If the connection was established by using signing, this MUST be the signature of the packet using the agreed-upon signing algorithm. The packet <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_b62c00dd-75c2-47bf-ab93-bd8681b8fee4">sequence ID</see> to be used in the calculation is the value in <b>bSeq</b>. This field MUST NOT be present if signing is not enabled for the connection.
		/// </summary>
		public long Signature { get; init; }

		/// <summary>
		/// The session identifier. When the packet is marked as <b>PACKET_CONTROL_KEEPALIVE_OR_CORRELATE</b> on connections reported as version 0x00010005 or higher, the <b>dwSessID</b> identifier MUST be set to the same <b>dwSessID</b> value specified in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/b3c67ec1-f73c-4c47-bd06-95cd4a3b4219">CONNECT</see> message originally associated with the connection, and there MUST NOT be any application <b>payload</b> data for the packet. Otherwise, <b>dwSessID</b> MUST NOT be present.
		/// </summary>
		public SessionId SessionId
		{
			get => _sessionId;
			init
			{
				_sessionId = value;
				KeepAliveOrCorrelate = value != SessionId.Empty;
			}
		}

		/// <summary>
		/// Application payload data. The size of the <b>payload</b> field is the total <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_a70f5e84-6960-42f0-a160-ba0281eb548d">UDP</see> payload size minus the amount of data consumed by DFRAME headers up to this point. If the <b>PACKET_CONTROL_COALESCE</b> flag is set, the application <b>payload</b> data is not a single message or portion of a message; it is instead organized according to the coalesced payload format, as specified in section 2.2.3.
		/// </summary>
		public IImmutableList<byte> Payload { get; init; } = ImmutableArray<byte>.Empty;

		public bool Data
		{
			get => Command.HasFlag(PacketCommand.Data);
		}

		public bool Reliable
		{
			get => Command.HasFlag(PacketCommand.Reliable);
			init =>
				Command = value
					? (Command | PacketCommand.Reliable)
					: (Command & ~PacketCommand.Reliable);
		}

		public bool Sequential
		{
			get => Command.HasFlag(PacketCommand.Sequential);
			init =>
				Command = value
					? (Command | PacketCommand.Sequential)
					: (Command & ~PacketCommand.Sequential);
		}

		public bool Poll
		{
			get => Command.HasFlag(PacketCommand.Poll);
			init =>
				Command = value ? (Command | PacketCommand.Poll) : (Command & ~PacketCommand.Poll);
		}

		public bool NewMessage
		{
			get => Command.HasFlag(PacketCommand.NewMessage);
			init =>
				Command = value
					? (Command | PacketCommand.NewMessage)
					: (Command & ~PacketCommand.NewMessage);
		}

		public bool EndMessage
		{
			get => Command.HasFlag(PacketCommand.EndMessage);
			init =>
				Command = value
					? (Command | PacketCommand.EndMessage)
					: (Command & ~PacketCommand.EndMessage);
		}

		public bool User1
		{
			get => Command.HasFlag(PacketCommand.User1);
			init =>
				Command = value
					? (Command | PacketCommand.User1)
					: (Command & ~PacketCommand.User1);
		}

		public bool User2
		{
			get => Command.HasFlag(PacketCommand.User2);
			init =>
				Command = value
					? (Command | PacketCommand.User2)
					: (Command & ~PacketCommand.User2);
		}

		public ulong SackMask
		{
			get => (SackMask1, SackMask2).ToUInt64();
			init => (SackMask1, SackMask2) = (value.LowUInt32, value.HighUInt32);
		}

		public ulong SendMask
		{
			get => (SendMask1, SendMask2).ToUInt64();
			init => (SendMask1, SendMask2) = (value.LowUInt32, value.HighUInt32);
		}

		public override string ToString() =>
			$"{nameof(DataFrame)} ["
			+ $"{nameof(Command)}={Command}, "
			+ $"{nameof(Control)}={Control}, "
			+ $"{nameof(SequenceId)}={SequenceId}, "
			+ $"{nameof(NextReceive)}={NextReceive}, "
			+ $"{nameof(SackMask)}={SackMask}, "
			+ $"{nameof(SendMask)}={SendMask}, "
			+ $"{nameof(Signature)}={Signature}, "
			+ $"{nameof(SessionId)}={SessionId}, "
			+ $"{nameof(Payload)}={BitConverter.ToString(Payload.ToArray())}]";
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/b3c67ec1-f73c-4c47-bd06-95cd4a3b4219
	/// <summary>
	/// The CONNECT packet is used to request a connection. If accepted, the response is a <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/2377d224-85b7-4c1a-8677-bd18a08dc5da">CONNECTED (section 2.2.1.2)</see> packet or a <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/ac197b6f-789f-4905-bae5-362ca920e177">CONNECTED_SIGNED (section 2.2.1.3)</see> packet, depending on whether packet signing is enabled.
	/// </summary>
	/// <remarks>
	/// <b>bCommand</b>: A command-code bitmask that contains values that are combined by using the bitwise OR operation from the following table. The <b>PACKET_COMMAND_CFRAME</b> flag MUST be set, and the <b>PACKET_COMMAND_POLL</b> flag SHOULD be set. All other bits MUST be set to zero, and the packet MUST be ignored if they are not.
	/// </remarks>
	[Immutable]
	public sealed record Connect() : ReliableMessage(PacketCommand.CommandFrame)
	{
		/// <summary>
		/// Extended operation code. It MUST be set to the following value:
		/// </summary>
		public ExtendedOpcode Opcode { get; } = ExtendedOpcode.Connect;

		/// <summary>
		/// A message identifier used to correlate responses. The initial value SHOULD be set to zero and SHOULD be incremented each time the connect packet is retried. The recipient MUST echo the value in <b>bRspId</b> when responding.
		/// </summary>
		public byte MessageId { get; init; }

		/// <summary>
		/// Not used in connect packets. This MUST be set to zero when sent and ignored on receipt.
		/// </summary>
		public byte ResponseId { get; init; }

		/// <summary>
		/// The version number of the sender's DirectPlay 8 Protocol, in <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_079478cb-f4c5-4ce5-b72b-2144da5d2ce7">little-endian</see> byte order, where the upper 16 bits are considered a major version number and the lower 16 bits are considered a minor version number. The major version number MUST be set to 0x0001; otherwise, the packet MUST be ignored. The minor version number SHOULD<a id="Appendix_A_Target_1"></a><a aria-label="Product behavior note 1" href="8a440fe2-28b1-44de-8a7e-abe94ce23cf9#Appendix_A_1" data-linktype="relative-path">&lt;1&gt;</a> be set to 0x0006 to indicate support for all features, including <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2e830c5b-5096-4360-a2e6-f88ec8962dce">coalescence</see> and signing.
		/// </summary>
		public int ProtocolVersion { get; init; }

		/// <summary>
		/// The session identifier used to correlate responses. The value is dependent upon the implementation and SHOULD be a random, nonpredictable number. This MUST NOT be set to zero unless <b>dwCurrentProtocolVersion</b> indicates a minor version less than 0x0005. This MUST remain the same value when retrying the CONNECT packet. The recipient MUST echo the value in <b>dwSessID</b> when responding.
		/// </summary>
		public SessionId SessionId { get; init; }

		/// <summary>
		/// The requestor's computer system <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2efd2022-ffd1-4be4-9ff3-aa6b1515948f">tick count</see>, in millisecond units and specified in little-endian byte order.
		/// </summary>
		public int Timestamp { get; init; }

		public bool CommandFrame
		{
			get => Command.HasFlag(PacketCommand.CommandFrame);
		}

		public bool Poll
		{
			get => Command.HasFlag(PacketCommand.Poll);
			init =>
				Command = value ? (Command | PacketCommand.Poll) : (Command & ~PacketCommand.Poll);
		}

		public ushort MajorVersion
		{
			get => ProtocolVersion.HighUInt16;
			init => ProtocolVersion = ProtocolVersion.WithHighUInt16(value);
		}

		public ushort MinorVersion
		{
			get => ProtocolVersion.LowUInt16;
			init => ProtocolVersion = ProtocolVersion.WithLowUInt16(value);
		}

		public override string ToString() =>
			$"{nameof(Connect)} ["
			+ $"{nameof(Command)}={Command}, "
			+ $"{nameof(Opcode)}={Opcode}, "
			+ $"{nameof(MessageId)}={MessageId}, "
			+ $"{nameof(ResponseId)}={ResponseId}, "
			+ $"{nameof(ProtocolVersion)}={ProtocolVersion}, "
			+ $"{nameof(SessionId)}={SessionId}, "
			+ $"{nameof(Timestamp)}={Timestamp}]";
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/2377d224-85b7-4c1a-8677-bd18a08dc5da
	/// <summary>
	/// The CONNECTED packet is used to accept a connection request or complete a connection handshake when signing is not enabled.
	/// </summary>
	/// <remarks>
	/// <b>bCommand</b>: A command-code bitmask that contains values that are combined by using the bitwise OR operation from the following table. The <b>PACKET_COMMAND_CFRAME</b> flag MUST be set. The <b>PACKET_COMMAND_POLL</b> flag MUST be set by a listener accepting a connection request and MUST NOT be set by a connector completing the connection handshake. All other bits MUST be set to zero and the packet MUST be ignored if they are not.
	/// </remarks>
	[Immutable]
	public sealed record Connected() : ReliableMessage(PacketCommand.CommandFrame)
	{
		/// <summary>
		/// An extended operation code. It MUST be set to the following value:
		/// </summary>
		public ExtendedOpcode Opcode { get; } = ExtendedOpcode.Connected;

		/// <summary>
		/// A message identifier. The initial value SHOULD be set to zero and SHOULD be incremented if the packet is retried.
		/// </summary>
		public byte MessageId { get; init; }

		/// <summary>
		/// A response identifier. This value MUST be set to the value of the <b>bMsgID</b> field in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/b3c67ec1-f73c-4c47-bd06-95cd4a3b4219">CONNECT</see> or CONNECTED message to which this is a response.
		/// </summary>
		public byte ResponseId { get; init; }

		/// <summary>
		/// The version number of the sender's DirectPlay 8 Protocol, in <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_079478cb-f4c5-4ce5-b72b-2144da5d2ce7">little-endian</see> byte order, where the upper 16 bits are considered a major version number and the lower 16 bits are considered a minor version number. The major version number MUST be set to 0x0001; otherwise, the packet MUST be ignored. The minor version number SHOULD<a id="Appendix_A_Target_2"></a><a aria-label="Product behavior note 2" href="8a440fe2-28b1-44de-8a7e-abe94ce23cf9#Appendix_A_2" data-linktype="relative-path">&lt;2&gt;</a> be set to 0x0006 to indicate support for all features, including <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2e830c5b-5096-4360-a2e6-f88ec8962dce">coalescence</see> and signing.
		/// </summary>
		public int ProtocolVersion { get; init; }

		/// <summary>
		/// The session identifier. This value MUST be set to the value of <b>dwSessID</b> specified in the CONNECT or CONNECTED message to which this is a response.
		/// </summary>
		public SessionId SessionId { get; init; }

		/// <summary>
		/// The sender's computer system <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2efd2022-ffd1-4be4-9ff3-aa6b1515948f">tick count</see>, in millisecond units, specified in little-endian byte order.
		/// </summary>
		public int Timestamp { get; init; }

		public bool CommandFrame
		{
			get => Command.HasFlag(PacketCommand.CommandFrame);
		}

		public bool Poll
		{
			get => Command.HasFlag(PacketCommand.Poll);
			init =>
				Command = value ? (Command | PacketCommand.Poll) : (Command & ~PacketCommand.Poll);
		}

		public ushort MajorVersion
		{
			get => ProtocolVersion.HighUInt16;
			init => ProtocolVersion = ProtocolVersion.WithHighUInt16(value);
		}

		public ushort MinorVersion
		{
			get => ProtocolVersion.LowUInt16;
			init => ProtocolVersion = ProtocolVersion.WithLowUInt16(value);
		}

		public override string ToString() =>
			$"{nameof(Connected)} ["
			+ $"{nameof(Command)}={Command}, "
			+ $"{nameof(Opcode)}={Opcode}, "
			+ $"{nameof(MessageId)}={MessageId}, "
			+ $"{nameof(ResponseId)}={ResponseId}, "
			+ $"{nameof(ProtocolVersion)}={ProtocolVersion}, "
			+ $"{nameof(SessionId)}={SessionId}, "
			+ $"{nameof(Timestamp)}={Timestamp}]";
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/77c2b46b-996c-4f0b-b362-53ab782fdfbb
	/// <summary>
	/// The HARD_DISCONNECT packet is used to quickly disconnect or <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_6aa258ea-917f-461a-9c54-1b1a66791965">acknowledge</see> quick disconnection without waiting for remaining packets to be delivered.
	/// </summary>
	/// <remarks>
	/// <b>bCommand</b>: The command-code bitmask that contains values that are combined by using the bitwise OR operation from the following table. The <b>PACKET_COMMAND_CFRAME</b> flag MUST be set. The <b>PACKET_COMMAND_POLL</b> flag SHOULD NOT be set. All other bits MUST be set to zero and the packet MUST be ignored if they are not.
	/// </remarks>
	[Immutable]
	public sealed record HardDisconnect() : ReliableMessage(PacketCommand.CommandFrame)
	{
		/// <summary>
		/// An extended operation code. It MUST be set to the following value:
		/// </summary>
		public ExtendedOpcode Opcode { get; } = ExtendedOpcode.HardDisconnect;

		/// <summary>
		/// The message identifier. The value SHOULD be the next&nbsp; incremented value after the <b>bMsgID</b> value used when sending the previous CFRAME message of any type other than <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_04967b3e-cdc6-4a45-a3d5-3e96a02770b9">SACK</see>, but the actual value used by a sender MUST be ignored on receipt.
		/// </summary>
		public byte MessageId { get; init; }

		/// <summary>
		/// The response identifier. This value SHOULD be set to zero, unless the connection is using <b>PACKET_SIGNING_FULL</b>; in which case, it MUST be set to the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_b62c00dd-75c2-47bf-ab93-bd8681b8fee4">sequence ID</see> of the next <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2d23f9e2-e672-4706-a346-5ca133d47473">data frame (DFRAME)</see> that would have been sent had HARD_DISCONNECT not occurred.
		/// </summary>
		public byte ResponseId { get; init; }

		/// <summary>
		/// The version number, in <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_079478cb-f4c5-4ce5-b72b-2144da5d2ce7">little-endian</see> byte order, of the requestor's DirectPlay 8 Protocol. The value SHOULD match the value previously sent in a <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/b3c67ec1-f73c-4c47-bd06-95cd4a3b4219">CONNECT</see>, <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/2377d224-85b7-4c1a-8677-bd18a08dc5da">CONNECTED</see>, or <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/ac197b6f-789f-4905-bae5-362ca920e177">CONNECTED_SIGNED</see> packet, and MUST be ignored on receipt.
		/// </summary>
		public int ProtocolVersion { get; init; }

		/// <summary>
		/// The session identifier. This value MUST be set to the same <b>dwSessID</b> value that is specified in the CONNECT message originally associated with the connection.
		/// </summary>
		public SessionId SessionId { get; init; }

		/// <summary>
		/// The sender's computer system <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2efd2022-ffd1-4be4-9ff3-aa6b1515948f">tick count</see>, in millisecond units, specified in little-endian byte order.
		/// </summary>
		public int Timestamp { get; init; }

		/// <summary>
		/// If the connection was established using signing, this MUST be the signature of the packet using the agreed-upon signing algorithm. The packet sequence ID to be used in the calculation is the value in <b>bRspId</b>. This field MUST NOT be present if signing is not enabled for the connection.
		/// </summary>
		public long Signature { get; init; }

		public bool CommandFrame
		{
			get => Command.HasFlag(PacketCommand.CommandFrame);
		}

		public bool Poll
		{
			get => Command.HasFlag(PacketCommand.Poll);
			init =>
				Command = value ? (Command | PacketCommand.Poll) : (Command & ~PacketCommand.Poll);
		}

		public ushort MajorVersion
		{
			get => ProtocolVersion.HighUInt16;
			init => ProtocolVersion = ProtocolVersion.WithHighUInt16(value);
		}

		public ushort MinorVersion
		{
			get => ProtocolVersion.LowUInt16;
			init => ProtocolVersion = ProtocolVersion.WithLowUInt16(value);
		}

		public override string ToString() =>
			$"{nameof(HardDisconnect)} ["
			+ $"{nameof(Command)}={Command}, "
			+ $"{nameof(Opcode)}={Opcode}, "
			+ $"{nameof(MessageId)}={MessageId}, "
			+ $"{nameof(ResponseId)}={ResponseId}, "
			+ $"{nameof(ProtocolVersion)}={ProtocolVersion}, "
			+ $"{nameof(SessionId)}={SessionId}, "
			+ $"{nameof(Timestamp)}={Timestamp}, "
			+ $"{nameof(Signature)}={Signature}]";
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/9ca3f71c-1040-40b0-9866-2e2e40cf65b0
	/// <summary>
	/// The SACK packet is used to selectively <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_6aa258ea-917f-461a-9c54-1b1a66791965">acknowledge</see> outstanding packets. Packet acknowledgment (ACK) is typically bundled in all user data packets using the <b>bSeq</b> and <b>bNRec</b> fields found in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2d23f9e2-e672-4706-a346-5ca133d47473">data frame (DFRAME)</see> header. However, the SACK packet is used when a dedicated ACK is requested (that is, when the <b>PACKET_COMMAND_POLL</b> bit in the <b>bCommand</b> header field is set) or when no user data remains for further bundled ACKs.
	/// </summary>
	/// <remarks>
	/// <b>bCommand</b>: The command-code bitmask that contains bitwise OR values from the following table. The <b>PACKET_COMMAND_CFRAME</b> flag MUST be set. The <b>PACKET_COMMAND_POLL</b> flag SHOULD NOT be set and SHOULD be ignored on receipt. All other bits MUST be set to zero and the packet MUST be ignored if they are not.
	/// </remarks>
	[Immutable]
	public sealed record Sack() : ReliableMessage(PacketCommand.CommandFrame)
	{
		private readonly uint _sackMask1;
		private readonly uint _sackMask2;
		private readonly uint _sendMask1;
		private readonly uint _sendMask2;

		/// <summary>
		/// An extended operation code. It MUST be set to the following value:
		/// </summary>
		public ExtendedOpcode Opcode { get; } = ExtendedOpcode.Sack;

		/// <summary>
		/// A status flag or flags. The value can be one or more of the following values. All other bits MUST be set to zero. The <b>SACK_FLAGS_RESPONSE</b> flag SHOULD be set and <b>bRetry</b> SHOULD be filled in properly.
		/// </summary>
		public SackFlags Flags { get; internal init; }

		public bool Response
		{
			get => Flags.HasFlag(SackFlags.Response);
			init => Flags = value ? (Flags | SackFlags.Response) : (Flags & ~SackFlags.Response);
		}

		public bool Sack1
		{
			get => Flags.HasFlag(SackFlags.Sack1);
			private init => Flags = value ? (Flags | SackFlags.Sack1) : (Flags & ~SackFlags.Sack1);
		}

		public bool Sack2
		{
			get => Flags.HasFlag(SackFlags.Sack2);
			private init => Flags = value ? (Flags | SackFlags.Sack2) : (Flags & ~SackFlags.Sack2);
		}

		public bool Send1
		{
			get => Flags.HasFlag(SackFlags.Send1);
			private init => Flags = value ? (Flags | SackFlags.Send1) : (Flags & ~SackFlags.Send1);
		}

		public bool Send2
		{
			get => Flags.HasFlag(SackFlags.Send2);
			private init => Flags = value ? (Flags | SackFlags.Send2) : (Flags & ~SackFlags.Send2);
		}

		/// <summary>
		/// Indicates whether the last received packet was a retry. This value MUST be ignored if <b>SACK_FLAGS_RESPONSE</b> is not set. The value SHOULD be set to zero if the last received DFRAME for the connection was not marked as a retry; otherwise, the value SHOULD be nonzero. Recipients MUST NOT require that any particular bit or bits be set in the nonzero case—only that at least one bit is set.
		/// </summary>
		public byte Retry { get; init; }

		/// <summary>
		/// This field represents the sequence number of the next DFRAME to send. SACK packets do not have sequence numbers of their own.
		/// </summary>
		public SequenceId NextSend { get; init; }

		/// <summary>
		/// The expected sequence number of the next packet received. If the <b>SACK_FLAGS_SACK_MASK1</b> or <b>SACK_FLAGS_SACK_MASK2</b> flag is set, the <b>bNRcv</b> field is supplemented with the corresponding additional <b>dwSACKMask1</b> or <b>dwSACKMask2</b> bitmask field that selectively acknowledges frames with sequence numbers higher than <b>bNRcv</b>.
		/// </summary>
		public SequenceId NextReceive { get; init; }

		/// <summary>
		/// This SHOULD be set to zero when sent and MUST be ignored on receipt.
		/// </summary>
		internal short Padding { get; init; }

		/// <summary>
		/// The sender's computer system <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_2efd2022-ffd1-4be4-9ff3-aa6b1515948f">tick count</see>, in millisecond units, specified in little-endian byte order.
		/// </summary>
		public int Timestamp { get; init; }

		/// <summary>
		/// The optional low 32 bits of the SACK mask, in little-endian byte order. The existence of this field in the packet is dependent upon the <b>bFlags</b> field having <b>SACK_FLAGS_SACK_MASK1</b> set.
		/// </summary>
		public uint SackMask1
		{
			get => _sackMask1;
			internal init
			{
				_sackMask1 = value;
				Sack1 = value != 0;
			}
		}

		/// <summary>
		/// The optional high 32 bits of the SACK mask, in little-endian byte order. The existence of this field in the packet is dependent upon the <b>bFlags</b> field having <b>SACK_FLAGS_SACK_MASK2</b> set.
		/// </summary>
		public uint SackMask2
		{
			get => _sackMask2;
			internal init
			{
				_sackMask2 = value;
				Sack2 = value != 0;
			}
		}

		/// <summary>
		/// The optional low 32 bits of the send mask, in little-endian byte order. The existence of this field in the packet is dependent upon the <b>bFlags</b> field having <b>SACK_FLAGS_SEND_MASK1</b> set.
		/// </summary>
		public uint SendMask1
		{
			get => _sendMask1;
			internal init
			{
				_sendMask1 = value;
				Send1 = value != 0;
			}
		}

		/// <summary>
		/// The optional high 32 bits of the send mask, in little-endian byte order. The existence of this field in the packet is dependent upon the <b>bFlags</b> field having <b>SACK_FLAGS_SEND_MASK2</b> set.
		/// </summary>
		public uint SendMask2
		{
			get => _sendMask2;
			internal init
			{
				_sendMask2 = value;
				Send2 = value != 0;
			}
		}

		/// <summary>
		/// If the connection was established using signing, this MUST be the signature of the packet using the agreed-upon signing algorithm. The packet <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/a4ef7612-93d2-4d94-a96c-dbe6a1890ed6#gt_b62c00dd-75c2-47bf-ab93-bd8681b8fee4">sequence ID</see> to be used in the calculation is the value in <b>bNSeq</b>. This field MUST NOT be present if signing is not enabled for the connection.
		/// </summary>
		public long Signature { get; init; }

		public bool CommandFrame
		{
			get => Command.HasFlag(PacketCommand.CommandFrame);
		}

		public bool Poll
		{
			get => Command.HasFlag(PacketCommand.Poll);
			init =>
				Command = value ? (Command | PacketCommand.Poll) : (Command & ~PacketCommand.Poll);
		}

		public ulong SackMask
		{
			get => (SackMask1, SackMask2).ToUInt64();
			init => (SackMask1, SackMask2) = (value.LowUInt32, value.HighUInt32);
		}

		public ulong SendMask
		{
			get => (SendMask1, SendMask2).ToUInt64();
			init => (SendMask1, SendMask2) = (value.LowUInt32, value.HighUInt32);
		}

		public override string ToString() =>
			$"{nameof(Sack)} ["
			+ $"{nameof(Command)}={Command}, "
			+ $"{nameof(Opcode)}={Opcode}, "
			+ $"{nameof(Flags)}={Flags}, "
			+ $"{nameof(Retry)}={Retry}, "
			+ $"{nameof(NextSend)}={NextSend}, "
			+ $"{nameof(NextReceive)}={NextReceive}, "
			+ $"{nameof(Timestamp)}={Timestamp}, "
			+ $"{nameof(SackMask)}={SackMask}, "
			+ $"{nameof(SendMask)}={SendMask}, "
			+ $"{nameof(Signature)}={Signature}]";
	}
}
