// Comments from: https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-dplhp/f6e87f5c-716b-46a5-8061-8a22d391997f
// Comments from: https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-dplhp/ef5b29b4-cc5b-4aea-82a4-c0f55296f1a2

using System.Collections.Immutable;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Messages.Enumeration;

/// <summary>
/// The messages of host and port enumeration ([MC-DPLHP]). They travel as plain datagrams, outside any connection, and
/// are told apart from the frames of [MC-DPL8R] by a first byte of 0.
/// </summary>
[GenerateMatch]
public abstract record EnumerationMessage
{
	private EnumerationMessage() { }

	/// <summary>
	/// Queries for hosts of a game session ([MC-DPLHP] section 2.2.1).
	/// </summary>
	[Immutable]
	public sealed record Query : EnumerationMessage
	{
		/// <summary>
		/// A value chosen by the sender and echoed in the response, to match responses to queries.
		/// </summary>
		public ushort EnumPayload { get; init; }

		/// <summary>
		/// Only hosts of this application should respond. <see langword="null"/> asks every host.
		/// </summary>
		public Guid? GuidApplication { get; init; }

		/// <summary>
		/// Application-specific data for the host to decide whether and how to respond.
		/// </summary>
		public IImmutableList<byte> ApplicationPayload { get; init; } = ImmutableArray<byte>.Empty;

		public override string ToString() =>
			$"{nameof(Query)} [{nameof(EnumPayload)}={EnumPayload}, {nameof(GuidApplication)}={GuidApplication}]";
	}

	/// <summary>
	/// A host's answer to a <see cref="Query"/> ([MC-DPLHP] section 2.2.2).
	/// </summary>
	[Immutable]
	public sealed record Response : EnumerationMessage
	{
		/// <summary>
		/// The <see cref="Query.EnumPayload"/> of the query this answers.
		/// </summary>
		public ushort EnumPayload { get; init; }

		/// <summary>
		/// Never has <see cref="SessionFlags.NoEnumerations"/>.
		/// </summary>
		public SessionFlags Flags { get; init; }

		public int MaxPlayers { get; init; }

		/// <summary>
		/// The number of players when the response was sent.
		/// </summary>
		public int CurrentPlayers { get; init; }

		public Guid GuidInstance { get; init; }

		public Guid GuidApplication { get; init; }

		public string SessionName { get; init; } = string.Empty;

		/// <summary>
		/// Game-specific data that changes infrequently, such as data set when the session was created.
		/// </summary>
		public IImmutableList<byte> ApplicationReservedData { get; init; } =
			ImmutableArray<byte>.Empty;

		/// <summary>
		/// Game-specific data that changes frequently, such as the current state of the game.
		/// </summary>
		public IImmutableList<byte> ApplicationData { get; init; } = ImmutableArray<byte>.Empty;

		public override string ToString() =>
			$"{nameof(Response)} [{nameof(EnumPayload)}={EnumPayload}, {nameof(SessionName)}={SessionName}, "
			+ $"{nameof(CurrentPlayers)}={CurrentPlayers}/{MaxPlayers}, {nameof(GuidInstance)}={GuidInstance}]";
	}
}
