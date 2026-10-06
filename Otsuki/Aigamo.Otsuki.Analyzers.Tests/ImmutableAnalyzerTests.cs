using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Aigamo.Otsuki.Analyzers.Tests;

public class ImmutableAnalyzerTests
{
	private const string ImmutableAttributeSource = """
		namespace Aigamo.Otsuki.Messages
		{
			[System.AttributeUsage(
				System.AttributeTargets.Class
					| System.AttributeTargets.Struct
					| System.AttributeTargets.Interface
			)]
			internal sealed class ImmutableAttribute : System.Attribute { }
		}
		""";

	private const string SupportTypesSource = """
		using Aigamo.Otsuki.Messages;

		enum E { A }
		readonly struct RS { }
		readonly record struct RRS(int Value);
		struct MS { public int Value; }
		class M { public int Value; }
		[Immutable] sealed class IC { }
		[Immutable] interface II { }
		[Immutable] sealed class G<T> { public T Value { get; } }
		""";

	private const string Usings = """
		using System;
		using System.Collections.Generic;
		using System.Collections.Immutable;
		using Aigamo.Otsuki.Messages;

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
			syntaxTrees:
			[
				CSharpSyntaxTree.ParseText(ImmutableAttributeSource, ParseOptions),
				CSharpSyntaxTree.ParseText(SupportTypesSource, ParseOptions),
				CSharpSyntaxTree.ParseText(Usings + source, ParseOptions),
			],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		compilation
			.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.Should()
			.BeEmpty();

		return await compilation
			.WithAnalyzers([new ImmutableAnalyzer()])
			.GetAnalyzerDiagnosticsAsync();
	}

	private static string GetLocationText(Diagnostic diagnostic)
	{
		var span = diagnostic.Location.SourceSpan;
		return diagnostic.Location.SourceTree!.ToString().Substring(span.Start, span.Length);
	}

	[Theory]
	[InlineData("[Immutable] class C { public int P { get; } }")]
	[InlineData("[Immutable] class C { public int P { get; init; } }")]
	[InlineData("[Immutable] class C { public int P => 0; }")]
	[InlineData("[Immutable] class C { public int P { get => 0; } }")]
	[InlineData("[Immutable] class C { private int P { get; init; } }")]
	[InlineData("[Immutable] class C { public int this[int i] => i; }")]
	[InlineData("[Immutable] struct S { public int P { get; init; } }")]
	[InlineData("[Immutable] record R(int P);")]
	[InlineData("[Immutable] readonly record struct S(int P);")]
	[InlineData("class C { public int P { get; set; } }")]
	[InlineData("class C { public List<int> P { get; set; } }")]
	internal async Task NoDiagnostic(string source)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[InlineData("[Immutable] class C { public int P { get; set; } }")]
	[InlineData("[Immutable] class C { public int P { get; private set; } }")]
	[InlineData("[Immutable] class C { private int P { get; set; } }")]
	[InlineData("[Immutable] class C { public static int P { get; set; } }")]
	[InlineData("[Immutable] class C { public int P { set { } } }")]
	[InlineData("[Immutable] struct S { public int P { get; set; } }")]
	[InlineData("[Immutable] record R { public int P { get; set; } }")]
	[InlineData("[Immutable] record struct S(int P);")]
	[InlineData("class Outer { [Immutable] class C { public int P { get; set; } } }")]
	internal async Task MutableSetter(string source)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics.Should().ContainSingle();
		diagnostics[0].Id.Should().Be(ImmutableAnalyzer.MutableSetterDiagnosticId);
		diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
		diagnostics[0].GetMessage().Should().StartWith("Property 'P' ");
	}

	[Fact]
	internal async Task MutableSetter_Indexer()
	{
		var diagnostics = await GetDiagnosticsAsync(
			"[Immutable] class C { public int this[int i] { get => i; set { } } }"
		);
		diagnostics.Should().ContainSingle();
		diagnostics[0].Id.Should().Be(ImmutableAnalyzer.MutableSetterDiagnosticId);
	}

	[Fact]
	internal async Task MutableSetter_OnlyMutablePropertiesAreReported()
	{
		var diagnostics = await GetDiagnosticsAsync(
			"""
			[Immutable]
			class C
			{
				public int A { get; init; }
				public int B { get; set; }
				public int C2 { get; }
				public int D { get; set; }
			}
			"""
		);
		diagnostics
			.Select(d => d.GetMessage())
			.Should()
			.BeEquivalentTo(
				"Property 'B' of immutable type 'C' must be get-only or init-only",
				"Property 'D' of immutable type 'C' must be get-only or init-only"
			);
	}

	[Fact]
	internal async Task MutableSetter_LocationIsSetAccessor()
	{
		var diagnostics = await GetDiagnosticsAsync(
			"[Immutable] class C { public int P { get; set; } }"
		);
		GetLocationText(diagnostics.Single()).Should().Be("set");
	}

	[Fact]
	internal async Task MutableSetter_LocationIsParameterForPositionalRecordStruct()
	{
		var diagnostics = await GetDiagnosticsAsync("[Immutable] record struct S(int P);");
		GetLocationText(diagnostics.Single()).Should().Be("P");
	}

	[Theory]
	[InlineData("bool")]
	[InlineData("char")]
	[InlineData("double")]
	[InlineData("decimal")]
	[InlineData("string")]
	[InlineData("string?")]
	[InlineData("int?")]
	[InlineData("Guid")]
	[InlineData("DateTime")]
	[InlineData("DateTimeOffset")]
	[InlineData("TimeSpan")]
	[InlineData("E")]
	[InlineData("RS")]
	[InlineData("RRS")]
	[InlineData("RRS?")]
	[InlineData("IC")]
	[InlineData("II")]
	[InlineData("G<int>")]
	[InlineData("G<IC>")]
	[InlineData("ImmutableArray<byte>")]
	[InlineData("IImmutableList<string>")]
	[InlineData("ImmutableDictionary<string, ImmutableArray<IC>>")]
	internal async Task ImmutableType(string type)
	{
		var diagnostics = await GetDiagnosticsAsync(
			$"[Immutable] class C {{ public {type} P {{ get; }} }}"
		);
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	internal async Task ImmutableType_TypeParameter()
	{
		var diagnostics = await GetDiagnosticsAsync(
			"[Immutable] class C<T> { public T P { get; } }"
		);
		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[InlineData("int[]", "int[]")]
	[InlineData("List<int>", "List<int>")]
	[InlineData("IReadOnlyList<int>", "IReadOnlyList<int>")]
	[InlineData("IEnumerable<int>", "IEnumerable<int>")]
	[InlineData("Dictionary<string, int>", "Dictionary<string, int>")]
	[InlineData("object", "object")]
	[InlineData("System.Text.StringBuilder", "StringBuilder")]
	[InlineData("Func<int>", "Func<int>")]
	[InlineData("(int, int)", "(int, int)")]
	[InlineData("M", "M")]
	[InlineData("MS", "MS")]
	[InlineData("MS?", "MS")]
	[InlineData("ImmutableArray<int>.Builder", "ImmutableArray<int>.Builder")]
	[InlineData("ImmutableArray<List<int>>", "List<int>")]
	[InlineData("IImmutableList<byte[]>", "byte[]")]
	[InlineData("ImmutableDictionary<string, M>", "M")]
	[InlineData("G<List<int>>", "List<int>")]
	internal async Task MutableType(string type, string mutableType)
	{
		var diagnostics = await GetDiagnosticsAsync(
			$"[Immutable] class C {{ public {type} P {{ get; }} }}"
		);
		diagnostics.Should().ContainSingle();
		diagnostics[0].Id.Should().Be(ImmutableAnalyzer.MutableTypeDiagnosticId);
		diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
		diagnostics[0]
			.GetMessage()
			.Should()
			.Be($"Property 'P' of immutable type 'C' uses mutable type '{mutableType}'");
	}

	[Theory]
	[InlineData("[Immutable] class C { public List<int> P { get; } }")]
	[InlineData("[Immutable] class C { public List<int> P => new(); }")]
	[InlineData("[Immutable] class C { public List<int> this[int i] => new(); }")]
	[InlineData("[Immutable] record R(List<int> P);")]
	[InlineData("[Immutable] readonly record struct S(List<int> P);")]
	internal async Task MutableType_LocationIsDeclaredType(string source)
	{
		var diagnostics = await GetDiagnosticsAsync(source);
		diagnostics.Should().ContainSingle();
		GetLocationText(diagnostics[0]).Should().Be("List<int>");
	}

	[Fact]
	internal async Task MutableSetterAndMutableType()
	{
		var diagnostics = await GetDiagnosticsAsync(
			"[Immutable] class C { public List<int> P { get; set; } }"
		);
		diagnostics
			.Select(d => d.Id)
			.Should()
			.BeEquivalentTo(
				ImmutableAnalyzer.MutableSetterDiagnosticId,
				ImmutableAnalyzer.MutableTypeDiagnosticId
			);
	}
}
