using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Makes bool the int locals that the original uses as flags (docs/Refactoring.md, Clean-up: integer truth tests): the
// locals that are only ever set to 0 or 1 (FALSE or TRUE) and only ever compared with 0 or 1.
//
//   var isHeard=0;                                  var isHeard=false;
//   isHeard=1;                               =>     isHeard=true;
//   for(...; n<=MaxUnitId && isHeard==0; n++)       for(...; n<=MaxUnitId && !isHeard; n++)
//
// `x!=0` and `x==1` become `x`, and `x==0` and `x!=1` become `!x`, which are the same truth, as x is never anything but
// 0 or 1. A local used in any other way (incremented, passed, returned, assigned another value or in an expression, or
// declared with others) stays int.
internal sealed class BoolLocals(CSharpProject project)
{
	private readonly CSharpProject _project = project;

	// 0 or 1, for a constant int expression that is one of them.
	private static int? Bit(ExpressionSyntax expression, SemanticModel model)
	{
		return
			model.GetConstantValue(expression) is { HasValue: true, Value: int value and (0 or 1) }
			? value
			: null;
	}

	private static string Literal(int bit)
	{
		return bit != 0 ? "true" : "false";
	}

	// The edits that make a local bool, or null if it is not only a flag.
	private static List<(TextSpan Span, string Text)>? Edits(
		VariableDeclaratorSyntax declarator,
		IEnumerable<IdentifierNameSyntax> uses,
		SemanticModel model
	)
	{
		if (
			declarator.Parent
			is not VariableDeclarationSyntax
			{
				Variables.Count: 1,
				Parent: LocalDeclarationStatementSyntax
			} declaration
		)
		{
			return null;
		}

		var edits = new List<(TextSpan Span, string Text)>();
		if (declarator.Initializer is { } initializer)
		{
			if (Bit(initializer.Value, model) is not { } bit)
			{
				return null;
			}

			edits.Add((initializer.Value.Span, Literal(bit)));
		}
		else if (declaration.Type.IsVar)
		{
			return null;
		}

		if (!declaration.Type.IsVar)
		{
			edits.Add((declaration.Type.Span, "bool"));
		}

		var reads = 0;
		foreach (var use in uses)
		{
			switch (use.Parent)
			{
				case AssignmentExpressionSyntax assignment
					when assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
						&& assignment.Left == use
						&& assignment.Parent is ExpressionStatementSyntax
						&& Bit(assignment.Right, model) is { } bit:
					edits.Add((assignment.Right.Span, Literal(bit)));
					break;
				case BinaryExpressionSyntax binary
					when binary.IsKind(SyntaxKind.EqualsExpression)
						|| binary.IsKind(SyntaxKind.NotEqualsExpression):
					if (
						Bit(binary.Left == use ? binary.Right : binary.Left, model)
						is not { } compared
					)
					{
						return null;
					}

					var isTrue = binary.IsKind(SyntaxKind.EqualsExpression) == (compared == 1);
					edits.Add(
						(binary.Span, isTrue ? use.Identifier.Text : "!" + use.Identifier.Text)
					);
					reads++;
					break;
				default:
					return null;
			}
		}

		return reads > 0 ? edits : null;
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> edits)
	{
		var end = int.MaxValue;
		foreach (var (span, replacement) in edits.OrderByDescending(e => e.Span.Start))
		{
			if (span.End > end)
			{
				throw new InvalidOperationException($"Overlapping edits at {span}.");
			}

			text = text[..span.Start] + replacement + text[span.End..];
			end = span.Start;
		}

		return text;
	}

	// Makes the flags bool, and writes the files that changed. Returns the locals made bool, and the errors of the result.
	public IReadOnlyList<string> Run()
	{
		var files = _project.ReadFiles();
		var compilation = _project.Compile(files, CSharpProject.FrameworkReferences);
		var baseline = compilation
			.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.Select(d => d.ToString())
			.ToHashSet();
		var report = new List<string>();
		var changed = new Dictionary<string, string>();
		foreach (var tree in compilation.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath)))
		{
			var model = compilation.GetSemanticModel(tree);
			var edits = new List<(TextSpan Span, string Text)>();
			foreach (
				var declarator in tree.GetRoot()
					.DescendantNodes()
					.OfType<VariableDeclaratorSyntax>()
			)
			{
				if (
					model.GetDeclaredSymbol(declarator)
						is not ILocalSymbol { Type.SpecialType: SpecialType.System_Int32 } local
					|| declarator.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault()
						is not { } member
				)
				{
					continue;
				}

				var uses = member
					.DescendantNodes()
					.OfType<IdentifierNameSyntax>()
					.Where(n =>
						n.Identifier.ValueText == local.Name
						&& SymbolEqualityComparer.Default.Equals(
							model.GetSymbolInfo(n).Symbol,
							local
						)
					);
				if (Edits(declarator, uses, model) is { } localEdits)
				{
					edits.AddRange(localEdits);
					report.Add(
						$"{Path.GetFileName(tree.FilePath)} {member.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.IdentifierToken)).ValueText} {local.Name}"
					);
				}
			}

			if (edits.Count > 0)
			{
				changed[tree.FilePath] = Apply(tree.GetText().ToString(), edits);
			}
		}

		var result = compilation;
		foreach (var (path, text) in changed)
		{
			var tree = result.SyntaxTrees.Single(t => t.FilePath == path);
			result = result.ReplaceSyntaxTree(
				tree,
				CSharpSyntaxTree.ParseText(text, _project.ParseOptions(), path)
			);
			File.WriteAllText(path, text);
		}

		report.AddRange(
			result
				.GetDiagnostics()
				.Where(d => d.Severity == DiagnosticSeverity.Error)
				.Select(d => d.ToString())
				.Where(d => !baseline.Contains(d))
		);
		return report;
	}
}
