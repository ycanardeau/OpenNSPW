using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Aigamo.Otsuki.Analyzers.Tests;

public class ActorStateAnalyzerTests
{
	// Actor<TMessage> as Aigamo.Otsuki.Actors has it, with what the analyzer looks at.
	private const string Actors = """
		using System;
		using System.Collections.Generic;
		using System.Threading;
		using System.Threading.Tasks;

		namespace Aigamo.Otsuki.Actors
		{
			public abstract class Actor<TMessage>
			{
				public int QueueLength => 0;
				public bool Post(TMessage message) => true;
				protected void Stop() { }
				protected abstract void Receive(TMessage message);
			}

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class CalledOnAnotherThreadAttribute : Attribute;
		}

		namespace Tests
		{
			using Aigamo.Otsuki.Actors;

			public interface ITransport
			{
				void Start([CalledOnAnotherThread] Action<int> onReceive);
				void Each(Action<int> run);
			}

			public interface IOutputs
			{
				void Send(int value);
			}

		""";

	private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

	private static readonly ImmutableArray<MetadataReference> References =
	[
		.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path)),
	];

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
	{
		var compilation = CSharpCompilation.Create(
			assemblyName: "Tests",
			syntaxTrees: [CSharpSyntaxTree.ParseText(Actors + source + "\n}", ParseOptions)],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		compilation
			.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.Should()
			.BeEmpty();

		return await compilation
			.WithAnalyzers([new ActorStateAnalyzer()])
			.GetAnalyzerDiagnosticsAsync();
	}

	private static string GetLocationText(Diagnostic diagnostic)
	{
		var span = diagnostic.Location.SourceSpan;
		return diagnostic.Location.SourceTree!.ToString().Substring(span.Start, span.Length);
	}

	[Theory]
	// The actor's own thread: Receive, the constructor, the outputs its core calls, and a function run there.
	[InlineData("class A : Actor<int> { int _n; protected override void Receive(int m) => _n++; }")]
	[InlineData(
		"class A : Actor<int> { int _n; A() { _n = 1; } protected override void Receive(int m) { } }"
	)]
	[InlineData(
		"class A : Actor<int>, IOutputs { int _n; void IOutputs.Send(int value) => _n += value; protected override void Receive(int m) { } }"
	)]
	[InlineData(
		"class A : Actor<int> { int _n; readonly ITransport _t; protected override void Receive(int m) => _t.Each(v => _n += v); }"
	)]
	// What any thread may do: post, read what is set once, and give a function to be run on the actor's thread.
	[InlineData(
		"class A : Actor<int> { public void Ping() => Post(1); protected override void Receive(int m) { } }"
	)]
	[InlineData(
		"class A : Actor<int> { readonly ITransport _t; public A(ITransport t) { _t = t; _t.Start(v => Post(v)); } protected override void Receive(int m) { } }"
	)]
	[InlineData(
		"class A : Actor<int> { readonly int[] _sizes = []; int Size(int i) => i * 2; public A(ITransport t) => t.Start(v => Post(Size(v))); protected override void Receive(int m) { } }"
	)]
	[InlineData(
		"class A : Actor<Action> { int _n; public void Bump() => Post(() => _n++); protected override void Receive(Action m) => m(); }"
	)]
	[InlineData(
		"class A : Actor<int> { readonly CancellationToken _token; async Task RunAsync() { await Task.Delay(1, _token); Post(1); } protected override void Receive(int m) { } }"
	)]
	[InlineData(
		"class A : Actor<int> { public string Name { get; } = \"a\"; public int Waiting => QueueLength; protected override void Receive(int m) { } }"
	)]
	internal async Task NoDiagnostic(string source)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[InlineData(
		"class A : Actor<int> { int _n; public int Count => _n; protected override void Receive(int m) => _n++; }",
		"Count",
		"_n"
	)]
	[InlineData(
		"class A : Actor<int> { readonly List<int> _items = []; public void Add(int i) => _items.Add(i); protected override void Receive(int m) { } }",
		"Add",
		"_items"
	)]
	[InlineData(
		"class A : Actor<int> { int _n; void Bump() => _n++; public void Go() => Bump(); protected override void Receive(int m) { } }",
		"Go",
		"_n, through Bump"
	)]
	[InlineData(
		"class A : Actor<int> { public void End() => Stop(); protected override void Receive(int m) { } }",
		"End",
		"Stop, not the actor's own"
	)]
	[InlineData(
		"class A : Actor<int> { int _n; protected override void Receive(int m) => Task.Run(() => _n++); }",
		"() => _n++",
		"_n"
	)]
	[InlineData(
		"class A : Actor<int> { int _n; void Bump() => _n++; protected override void Receive(int m) => Task.Run(Bump); }",
		"Bump",
		"_n"
	)]
	[InlineData(
		"class A : Actor<int> { int _n; public A(ITransport t) => t.Start(v => _n += v); protected override void Receive(int m) { } }",
		"v => _n += v",
		"_n"
	)]
	[InlineData(
		"class A : Actor<int> { int _n; protected override void Receive(int m) => new Timer(_ => _n++); }",
		"_ => _n++",
		"_n"
	)]
	[InlineData(
		"class A : Actor<int> { int _n; protected override void Receive(int m) => CancellationToken.None.Register(() => _n++); }",
		"() => _n++",
		"_n"
	)]
	[InlineData(
		"class A : Actor<int> { int _n; async Task RunAsync() { await Task.Yield(); _n++; } protected override void Receive(int m) { } }",
		"RunAsync",
		"_n"
	)]
	internal async Task ReportsStateTouchedOnAnotherThread(
		string source,
		string reported,
		string touched
	)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics
			.Should()
			.ContainSingle()
			.Which.Should()
			.Match<Diagnostic>(d =>
				d.Id == ActorStateAnalyzer.StateDiagnosticId
				&& GetLocationText(d) == reported
				&& d.GetMessage().Contains($"({touched})")
			);
	}

	[Fact]
	internal async Task LooksOnlyAtActors()
	{
		var diagnostics = await GetDiagnosticsAsync(
			"class C { int _n; public int Count => _n; public void Go() => Task.Run(() => _n++); }"
		);
		diagnostics.Should().BeEmpty();
	}
}
