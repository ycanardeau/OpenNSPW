using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki;

/// <summary>
/// A group in the session, like DPN_GROUP_INFO, with its members.
/// </summary>
/// <param name="Owner">The player that created the group.</param>
public sealed record GroupInfo(
	Dpnid Id,
	string Name,
	ImmutableArray<byte> Data,
	GroupFlags Flags,
	Dpnid Owner,
	ImmutableArray<Dpnid> Members
);

/// <summary>
/// Why a group went, like DPNDESTROYGROUPREASON_*.
/// </summary>
public enum DestroyGroupReason
{
	Normal = 1,

	/// <summary>
	/// An <see cref="GroupFlags.AutoDestruct"/> group whose creator left.
	/// </summary>
	AutoDestructed = 2,

	SessionTerminated = 3,
}

/// <summary>
/// A DirectPlay operation failed with <see cref="Result"/>, like a failing HRESULT.
/// </summary>
public sealed class DirectPlayException(ResultCode result, string message) : Exception(message)
{
	public ResultCode Result { get; } = result;
}
