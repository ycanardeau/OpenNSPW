using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Aigamo.Otsuki.Analyzers;

/// <summary>
/// Keeps an actor's state its own thread's: reports code of an actor (a type derived from <c>Actor&lt;TMessage&gt;</c>)
/// that runs on another thread and touches the actor's state (<see cref="StateDiagnosticId"/>).
/// </summary>
/// <remarks>
/// <para>Code of the actor that runs on another thread:</para>
/// <list type="bullet">
/// <item>its members that other code calls, on its own thread: the methods, properties and indexers that are not
/// private, but the constructors, before another thread can see the actor, the explicit implementations of interfaces,
/// which the actor's core calls as it handles a message, and the overrides, which its mailbox calls;</item>
/// <item>an async function, whose code after an <c>await</c> runs on the thread pool;</item>
/// <item>a function given to what calls it on another thread: <c>Task.Run</c>, a continuation, the thread pool, a
/// cancellation token's callback, a timer, a parameter marked <c>[CalledOnAnotherThread]</c>, or a constructor, which may
/// keep it for later.</item>
/// </list>
/// <para>Such code touches the actor's state if it reads or writes a field of the actor that is not read-only, or one
/// that holds a mutable collection; uses a property of the actor but a read-only one; or calls a method of the actor
/// that touches it, or that is not the actor's own, but <c>Post</c>. It may post to the actor, and read what is set
/// once as the actor is made.</para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ActorStateAnalyzer : DiagnosticAnalyzer
{
	public const string StateDiagnosticId = "OTSUKI0007";

	private const string ActorTypeName = "Aigamo.Otsuki.Actors.Actor<TMessage>";

	private const string CalledOnAnotherThreadName = "CalledOnAnotherThreadAttribute";

	// The members of Actor<TMessage> that any thread may use.
	private static readonly ImmutableHashSet<string> ThreadSafeActorMembers =
		ImmutableHashSet.Create("Post", "QueueLength", "Completion");

	// The methods of .NET that call what they are given on another thread, by type, and by name ("" for every method).
	private static readonly ImmutableDictionary<string, ImmutableHashSet<string>> ElsewhereMethods =
		new Dictionary<string, ImmutableHashSet<string>>
		{
			["System.Threading.Tasks.Task"] = ["Run", "ContinueWith"],
			["System.Threading.Tasks.Task<TResult>"] = ["ContinueWith"],
			["System.Threading.Tasks.TaskFactory"] = [""],
			["System.Threading.Tasks.TaskFactory<TResult>"] = [""],
			["System.Threading.ThreadPool"] = [""],
			["System.Threading.CancellationToken"] = ["Register", "UnsafeRegister"],
			["System.TimeProvider"] = ["CreateTimer"],
		}.ToImmutableDictionary();

	private static readonly DiagnosticDescriptor StateRule = new(
		id: StateDiagnosticId,
		title: "An actor's state is touched only on its own thread",
		messageFormat: "{0} runs on another thread than the actor's, and touches its state ({1}): post a message to the actor instead",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
	[StateRule];

	// What one function of an actor does with the actor's state: what it touches, and the actor's methods and the
	// functions within it that it calls.
	private sealed class Function
	{
		public ConcurrentQueue<string> Touches { get; } = new();

		public ConcurrentQueue<IMethodSymbol> Calls { get; } = new();
	}

	private static bool IsActorType(INamedTypeSymbol? type) =>
		type?.OriginalDefinition.ToDisplayString() == ActorTypeName;

	private static bool IsActor(INamedTypeSymbol type)
	{
		for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			if (IsActorType(baseType))
				return true;
		}

		return false;
	}

	private static bool IsOnThis(IOperation? instance) =>
		instance
			is IInstanceReferenceOperation
			{
				ReferenceKind: InstanceReferenceKind.ContainingTypeInstance
			};

	private static bool IsMutableCollection(ITypeSymbol type) =>
		type is IArrayTypeSymbol
		|| type.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic";

	private static bool IsReadOnlyAutoProperty(IPropertySymbol property) =>
		property.SetMethod is null
		&& property
			.ContainingType.GetMembers()
			.OfType<IFieldSymbol>()
			.Any(field => SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, property));

	// The function that an operation is in: the innermost lambda, anonymous method or local function around it, or the
	// member whose body it is.
	private static IMethodSymbol? FunctionOf(IOperation operation, ISymbol containing)
	{
		for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
		{
			if (parent is IAnonymousFunctionOperation anonymous)
				return anonymous.Symbol;
			if (parent is ILocalFunctionOperation local)
				return local.Symbol;
		}

		return containing as IMethodSymbol;
	}

	// The function that a delegate is made of, if the argument is a lambda, an anonymous method, a local function or a
	// method of the actor.
	private static IMethodSymbol? DelegatedFunction(IOperation value) =>
		value switch
		{
			IDelegateCreationOperation creation => DelegatedFunction(creation.Target),
			IConversionOperation conversion => DelegatedFunction(conversion.Operand),
			IAnonymousFunctionOperation anonymous => anonymous.Symbol,
			IMethodReferenceOperation reference
				when reference.Method.MethodKind == MethodKind.LocalFunction
					|| IsOnThis(reference.Instance) => reference.Method.OriginalDefinition,
			_ => null,
		};

	// Whether a lambda or anonymous method is an argument, made into a delegate or not.
	private static bool IsGiven(IOperation function)
	{
		var parent = function.Parent;
		while (parent is IDelegateCreationOperation or IConversionOperation)
			parent = parent.Parent;
		return parent is IArgumentOperation;
	}

	private static bool CallsElsewhere(IMethodSymbol method, IParameterSymbol? parameter)
	{
		if (
			parameter is not null
			&& parameter
				.GetAttributes()
				.Any(a => a.AttributeClass?.Name == CalledOnAnotherThreadName)
		)
			return true;

		return ElsewhereMethods.TryGetValue(
				method.ContainingType.OriginalDefinition.ToDisplayString(),
				out var names
			) && (names.Contains("") || names.Contains(method.Name));
	}

	// The members that other code calls on its own thread: not private, not a constructor, an override or an explicit
	// implementation of an interface.
	private static bool IsCalledByOthers(ISymbol member) =>
		member.DeclaredAccessibility != Accessibility.Private
		&& !member.IsStatic
		&& !member.IsOverride
		&& member switch
		{
			IMethodSymbol method => method.MethodKind is MethodKind.Ordinary
				&& method.ExplicitInterfaceImplementations.IsEmpty,
			IPropertySymbol property => property.ExplicitInterfaceImplementations.IsEmpty,
			_ => false,
		};

	private static void AnalyzeActor(SymbolStartAnalysisContext context)
	{
		var actor = (INamedTypeSymbol)context.Symbol;
		if (!IsActor(actor))
			return;

		var functions = new ConcurrentDictionary<IMethodSymbol, Function>(
			SymbolEqualityComparer.Default
		);
		var elsewhere =
			new ConcurrentQueue<(IMethodSymbol Function, Location Location, string What)>();

		Function Of(IMethodSymbol function) =>
			functions.GetOrAdd(function.OriginalDefinition, _ => new Function());

		bool IsOwn(ISymbol member) =>
			SymbolEqualityComparer.Default.Equals(
				member.ContainingType.OriginalDefinition,
				actor.OriginalDefinition
			);

		void Touch(IOperation operation, ISymbol containing, string what)
		{
			if (FunctionOf(operation, containing) is { } function)
				Of(function).Touches.Enqueue(what);
		}

		void Call(IOperation operation, ISymbol containing, IMethodSymbol method)
		{
			if (IsActorType(method.ContainingType) && ThreadSafeActorMembers.Contains(method.Name))
				return;

			if (!IsOwn(method))
			{
				Touch(operation, containing, $"{method.Name}, not the actor's own");
				return;
			}

			if (FunctionOf(operation, containing) is { } function)
				Of(function).Calls.Enqueue(method.OriginalDefinition);
		}

		context.RegisterOperationAction(
			operationContext =>
			{
				var containing = operationContext.ContainingSymbol;
				switch (operationContext.Operation)
				{
					case IFieldReferenceOperation field
						when IsOnThis(field.Instance) && !field.Field.IsStatic:
						if (!field.Field.IsReadOnly || IsMutableCollection(field.Field.Type))
							Touch(field, containing, field.Field.Name);
						break;
					case IPropertyReferenceOperation property when IsOnThis(property.Instance):
						if (
							IsActorType(property.Property.ContainingType)
								? !ThreadSafeActorMembers.Contains(property.Property.Name)
								: !IsOwn(property.Property)
									|| !IsReadOnlyAutoProperty(property.Property)
						)
							Touch(property, containing, property.Property.Name);
						break;
					case IInvocationOperation invocation:
						if (
							IsOnThis(invocation.Instance)
							|| invocation.TargetMethod.MethodKind == MethodKind.LocalFunction
						)
							Call(invocation, containing, invocation.TargetMethod);
						foreach (var argument in invocation.Arguments)
						{
							if (
								CallsElsewhere(invocation.TargetMethod, argument.Parameter)
								&& DelegatedFunction(argument.Value) is { } given
							)
								elsewhere.Enqueue(
									(
										given,
										argument.Syntax.GetLocation(),
										$"What is given to {invocation.TargetMethod.Name}"
									)
								);
						}
						break;
					case IObjectCreationOperation creation:
						foreach (var argument in creation.Arguments)
						{
							if (DelegatedFunction(argument.Value) is { } given)
								elsewhere.Enqueue(
									(
										given,
										argument.Syntax.GetLocation(),
										$"What is given to a new {creation.Type?.Name}"
									)
								);
						}
						break;
					case IMethodReferenceOperation reference when IsOnThis(reference.Instance):
						// A method of the actor made into a delegate runs where the delegate is called.
						Call(reference, containing, reference.Method);
						break;
					case IAnonymousFunctionOperation anonymous:
						// What a function within a function does, the outer one does too, if it calls it: one given to
						// another method runs where that method has it run, which its posting to the actor may make the
						// actor's thread (Ask), and is reported only if given to what runs it on another thread.
						if (!IsGiven(anonymous) && FunctionOf(anonymous, containing) is { } outer)
							Of(outer).Calls.Enqueue(anonymous.Symbol.OriginalDefinition);
						if (anonymous.Symbol.IsAsync)
							elsewhere.Enqueue(
								(
									anonymous.Symbol,
									anonymous.Syntax.GetLocation(),
									"An async lambda"
								)
							);
						break;
					case ILocalFunctionOperation local when local.Symbol.IsAsync:
						elsewhere.Enqueue(
							(
								local.Symbol,
								local.Syntax.GetLocation(),
								$"'{local.Symbol.Name}', which is async,"
							)
						);
						break;
				}
			},
			OperationKind.FieldReference,
			OperationKind.PropertyReference,
			OperationKind.Invocation,
			OperationKind.ObjectCreation,
			OperationKind.MethodReference,
			OperationKind.AnonymousFunction,
			OperationKind.LocalFunction
		);

		context.RegisterSymbolEndAction(endContext =>
		{
			var touches = new Dictionary<IMethodSymbol, string?>(SymbolEqualityComparer.Default);

			// The first state that a function touches, itself or through what it calls, or null.
			string? TouchOf(IMethodSymbol function)
			{
				if (touches.TryGetValue(function, out var known))
					return known;

				touches[function] = null; // A call back into a function being looked at adds nothing.
				if (!functions.TryGetValue(function, out var facts))
					return null;

				var touched = facts.Touches.FirstOrDefault();
				if (touched is null)
				{
					foreach (var callee in facts.Calls)
					{
						if (TouchOf(callee) is { } through)
						{
							touched =
								callee.MethodKind == MethodKind.AnonymousFunction
									? through
									: $"{through}, through {callee.Name}";
							break;
						}
					}
				}

				touches[function] = touched;
				return touched;
			}

			foreach (var member in actor.GetMembers())
			{
				var function = member switch
				{
					IMethodSymbol method => method,
					IPropertySymbol property => property.GetMethod,
					_ => null,
				};
				if (function is null)
					continue;

				if (IsCalledByOthers(member) && TouchOf(function) is { } touched)
					endContext.ReportDiagnostic(
						Diagnostic.Create(
							StateRule,
							member.Locations[0],
							$"'{member.Name}', which others call,",
							touched
						)
					);
				else if (
					member is IMethodSymbol { IsAsync: true }
					&& TouchOf(function) is { } touchedAfterAwait
				)
					endContext.ReportDiagnostic(
						Diagnostic.Create(
							StateRule,
							member.Locations[0],
							$"'{member.Name}', which is async,",
							touchedAfterAwait
						)
					);
			}

			foreach (var (function, location, what) in elsewhere)
			{
				if (TouchOf(function.OriginalDefinition) is { } touched)
					endContext.ReportDiagnostic(
						Diagnostic.Create(StateRule, location, what, touched)
					);
			}
		});
	}

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSymbolStartAction(AnalyzeActor, SymbolKind.NamedType);
	}
}
