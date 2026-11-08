using Aigamo.Otsuki.Messages.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Messages.Tests.Reliable;

public class ReliableMessageSerializerTests
{
	public static TheoryData<byte[], ReliableMessage?> TestData()
	{
		var data = new TheoryData<byte[], ReliableMessage?>();
		foreach (
			var row in DataFrameMessageSerializerTests
				.TestData()
				.Concat(ConnectMessageSerializerTests.TestData())
				.Concat(ConnectedMessageSerializerTests.TestData())
				.Concat(HardDisconnectMessageSerializerTests.TestData())
				.Concat(SackMessageSerializerTests.TestData())
		)
		{
			data.Add((byte[])row[0]!, (ReliableMessage)row[1]!);
		}

		data.Add([0], null);
		data.Add([0, 1, 2, 3], null);
		data.Add([1, 2, 3, 4], null);
		return data;
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
