using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Declares each local where it is first assigned, instead of at the top of the method (docs/Refactoring.md,
// Functions):
//
//   double wrk_x;                    var wrk_x=unit.Position.X-x;
//   int i;                     =>    for( var i=0; i<8; i++ )
//   wrk_x=unit.Position.X-x;
//   for( i=0; i<8; i++ )
//
// A local whose uses fall into runs that each start by assigning it, such as two loops over i, becomes one local per
// run, where the runs are in different scopes. A run may also lie inside a block, a branch of an if, the body of a loop or a section of a switch, where the
// local is then declared. The declaration takes `var` when the value has the local's type, or an integer literal that
// becomes a double literal (`0` to `0.0`, the same value), and the type otherwise.
//
// The result is compiled, and a local whose new declarations do not compile (one that is read before it is assigned in
// a run, or whose name is used in a scope it moves into) stays as it was. Every read of a moved local is then preceded
// by an assignment in its run, so it reads the same value as before. Locals used in lambdas or local functions, whose
// address or a ref to which is taken, or whose declaration has a comment, are left as they are.
internal sealed class Declarations(CSharpProject project)
{
	private sealed record Local(ILocalSymbol Symbol, VariableDeclaratorSyntax Declarator, LocalDeclarationStatementSyntax Statement);

	private readonly CSharpProject _project = project;

	private static bool References(SyntaxNode node, IReadOnlyList<IdentifierNameSyntax> uses)
	{
		return uses.Any(u => node.Span.Contains(u.Span));
	}

	// The assignment `local = value` that a statement starts with, where value does not use the local: an expression
	// statement, or the only initializer of a for.
	private static AssignmentExpressionSyntax? FirstWrite(StatementSyntax statement, IReadOnlyList<IdentifierNameSyntax> uses)
	{
		var assignment = statement switch
		{
			ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax a } => a,
			ForStatementSyntax { Declaration: null, Initializers: [AssignmentExpressionSyntax a] } => a,
			_ => null,
		};
		return assignment is { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax left }
			&& uses.Contains(left) && !References(assignment.Right, uses) ? assignment : null;
	}

	private static bool StartsRun(StatementSyntax statement, IReadOnlyList<IdentifierNameSyntax> uses)
	{
		return FirstWrite(statement, uses) is not null || (statement is not ExpressionStatementSyntax && PlaceInside(statement, uses) is not null);
	}

	// The declarations that place a local's uses in a list of statements, from `start`: the assignments that start a run,
	// or null where a run cannot be placed.
	private static List<AssignmentExpressionSyntax>? Place(SyntaxList<StatementSyntax> list, int start, IReadOnlyList<IdentifierNameSyntax> uses)
	{
		var runs = new List<List<StatementSyntax>>();
		foreach (var statement in list.Skip(start).Where(s => References(s, uses)))
		{
			// A local declared by an expression statement is in scope to the end of the list, so the run goes on to it. A
			// statement that assigns the local first, or that uses it only in runs of its own, starts a run.
			if (runs.Count == 0 || (runs[^1][0] is not ExpressionStatementSyntax && StartsRun(statement, uses)))
			{
				runs.Add([statement]);
			}
			else
			{
				runs[^1].Add(statement);
			}
		}

		var placed = new List<AssignmentExpressionSyntax>();
		foreach (var run in runs)
		{
			if (FirstWrite(run[0], uses) is { } write && (run[0] is ExpressionStatementSyntax || run.Count == 1))
			{
				placed.Add(write);
			}
			else if (run.Count == 1 && PlaceInside(run[0], uses) is { } inner)
			{
				placed.AddRange(inner);
			}
			else
			{
				return null;
			}
		}

		return placed;
	}

	// The declarations that place a local's uses inside a statement that uses it only in the statements it contains.
	private static List<AssignmentExpressionSyntax>? PlaceInside(StatementSyntax statement, IReadOnlyList<IdentifierNameSyntax> uses)
	{
		List<AssignmentExpressionSyntax>? Body(StatementSyntax? body)
		{
			return body switch
			{
				null => [],
				_ when !References(body, uses) => [],
				BlockSyntax block => Place(block.Statements, 0, uses),
				IfStatementSyntax nested => PlaceInside(nested, uses),
				_ => null,
			};
		}

		switch (statement)
		{
			case BlockSyntax block:
				return Place(block.Statements, 0, uses);
			case IfStatementSyntax @if when !References(@if.Condition, uses):
				return Body(@if.Statement) is { } then && Body(@if.Else?.Statement) is { } @else ? [.. then, .. @else] : null;
			case ForStatementSyntax @for when (@for.Condition is null || !References(@for.Condition, uses))
				&& !@for.Initializers.Concat(@for.Incrementors).Any(e => References(e, uses)) && (@for.Declaration is null || !References(@for.Declaration, uses)):
				return Body(@for.Statement);
			case WhileStatementSyntax @while when !References(@while.Condition, uses):
				return Body(@while.Statement);
			case DoStatementSyntax @do when !References(@do.Condition, uses):
				return Body(@do.Statement);
			case ForEachStatementSyntax @foreach when !References(@foreach.Expression, uses):
				return Body(@foreach.Statement);
			case SwitchStatementSyntax @switch when !References(@switch.Expression, uses):
				var placed = new List<AssignmentExpressionSyntax>();
				foreach (var section in @switch.Sections)
				{
					if (section.Labels.Any(l => References(l, uses)) || Place(section.Statements, 0, uses) is not { } inner)
					{
						return null;
					}

					placed.AddRange(inner);
				}

				return placed;
			case UnsafeStatementSyntax @unsafe:
				return Place(@unsafe.Block.Statements, 0, uses);
			case CheckedStatementSyntax @checked:
				return Place(@checked.Block.Statements, 0, uses);
			default:
				return null;
		}
	}

	// Whether the local must stay where it is: used in a lambda or a local function, or by address or by ref.
	private static bool IsPinned(IReadOnlyList<IdentifierNameSyntax> uses, SyntaxNode scope)
	{
		return uses.Any(u => u.Ancestors().TakeWhile(a => a != scope).Any(a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
			|| u.Parent is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.AddressOfExpression }
			|| u.Parent is RefExpressionSyntax);
	}

	// The type of a new declaration: `var` when the value has the local's type, and the value, which an integer literal
	// assigned to a double becomes a double literal for.
	private static (string Type, ExpressionSyntax? Literal, string? NewLiteral) DeclaredType(Local local, ExpressionSyntax value, SemanticModel model)
	{
		var type = model.GetTypeInfo(value).Type;
		if (SymbolEqualityComparer.Default.Equals(type, local.Symbol.Type))
		{
			return ("var", null, null);
		}

		var literal = value is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.UnaryMinusExpression, Operand: LiteralExpressionSyntax l } ? l : value as LiteralExpressionSyntax;
		if (local.Symbol.Type.SpecialType == SpecialType.System_Double && literal is { RawKind: (int)SyntaxKind.NumericLiteralExpression } && literal.Token.Text.All(char.IsAsciiDigit))
		{
			return ("var", literal, $"{literal.Token.Text}.0");
		}

		return (local.Symbol.Type.ToMinimalDisplayString(model, value.SpanStart), null, null);
	}

	private static List<Local> Locals(SyntaxTree tree, SemanticModel model)
	{
		var locals = new List<Local>();
		foreach (var statement in tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
		{
			if (statement.IsConst || statement.UsingKeyword != default || statement.Declaration.Type is RefTypeSyntax
				|| statement.Parent is not BlockSyntax and not SwitchSectionSyntax
				|| statement.DescendantTrivia().Where(t => t.SpanStart >= statement.SpanStart)
					.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || (t.IsKind(SyntaxKind.MultiLineCommentTrivia) && !t.ToString().Contains("C4701"))))
			{
				continue;
			}

			foreach (var declarator in statement.Declaration.Variables)
			{
				// A local declared with a value keeps it, unless the value is `default`, which the port adds where C# finds
				// the local read before it is assigned (/* C4701 */).
				if (declarator.Initializer is { Value: not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression } })
				{
					continue;
				}

				if (model.GetDeclaredSymbol(declarator) is ILocalSymbol symbol)
				{
					locals.Add(new Local(symbol, declarator, statement));
				}
			}
		}

		return locals;
	}

	private static List<IdentifierNameSyntax> Uses(Local local, SyntaxNode scope, SemanticModel model)
	{
		return [.. scope.DescendantNodes().OfType<IdentifierNameSyntax>()
			.Where(n => n.Identifier.ValueText == local.Symbol.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, local.Symbol))];
	}

	// The edits that move a local's declaration, or null if it stays.
	private static List<(TextSpan Span, string Text)>? Move(Local local, SemanticModel model)
	{
		var scope = local.Statement.Parent!;
		var uses = Uses(local, scope, model);
		if (IsPinned(uses, scope))
		{
			return null;
		}

		var list = scope is BlockSyntax block ? block.Statements : ((SwitchSectionSyntax)scope).Statements;
		if (Place(list, list.IndexOf(local.Statement) + 1, uses) is not { } writes)
		{
			return null;
		}

		var edits = new List<(TextSpan Span, string Text)>();
		foreach (var write in writes)
		{
			var (type, literal, newLiteral) = DeclaredType(local, write.Right, model);
			edits.Add((new TextSpan(write.SpanStart, 0), $"{type} "));
			if (literal is not null)
			{
				edits.Add((literal.Span, newLiteral!));
			}
		}

		return edits;
	}

	// The edit that removes the moved declarators from a declaration: the whole statement and its line, or the moved
	// declarators.
	private static (TextSpan Span, string Text) Remove(LocalDeclarationStatementSyntax statement, IReadOnlySet<VariableDeclaratorSyntax> moved, SourceText text)
	{
		var kept = statement.Declaration.Variables.Where(v => !moved.Contains(v)).ToList();
		if (kept.Count > 0)
		{
			var declaration = $"{statement.Declaration.Type.ToString().Trim()} {string.Join(", ", kept.Select(v => v.ToFullString().Trim()))}";
			return (statement.Declaration.Span, declaration);
		}

		var line = text.Lines.GetLineFromPosition(statement.SpanStart);
		var whole = text.ToString(line.Span).Trim() == statement.ToString().Trim();
		return whole ? (line.SpanIncludingLineBreak, "") : (statement.Span, "");
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> edits)
	{
		foreach (var (span, replacement) in edits.OrderByDescending(e => e.Span.Start).ThenByDescending(e => e.Span.Length))
		{
			text = text[..span.Start] + replacement + text[span.End..];
		}

		return text;
	}

	// The new text of a file, with the locals that are not left out moved, and the moved locals by their declarators.
	private static (string Text, Dictionary<VariableDeclaratorSyntax, List<(TextSpan Span, string Text)>> Moved) Rewrite(SyntaxTree tree, SemanticModel model, IReadOnlySet<int> leftOut)
	{
		var text = tree.GetText();
		var moved = new Dictionary<VariableDeclaratorSyntax, List<(TextSpan Span, string Text)>>();
		foreach (var local in Locals(tree, model))
		{
			if (!leftOut.Contains(local.Declarator.SpanStart) && Move(local, model) is { } edits)
			{
				moved[local.Declarator] = edits;
			}
		}

		var removals = moved.Keys.GroupBy(d => (LocalDeclarationStatementSyntax)d.Parent!.Parent!)
			.Select(g => Remove(g.Key, g.ToHashSet(), text));
		return (Apply(text.ToString(), moved.Values.SelectMany(e => e).Concat(removals)), moved);
	}

	private static string Signature(MemberDeclarationSyntax member)
	{
		return member switch
		{
			MethodDeclarationSyntax m => $"{m.Identifier.ValueText}{m.ParameterList}",
			ConstructorDeclarationSyntax c => $".ctor{c.ParameterList}",
			PropertyDeclarationSyntax p => p.Identifier.ValueText,
			_ => member.ToString(),
		};
	}

	// Moves the declarations of the project's files, and writes the files that changed. Returns their paths and the
	// number of locals moved in each.
	public IReadOnlyList<string> Run()
	{
		var files = _project.ReadFiles();
		var compilation = _project.Compile(files, CSharpProject.FrameworkReferences);
		var written = new List<string>();
		foreach (var tree in compilation.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath)).ToList())
		{
			var model = compilation.GetSemanticModel(tree);
			var baseline = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree == tree).Select(d => d.GetMessage()).ToHashSet();
			var leftOut = new HashSet<int>();
			while (true)
			{
				var (newText, moved) = Rewrite(tree, model, leftOut);
				if (moved.Count == 0)
				{
					break;
				}

				var newTree = CSharpSyntaxTree.ParseText(newText, _project.ParseOptions(), tree.FilePath);
				var newCompilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(t => t.FilePath == tree.FilePath), newTree);
				var errors = newCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree == newTree && !baseline.Contains(d.GetMessage())).ToList();
				if (errors.Count == 0)
				{
					File.WriteAllText(tree.FilePath, newText);
					files[tree.FilePath] = newText;
					compilation = newCompilation;
					written.Add($"{tree.FilePath}: {moved.Count}");
					break;
				}

				// Leave out the locals that the errors name, or else all the moved locals of the members with errors.
				var newRoot = newTree.GetRoot();
				var oldRoot = tree.GetRoot();
				var before = leftOut.Count;
				foreach (var error in errors)
				{
					var token = newRoot.FindToken(error.Location.SourceSpan.Start);
					var member = newRoot.FindNode(error.Location.SourceSpan).AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault(m => m is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax);
					var name = token.IsKind(SyntaxKind.IdentifierToken) ? token.ValueText : null;
					var oldMember = member is null ? null : oldRoot.DescendantNodes().OfType<MemberDeclarationSyntax>()
						.FirstOrDefault(m => m.GetType() == member.GetType() && m is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax && Signature(m) == Signature(member));
					var candidates = moved.Keys.Where(d => oldMember is null || oldMember.Span.Contains(d.Span)).ToList();
					var named = candidates.Where(d => d.Identifier.ValueText == name).ToList();
					foreach (var declarator in named.Count > 0 ? named : candidates)
					{
						leftOut.Add(declarator.SpanStart);
					}
				}

				if (leftOut.Count == before)
				{
					throw new InvalidOperationException($"Errors outside the moved locals in {tree.FilePath}: {errors[0]}");
				}
			}
		}

		return written;
	}
}
