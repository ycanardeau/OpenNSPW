using System.Collections.Immutable;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aigamo.Otsuki.Messages.Enumeration;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Enumeration;

/// <summary>
/// The client side of host enumeration ([MC-DPLHP]): sends queries on a transport of its own, at regular intervals,
/// and matches the responses to them by <b>EnumPayload</b> to measure the round-trip time. Each enumeration is
/// independent of the peer's session, so a peer can enumerate before it hosts or connects.
/// </summary>
internal static class HostEnumerator
{
	private static ApplicationDescription ToApplication(EnumerationMessage.Response response) =>
		new()
		{
			GuidApplication = response.GuidApplication,
			GuidInstance = response.GuidInstance,
			Flags = response.Flags,
			MaxPlayers = response.MaxPlayers,
			SessionName = response.SessionName,
			ApplicationReservedData = [.. response.ApplicationReservedData],
		};

	public static async IAsyncEnumerable<EnumeratedHost> EnumerateAsync(
		PeerOptions options,
		EndPoint target,
		Guid? guidApplication,
		ImmutableArray<byte> payload,
		EnumHostsOptions enumOptions,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	)
	{
		var timeProvider = options.TimeProvider;
		var responses = Channel.CreateUnbounded<(
			EndPoint Sender,
			EnumerationMessage.Response Response
		)>(new UnboundedChannelOptions { SingleReader = true });
		using IDatagramTransport transport = options.TransportFactory(AnyEndPoint.For(target));
		transport.Start(
			(sender, datagram) =>
			{
				if (
					EnumerationMessageSerializer.Default.Deserialize(datagram)
					is EnumerationMessage.Response response
				)
					responses.Writer.TryWrite((sender, response));
			}
		);

		// Queries are numbered from a random start, so that responses to an earlier enumeration do not match.
		var first = (ushort)Random.Shared.Next(ushort.MaxValue + 1);
		var sentAt = new Dictionary<ushort, long>();
		var start = timeProvider.GetTimestamp();
		var sent = 0;
		var count = Math.Max(1, enumOptions.Count);
		var end = enumOptions.RetryInterval * (count - 1) + enumOptions.Timeout;

		void SendDueQueries()
		{
			var elapsed = timeProvider.GetElapsedTime(start);
			while (sent < count && enumOptions.RetryInterval * sent <= elapsed)
			{
				var enumPayload = (ushort)(first + sent);
				sentAt[enumPayload] = timeProvider.GetTimestamp();
				transport.Send(
					target,
					EnumerationMessageSerializer.Default.Serialize(
						new EnumerationMessage.Query
						{
							EnumPayload = enumPayload,
							GuidApplication = guidApplication,
							ApplicationPayload = payload,
						}
					)
				);
				sent++;
			}
		}

		while (true)
		{
			SendDueQueries();
			var elapsed = timeProvider.GetElapsedTime(start);
			if (elapsed >= end)
				yield break;

			var nextQuery = sent < count ? enumOptions.RetryInterval * sent : end;
			var wait = (nextQuery < end ? nextQuery : end) - elapsed;
			using var delay = new CancellationTokenSource(
				wait > TimeSpan.Zero ? wait : TimeSpan.Zero,
				timeProvider
			);
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
				cancellationToken,
				delay.Token
			);
			(EndPoint Sender, EnumerationMessage.Response Response) item;
			try
			{
				item = await responses.Reader.ReadAsync(timeout.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				continue;
			}

			if (!sentAt.TryGetValue(item.Response.EnumPayload, out var queriedAt))
				continue;

			if (guidApplication is Guid guid && item.Response.GuidApplication != guid)
				continue;

			yield return new EnumeratedHost(
				item.Sender,
				ToApplication(item.Response),
				item.Response.CurrentPlayers,
				[.. item.Response.ApplicationData],
				timeProvider.GetElapsedTime(queriedAt)
			);
		}
	}
}
