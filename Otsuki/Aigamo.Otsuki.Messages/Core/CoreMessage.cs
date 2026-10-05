using System.Collections.Immutable;
using System.Text;
using Aigamo.MatchGenerator;
using Flags = Aigamo.Otsuki.Messages.Core.NameTableEntryFlags;

namespace Aigamo.Otsuki.Messages.Core;

[GenerateMatch]
public abstract record CoreMessage
{
	/// <summary>
	/// A 32-bit field that contains the packet type.
	/// </summary>
	public PacketType PacketType { get; }

	private CoreMessage(PacketType packetType)
	{
		PacketType = packetType;
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/aa00e9b1-8169-4aef-aa4c-1b0619d45c24
	/// <summary>
	/// This is the first message passed into a host/server to initiate the connect sequence.
	/// </summary>
	[Immutable]
	public sealed record PlayerConnectInfo() : CoreMessage(PacketType.PlayerConnectInfo)
	{
		internal NullTerminatedAsciiString UrlInternal { get; init; } = string.Empty;

		internal NullTerminatedUnicodeString PasswordInternal { get; init; } = string.Empty;

		internal NullTerminatedUnicodeString NameInternal { get; init; } = string.Empty;

		/// <summary>
		/// A 32-bit field that specifies the connect flags.
		/// </summary>
		public ObjectType Flags { get; init; }

		/// <summary>
		/// A 32-bit field that specifies the DirectPlay version.
		/// </summary>
		public DnetVersion DnetVersion { get; init; }

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the data in the <b>name</b> field. If <b>dwNameOffset</b> is set to 0, <b>dwNameSize</b> SHOULD also be 0. If <b>dwNameOffset</b> is not 0, <b>dwNameSize</b> SHOULD also not be 0.
		/// </summary>
		internal int NameSize => NameInternal.Length;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the password. If <b>dwPasswordOffset</b> is set to 0, <b>dwPasswordSize</b> SHOULD also be 0. If <b>dwPasswordOffset</b> is not 0, <b>dwPasswordSize</b> SHOULD also not be 0.
		/// </summary>
		internal int PasswordSize => PasswordInternal.Length;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the <b>url</b> field. If <b>dwURLOffset</b> is 0, <b>dwURLSize</b> SHOULD also be 0. If <b>dwURLOffset</b> is not 0, <b>dwURLSize</b> SHOULD also not be 0.
		/// </summary>
		internal int UrlSize => UrlInternal.Length;

		/// <summary>
		/// A 128-bit field that contains the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_f49694cc-c350-462d-ab8e-816f0103c6c1">GUID</see> that identifies the particular instance of the server/host application to which the client/peer is attempting to connect. Each instance of a DirectPlay server/host application generates a new unique GUID each time the application hosts a new game session. In order for the client/peer to connect, the value of <b>guidInstance</b> MUST match the value of the GUID instance defined on the server/host or the value MUST be all zeroes. If a different, nonzero GUID instance value is specified, the recipient MUST send a <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/7fc9f0e0-27d4-4975-b520-079737d5cb0b">DN_CONNECT_FAILED</see> message with the result code DPNERR_INVALIDINSTANCE (0x80158380) and terminate the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/../mc-dpl8r/7a35d96c-daca-4311-bc2b-bd6a2f50bf14">[MC-DPL8R]</see> connection. For information on how a client/peer retrieves the value of the GUID instance defined on the server/host, see the description of the <b>ApplicationInstanceGUID</b> field in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/../mc-dplhp/ef5b29b4-cc5b-4aea-82a4-c0f55296f1a2">EnumResponse</see> message defined in <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/../mc-dplhp/1a901a85-f85c-497c-aac7-1e172a894243">[MC-DPLHP]</see> section 2.2.2.
		/// </summary>
		public Guid GuidInstance { get; init; }

		/// <summary>
		/// A 128-bit field that specifies the application's assigned GUID. This is the unique identifier for the specific application, not per instance.
		/// </summary>
		public Guid GuidApplication { get; init; }

		/// <summary>
		/// A variable-length field that specifies alternative address data used to connect the client. This field's position is determined by <b>dwAlternateAddressDataOffset</b> and the size stated in <b>dwAlternateAddressDataSize</b>. The addresses that are passed into the <b>alternateAddressData</b> field are formatted via the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/bb72c589-3c28-4e77-ba36-995e1a825e6b">DN_ALTERNATE_ADDRESS</see> structure. Because DN_ALTERNATE_ADDRESS contains its own size, multiple alternate addresses can be passed in by appending the DN_ALTERNATE_ADDRESS structures together. However, the maximum number of alternate addresses that can be passed in at a single time is limited to 12.
		/// </summary>
		internal /* TODO: make public */
		IImmutableList<AlternateAddress> AlternateAddresses { get; init; } =
			ImmutableArray<AlternateAddress>.Empty;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the <b>alternateAddressData</b> field. If <b>dwAlternateAddressDataOffset</b> is set to 0, <b>dwAlternateAddressDataSize</b> SHOULD also be 0. If <b>dwAlternateAddressDataOffset</b> is not 0, <b>dwAlternateAddressDataSize</b> SHOULD also not be 0.
		/// </summary>
		internal int AlternateAddressDataSize =>
			AlternateAddresses.Sum(a => a.ToByteArray().Length);

		/// <summary>
		/// A variable-length field that contains a 0-terminated byte character array that specifies the client URL. This field's position is determined by <b>dwURLOffset</b> and the size stated in <b>dwURLSize</b>. It is defined in <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/180a7d08-8b45-4b32-a971-4dfc6a19f9ac">DN_ADDRESSING_URL</see>.
		/// </summary>
		public string Url
		{
			get => UrlInternal;
			init => UrlInternal = value;
		}

		/// <summary>
		/// A variable-length field that contains a byte array that provides the connection data. This field's position is determined by <b>dwConnectDataOffset</b> and the size stated in <b>dwConnectDataSize</b>.
		/// </summary>
		public IImmutableList<byte> ConnectData { get; init; } = ImmutableArray<byte>.Empty;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the <b>connectData</b> field. If <b>dwConnectDataOffset</b> is 0, <b>dwConnectDataSize</b> SHOULD also be 0. If <b>dwConnectDataOffset</b> is not 0, <b>dwConnectDataSize</b> SHOULD also not be 0.
		/// </summary>
		internal int ConnectDataSize => ConnectData.Count;

		/// <summary>
		/// A variable-length field that contains a 0-terminated <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_30c30de3-1d00-4d0d-9109-fcc0094fc95a">wide character</see> array that specifies the application password data. This field's position is determined by <b>dwPasswordOffset</b> and the size stated in <b>dwPasswordSize</b>. This data is passed in clear text to the protocol layer.
		/// </summary>
		public string Password
		{
			get => PasswordInternal;
			init => PasswordInternal = value;
		}

		/// <summary>
		/// A variable-length field that contains a byte array that specifies the application data. This field's position is determined by <b>dwDataOffset</b> and the size stated in <b>dwDataSize</b>.
		/// </summary>
		public IImmutableList<byte> Data { get; init; } = ImmutableArray<byte>.Empty;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the <b>data</b> field. If <b>dwDataOffset</b> is set to 0, <b>dwDataSize</b> SHOULD also be 0. If <b>dwDataOffset</b> is not 0, <b>dwDataSize</b> SHOULD also not be 0.
		/// </summary>
		internal int DataSize => Data.Count;

		/// <summary>
		/// A variable-length field that contains a 0-terminated wide character array that specifies the client/peer name. This field's position is determined by <b>dwNameOffset</b> and the size stated in <b>dwNameSize</b>.
		/// </summary>
		public string Name
		{
			get => NameInternal;
			init => NameInternal = value;
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(PlayerConnectInfo)}:");
			builder.AppendLine($"\t{nameof(Flags)}: {Flags}");
			builder.AppendLine($"\t{nameof(DnetVersion)}: {DnetVersion}");
			builder.AppendLine($"\t{nameof(GuidInstance)}: {GuidInstance}");
			builder.AppendLine($"\t{nameof(GuidApplication)}: {GuidApplication}");

			foreach (var a in AlternateAddresses)
				builder.AppendLine(
					string.Join("\n", a.ToString().Split('\n').Select(l => "\t" + l))
				);

			builder.AppendLine($"\t{nameof(Url)}: {Url}");
			builder.AppendLine(
				$"\t{nameof(ConnectData)}: {BitConverter.ToString(ConnectData.ToArray())}"
			);
			builder.AppendLine($"\t{nameof(Password)}: {Password}");
			builder.AppendLine($"\t{nameof(Data)}: {BitConverter.ToString(Data.ToArray())}");
			builder.AppendLine($"\t{nameof(Name)}: {Name}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/7fc9f0e0-27d4-4975-b520-079737d5cb0b
	/// <summary>
	/// The DN_CONNECT_FAILED packet indicates that a connection attempt failed.
	/// </summary>
	[Immutable]
	public sealed record ConnectFailed() : CoreMessage(PacketType.ConnectFailed)
	{
		/// <summary>
		/// A 32-bit field that contains the failure code.
		/// </summary>
		public ResultCode ResultCode { get; init; }

		/// <summary>
		/// A variable-length field that contains an array of bytes that provides a reply message from the application identifying the connection failure. Reply data is only expected when the failure type is <b>DPNERR_HOSTREJECTEDCONNECTION</b>.
		/// </summary>
		public IImmutableList<byte> Reply { get; init; } = ImmutableArray<byte>.Empty;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the data in the <b>reply</b> field. If <b>dwReplyOffset</b> is 0, <b>dwReplySize</b> SHOULD also be 0. If <b>dwReplyOffset</b> is not 0, <b>dwReplySize</b> SHOULD also not be 0.
		/// </summary>
		internal int ReplySize => Reply.Count;

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(ConnectFailed)}:");
			builder.AppendLine($"\t{nameof(ResultCode)}: {ResultCode}");
			builder.AppendLine($"\t{nameof(Reply)}: {BitConverter.ToString(Reply.ToArray())}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/075e84f0-b26c-4f10-9f69-21a0813dfc54
	/// <summary>
	/// The DN_SEND_CONNECT_INFO packet is sent from the host/server indicating to the connecting peer/client that it has joined the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_cb5007f6-e2af-44f6-abe1-c8a5ff856254">game session</see>.
	/// </summary>
	[Immutable]
	public sealed record SendConnectInfo() : CoreMessage(PacketType.SendConnectInfo)
	{
		internal NullTerminatedUnicodeString PasswordInternal { get; init; } = string.Empty;

		internal NullTerminatedUnicodeString SessionNameInternal { get; init; } = string.Empty;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the application description information. This includes all fields starting with <b>dwSize</b> through <b>guidApplication</b>.
		/// </summary>
		internal int Size => 80;

		/// <summary>
		/// A 32-bit integer that specifies the application flags.
		/// </summary>
		public SessionFlags Flags { get; init; }

		/// <summary>
		/// A 32-bit integer that specifies the maximum number of clients/peers allowed in the game session. A value of 0 indicates that the maximum number of <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_0258d4e2-d4f7-4099-ae0f-a02fad73e824">players</see> is not specified.
		/// </summary>
		public int MaxPlayers { get; init; }

		/// <summary>
		/// A 32-bit integer that specifies the current number of clients/peers in the game session.
		/// </summary>
		public int CurrentPlayers { get; init; }

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the <b>sessionName</b> field. If <b>dwSessionNameOffset</b> is 0, <b>dwSessionNameSize</b> MUST be 0. If <b>dwSessionNameOffset</b> is not 0, <b>dwSessionNameSize</b> MUST NOT be 0.
		/// </summary>
		internal int SessionNameSize => SessionNameInternal.Length;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the password. If <b>dwPasswordOffset</b> is 0, <b>dwPasswordSize</b> MUST be 0. If <b>dwPasswordOffset</b> is not 0, <b>dwPasswordSize</b> MUST NOT be 0.
		/// </summary>
		internal int PasswordSize => PasswordInternal.Length;

		/// <summary>
		/// A 128-bit field that contains the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_f49694cc-c350-462d-ab8e-816f0103c6c1">GUID</see> that identifies the particular instance of the server/host application. The value of this field implicitly SHOULD match the value of the <b>guidInstance</b> field specified in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/d2f5c735-5947-4b76-98c0-d447d9d36de8">DN_INTERNAL_MESSAGE_PLAYER_CONNECT_INFO</see> or <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/aa00e9b1-8169-4aef-aa4c-1b0619d45c24">DN_INTERNAL_MESSAGE_PLAYER_CONNECT_INFO_EX</see> message, unless that field contained all zeroes, in which case this <b>guidInstance</b> value informs the receiving client of the actual game session instance GUID.
		/// </summary>
		public Guid GuidInstance { get; init; }

		/// <summary>
		/// The application GUID as defined by the host/server.
		/// </summary>
		public Guid GuidApplication { get; init; }

		/// <summary>
		/// A 32-bit integer created by the server/host that provides the identifier for the new client joining the game session. For more information, see <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">DN_DPNID</see>.
		/// </summary>
		public Dpnid Dpnid { get; init; }

		/// <summary>
		/// A 32-bit integer that specifies the current <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_d6292f62-e604-4ac7-9b20-87dde6efb93b">name table</see> version.
		/// </summary>
		public int Version { get; init; }

		/// <summary>
		/// Not used.
		/// </summary>
		internal int VersionNotUsed { get; init; }

		/// <summary>
		/// This field contains a variable-length array of DN_NAMETABLE_ENTRY_INFO structures. The length of this array is described above in the <b>dwEntryCount</b> field. Each entry in this array describes a player or group in the game session. In peer-to-peer mode, the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_96048ee4-02d7-484e-a53b-3b8ed355251d">host</see> MUST transmit entries for all existing participants and the new participant. In <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_a907c749-671d-466b-b589-8c6dea31403f">client/server mode</see>, the server MUST transmit only two entries: one for the server player and one for the new participant.
		/// </summary>
		public IImmutableList<NameTableEntryInfo> NameTableEntries { get; init; } =
			ImmutableArray<NameTableEntryInfo>.Empty;

		/// <summary>
		/// A 32-bit integer that provides the number of entries in the name table contained in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/87370443-4103-4153-a8ac-a6728c39b7e5">DN_NAMETABLE_ENTRY_INFO</see> field below. These are in essence players in the game session.
		/// </summary>
		public int EntryCount => NameTableEntries.Count();

		/// <summary>
		/// This field contains a variable-length array of DN_NAMETABLE_MEMBERSHIP_INFO structures. The length of this array is described above in the <b>dwMembershipCount</b> field. Each entry in this array describes a player/group combination.
		/// </summary>
		public IImmutableList<NameTableMembershipInfo> NameTableMemberships { get; init; } =
			ImmutableArray<NameTableMembershipInfo>.Empty;

		/// <summary>
		/// A 32-bit integer that provides the number of memberships in the name table contained in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/772ec104-55b6-4375-9111-cebd7bca1690">DN_NAMETABLE_MEMBERSHIP_INFO</see> field below. These are in essence player to <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_51c51c14-7f9d-4c0b-a69c-d3e059bfffac">group</see> combinations.
		/// </summary>
		public int MembershipCount => NameTableMemberships.Count();

		/// <summary>
		/// A variable-length field that contains a 0-terminated character array that specifies the application reserved data. This field's position is determined by <b>dwApplicationReservedDataOffset</b> and the size stated in <b>dwApplicationReservedDataSize</b>.
		/// </summary>
		public IImmutableList<byte> ApplicationReservedData { get; init; } =
			ImmutableArray<byte>.Empty;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the <b>applicationReservedData</b> field. If <b>dwApplicationReservedDataOffset</b> is 0, <b>dwApplicationReservedDataSize</b> MUST also be 0. If <b>dwApplicationReservedDataOffset</b> is not 0, <b>dwApplicationReservedDataSize</b> MUST NOT be 0.
		/// </summary>
		internal int ApplicationReservedDataSize => ApplicationReservedData.Count;

		/// <summary>
		/// A variable-length field that contains a byte array that provides the reserved data. This field's position is determined by <b>dwReservedDataOffset</b> and the size stated in <b>dwReservedDataSize</b>.
		/// </summary>
		public IImmutableList<byte> ReservedData { get; init; } = ImmutableArray<byte>.Empty;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the <b>reservedData</b> field. If <b>dwReservedDataOffset</b> is 0, <b>dwReservedDataSize</b> MUST be 0. If <b>dwReservedDataOffset</b> is not 0, <b>dwReservedDataSize</b> MUST NOT be 0.
		/// </summary>
		internal int ReservedDataSize => ReservedData.Count;

		/// <summary>
		/// A variable-length field that contains a 0-terminated wide character array that specifies the application password data. This field's position is determined by <b>dwPasswordOffset</b> and the size stated in <b>dwPasswordSize</b>. This data is passed in clear text to the protocol layer.
		/// </summary>
		public string Password
		{
			get => PasswordInternal;
			init => PasswordInternal = value;
		}

		/// <summary>
		/// A variable-length field that contains a 0-terminated wide character array that specifies the game session name. This field's position is determined by <b>dwSessionNameOffset</b> and the size stated in <b>dwSessionNameSize</b>.
		/// </summary>
		public string SessionName
		{
			get => SessionNameInternal;
			init => SessionNameInternal = value;
		}

		/// <summary>
		/// A variable-length field that contains a byte array that provides the reply. This field's position is determined by <b>dwReplyOffset</b> and the size stated in <b>dwReplySize</b>.
		/// </summary>
		public IImmutableList<byte> Reply { get; init; } = ImmutableArray<byte>.Empty;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the <b>reply</b> field. If <b>dwReplyOffset</b> is set to 0, <b>dwReplySize</b> MUST be 0. If <b>dwReplyOffset</b> is not 0, <b>dwReplySize</b> MUST NOT be 0.
		/// </summary>
		internal int ReplySize => Reply.Count;

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(SendConnectInfo)}:");
			builder.AppendLine($"\t{nameof(Flags)}: {Flags}");
			builder.AppendLine($"\t{nameof(MaxPlayers)}: {MaxPlayers}");
			builder.AppendLine($"\t{nameof(CurrentPlayers)}: {CurrentPlayers}");
			builder.AppendLine($"\t{nameof(GuidInstance)}: {GuidInstance}");
			builder.AppendLine($"\t{nameof(GuidApplication)}: {GuidApplication}");
			builder.AppendLine($"\t{nameof(Dpnid)}: {Dpnid}");
			builder.AppendLine($"\t{nameof(Version)}: {Version}");
			builder.AppendLine($"\t{nameof(VersionNotUsed)}: {VersionNotUsed}");

			foreach (var e in NameTableEntries)
				builder.AppendLine(
					string.Join("\n", e.ToString().Split('\n').Select(l => "\t" + l))
				);

			foreach (var m in NameTableMemberships)
				builder.AppendLine(
					string.Join("\n", m.ToString().Split('\n').Select(l => "\t" + l))
				);

			builder.AppendLine(
				$"\t{nameof(ApplicationReservedData)}: {BitConverter.ToString(ApplicationReservedData.ToArray())}"
			);
			builder.AppendLine(
				$"\t{nameof(ReservedData)}: {BitConverter.ToString(ReservedData.ToArray())}"
			);
			builder.AppendLine($"\t{nameof(Password)}: {Password}");
			builder.AppendLine($"\t{nameof(SessionName)}: {SessionName}");
			builder.AppendLine($"\t{nameof(Reply)}: {BitConverter.ToString(Reply.ToArray())}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8d48ddad-232f-480a-9a50-5b531d971ec2
	/// <summary>
	/// The DN_ADD_PLAYER packet is sent from the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_96048ee4-02d7-484e-a53b-3b8ed355251d">host</see> and instructs <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_e5d0d91c-9a39-493f-ab1b-f36ce840e6a2">peers</see> to add a specified peer to the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_cb5007f6-e2af-44f6-abe1-c8a5ff856254">game session</see>.
	/// </summary>
	[Immutable]
	public sealed record AddPlayer() : CoreMessage(PacketType.AddPlayer)
	{
		internal NullTerminatedAsciiString UrlInternal { get; init; } = string.Empty;

		internal NullTerminatedUnicodeString NameInternal { get; init; } = string.Empty;

		/// <summary>
		/// A 32-bit field that contains the identifier of the peer to add. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid Dpnid { get; init; }

		/// <summary>
		/// A 32-bit field that contains the identifier of the game session owner. For more information, see section 2.2.7.
		/// </summary>
		public Dpnid DpnidOwner { get; init; }

		/// <summary>
		/// A 32-bit field that contains player flags.
		/// </summary>
		public Flags Flags { get; internal init; }

		/// <summary>
		/// A 32-bit field that specifies the current name table version number.
		/// </summary>
		public int Version { get; init; }

		/// <summary>
		/// Not used.
		/// </summary>
		internal int VersionNotUsed { get; init; }

		/// <summary>
		/// A 32-bit field that contains the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_1a73cb63-9f53-49e9-9f3e-19ce82ab5a6d">DirectPlay</see> version of the client being added to the game session.
		/// </summary>
		public DnetVersion DnetClientVersion { get; init; }

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the name. If <b>dwNameOffset</b> is 0, <b>dwNameSize</b> SHOULD also be 0. If <b>dwNameOffset</b> is not 0, <b>dwNameSize</b> SHOULD also not be 0.
		/// </summary>
		internal int NameSize => NameInternal.Length;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the connecting peer's URL address.
		/// </summary>
		internal int UrlSize => UrlInternal.Length;

		/// <summary>
		/// A variable-length field that contains an array of characters that specify the client URL.
		/// </summary>
		public string Url
		{
			get => UrlInternal;
			init => UrlInternal = value;
		}

		/// <summary>
		/// A variable-length field that specifies a byte array of characters that contain user data.
		/// </summary>
		public IImmutableList<byte> Data { get; init; } = ImmutableArray<byte>.Empty;

		/// <summary>
		/// A 32-bit field that specifies the size, in bytes, of the peer data. If <b>dwDataOffset</b> is 0, <b>dwDataSize</b> SHOULD also be 0. If <b>dwDataOffset</b> is not 0, <b>dwDataSize</b> SHOULD also not be 0.
		/// </summary>
		internal int DataSize => Data.Count;

		/// <summary>
		/// A variable-length field that specifies an array of <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_30c30de3-1d00-4d0d-9109-fcc0094fc95a">wide characters</see> that contain the peer name including the NULL termination character.
		/// </summary>
		public string Name
		{
			get => NameInternal;
			init => NameInternal = value;
		}

		public bool Local
		{
			get => Flags.HasFlag(Flags.Local);
			init => Flags = value ? (Flags | Flags.Local) : (Flags & ~Flags.Local);
		}

		public bool Host
		{
			get => Flags.HasFlag(Flags.Host);
			init => Flags = value ? (Flags | Flags.Host) : (Flags & ~Flags.Host);
		}

		public bool AllPlayersGroup
		{
			get => Flags.HasFlag(Flags.AllPlayersGroup);
			init =>
				Flags = value ? (Flags | Flags.AllPlayersGroup) : (Flags & ~Flags.AllPlayersGroup);
		}

		public bool Group
		{
			get => Flags.HasFlag(Flags.Group);
			init => Flags = value ? (Flags | Flags.Group) : (Flags & ~Flags.Group);
		}

		public bool GroupAutoDestruct
		{
			get => Flags.HasFlag(Flags.GroupAutoDestruct);
			init =>
				Flags = value
					? (Flags | Flags.GroupAutoDestruct)
					: (Flags & ~Flags.GroupAutoDestruct);
		}

		public bool Peer
		{
			get => Flags.HasFlag(Flags.Peer);
			init => Flags = value ? (Flags | Flags.Peer) : (Flags & ~Flags.Peer);
		}

		public bool Client
		{
			get => Flags.HasFlag(Flags.Client);
			init => Flags = value ? (Flags | Flags.Client) : (Flags & ~Flags.Client);
		}

		public bool Server
		{
			get => Flags.HasFlag(Flags.Server);
			init => Flags = value ? (Flags | Flags.Server) : (Flags & ~Flags.Server);
		}

		public bool Connecting
		{
			get => Flags.HasFlag(Flags.Connecting);
			init => Flags = value ? (Flags | Flags.Connecting) : (Flags & ~Flags.Connecting);
		}

		public bool Available
		{
			get => Flags.HasFlag(Flags.Available);
			init => Flags = value ? (Flags | Flags.Available) : (Flags & ~Flags.Available);
		}

		public bool Disconnecting
		{
			get => Flags.HasFlag(Flags.Disconnecting);
			init => Flags = value ? (Flags | Flags.Disconnecting) : (Flags & ~Flags.Disconnecting);
		}

		public bool Indicated
		{
			get => Flags.HasFlag(Flags.Indicated);
			init => Flags = value ? (Flags | Flags.Indicated) : (Flags & ~Flags.Indicated);
		}

		public bool Created
		{
			get => Flags.HasFlag(Flags.Created);
			init => Flags = value ? (Flags | Flags.Created) : (Flags & ~Flags.Created);
		}

		public bool NeedToDestroy
		{
			get => Flags.HasFlag(Flags.NeedToDestroy);
			init => Flags = value ? (Flags | Flags.NeedToDestroy) : (Flags & ~Flags.NeedToDestroy);
		}

		public bool InUse
		{
			get => Flags.HasFlag(Flags.InUse);
			init => Flags = value ? (Flags | Flags.InUse) : (Flags & ~Flags.InUse);
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(AddPlayer)}:");
			builder.AppendLine($"\t{nameof(Dpnid)}: {Dpnid}");
			builder.AppendLine($"\t{nameof(DpnidOwner)}: {DpnidOwner}");
			builder.AppendLine($"\t{nameof(Flags)}: {Flags}");
			builder.AppendLine($"\t{nameof(Version)}: {Version}");
			builder.AppendLine($"\t{nameof(VersionNotUsed)}: {VersionNotUsed}");
			builder.AppendLine($"\t{nameof(DnetClientVersion)}: {DnetClientVersion}");
			builder.AppendLine($"\t{nameof(Url)}: {Url}");
			builder.AppendLine($"\t{nameof(Data)}: {BitConverter.ToString(Data.ToArray())}");
			builder.AppendLine($"\t{nameof(Name)}: {Name}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8b46432b-bebb-4f94-9961-fed0c9b0fa09
	/// <summary>
	/// The DN_ACK_CONNECT_INFO packet is sent from the client/peer to the server/host to <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_6aa258ea-917f-461a-9c54-1b1a66791965">acknowledge</see> the receipt of connection information. This packet contains no user data beyond the packet type field.
	/// </summary>
	[Immutable]
	public sealed record AckConnectInfo() : CoreMessage(PacketType.AckConnectInfo)
	{
		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(AckConnectInfo)}:");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/6b9cdb3e-09b4-49ba-a7c7-31a4c9a8a02f
	/// <summary>
	/// The DN_INSTRUCT_CONNECT packet instructs a peer to connect to a designated peer. This packet uses the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/../mc-dpl8r/b3c67ec1-f73c-4c47-bd06-95cd4a3b4219">CONNECT</see> and <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/../mc-dpl8r/2377d224-85b7-4c1a-8677-bd18a08dc5da">CONNECTED</see> packets defined in <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/../mc-dpl8r/7a35d96c-daca-4311-bc2b-bd6a2f50bf14">[MC-DPL8R]</see> sections 2.2.1.1 and 2.2.1.2. For an example of the message sequence for these packets, see [MC-DPL8R] section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/../mc-dpl8r/f02dc0e7-3ab0-4f40-80da-bf8dc09f9f7f">4.1</see>.
	/// </summary>
	[Immutable]
	public sealed record InstructConnect() : CoreMessage(PacketType.InstructConnect)
	{
		/// <summary>
		/// A 32-bit field that contains the identifier of the designated client to which the connection is being made. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid Dpnid { get; init; }

		/// <summary>
		/// A 32-bit field that contains the current version of the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_d6292f62-e604-4ac7-9b20-87dde6efb93b">name table</see>.
		/// </summary>
		public int Version { get; init; }

		/// <summary>
		/// Not used.
		/// </summary>
		internal int VersionNotUsed { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(InstructConnect)}:");
			builder.AppendLine($"\t{nameof(Dpnid)}: {Dpnid}");
			builder.AppendLine($"\t{nameof(Version)}: {Version}");
			builder.AppendLine($"\t{nameof(VersionNotUsed)}: {VersionNotUsed}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/c25eab29-d8c1-4d92-a80e-d478fa4b6cb5
	/// <summary>
	/// The DN_SEND_PLAYER_DPNID packet is used to send a user identification number to another client.
	/// </summary>
	[Immutable]
	public sealed record SendPlayerDpnid() : CoreMessage(PacketType.SendPlayerDpnid)
	{
		/// <summary>
		/// A 32-bit field that contains the identifier of the client/peer. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid Dpnid { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(SendPlayerDpnid)}:");
			builder.AppendLine($"\t{nameof(Dpnid)}: {Dpnid}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/87a60f3e-e8d7-4277-bb58-a25a7d74f5ef
	/// <summary>
	/// The DN_INSTRUCTED_CONNECT_FAILED packet is sent from a peer to indicate that it was unable to carry out a host instruction to connect to a new peer.
	/// </summary>
	[Immutable]
	public sealed record InstructedConnectFailed() : CoreMessage(PacketType.InstructedConnectFailed)
	{
		/// <summary>
		/// A 32-bit field that contains the identifier for the peer to which the attempted connection failed. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid Dpnid { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(InstructedConnectFailed)}:");
			builder.AppendLine($"\t{nameof(Dpnid)}: {Dpnid}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/0f03ca85-5bf5-4262-a846-117735a3edca
	/// <summary>
	/// The DN_CONNECT_ATTEMPT_FAILED packet is sent from the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_96048ee4-02d7-484e-a53b-3b8ed355251d">host</see> to a connecting <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_e5d0d91c-9a39-493f-ab1b-f36ce840e6a2">peer</see> to indicate that an existing peer in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_cb5007f6-e2af-44f6-abe1-c8a5ff856254">game session</see> was unable to carry out the host's instruction to connect to a new peer.
	/// </summary>
	[Immutable]
	public sealed record ConnectAttemptFailed() : CoreMessage(PacketType.ConnectAttemptFailed)
	{
		/// <summary>
		/// A 32-bit field that contains the identifier for the existing peer in the game session that was unable to connect to the new peer. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid Dpnid { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(ConnectAttemptFailed)}:");
			builder.AppendLine($"\t{nameof(Dpnid)}: {Dpnid}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/f3ae8504-5358-4af8-b67d-330250c45832
	/// <summary>
	/// The DN_TERMINATE_SESSION packet instructs the client or the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_e5d0d91c-9a39-493f-ab1b-f36ce840e6a2">peer</see> to disconnect from the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_cb5007f6-e2af-44f6-abe1-c8a5ff856254">game session</see>.
	/// </summary>
	[Immutable]
	public sealed record TerminateSession() : CoreMessage(PacketType.TerminateSession)
	{
		/// <summary>
		/// A variable-length field that contains a byte array from the application that describes why the client or the peer is being terminated from the game session.
		/// </summary>
		public IImmutableList<byte> TerminateData { get; init; } = ImmutableArray<byte>.Empty;

		/// <summary>
		/// A 32-bit field that contains the size, in bytes, of the terminate data. If <b>dwTerminateDataOffset</b> is 0, <b>dwTerminateDataSize</b> SHOULD also be 0. If <b>dwTerminateDataOffset</b> is not 0, <b>dwTerminateDataSize</b> SHOULD also not be 0.
		/// </summary>
		internal int TerminateDataSize => TerminateData.Count;

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(TerminateSession)}:");
			builder.AppendLine(
				$"\t{nameof(TerminateData)}: {BitConverter.ToString(TerminateData.ToArray())}"
			);
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/7ec53834-2541-468e-8b14-f6beb304f454
	/// <summary>
	/// The DN_DESTROY_PLAYER packet instructs the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_e5d0d91c-9a39-493f-ab1b-f36ce840e6a2">peer</see> to remove a specified user from its <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_d6292f62-e604-4ac7-9b20-87dde6efb93b">name table</see>.
	/// </summary>
	[Immutable]
	public sealed record DestroyPlayer() : CoreMessage(PacketType.DestroyPlayer)
	{
		/// <summary>
		/// A 32-bit field that contains the identifier of the client or server to remove from the name table. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid DpnidLeaving { get; init; }

		/// <summary>
		/// A 32-bit field that contains the current name table version number.
		/// </summary>
		public int Version { get; init; }

		/// <summary>
		/// Not used.
		/// </summary>
		internal int VersionNotUsed { get; init; }

		/// <summary>
		/// A 32-bit field that contains the reason for terminating the specified client or server.
		/// </summary>
		public DestroyPlayerFlags Reason { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(DestroyPlayer)}:");
			builder.AppendLine($"\t{nameof(DpnidLeaving)}: {DpnidLeaving}");
			builder.AppendLine($"\t{nameof(Version)}: {Version}");
			builder.AppendLine($"\t{nameof(VersionNotUsed)}: {VersionNotUsed}");
			builder.AppendLine($"\t{nameof(Reason)}: {Reason}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/95336dad-e3d8-4475-8c52-40d271977f3b
	/// <summary>
	/// The DN_HOST_MIGRATE packet is sent from the new <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_96048ee4-02d7-484e-a53b-3b8ed355251d">host</see> to all remaining <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_e5d0d91c-9a39-493f-ab1b-f36ce840e6a2">peers</see> in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_cb5007f6-e2af-44f6-abe1-c8a5ff856254">game session</see> to notify them that a migration is taking place.
	/// </summary>
	[Immutable]
	public sealed record HostMigrate() : CoreMessage(PacketType.HostMigrate)
	{
		/// <summary>
		/// A 32-bit field that contains the identifier for the host that has just disconnected. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid DpnidOldHost { get; init; }

		/// <summary>
		/// A 32-bit field that contains the identifier for the newly assigned host that is in the process of migrating. For more information, see section 2.2.7.
		/// </summary>
		public Dpnid DpnidNewHost { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(HostMigrate)}:");
			builder.AppendLine($"\t{nameof(DpnidOldHost)}: {DpnidOldHost}");
			builder.AppendLine($"\t{nameof(DpnidNewHost)}: {DpnidNewHost}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/00550341-76c2-47c8-b8a0-04872f646a13
	/// <summary>
	/// The DN_NAMETABLE_VERSION packet specifies the version number of the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_d6292f62-e604-4ac7-9b20-87dde6efb93b">name table</see>.
	/// </summary>
	[Immutable]
	public sealed record NameTableVersion() : CoreMessage(PacketType.NameTableVersion)
	{
		/// <summary>
		/// A 32-bit field that contains the current name table version number.
		/// </summary>
		public int Version { get; init; }

		/// <summary>
		/// Not used.
		/// </summary>
		internal int VersionNotUsed { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(NameTableVersion)}:");
			builder.AppendLine($"\t{nameof(Version)}: {Version}");
			builder.AppendLine($"\t{nameof(VersionNotUsed)}: {VersionNotUsed}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8155a19e-e173-410b-b750-8bb6668074f5
	/// <summary>
	/// The DN_RESYNC_VERSION packet is used to request that the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_d6292f62-e604-4ac7-9b20-87dde6efb93b">name table</see> version number be resynchronized to the current version number.
	/// </summary>
	[Immutable]
	public sealed record ResyncVersion() : CoreMessage(PacketType.ResyncVersion)
	{
		/// <summary>
		/// A 32-bit field that contains the current name table version number.
		/// </summary>
		public int Version { get; init; }

		/// <summary>
		/// Not used.
		/// </summary>
		internal int VersionNotUsed { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(ResyncVersion)}:");
			builder.AppendLine($"\t{nameof(Version)}: {Version}");
			builder.AppendLine($"\t{nameof(VersionNotUsed)}: {VersionNotUsed}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/91b0cfb1-9a46-476a-b3f0-e34a10554ce5
	/// <summary>
	/// The DN_REQ_INTEGRITY_CHECK packet requests that a <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_96048ee4-02d7-484e-a53b-3b8ed355251d">host</see> determine whether a target client is still in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_cb5007f6-e2af-44f6-abe1-c8a5ff856254">game session</see>.
	/// </summary>
	[Immutable]
	public sealed record RequestIntegrityCheck() : CoreMessage(PacketType.RequestIntegrityCheck)
	{
		/// <summary>
		/// A 32-bit field that contains the context for the request operation. Values for the <b>dwReqContext</b> field SHOULD be ignored by the recipient.
		/// </summary>
		public int RequestContext { get; init; }

		/// <summary>
		/// A 32-bit field that contains the identifier of the selected target peer for the host to validate. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid DpnidTarget { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(RequestIntegrityCheck)}:");
			builder.AppendLine($"\t{nameof(RequestContext)}: {RequestContext}");
			builder.AppendLine($"\t{nameof(DpnidTarget)}: {DpnidTarget}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/cfe3ca4e-8a7b-41fa-82c7-206ab2b74660
	/// <summary>
	/// The DN_INTEGRITY_CHECK packet is a request from a <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_96048ee4-02d7-484e-a53b-3b8ed355251d">host</see> to a peer inquiring whether the peer is still in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_cb5007f6-e2af-44f6-abe1-c8a5ff856254">game session</see>.
	/// </summary>
	[Immutable]
	public sealed record IntegrityCheck() : CoreMessage(PacketType.IntegrityCheck)
	{
		/// <summary>
		/// A 32-bit field that contains the identifier of the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_e5d0d91c-9a39-493f-ab1b-f36ce840e6a2">peer</see> requesting this validation. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid DpnidRequesting { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(IntegrityCheck)}:");
			builder.AppendLine($"\t{nameof(DpnidRequesting)}: {DpnidRequesting}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/c726feed-e889-43c4-a142-2c077d02487c
	/// <summary>
	/// The DN_INTEGRITY_CHECK_RESPONSE packet is a response from a peer to the host confirming that it is still in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_cb5007f6-e2af-44f6-abe1-c8a5ff856254">game session</see>.
	/// </summary>
	[Immutable]
	public sealed record IntegrityCheckResponse() : CoreMessage(PacketType.IntegrityCheckResponse)
	{
		/// <summary>
		/// Identifier of the peer that requested the validation. For more information, see section <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/65b0f61c-4f93-42c9-953f-2299e686b497">2.2.7</see>.
		/// </summary>
		public Dpnid DpnidRequesting { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(IntegrityCheckResponse)}:");
			builder.AppendLine($"\t{nameof(DpnidRequesting)}: {DpnidRequesting}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/66f773bc-8237-4acd-8ff7-634ee2791440
	/// <summary>
	/// The DN_REQ_NAMETABLE_OP packet is sent from the new <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_96048ee4-02d7-484e-a53b-3b8ed355251d">host</see> to a <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_e5d0d91c-9a39-493f-ab1b-f36ce840e6a2">peer</see> with a newer <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_d6292f62-e604-4ac7-9b20-87dde6efb93b">name table</see> to request that the peer send back name table operations that have not yet been performed on the host. If no newer name table exists, this message is not sent.
	/// </summary>
	[Immutable]
	public sealed record RequestNameTableOperations()
		: CoreMessage(PacketType.RequestNameTableOperations)
	{
		/// <summary>
		/// A 32-bit field that contains the current name table version number of the host.
		/// </summary>
		public int Version { get; init; }

		/// <summary>
		/// Not used.
		/// </summary>
		internal int VersionNotUsed { get; init; }

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(RequestNameTableOperations)}:");
			builder.AppendLine($"\t{nameof(Version)}: {Version}");
			builder.AppendLine($"\t{nameof(VersionNotUsed)}: {VersionNotUsed}");
			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/9022cd57-de73-42c6-bfb3-5f996be40623
	/// <summary>
	/// The DN_ACK_NAMETABLE_OP packet is sent from the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_e5d0d91c-9a39-493f-ab1b-f36ce840e6a2">peer</see> that is being queried for <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_d6292f62-e604-4ac7-9b20-87dde6efb93b">name table</see> information back to the new <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_96048ee4-02d7-484e-a53b-3b8ed355251d">host</see>. It will include all entries missing from the new host's name table.
	/// </summary>
	[Immutable]
	public sealed record AckNameTableOperations() : CoreMessage(PacketType.AckNameTableOperations)
	{
		[Immutable]
		internal sealed record Entry
		{
			/// <summary>
			/// A 32-bit field that contains the internal message for the given <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_f3af3b08-b79e-477d-a4ad-76f4168485e4">name table entry</see>.
			/// </summary>
			public PacketType MessageId { get; init; }

			/// <summary>
			/// A variable length field that contains the portion of the packet originally associated with the name table operation, except for the <b>dwPacketType</b> field, as indicated by the <b>dwMsgId</b> field. Each operation buffer is atomic to itself. For example, an <b>op</b> value corresponding to a <b>dwMsgId</b> field value of 0x000000D1 would contain the <b>dpnidLeaving</b>, <b>dwVersion</b>, <b>dwVersionNotUsed</b>, and <b>dwDestroyReason</b> field information from an original DN_DESTROY_PLAYER packet.
			/// </summary>
			public IImmutableList<byte> Operation { get; init; } = ImmutableArray<byte>.Empty;

			/// <summary>
			/// A 32-bit field that contains the size for the given operation buffer.
			/// </summary>
			internal int OperationSize => Operation.Count;

			public Entry() { }

			public Entry(BinaryReader reader)
			{
				MessageId = (PacketType)reader.ReadInt32();
			}

			public byte[] ToByteArray()
			{
				using var stream = new MemoryStream();
				using var writer = new BinaryWriter(stream);

				writer.Write((int)MessageId);

				return stream.ToArray();
			}

			public override string ToString()
			{
				var builder = new StringBuilder();
				builder.AppendLine($"{nameof(Entry)}:");
				builder.AppendLine($"\t{nameof(MessageId)}: {MessageId}");
				builder.AppendLine(
					$"\t{nameof(Operation)}: {BitConverter.ToString(Operation.ToArray())}"
				);
				return builder.ToString();
			}
		}

		internal IImmutableList<Entry> EntriesInternal { get; init; } = ImmutableArray<Entry>.Empty;

		/// <summary>
		/// A 32-bit field that contains the number of name table entries included. The <b>dwMsgId</b>, <b>dwOpOffset</b>, <b>dwOpSize</b>, and <b>op</b> fields are present in a DN_ACK_NAMETABLE_OP message <b>dwNumEntries</b> times.
		/// </summary>
		internal int NumEntries => EntriesInternal.Count;

		public IImmutableList<CoreMessage?> Entries
		{
			get =>
				EntriesInternal
					.Select(entry =>
						CoreMessageSerializer.Default.Deserialize(
							BitConverter
								.GetBytes((int)entry.MessageId)
								.Concat(entry.Operation)
								.ToArray()
						)
					)
					.ToImmutableArray();
			init =>
				EntriesInternal = value
					.Select(entry => new Entry
					{
						MessageId = entry?.PacketType ?? 0,
						Operation = entry is not null
							? CoreMessageSerializer
								.Default.Serialize(entry)
								.Skip(4)
								.ToImmutableArray()
							: ImmutableArray<byte>.Empty,
					})
					.ToImmutableArray();
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(AckNameTableOperations)}:");
			builder.AppendLine($"\t{nameof(NumEntries)}: {NumEntries}");

			foreach (var e in Entries)
				builder.AppendLine(
					string.Join(
						"\n",
						(e?.ToString() ?? string.Empty).Split('\n').Select(l => "\t" + l)
					)
				);

			return builder.ToString();
		}
	}

	// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/327e43ea-e63a-4d7f-87fd-83b6166cfd98
	/// <summary>
	/// The DN_HOST_MIGRATE_COMPLETE packet informs <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_e5d0d91c-9a39-493f-ab1b-f36ce840e6a2">peers</see> that the session-hosting responsibilities have successfully migrated from the departing old <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8cs/8195991d-b7e3-4435-9e9f-2c3ab57eda8c#gt_96048ee4-02d7-484e-a53b-3b8ed355251d">host</see>.
	/// </summary>
	[Immutable]
	public sealed record HostMigrateComplete() : CoreMessage(PacketType.HostMigrateComplete)
	{
		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine($"{nameof(HostMigrateComplete)}:");
			return builder.ToString();
		}
	}
}
