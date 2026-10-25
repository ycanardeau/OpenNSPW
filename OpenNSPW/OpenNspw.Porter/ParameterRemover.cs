using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Removes parameters that their methods never use, and the arguments that every call passes for them, in the port and
// in the projects that use it. The parameters are given by where the analyzer reports them (IDE0060): lines of a file,
// a source file, a line and a column, as `dotnet format` prints them.
//
// A parameter is removed only if every argument passed for it is free of side effects: a literal, a local, a parameter,
// a field, an auto-property, `this`, `default`, `null`, `sizeof`, `typeof`, the address of one of those, a call of a
// method that only returns one of its parameters (as `TEXT("...")`), or a cast, an operator or a conditional of them,
// with no user-defined operator; anything else (another call or property, an element of an array, which can throw) is
// reported, and the parameter kept. A method that is used other than by calling it (as a delegate), that
// overrides or implements another, or that is virtual keeps all its parameters, and is reported.
internal sealed class ParameterRemover(
	CSharpProject port,
	IReadOnlyList<CSharpProject> users,
	IReadOnlyList<(string File, int Line, int Column)> positions
)
{
	private readonly CSharpProject _port = port;
	private readonly IReadOnlyList<CSharpProject> _users = users;
	private readonly IReadOnlyList<(string File, int Line, int Column)> _positions = positions;

	public static List<(string File, int Line, int Column)> ReadPositions(string path)
	{
		return
		[
			.. File.ReadLines(path)
				.Select(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
				.Where(p => p.Length == 3)
				.Select(p => (Path.GetFullPath(p[0]), int.Parse(p[1]), int.Parse(p[2]))),
		];
	}

	// Whether a method only returns one of its parameters, as `TEXT(s)` and `MAKEINTRESOURCE(i)` do.
	private static bool ReturnsItsParameter(IMethodSymbol method)
	{
		return method.DeclaringSyntaxReferences is [var reference]
			&& reference.GetSyntax() is MethodDeclarationSyntax declaration
			&& (
				declaration.ExpressionBody?.Expression
				?? (
					declaration.Body?.Statements is [ReturnStatementSyntax statement]
						? statement.Expression
						: null
				)
			)
				is IdentifierNameSyntax name
			&& method.Parameters.Any(p => p.Name == name.Identifier.ValueText);
	}

	// Whether a property is an auto-property, whose accessors only read and write its field.
	private static bool IsAutoProperty(IPropertySymbol property)
	{
		return property.DeclaringSyntaxReferences is [var reference]
			&& reference.GetSyntax()
				is PropertyDeclarationSyntax { ExpressionBody: null, AccessorList: { } accessors }
			&& accessors.Accessors.All(a => a.Body is null && a.ExpressionBody is null);
	}

	private static bool IsPure(IOperation? operation)
	{
		return operation switch
		{
			null => true,
			ILiteralOperation
			or IDefaultValueOperation
			or ILocalReferenceOperation
			or IParameterReferenceOperation
			or IInstanceReferenceOperation
			or ISizeOfOperation
			or ITypeOfOperation
			or INameOfOperation => true,
			IFieldReferenceOperation field => IsPure(field.Instance),
			IPropertyReferenceOperation property => property.Arguments.Length == 0
				&& IsAutoProperty(property.Property)
				&& IsPure(property.Instance),
			IInvocationOperation invocation => ReturnsItsParameter(invocation.TargetMethod)
				&& IsPure(invocation.Instance)
				&& invocation.Arguments.All(a => IsPure(a.Value)),
			IConversionOperation conversion => conversion.OperatorMethod is null
				&& IsPure(conversion.Operand),
			IBinaryOperation binary => binary.OperatorMethod is null
				&& IsPure(binary.LeftOperand)
				&& IsPure(binary.RightOperand),
			IUnaryOperation unary => unary.OperatorMethod is null && IsPure(unary.Operand),
			IAddressOfOperation address => IsPure(address.Reference),
			IConditionalOperation conditional => IsPure(conditional.Condition)
				&& IsPure(conditional.WhenTrue)
				&& IsPure(conditional.WhenFalse),
			_ => false,
		};
	}

	// Whether a method's parameters may be changed: an ordinary method or a constructor, that no other member depends on.
	private static bool CanChange(IMethodSymbol method)
	{
		if (
			method.IsVirtual
			|| method.IsAbstract
			|| method.IsOverride
			|| method.IsExtern
			|| method.ExplicitInterfaceImplementations.Length > 0
			|| method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor)
		)
		{
			return false;
		}

		var type = method.ContainingType;
		return !type
			.AllInterfaces.SelectMany(i => i.GetMembers())
			.Any(m =>
				SymbolEqualityComparer.Default.Equals(
					type.FindImplementationForInterfaceMember(m),
					method
				)
			);
	}

	private static string? IdOf(ISymbol? symbol)
	{
		return symbol is IMethodSymbol { ReducedFrom: { } reduced }
			? reduced.GetDocumentationCommentId()
			: symbol?.OriginalDefinition.GetDocumentationCommentId();
	}

	private static bool IsBlank(IEnumerable<SyntaxTrivia> trivia)
	{
		return trivia.All(t =>
			t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia)
		);
	}

	// The spans to delete from a list of items to remove the items at the given indices: each run of removed items with
	// the separator after it, or, for a run at the end, the separator before it. When all of them go, so does the white
	// space around them, so that `( a )` becomes `()`.
	private static IEnumerable<TextSpan> Deletions<T>(
		SeparatedSyntaxList<T> items,
		IReadOnlySet<int> removed
	)
		where T : SyntaxNode
	{
		var i = 0;
		while (i < items.Count)
		{
			if (!removed.Contains(i))
			{
				i++;
				continue;
			}

			var first = i;
			while (i < items.Count && removed.Contains(i))
			{
				i++;
			}

			var last = i - 1;
			if (i < items.Count)
			{
				yield return TextSpan.FromBounds(items[first].SpanStart, items[i].SpanStart);
			}
			else if (first > 0)
			{
				yield return TextSpan.FromBounds(items[first - 1].Span.End, items[last].Span.End);
			}
			else
			{
				var open = items[first].GetFirstToken().GetPreviousToken();
				var close = items[last].GetLastToken().GetNextToken();
				var start = IsBlank(open.TrailingTrivia.Concat(items[first].GetLeadingTrivia()))
					? open.Span.End
					: items[first].SpanStart;
				var end = IsBlank(items[last].GetTrailingTrivia().Concat(close.LeadingTrivia))
					? close.SpanStart
					: items[last].Span.End;
				yield return TextSpan.FromBounds(start, end);
			}
		}
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> edits)
	{
		foreach (var (span, replacement) in edits.OrderByDescending(e => e.Span.Start))
		{
			text = text[..span.Start] + replacement + text[span.End..];
		}

		return text;
	}

	private sealed record Unused(
		string MethodId,
		int Ordinal,
		string Name,
		ParameterSyntax Declaration
	);

	// A call: its arguments, the indices of the arguments for each parameter ordinal, and whether they are all free of
	// side effects.
	private sealed record Call(
		SyntaxTree Tree,
		BaseArgumentListSyntax List,
		Dictionary<int, List<int>> Indices,
		Dictionary<int, bool> Pure
	);

	// Whether a method reads or writes a parameter, which a position that no longer points where the analyzer reported
	// would find.
	private static bool IsUsed(IParameterSymbol parameter, CSharpCompilation compilation)
	{
		return parameter.ContainingSymbol.DeclaringSyntaxReferences.Any(r =>
		{
			var model = compilation.GetSemanticModel(r.SyntaxTree);
			return r.GetSyntax()
				.DescendantNodes()
				.OfType<IdentifierNameSyntax>()
				.Any(n =>
					SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, parameter)
				);
		});
	}

	// The parameters at the positions, by method.
	private List<Unused> Resolve(CSharpCompilation compilation)
	{
		var unused = new List<Unused>();
		foreach (var (file, line, column) in _positions)
		{
			var tree = compilation.SyntaxTrees.Single(t => Path.GetFullPath(t.FilePath) == file);
			var position = tree.GetText().Lines[line - 1].Start + column - 1;
			var parameter = tree.GetRoot()
				.FindToken(position)
				.Parent?.AncestorsAndSelf()
				.OfType<ParameterSyntax>()
				.FirstOrDefault();
			if (
				parameter is null
				|| compilation.GetSemanticModel(tree).GetDeclaredSymbol(parameter)
					is not { ContainingSymbol: IMethodSymbol method } symbol
			)
			{
				Console.WriteLine($"{file}({line},{column}): no parameter");
				continue;
			}

			if (!CanChange(method))
			{
				Console.WriteLine($"{method}: kept, it overrides, implements or is virtual");
				continue;
			}

			if (IsUsed(symbol, compilation))
			{
				Console.WriteLine($"{method} {symbol.Name}: kept, it is used");
				continue;
			}

			unused.Add(new Unused(IdOf(method)!, symbol.Ordinal, symbol.Name, parameter));
		}

		return unused;
	}

	// The argument lists of the calls of the methods in a compilation, and the arguments of each for the parameter
	// ordinals; null for a method used other than by calling it.
	private static void FindCalls(
		CSharpCompilation compilation,
		IEnumerable<string> files,
		Dictionary<string, List<Call>?> calls
	)
	{
		foreach (var tree in compilation.SyntaxTrees.Where(t => files.Contains(t.FilePath)))
		{
			var model = compilation.GetSemanticModel(tree);
			foreach (var name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
			{
				var info = model.GetSymbolInfo(name);
				if (
					IdOf(info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is not { } id
					|| !calls.TryGetValue(id, out var list)
					|| list is null
					|| name.Parent is ParameterSyntax
				)
				{
					continue;
				}

				var expression =
					name.Parent is MemberAccessExpressionSyntax access && access.Name == name
						? (ExpressionSyntax)access
						: name;
				if (
					expression.Parent is not InvocationExpressionSyntax invocation
					|| invocation.Expression != expression
				)
				{
					Console.WriteLine(
						$"{tree.FilePath}({name.GetLocation().GetLineSpan().StartLinePosition.Line + 1}): {id} is used other than by a call; its parameters are kept"
					);
					calls[id] = null;
					continue;
				}

				Add(list, tree, model, invocation.ArgumentList, invocation);
			}

			foreach (
				var creation in tree.GetRoot()
					.DescendantNodes()
					.OfType<BaseObjectCreationExpressionSyntax>()
			)
			{
				if (
					IdOf(model.GetSymbolInfo(creation).Symbol) is { } id
					&& calls.TryGetValue(id, out var list)
					&& list is not null
					&& creation.ArgumentList is { } arguments
				)
				{
					Add(list, tree, model, arguments, creation);
				}
			}
		}
	}

	private static void Add(
		List<Call> list,
		SyntaxTree tree,
		SemanticModel model,
		BaseArgumentListSyntax arguments,
		SyntaxNode call
	)
	{
		var operation = model.GetOperation(call);
		var operationArguments = operation switch
		{
			IInvocationOperation i => i.Arguments,
			IObjectCreationOperation o => o.Arguments,
			_ => [],
		};
		var indices = new Dictionary<int, List<int>>();
		var pure = new Dictionary<int, bool>();
		foreach (var argument in operationArguments)
		{
			if (argument.Parameter is null || argument.ArgumentKind == ArgumentKind.DefaultValue)
			{
				continue;
			}

			var ordinal = argument.Parameter.Ordinal;
			if (
				argument.ArgumentKind == ArgumentKind.ParamArray
				&& argument.Value is IArrayCreationOperation { Initializer: { } initializer }
			)
			{
				var elements = initializer.ElementValues;
				indices[ordinal] =
				[
					.. elements.Select(e =>
						arguments.Arguments.IndexOf(a => a.Expression == e.Syntax)
					),
				];
				pure[ordinal] = elements.All(IsPure);
				continue;
			}

			// An out argument cannot go: the method writes it.
			indices[ordinal] =
			[
				arguments.Arguments.IndexOf(a =>
					a == argument.Syntax || a.Expression == argument.Value.Syntax
				),
			];
			pure[ordinal] =
				argument.Syntax
					is not ArgumentSyntax { RefKindKeyword.RawKind: (int)SyntaxKind.OutKeyword }
				&& IsPure(argument.Value);
		}

		list.Add(new Call(tree, arguments, indices, pure));
	}

	// Removes the parameters, and writes the files that changed. Returns their paths.
	public IReadOnlyList<string> Run()
	{
		var portFiles = _port.ReadFiles();
		var portCompilation = _port.Compile(portFiles, CSharpProject.FrameworkReferences);
		var unused = Resolve(portCompilation);
		var calls = unused
			.Select(u => u.MethodId)
			.Distinct()
			.ToDictionary(id => id, _ => (List<Call>?)[]);

		var compilations = new List<(
			CSharpCompilation Compilation,
			Dictionary<string, string> Files
		)>
		{
			(portCompilation, portFiles),
		};
		var except = new HashSet<string>([_port.Name, .. _users.Select(u => u.Name)]);
		var compiled = new List<MetadataReference> { portCompilation.ToMetadataReference() };
		foreach (var user in _users)
		{
			var files = user.ReadFiles();
			var compilation = user.Compile(
				files,
				[
					.. CSharpProject.FrameworkReferences,
					.. user.OutputReferences(except),
					.. compiled,
				]
			);
			compiled.Add(compilation.ToMetadataReference());
			compilations.Add((compilation, files));
		}

		foreach (var (compilation, files) in compilations)
		{
			FindCalls(compilation, files.Keys, calls);
		}

		// The parameters every call can lose, by method.
		var removable = new List<Unused>();
		foreach (var parameter in unused)
		{
			if (calls[parameter.MethodId] is not { } list)
			{
				continue;
			}

			var impure = list.FirstOrDefault(c =>
				c.Pure.TryGetValue(parameter.Ordinal, out var pure) && !pure
			);
			if (impure is not null)
			{
				Console.WriteLine(
					$"{parameter.MethodId} {parameter.Name}: kept, for {impure.List.Parent} at {impure.Tree.FilePath}({impure.List.GetLocation().GetLineSpan().StartLinePosition.Line + 1})"
				);
				continue;
			}

			removable.Add(parameter);
		}

		var edits = new Dictionary<string, List<(TextSpan Span, string Text)>>();
		void AddEdits(SyntaxTree tree, IEnumerable<TextSpan> deletions)
		{
			if (!edits.TryGetValue(tree.FilePath, out var list))
			{
				edits[tree.FilePath] = list = [];
			}

			list.AddRange(deletions.Select(d => (d, "")));
		}

		foreach (var group in removable.GroupBy(p => p.Declaration.Parent!))
		{
			var parameters = (ParameterListSyntax)group.Key;
			AddEdits(
				parameters.SyntaxTree,
				Deletions(
					parameters.Parameters,
					group.Select(p => parameters.Parameters.IndexOf(p.Declaration)).ToHashSet()
				)
			);
		}

		foreach (var method in removable.GroupBy(p => p.MethodId))
		{
			foreach (var call in calls[method.Key]!)
			{
				var removed = method
					.SelectMany(p => call.Indices.TryGetValue(p.Ordinal, out var i) ? i : [])
					.Where(i => i >= 0)
					.ToHashSet();
				AddEdits(call.Tree, Deletions(call.List.Arguments, removed));
			}
		}

		var written = new List<string>();
		foreach (var (path, fileEdits) in edits)
		{
			var text = File.ReadAllText(path);
			File.WriteAllText(path, Apply(text, fileEdits));
			written.Add(path);
		}

		foreach (var parameter in removable)
		{
			Console.WriteLine($"{parameter.MethodId} {parameter.Name}: removed");
		}

		return written;
	}
}
