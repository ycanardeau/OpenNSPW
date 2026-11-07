using Aigamo.Otsuki.Messages.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Reliable;

public class ReliableMessageSerializerTests
{
	public static IEnumerable<object?[]> TestData()
	{
		static IEnumerable<object?[]> TestDataCore()
		{
			yield return new object?[] { new byte[] { 0 }, null };
			yield return new object?[] { new byte[] { 0, 1, 2, 3 }, null };
			yield return new object?[] { new byte[] { 1, 2, 3, 4 }, null };
		}

		return DataFrameMessageSerializerTests
			.TestData()
			.Concat(ConnectMessageSerializerTests.TestData())
			.Concat(ConnectedMessageSerializerTests.TestData())
			.Concat(HardDisconnectMessageSerializerTests.TestData())
			.Concat(SackMessageSerializerTests.TestData())
			.Concat(TestDataCore());
	}

	[Theory]
	[MemberData(nameof(TestData))]
	internal void Deserialize(byte[] data, ReliableMessage? expected)
	{
		static byte[]? Serialize(ReliableMessage? message) =>
			message is not null ? ReliableMessageSerializer.Default.Serialize(message) : null;
		Serialize(ReliableMessageSerializer.Default.Deserialize(data))
			.Should()
			.Equal(Serialize(expected));
	}
}
