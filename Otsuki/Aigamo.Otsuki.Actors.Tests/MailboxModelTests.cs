using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Actors.Tests;

/// <summary>
/// The model that GenMC checks (Aigamo.Otsuki.Actors.Verification) follows Mailbox.cs as it is: it records the hash of
/// the Mailbox.cs it follows, which must be this one's.
/// </summary>
public sealed partial class MailboxModelTests
{
	[GeneratedRegex(@"^// Mailbox\.cs sha256: ([0-9a-f]{64})$", RegexOptions.Multiline)]
	private static partial Regex RecordedHash();

	private static string OtsukiDirectory([CallerFilePath] string path = "") =>
		Path.GetDirectoryName(Path.GetDirectoryName(path))!;

	// The file with its lines ended by LF, as the repository has them, whatever a checkout ends them with: Git for
	// Windows, as on the CI's Windows runners, ends them with CRLF, where a multiline $ would not match before the CR.
	private static string ReadWithLf(string path) => File.ReadAllText(path).Replace("\r\n", "\n");

	private static string HashOf(string path) =>
		Convert
			.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ReadWithLf(path))))
			.ToLowerInvariant();

	[Fact]
	public void TheModelFollowsTheMailboxAsItIs()
	{
		var model = ReadWithLf(
			Path.Combine(OtsukiDirectory(), "Aigamo.Otsuki.Actors.Verification", "mailbox.h")
		);
		var recorded = RecordedHash().Match(model);
		recorded.Success.Should().BeTrue("the model records the hash of the Mailbox.cs it follows");

		recorded
			.Groups[1]
			.Value.Should()
			.Be(
				HashOf(Path.Combine(OtsukiDirectory(), "Aigamo.Otsuki.Actors", "Mailbox.cs")),
				"Mailbox.cs has changed since the model was brought along: change mailbox.h as Mailbox.cs changed, run "
					+ "verify.sh, and record the new hash"
			);
	}
}
