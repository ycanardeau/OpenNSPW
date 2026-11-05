using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aigamo.Otsuki.Analyzers;

/// <summary>
/// Keeps threads from sharing state but through the actors' mailboxes: reports a <c>lock</c> statement, a
/// <c>volatile</c> field, and a use of the locks, signals, atomics, channels and concurrent collections of .NET, in
/// every assembly but Aigamo.Otsuki.Actors, whose mailbox is the only concurrent code
/// (<see cref="SharedStateDiagnosticId"/>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConcurrencyAnalyzer : DiagnosticAnalyzer
{
	public const string SharedStateDiagnosticId = "OTSUKI0003";

	// The assembly of Mailbox<T>, Latest<T> and Sequence, which may use what the others may not.
	private const string ActorsAssemblyName = "Aigamo.Otsuki.Actors";

	private static readonly ImmutableHashSet<string> SharedStateTypeNames = ImmutableHashSet.Create(
		"System.Threading.Monitor",
		"System.Threading.Lock",
		"System.Threading.SpinLock",
		"System.Threading.Mutex",
		"System.Threading.Semaphore",
		"System.Threading.SemaphoreSlim",
		"System.Threading.ReaderWriterLock",
		"System.Threading.ReaderWriterLockSlim",
		"System.Threading.ManualResetEvent",
		"System.Threading.ManualResetEventSlim",
		"System.Threading.AutoResetEvent",
		"System.Threading.CountdownEvent",
		"System.Threading.Barrier",
		"System.Threading.Interlocked",
		"System.Threading.Volatile",
		"System.Collections.Immutable.ImmutableInterlocked"
	);

	private static readonly ImmutableHashSet<string> SharedStateNamespaces =
		ImmutableHashSet.Create("System.Threading.Channels", "System.Collections.Concurrent");

	private static readonly DiagnosticDescriptor SharedStateRule = new(
		id: SharedStateDiagnosticId,
		title: "Threads share state only through the actors' mailboxes",
		messageFormat: "'{0}' shares state between threads: post a message to an actor's mailbox instead (Aigamo.Otsuki.Actors)",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(SharedStateRule);

	private static bool IsSharedStateNamespace(INamespaceSymbol? ns) =>
		ns is not null && SharedStateNamespaces.Contains(ns.ToDisplayString());

	private static bool IsSharedStateType(INamedTypeSymbol? type) =>
		type is not null
		&& (
			SharedStateTypeNames.Contains(type.OriginalDefinition.ToDisplayString())
			|| IsSharedStateNamespace(type.ContainingNamespace)
		);

	private static void Report(SyntaxNodeAnalysisContext context, Location location, string name) =>
		context.ReportDiagnostic(Diagnostic.Create(SharedStateRule, location, name));

	// A name of one of the types, which any use of them or of their members names, as their instances come only from
	// them; or, in a using directive, of one of the namespaces. The name that a qualified name ends with is the one
	// reported, once.
	private static void AnalyzeName(SyntaxNodeAnalysisContext context)
	{
		var name = (SimpleNameSyntax)context.Node;
		if (name.Parent is QualifiedNameSyntax qualified && qualified.Left == name)
			return;

		var symbol = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol;
		if (symbol is INamespaceSymbol ns)
		{
			if (
				name.FirstAncestorOrSelf<UsingDirectiveSyntax>() is { } directive
				&& IsSharedStateNamespace(ns)
			)
				Report(context, directive.NamespaceOrType.GetLocation(), ns.ToDisplayString());
			return;
		}

		if (symbol is INamedTypeSymbol type && IsSharedStateType(type))
			Report(context, name.GetLocation(), type.ToDisplayString());
	}

	private static void AnalyzeLock(SyntaxNodeAnalysisContext context) =>
		Report(context, ((LockStatementSyntax)context.Node).LockKeyword.GetLocation(), "lock");

	private static void AnalyzeField(SyntaxNodeAnalysisContext context)
	{
		var field = (FieldDeclarationSyntax)context.Node;
		foreach (var modifier in field.Modifiers)
		{
			if (modifier.IsKind(SyntaxKind.VolatileKeyword))
				Report(context, modifier.GetLocation(), "volatile");
		}
	}

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(startContext =>
		{
			if (startContext.Compilation.AssemblyName == ActorsAssemblyName)
				return;

			startContext.RegisterSyntaxNodeAction(
				AnalyzeName,
				SyntaxKind.IdentifierName,
				SyntaxKind.GenericName
			);
			startContext.RegisterSyntaxNodeAction(AnalyzeLock, SyntaxKind.LockStatement);
			startContext.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
		});
	}
}
