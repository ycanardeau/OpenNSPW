using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Aigamo.Otsuki.Analyzers.Tests;

public class CoreAnalyzerTests
{
	private const string Usings = """
		using System;
		using System.Collections.Generic;
		using System.Collections.Immutable;
		using System.Linq;

		""";

	private const string CoreAssemblyName = "Aigamo.Otsuki.Core";

	private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

	private static readonly ImmutableArray<MetadataReference> References =
	[
		.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path)),
	];

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
		string source,
		string assemblyName = CoreAssemblyName
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

		return await compilation.WithAnalyzers([new CoreAnalyzer()]).GetAnalyzerDiagnosticsAsync();
	}

	private static string GetLocationText(Diagnostic diagnostic)
	{
		var span = diagnostic.Location.SourceSpan;
		return diagnostic.Location.SourceTree!.ToString().Substring(span.Start, span.Length);
	}

	[Theory]
	[InlineData("class C { int _n; void M() => _n++; }")]
	[InlineData("class C { readonly List<int> _items = []; void M() => _items.Add(1); }")]
	[InlineData("class C { TimeSpan Due(TimeSpan now) => now + TimeSpan.FromSeconds(1); }")]
	[InlineData("class C { const int Max = 3; static readonly string Name = \"c\"; }")]
	[InlineData("class C { static ImmutableArray<int> Empty { get; } = []; }")]
	[InlineData("class C { static readonly Func<int, int> Twice = n => n * 2; }")]
	[InlineData("class C { static int Count => 3; }")]
	[InlineData("class C { System.Net.Sockets.AddressFamily Family => default; }")]
	[InlineData(
		"using System.Net.Sockets; class C { AddressFamily Family => AddressFamily.InterNetwork; }"
	)]
	internal async Task NoDiagnostic(string source)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[InlineData(
		"class C { object M() => System.Threading.Tasks.Task.CompletedTask; }",
		"Task"
	)]
	[InlineData(
		"class C { readonly System.Threading.Tasks.TaskCompletionSource _c = new(); }",
		"TaskCompletionSource"
	)]
	[InlineData("class C { void M(System.Threading.CancellationToken token) { } }", "CancellationToken")]
	[InlineData("class C { void M() => System.Threading.ThreadPool.QueueUserWorkItem(_ => { }); }", "ThreadPool")]
	[InlineData("class C { void M(System.Threading.ITimer timer) => timer.Dispose(); }", "ITimer")]
	[InlineData("class C { long M(TimeProvider time) => time.GetTimestamp(); }", "TimeProvider")]
	[InlineData("class C { long M() => System.Diagnostics.Stopwatch.GetTimestamp(); }", "Stopwatch")]
	[InlineData("class C { DateTime M() => DateTime.UtcNow; }", "UtcNow")]
	[InlineData("class C { int M() => Environment.TickCount; }", "TickCount")]
	[InlineData(
		"class C { void M(System.Net.Sockets.Socket socket) => socket.Dispose(); }",
		"Socket"
	)]
	[InlineData(
		"class C { object M() => System.Net.Dns.GetHostAddresses(\"host\"); }",
		"Dns"
	)]
	[InlineData("class C { object M(int[] items) => items.AsParallel(); }", "items.AsParallel()")]
	[InlineData("using System.Threading; class C { }", "System.Threading")]
	[InlineData("class C { async void M() { } }", "async")]
	[InlineData("class C { void M() { Action a = async () => { }; } }", "async")]
	internal async Task ReportsThreads(string source, string reported)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics
			.Should()
			.ContainSingle()
			.Which.Should()
			.Match<Diagnostic>(d =>
				d.Id == CoreAnalyzer.ThreadDiagnosticId && GetLocationText(d) == reported
			);
	}

	[Theory]
	[InlineData("class C { static int _count; }", "_count")]
	[InlineData("class C { static readonly List<int> Items = []; }", "Items")]
	[InlineData("class C { static readonly byte[] Buffer = new byte[4]; }", "Buffer")]
	[InlineData("class C { static string Name { get; set; } = \"c\"; }", "Name")]
	[InlineData("class C { static Dictionary<int, int> Table { get; } = []; }", "Table")]
	internal async Task ReportsStaticState(string source, string reported)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics
			.Should()
			.ContainSingle()
			.Which.Should()
			.Match<Diagnostic>(d =>
				d.Id == CoreAnalyzer.StaticStateDiagnosticId && GetLocationText(d) == reported
			);
	}

	[Theory]
	[InlineData("Aigamo.Otsuki")]
	[InlineData("Aigamo.Otsuki.Actors")]
	internal async Task AllowsTheShell(string assemblyName)
	{
		var diagnostics = await GetDiagnosticsAsync(
			"class C { static int _count; async System.Threading.Tasks.Task M() => await System.Threading.Tasks.Task.Yield(); }",
			assemblyName
		);
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	internal async Task ChecksTheWireFormats()
	{
		var diagnostics = await GetDiagnosticsAsync(
			"class C { static int _count; }",
			assemblyName: "Aigamo.Otsuki.Messages"
		);
		diagnostics.Should().ContainSingle(d => d.Id == CoreAnalyzer.StaticStateDiagnosticId);
	}
}
