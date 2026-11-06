using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Aigamo.Otsuki.Analyzers;

/// <summary>
/// Keeps Aigamo.Otsuki.Core a core: code that runs only on the thread that calls it, at the time it is told, and
/// keeps its state in its own objects; and Aigamo.Otsuki.Messages, whose wire formats the core runs. In those
/// assemblies it reports a use of the threads, tasks, timers, sockets and actors of .NET and of Otsuki, a read of a
/// clock, a draw of randomness and an <c>async</c> function (<see cref="ThreadDiagnosticId"/>), and a static field
/// that can change (<see cref="StaticStateDiagnosticId"/>). A race inside the core then needs code that does not
/// build, and a run given the same inputs does the same.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CoreAnalyzer : DiagnosticAnalyzer
{
	public const string ThreadDiagnosticId = "OTSUKI0004";

	public const string StaticStateDiagnosticId = "OTSUKI0005";

	private static readonly ImmutableHashSet<string> CoreAssemblyNames = ImmutableHashSet.Create(
		"Aigamo.Otsuki.Core",
		"Aigamo.Otsuki.Messages"
	);

	private const string ActorsAssemblyName = "Aigamo.Otsuki.Actors";

	// Namespaces whose every type but an enum starts, schedules, waits for or shares work across threads, or talks to
	// the network; an enum, such as an address family, is only a value. Then single types that do, or that read a
	// clock.
	private static readonly ImmutableArray<string> ThreadNamespaces = ImmutableArray.Create(
		"System.Threading",
		"System.Timers",
		"System.Collections.Concurrent",
		"System.Net.Sockets"
	);

	// The namespaces whose using directive is reported: those with no enum the core may need.
	private static readonly ImmutableArray<string> ThreadOnlyNamespaces = ImmutableArray.Create(
		"System.Threading",
		"System.Timers",
		"System.Collections.Concurrent"
	);

	private static readonly ImmutableHashSet<string> ThreadTypeNames = ImmutableHashSet.Create(
		"System.TimeProvider",
		"System.Diagnostics.Stopwatch",
		"System.Net.Dns",
		"System.Random",
		"System.Security.Cryptography.RandomNumberGenerator",
		"System.Linq.ParallelEnumerable"
	);

	// Members that read a clock or draw randomness: the core is told the time with each input, and asks its outputs for
	// random bytes, so that a test can make it repeat.
	private static readonly ImmutableHashSet<string> AmbientMemberNames = ImmutableHashSet.Create(
		"System.DateTime.Now",
		"System.DateTime.UtcNow",
		"System.DateTime.Today",
		"System.DateTimeOffset.Now",
		"System.DateTimeOffset.UtcNow",
		"System.Environment.TickCount",
		"System.Environment.TickCount64",
		"System.Guid.NewGuid"
	);

	// Collections whose static instance would be state shared by every core of the process: these namespaces
	// themselves, not System.Collections.Immutable below them.
	private static readonly ImmutableHashSet<string> MutableCollectionNamespaces =
		ImmutableHashSet.Create(
			"System.Collections",
			"System.Collections.Generic",
			"System.Collections.ObjectModel"
		);

	private static readonly DiagnosticDescriptor ThreadRule = new(
		id: ThreadDiagnosticId,
		title: "The core runs only on the thread that calls it",
		messageFormat: "'{0}' is not for the core: it is told the time, asks for randomness and gives its effects to its outputs, which its shell carries out",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	private static readonly DiagnosticDescriptor StaticStateRule = new(
		id: StaticStateDiagnosticId,
		title: "The core keeps no state in statics",
		messageFormat: "'{0}' is static state, which every core of the process shares: keep it in the core's own objects",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(ThreadRule, StaticStateRule);

	private static bool IsIn(INamespaceSymbol? ns, ImmutableArray<string> namespaces)
	{
		if (ns is null || ns.IsGlobalNamespace)
			return false;

		var name = ns.ToDisplayString();
		return namespaces.Any(n => name == n || name.StartsWith(n + ".", StringComparison.Ordinal));
	}

	private static bool IsThreadType(ITypeSymbol? type) =>
		type is INamedTypeSymbol { TypeKind: not TypeKind.Enum } named
		&& (
			ThreadTypeNames.Contains(named.OriginalDefinition.ToDisplayString())
			|| IsIn(named.ContainingNamespace, ThreadNamespaces)
			|| named.ContainingAssembly?.Name == ActorsAssemblyName
		);

	private static bool IsAmbientMember(ISymbol symbol) =>
		symbol is IPropertySymbol or IMethodSymbol
		&& symbol.ContainingType is { } type
		&& AmbientMemberNames.Contains($"{type.OriginalDefinition.ToDisplayString()}.{symbol.Name}");

	private static bool IsMutableCollection(ITypeSymbol type) =>
		type is IArrayTypeSymbol
		|| type is INamedTypeSymbol named
			&& MutableCollectionNamespaces.Contains(named.ContainingNamespace.ToDisplayString())
		|| type.OriginalDefinition.ToDisplayString() == "System.Text.StringBuilder";

	private static void Report(
		SyntaxNodeAnalysisContext context,
		DiagnosticDescriptor rule,
		Location location,
		string name
	) => context.ReportDiagnostic(Diagnostic.Create(rule, location, name));

	// A name of one of the types or members, or, in a using directive, of one of the namespaces. The name that a
	// qualified name ends with is the one reported, once.
	private static void AnalyzeName(SyntaxNodeAnalysisContext context)
	{
		var name = (SimpleNameSyntax)context.Node;
		if (name.Parent is QualifiedNameSyntax qualified && qualified.Left == name)
			return;

		var symbol = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol;
		switch (symbol)
		{
			case INamespaceSymbol ns:
				if (
					name.FirstAncestorOrSelf<UsingDirectiveSyntax>() is { } directive
					&& IsIn(ns, ThreadOnlyNamespaces)
				)
					Report(
						context,
						ThreadRule,
						directive.NamespaceOrType.GetLocation(),
						ns.ToDisplayString()
					);
				return;
			case INamedTypeSymbol type when IsThreadType(type):
				Report(context, ThreadRule, name.GetLocation(), type.ToDisplayString());
				return;
			case { } member when IsAmbientMember(member):
				Report(
					context,
					ThreadRule,
					name.GetLocation(),
					$"{member.ContainingType.ToDisplayString()}.{member.Name}"
				);
				return;
		}
	}

	// Whether a call names the type of its method, which the name rule reports, as Task.Run(...) does and
	// items.AsParallel() does not.
	private static bool NamesItsType(IInvocationOperation invocation) =>
		invocation.Syntax is InvocationExpressionSyntax
		{
			Expression: MemberAccessExpressionSyntax { Expression: var receiver },
		}
		&& invocation.SemanticModel?.GetSymbolInfo(receiver).Symbol is INamedTypeSymbol;

	// A call that returns one of the types without naming it, such as a method of another assembly that returns a
	// task, or an extension method of one, such as AsParallel.
	private static void AnalyzeInvocation(OperationAnalysisContext context)
	{
		var invocation = (IInvocationOperation)context.Operation;
		var method = invocation.TargetMethod;
		if (
			IsThreadType(invocation.Type) && !IsThreadType(method.ContainingType)
			|| method.IsExtensionMethod
				&& IsThreadType(method.ContainingType)
				&& !NamesItsType(invocation)
		)
			context.ReportDiagnostic(
				Diagnostic.Create(
					ThreadRule,
					invocation.Syntax.GetLocation(),
					IsThreadType(invocation.Type)
						? invocation.Type!.ToDisplayString()
						: method.ContainingType.ToDisplayString()
				)
			);
	}

	private static void ReportAsync(SyntaxNodeAnalysisContext context, SyntaxTokenList modifiers)
	{
		foreach (var modifier in modifiers)
		{
			if (modifier.IsKind(SyntaxKind.AsyncKeyword))
				Report(context, ThreadRule, modifier.GetLocation(), "async");
		}
	}

	private static void AnalyzeAsync(SyntaxNodeAnalysisContext context)
	{
		switch (context.Node)
		{
			case MethodDeclarationSyntax method:
				ReportAsync(context, method.Modifiers);
				break;
			case LocalFunctionStatementSyntax function:
				ReportAsync(context, function.Modifiers);
				break;
			case AnonymousFunctionExpressionSyntax function:
				ReportAsync(context, function.Modifiers);
				break;
		}
	}

	// A static field that can be assigned, or that holds a collection that can be changed.
	private static void AnalyzeField(SyntaxNodeAnalysisContext context)
	{
		var field = (FieldDeclarationSyntax)context.Node;
		if (!field.Modifiers.Any(SyntaxKind.StaticKeyword) || field.Modifiers.Any(SyntaxKind.ConstKeyword))
			return;

		foreach (var variable in field.Declaration.Variables)
		{
			if (
				context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken)
				is IFieldSymbol symbol
				&& (!symbol.IsReadOnly || IsMutableCollection(symbol.Type))
			)
				Report(context, StaticStateRule, variable.Identifier.GetLocation(), symbol.Name);
		}
	}

	// A static property with a setter, or an automatic one that holds a collection that can be changed.
	private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
	{
		var property = (PropertyDeclarationSyntax)context.Node;
		if (
			!property.Modifiers.Any(SyntaxKind.StaticKeyword)
			|| context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken)
				is not { } symbol
		)
			return;

		var isAutomatic =
			property.AccessorList?.Accessors.All(a => a.Body is null && a.ExpressionBody is null)
			?? false;
		if (
			symbol.SetMethod is not null
			|| isAutomatic && IsMutableCollection(symbol.Type)
		)
			Report(context, StaticStateRule, property.Identifier.GetLocation(), symbol.Name);
	}

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(startContext =>
		{
			if (!CoreAssemblyNames.Contains(startContext.Compilation.AssemblyName ?? string.Empty))
				return;

			startContext.RegisterSyntaxNodeAction(
				AnalyzeName,
				SyntaxKind.IdentifierName,
				SyntaxKind.GenericName
			);
			startContext.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
			startContext.RegisterSyntaxNodeAction(
				AnalyzeAsync,
				SyntaxKind.MethodDeclaration,
				SyntaxKind.LocalFunctionStatement,
				SyntaxKind.SimpleLambdaExpression,
				SyntaxKind.ParenthesizedLambdaExpression,
				SyntaxKind.AnonymousMethodExpression
			);
			startContext.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
			startContext.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		});
	}
}
