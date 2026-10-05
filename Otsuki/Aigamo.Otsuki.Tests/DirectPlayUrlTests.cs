using System.Net;
using Aigamo.Otsuki.Session;
using Aigamo.Results;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

public class DirectPlayUrlTests
{
	// Sent by DirectPlay 9 in DN_SEND_CONNECT_INFO.
	private const string SampleUrl =
		"x-directplay:/provider=%7BEBFE7BA0-628D-11D2-AE0F-006097B01411%7D;hostname=192.168.11.100;port=50000;alt=131708fefe8000000000000000290f5fffec4beaf070208fec0a80b64";

	[Fact]
	public void ToEndPoint_ReadsHostnameAndPort()
	{
		var url = new DirectPlayUrl(SampleUrl);

		url.ToEndPoint()
			.Should()
			.Be(
				Result.Ok<IPEndPoint, DirectPlayUrlError>(
					new IPEndPoint(IPAddress.Parse("192.168.11.100"), 50000)
				)
			);
		url.Components["provider"].Should().Be("{EBFE7BA0-628D-11D2-AE0F-006097B01411}");
		url.ToString().Should().Be(SampleUrl);
	}

	[Fact]
	public void FromEndPoint_RoundTrips()
	{
		var endPoint = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 2302);

		var url = DirectPlayUrl.FromEndPoint(endPoint);

		url.Value.Should()
			.Be(
				"x-directplay:/provider=%7BEBFE7BA0-628D-11D2-AE0F-006097B01411%7D;hostname=10.0.0.1;port=2302"
			);
		url.ToEndPoint().Should().Be(Result.Ok<IPEndPoint, DirectPlayUrlError>(endPoint));
	}

	[Theory]
	[InlineData("", nameof(DirectPlayUrlError.NotDirectPlayUrl))]
	[InlineData("http://10.0.0.1:2302", nameof(DirectPlayUrlError.NotDirectPlayUrl))]
	[InlineData("x-directplay:/port=2302", nameof(DirectPlayUrlError.MissingHostname))]
	[InlineData("x-directplay:/hostname=10.0.0.1", nameof(DirectPlayUrlError.InvalidPort))]
	[InlineData("x-directplay:/hostname=10.0.0.1;port=abc", nameof(DirectPlayUrlError.InvalidPort))]
	[InlineData(
		"x-directplay:/hostname=10.0.0.1;port=65536",
		nameof(DirectPlayUrlError.InvalidPort)
	)]
	public void ToEndPoint_ReportsWhyTheUrlIsUnusable(string value, string error)
	{
		new DirectPlayUrl(value)
			.ToEndPoint()
			.Should()
			.Be(
				Result.Error<IPEndPoint, DirectPlayUrlError>(Enum.Parse<DirectPlayUrlError>(error))
			);
	}
}
