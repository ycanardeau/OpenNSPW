using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Gives the element of a table that a loop works on a ref local (docs/Refactoring.md, Tables and loops):
//
//   for( m=1; m<=MaxUnitId; m++)           for( m=1; m<=MaxUnitId; m++)
//       {                                      {
//       if( Units[m].Side!=0 )        =>       ref var unit = ref Units[m];
//           Units[m].Speed=0;                  if( unit.Side!=0 )
//                                                  unit.Speed=0;
//
// A ref local refers to the same element as Units[m] does each time, so the code does the same. It is only given when
//
// - the loop's header is `v = <constant>; v < or <= <bound>; v++`, and the body is a block that never assigns v, so that
//   the element is the same throughout the body;
// - the bound keeps v inside the table (a constant, or MaxUnitId for Units), because the ref local takes the element at
//   the start of the body, even in an iteration whose code would not have used it;
// - the body uses the element at least twice.
//
// The local is named after the table (unit, fire, effect, cloud), or `other` for a loop inside a loop that has the
// name, and only with a name that the method does not use yet.
//
// The methods named in `parameterMethods` get the same for the unit their int parameter numbers, at the start of the
// method: `ref var unit = ref Units[m];`. The parameter must never be assigned, and the method must use the unit at least
// twice. Taking the element at the start is only safe if the parameter is always inside the table, or if the method
// uses the unit before anything else can happen, which is for a person to check, so these methods are named.
// The loops in them whose ref local is already named unit get the name other.
internal sealed class RefLocals(CSharpProject project, IReadOnlySet<string> parameterMethods)
{
	private sealed record Table(string Field, int Length, string Name, string? Bound);

	private static readonly Table[] Tables =
	[
		// MaxUnitId is only ever set to USA_PLANE_END (180).
		new("Units", 256, "unit", "MaxUnitId"),
		new("Fires", 512, "fire", null),
		new("Effects", 1024, "effect", null),
		new("Clouds", 4096, "cloud", null),
	];

	private readonly CSharpProject _project = project;
	private readonly IReadOnlySet<string> _parameterMethods = parameterMethods;

	private static bool Assigns(SyntaxNode body, string variable)
	{
		return body.DescendantNodes().Any(n => n switch
		{
			AssignmentExpressionSyntax a => a.Left is IdentifierNameSyntax { Identifier.ValueText: var name } && name == variable,
			PostfixUnaryExpressionSyntax p => p.Operand is IdentifierNameSyntax { Identifier.ValueText: var name } && name == variable,
			PrefixUnaryExpressionSyntax p => (p.IsKind(SyntaxKind.PreIncrementExpression) || p.IsKind(SyntaxKind.PreDecrementExpression))
				&& p.Operand is IdentifierNameSyntax { Identifier.ValueText: var name } && name == variable,
			ArgumentSyntax a => !a.RefKindKeyword.IsKind(SyntaxKind.None) && a.Expression is IdentifierNameSyntax { Identifier.ValueText: var name } && name == variable,
			_ => false,
		});
	}

	// The loop variable of a `v = <constant >= 0>; v < or <= <bound>; v++` header whose bound keeps v inside the table.
	private static string? LoopVariable(ForStatementSyntax loop, Table table, SemanticModel model)
	{
		if (loop.Declaration is not null
			|| loop.Initializers is not [AssignmentExpressionSyntax { Left: IdentifierNameSyntax variable, Right: var start }]
			|| model.GetConstantValue(start) is not { HasValue: true, Value: int first } || first < 0
			|| loop.Incrementors is not [PostfixUnaryExpressionSyntax { Operand: IdentifierNameSyntax incremented } increment]
			|| !increment.IsKind(SyntaxKind.PostIncrementExpression) || incremented.Identifier.ValueText != variable.Identifier.ValueText
			|| loop.Condition is not BinaryExpressionSyntax { Left: IdentifierNameSyntax compared } condition
			|| compared.Identifier.ValueText != variable.Identifier.ValueText)
		{
			return null;
		}

		var inclusive = condition.IsKind(SyntaxKind.LessThanOrEqualExpression);
		if (!inclusive && !condition.IsKind(SyntaxKind.LessThanExpression))
		{
			return null;
		}

		var bounded = condition.Right is IdentifierNameSyntax { Identifier.ValueText: var bound } && bound == table.Bound
			|| model.GetConstantValue(condition.Right) is { HasValue: true, Value: int last } && (inclusive ? last < table.Length : last <= table.Length);
		return bounded ? variable.Identifier.ValueText : null;
	}

	private static bool IsElement(SyntaxNode node, Table table, string variable)
	{
		return node is ElementAccessExpressionSyntax
		{
			Expression: IdentifierNameSyntax { Identifier.ValueText: var field },
			ArgumentList.Arguments: [{ RefKindKeyword.RawKind: (int)SyntaxKind.None, Expression: IdentifierNameSyntax { Identifier.ValueText: var index } }],
		} && field == table.Field && index == variable;
	}

	private static string Indentation(SourceText text, BlockSyntax body)
	{
		var first = body.Statements.FirstOrDefault() ?? (SyntaxNode)body;
		var line = text.Lines.GetLineFromPosition(first.SpanStart);
		return text.ToString(TextSpan.FromBounds(line.Start, first.SpanStart)).TrimEnd() == ""
			? text.ToString(TextSpan.FromBounds(line.Start, first.SpanStart))
			: "\t";
	}

	// The ref local for the unit that a parameter numbers, at the start of the method, and the renames of the loops' ref
	// locals named unit to other.
	private static void ParameterEdits(MethodDeclarationSyntax method, SourceText text, List<(TextSpan Span, string Text)> edits)
	{
		if (method.Body is not { } body)
		{
			return;
		}

		var units = Tables[0];
		foreach (var parameter in method.ParameterList.Parameters)
		{
			var name = parameter.Identifier.ValueText;
			if (parameter.Type?.ToString() != "int" || parameter.Modifiers.Count > 0 || Assigns(body, name))
			{
				continue;
			}

			var elements = body.DescendantNodes().Where(n => IsElement(n, units, name)).ToList();
			if (elements.Count < 2)
			{
				continue;
			}

			var tokens = method.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).ToList();
			var loops = body.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
				.Where(d => d.Declaration.Variables is [{ Identifier.ValueText: "unit" }] && d.Declaration.Type is RefTypeSyntax)
				.Select(d => d.Parent!)
				.ToList();
			var unitTokens = tokens.Where(t => t.ValueText == "unit").ToList();
			if (unitTokens.Count > 0)
			{
				// Every use of unit must be in a loop that declares it, and other must be free.
				if (tokens.Any(t => t.ValueText == "other") || unitTokens.Any(t => !loops.Any(l => l.Span.Contains(t.Span))))
				{
					continue;
				}

				edits.AddRange(unitTokens.Select(t => (t.Span, "other")));
			}

			var first = body.Statements.FirstOrDefault();
			var indentation = first is null ? "\t" : text.ToString(TextSpan.FromBounds(text.Lines.GetLineFromPosition(first.SpanStart).Start, first.SpanStart));
			edits.Add((new TextSpan(body.OpenBraceToken.Span.End, 0), $"\n{indentation}ref var unit = ref Units[{name}];"));
			edits.AddRange(elements.Select(e => (e.Span, "unit")));
			return;
		}
	}

	private List<(TextSpan Span, string Text)> Edits(SyntaxTree tree, SemanticModel model)
	{
		var text = tree.GetText();
		var edits = new List<(TextSpan Span, string Text)>();
		foreach (var method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => _parameterMethods.Contains(m.Identifier.ValueText)))
		{
			ParameterEdits(method, text, edits);
		}

		if (edits.Count > 0)
		{
			return edits;
		}

		foreach (var method in tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
		{
			var used = method.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText).ToHashSet();
			var names = new Dictionary<ForStatementSyntax, string>();
			foreach (var loop in method.DescendantNodes().OfType<ForStatementSyntax>())
			{
				if (loop.Statement is not BlockSyntax body)
				{
					continue;
				}

				foreach (var table in Tables)
				{
					if (LoopVariable(loop, table, model) is not { } variable || Assigns(body, variable))
					{
						continue;
					}

					var elements = body.DescendantNodes().Where(n => IsElement(n, table, variable)).ToList();
					if (elements.Count < 2)
					{
						continue;
					}

					// Names taken by the loops around this one, which C# does not let an inner local reuse.
					var outer = loop.Ancestors().OfType<ForStatementSyntax>().Select(l => names.GetValueOrDefault(l)).OfType<string>().ToHashSet();
					var name = new[] { table.Name, "other" }.FirstOrDefault(n => !used.Contains(n) && !outer.Contains(n));
					if (name is null)
					{
						continue;
					}

					names[loop] = name;
					edits.Add((new TextSpan(body.OpenBraceToken.Span.End, 0), $"\n{Indentation(text, body)}ref var {name} = ref {table.Field}[{variable}];"));
					edits.AddRange(elements.Select(e => (e.Span, name)));
				}
			}
		}

		return edits;
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> edits)
	{
		var applied = new List<(TextSpan Span, string Text)>();
		foreach (var edit in edits.OrderBy(e => e.Span.Start).ThenByDescending(e => e.Span.Length))
		{
			// An element inside an element (Units[Units[m].GroupLeader]) is replaced with the outer one only when they
			// do not overlap; the inner one is replaced on the next run.
			if (applied.Count == 0 || edit.Span.Start >= applied[^1].Span.End)
			{
				applied.Add(edit);
			}
		}

		foreach (var (span, replacement) in applied.AsEnumerable().Reverse())
		{
			text = text[..span.Start] + replacement + text[span.End..];
		}

		return text;
	}

	// Adds the ref locals in the project's files, and writes the files that changed. Returns their paths.
	public IReadOnlyList<string> Run()
	{
		var files = _project.ReadFiles();
		var compilation = _project.Compile(files, CSharpProject.FrameworkReferences);
		var written = new List<string>();
		foreach (var tree in compilation.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath)))
		{
			var edits = Edits(tree, compilation.GetSemanticModel(tree));
			if (edits.Count > 0)
			{
				File.WriteAllText(tree.FilePath, Apply(files[tree.FilePath], edits));
				written.Add(tree.FilePath);
			}
		}

		return written;
	}
}
