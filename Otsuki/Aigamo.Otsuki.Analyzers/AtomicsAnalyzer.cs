using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Aigamo.Otsuki.Analyzers;

/// <summary>
/// Keeps the words that the threads of Aigamo.Otsuki.Actors share in order on every architecture: a field that an
/// <c>Interlocked</c> method takes by reference is read and written only so, but in the constructors of its type, before
/// another thread can see it; and nothing is <c>Volatile</c> or <c>volatile</c>, which order a read or a write only one
/// way, so that a write and a read after it may be seen the other way round (<see cref="SharedWordDiagnosticId"/>).
/// Every <c>Interlocked</c> method is a full fence on every architecture .NET runs on, so the threads see the shared words
/// in one order, the order of the program: the mailbox is then right on every processor if it is right when its threads'
/// steps are taken one at a time.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AtomicsAnalyzer : DiagnosticAnalyzer
{
	public const string SharedWordDiagnosticId = "OTSUKI0006";

	private const string ActorsAssemblyName = "Aigamo.Otsuki.Actors";

	private static readonly DiagnosticDescriptor SharedWordRule = new(
		id: SharedWordDiagnosticId,
		title: "A word that threads share is read and written only with Interlocked",
		messageFormat: "'{0}' {1}: read and write the words that threads share only with Interlocked, a full fence on every architecture",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
	[SharedWordRule];

	private static bool IsInterlocked(IMethodSymbol method) =>
		method.ContainingType.ToDisplayString() == typeof(Interlocked).FullName;

	// The field, if the operation is one that an Interlocked method takes by reference (ref, or ref readonly as
	// Interlocked.Read does).
	private static IFieldSymbol? InterlockedWord(IFieldReferenceOperation reference) =>
		reference.Parent
			is IArgumentOperation
			{
				Parameter.RefKind: not RefKind.None,
				Parent: IInvocationOperation invocation,
			}
		&& IsInterlocked(invocation.TargetMethod)
			? reference.Field.OriginalDefinition
			: null;

	private static bool IsInConstructorOf(IOperation operation, INamedTypeSymbol type) =>
		operation.SemanticModel?.GetEnclosingSymbol(operation.Syntax.SpanStart)
			is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor
		&& SymbolEqualityComparer.Default.Equals(constructor.ContainingType, type);

	private static void Report(
		Action<Diagnostic> report,
		Location location,
		string name,
		string why
	) => report(Diagnostic.Create(SharedWordRule, location, name, why));

	private static void AnalyzeVolatileCall(OperationAnalysisContext context)
	{
		var invocation = (IInvocationOperation)context.Operation;
		if (invocation.TargetMethod.ContainingType.ToDisplayString() == typeof(Volatile).FullName)
			Report(
				context.ReportDiagnostic,
				invocation.Syntax.GetLocation(),
				"Volatile",
				"orders a read or a write only one way"
			);
	}

	private static void AnalyzeVolatileField(SymbolAnalysisContext context)
	{
		var field = (IFieldSymbol)context.Symbol;
		if (field.IsVolatile)
			Report(
				context.ReportDiagnostic,
				field.Locations[0],
				field.Name,
				"is volatile, which orders a read or a write only one way"
			);
	}

	// Within a type, the fields that Interlocked takes by reference, and every other reference to a field, reported once
	// the whole type is seen.
	private static void AnalyzeType(SymbolStartAnalysisContext context)
	{
		var type = (INamedTypeSymbol)context.Symbol;
		var words = new ConcurrentDictionary<IFieldSymbol, bool>(SymbolEqualityComparer.Default);
		var others = new ConcurrentBag<IFieldReferenceOperation>();
		context.RegisterOperationAction(
			operationContext =>
			{
				var reference = (IFieldReferenceOperation)operationContext.Operation;
				if (InterlockedWord(reference) is { } word)
					words.TryAdd(word, true);
				else if (!IsInConstructorOf(reference, type))
					others.Add(reference);
			},
			OperationKind.FieldReference
		);
		context.RegisterSymbolEndAction(endContext =>
		{
			foreach (var reference in others)
			{
				if (words.ContainsKey(reference.Field.OriginalDefinition))
					Report(
						endContext.ReportDiagnostic,
						reference.Syntax.GetLocation(),
						reference.Field.Name,
						"is a word that Interlocked shares, read or written here without it"
					);
			}
		});
	}

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(startContext =>
		{
			if (startContext.Compilation.AssemblyName != ActorsAssemblyName)
				return;

			startContext.RegisterOperationAction(AnalyzeVolatileCall, OperationKind.Invocation);
			startContext.RegisterSymbolAction(AnalyzeVolatileField, SymbolKind.Field);
			startContext.RegisterSymbolStartAction(AnalyzeType, SymbolKind.NamedType);
		});
	}
}
