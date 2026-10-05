namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// Timing and sizing parameters of the DirectPlay 8 reliable protocol.
/// The defaults are the values recommended by [MC-DPL8R] section 3.1.2.
/// </summary>
public sealed record ReliableProfile
{
	public static ReliableProfile Default { get; } = new();

	public TimeSpan ConnectRetryInterval { get; init; } = TimeSpan.FromMilliseconds(200);

	public TimeSpan MaxConnectRetryInterval { get; init; } = TimeSpan.FromSeconds(5);

	public int MaxConnectRetries { get; init; } = 14;

	public TimeSpan DelayedAcknowledgmentTimeout { get; init; } = TimeSpan.FromMilliseconds(100);

	/// <summary>
	/// Used when acknowledging out-of-order or duplicate packets.
	/// </summary>
	public TimeSpan ShortDelayedAcknowledgmentTimeout { get; init; } =
		TimeSpan.FromMilliseconds(20);

	public TimeSpan DelayedSendMaskTimeout { get; init; } = TimeSpan.FromMilliseconds(40);

	/// <summary>
	/// Round-trip time assumed until the first measurement.
	/// </summary>
	public TimeSpan InitialRoundTripTime { get; init; } = TimeSpan.FromMilliseconds(100);

	public TimeSpan MaxRetryInterval { get; init; } = TimeSpan.FromSeconds(5);

	public int MaxRetries { get; init; } = 10;

	public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromSeconds(25);

	public TimeSpan HardDisconnectInterval { get; init; } = TimeSpan.FromMilliseconds(100);

	public int HardDisconnectCount { get; init; } = 3;

	/// <summary>
	/// How long to wait for the partner's END_STREAM after our own END_STREAM was acknowledged.
	/// </summary>
	public TimeSpan GracefulDisconnectTimeout { get; init; } = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Maximum number of unacknowledged data frames. The protocol allows at most 64.
	/// </summary>
	public int MaxWindowSize { get; init; } = 64;

	/// <summary>
	/// Maximum payload carried by a single data frame. Larger messages are split.
	/// </summary>
	public int MaxFramePayloadSize { get; init; } = 1200;

	/// <summary>
	/// Maximum size of a reassembled multi-frame message. Larger messages terminate the connection.
	/// </summary>
	public int MaxMessageSize { get; init; } = 1024 * 1024;
}
