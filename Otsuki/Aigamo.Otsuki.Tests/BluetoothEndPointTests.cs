using System.Net;
using Aigamo.Otsuki.Bluetooth;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

public class BluetoothEndPointTests
{
	[Theory]
	[InlineData("00:1A:7D:DA:71:13", 0x001A7DDA7113UL)]
	[InlineData("00-1a-7d-da-71-13", 0x001A7DDA7113UL)]
	[InlineData("FF:FF:FF:FF:FF:FF", 0xFFFFFFFFFFFFUL)]
	public void Parse_ReadsTheAddress(string text, ulong address)
	{
		var endPoint = BluetoothEndPoint.Parse(text);

		endPoint.Address.Should().Be(address);
		endPoint.ToString().Should().Be(text.ToUpperInvariant().Replace('-', ':'));
	}

	[Theory]
	[InlineData("")]
	[InlineData("00:1A:7D:DA:71")]
	[InlineData("00:1A:7D:DA:71:13:00")]
	[InlineData("00:1A:7D:DA:71:1")]
	[InlineData("00:1A:7D:DA:71:GG")]
	[InlineData("192.168.0.1")]
	public void TryParse_RejectsWhatIsNotAnAddress(string text)
	{
		BluetoothEndPoint.TryParse(text, out var endPoint).Should().BeFalse();
		endPoint.Should().BeNull();
	}

	[Fact]
	public void Constructor_RejectsMoreThan48Bits()
	{
		FluentActions
			.Invoking(() => new BluetoothEndPoint(1UL << 48))
			.Should()
			.Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void EndPoints_AreEqualByAddress()
	{
		var endPoints = new Dictionary<EndPoint, int> { [new BluetoothEndPoint(0x1234)] = 1 };

		endPoints.Should().ContainKey(BluetoothEndPoint.Parse("00:00:00:00:12:34"));
		new BluetoothEndPoint(0x1234).Should().NotBe(new BluetoothEndPoint(0x1235));
		new BluetoothEndPoint(0x1234).Should().NotBe(new NamedEndPoint("00:00:00:00:12:34"));
	}

	// The test host declares no use of Bluetooth, so the tests must never touch it: on macOS, the first call would end
	// the process.
	[Fact]
	public void IsUsageDescribed_IsFalseForTheTestHost()
	{
		BluetoothTransport.IsUsageDescribed.Should().BeFalse();
	}

	// Windows asks desktop apps for no declaration.
	[Fact]
	public void IsAllowed_OnlyOnWindowsForTheTestHost()
	{
		BluetoothTransport.IsAllowed.Should().Be(OperatingSystem.IsWindows());
	}

	[Fact]
	public void CreateFactory_RejectsANonBluetoothEndPoint()
	{
		var factory = BluetoothTransport.CreateFactory(Guid.NewGuid(), "Test");

		FluentActions
			.Invoking(() => factory(new IPEndPoint(IPAddress.Any, 0)))
			.Should()
			.Throw<NotSupportedException>();
	}
}
