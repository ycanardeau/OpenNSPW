using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Aigamo.Otsuki.Analyzers.Tests;

public class AtomicsAnalyzerTests
{
	private const string Usings = """
		using System;
		using System.Threading;

		""";

	private const string ActorsAssemblyName = "Aigamo.Otsuki.Actors";

	private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

	private static readonly ImmutableArray<MetadataReference> References =
	[
		.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path)),
	];

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
		string source,
		string assemblyName = ActorsAssemblyName
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
			.WithAnalyzers([new AtomicsAnalyzer()])
			.GetAnalyzerDiagnosticsAsync();
	}

	private static string GetLocationText(Diagnostic diagnostic)
	{
		var span = diagnostic.Location.SourceSpan;
		return diagnostic.Location.SourceTree!.ToString().Substring(span.Start, span.Length);
	}

	[Theory]
	[InlineData(
		"class C { int _n; void M() => Interlocked.Increment(ref _n); int N => Interlocked.CompareExchange(ref _n, 0, 0); }"
	)]
	[InlineData("class C { int _n; C() { _n = 1; } void M() => Interlocked.Exchange(ref _n, 0); }")]
	[InlineData("class C { int _n = 1; void M() => Interlocked.Exchange(ref _n, 0); }")]
	[InlineData(
		"class C { long _n; long M() => Interlocked.Read(ref _n); void N() => Interlocked.Add(ref _n, 1); }"
	)]
	[InlineData("class C { int _owned; void M() => _owned++; }")]
	[InlineData(
		"class C { object _o = new(); object M() => Interlocked.CompareExchange(ref _o, null!, null!); }"
	)]
	internal async Task NoDiagnostic(string source)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[InlineData(
		"class C { int _n; void M() => Interlocked.Increment(ref _n); int N => _n; }",
		"_n"
	)]
	[InlineData("class C { int _n; void M() { _n = 0; Interlocked.Exchange(ref _n, 1); } }", "_n")]
	[InlineData(
		"class C { int _n; void M() => Interlocked.Increment(ref _n); void Set(ref int word) { } void N() => Set(ref _n); }",
		"_n"
	)]
	[InlineData(
		"class C { int _n; void M() => Volatile.Write(ref _n, 0); }",
		"Volatile.Write(ref _n, 0)"
	)]
	[InlineData("class C { int _n; int M() => Volatile.Read(ref _n); }", "Volatile.Read(ref _n)")]
	[InlineData("class C { volatile int _n; }", "_n")]
	internal async Task ReportsASharedWordOutsideInterlocked(string source, string reported)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics
			.Should()
			.ContainSingle()
			.Which.Should()
			.Match<Diagnostic>(d =>
				d.Id == AtomicsAnalyzer.SharedWordDiagnosticId && GetLocationText(d) == reported
			);
	}

	// The other assemblies use no atomics at all (OTSUKI0003).
	[Fact]
	internal async Task AnalyzesOnlyTheActorsAssembly()
	{
		var diagnostics = await GetDiagnosticsAsync(
			"class C { int _n; void M() => Volatile.Write(ref _n, 0); }",
			assemblyName: "Tests"
		);
		diagnostics.Should().BeEmpty();
	}
}
