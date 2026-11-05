using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Aigamo.Otsuki.Analyzers.Tests;

public class ConcurrencyAnalyzerTests
{
	private const string Usings = """
		using System;
		using System.Collections.Generic;
		using System.Threading;
		using System.Threading.Tasks;

		""";

	private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

	private static readonly ImmutableArray<MetadataReference> References =
	[
		.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path)),
	];

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
		string source,
		string assemblyName = "Tests"
	)
	{
		var compilation = CSharpCompilation.Create(
			assemblyName: assemblyName,
			syntaxTrees: [CSharpSyntaxTree.ParseText(Usings + source, ParseOptions)],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		compilation
			.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.Should()
			.BeEmpty();

		return await compilation
			.WithAnalyzers([new ConcurrencyAnalyzer()])
			.GetAnalyzerDiagnosticsAsync();
	}

	private static string GetLocationText(Diagnostic diagnostic)
	{
		var span = diagnostic.Location.SourceSpan;
		return diagnostic.Location.SourceTree!.ToString().Substring(span.Start, span.Length);
	}

	[Theory]
	[InlineData("class C { int _n; void M() => _n++; }")]
	[InlineData("class C { readonly List<int> _items = []; void M() => _items.Add(1); }")]
	[InlineData("class C { async Task M() { var t = new TaskCompletionSource(); await t.Task; } }")]
	[InlineData(
		"class C { void M(CancellationToken token) => token.ThrowIfCancellationRequested(); }"
	)]
	[InlineData("class C { [ThreadStatic] static int _n; }")]
	[InlineData("class C { void M(Thread thread) => thread.Join(); }")]
	internal async Task NoDiagnostic(string source)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[InlineData("class C { readonly object _gate = new(); void M() { lock (_gate) { } } }", "lock")]
	[InlineData("class C { volatile bool _done; }", "volatile")]
	[InlineData("class C { int _n; void M() => Interlocked.Increment(ref _n); }", "Interlocked")]
	[InlineData("class C { bool _b; bool M() => Volatile.Read(ref _b); }", "Volatile")]
	[InlineData("class C { void M(object o) => Monitor.Enter(o); }", "Monitor")]
	[InlineData("class C { readonly Lock _lock = new(); }", "Lock")]
	[InlineData("class C { readonly SemaphoreSlim _s = new(0); }", "SemaphoreSlim")]
	[InlineData("class C { readonly ManualResetEventSlim _e = new(); }", "ManualResetEventSlim")]
	[InlineData(
		"class C { readonly System.Collections.Concurrent.ConcurrentQueue<int> _q = new(); }",
		"ConcurrentQueue<int>"
	)]
	[InlineData(
		"class C { void M() => System.Threading.Channels.Channel.CreateUnbounded<int>(); }",
		"Channel"
	)]
	[InlineData(
		"using System.Collections.Concurrent; class C { }",
		"System.Collections.Concurrent"
	)]
	internal async Task ReportsSharedState(string source, string reported)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics
			.Should()
			.ContainSingle()
			.Which.Should()
			.Match<Diagnostic>(d =>
				d.Id == ConcurrencyAnalyzer.SharedStateDiagnosticId
				&& GetLocationText(d) == reported
			);
	}

	[Fact]
	internal async Task AllowsTheActorsAssembly()
	{
		var diagnostics = await GetDiagnosticsAsync(
			"class C { int _n; void M() => Interlocked.Increment(ref _n); }",
			assemblyName: "Aigamo.Otsuki.Actors"
		);
		diagnostics.Should().BeEmpty();
	}
}
