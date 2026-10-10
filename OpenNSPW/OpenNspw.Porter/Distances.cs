using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Replaces the original's computation of a distance, written out about 70 times, with a call of nspw_math.Distance,
// which computes exactly the same (docs/Refactoring.md, Value types):
//
//   if(wrk_x==0) wrk_x=1;
//   if(wrk_y==0) wrk_y=1;
//   drctn=atan2(wrk_y,wrk_x)*RAD_to;
//   if(drctn<0) drctn=360+drctn;
//   if(wrk_x<0) wrk_x=0-wrk_x;                 =>  dstc=Distance(wrk_x, wrk_y);
//   if(wrk_y<0) wrk_y=0-wrk_y;
//   if(drctn>=180) drctn=drctn-180;
//   if(drctn>=90) drctn=90-(drctn-90);
//   dstc=(wrk_x)/(CosDegrees(drctn));
//
// The statements change wrk_x, wrk_y and drctn, and the call does not, so a site is only replaced when
//
// - the three are double locals or parameters, and what gets the distance is a double that does not use them;
// - none of the three flows out of the statements: each is written again before it is read, or never read;
// - the method never takes the address of one of them, which data flow analysis does not see as a read;
// - no comment or directive is inside the statements, so that nothing is lost.
internal sealed class Distances(CSharpProject project)
{
	private readonly CSharpProject _project = project;

	private static string Normalized(SyntaxNode node)
	{
		return string.Concat(node.ToString().Where(c => !char.IsWhiteSpace(c)));
	}

	// The statements that the computation consists of, with x, y and d for the variables, without whitespace.
	private static string[] Template(string x, string y, string d)
	{
		return
		[
			$"if({x}==0){x}=1;",
			$"if({y}==0){y}=1;",
			$"{d}=atan2({y},{x})*RAD_to;",
			$"if({d}<0){d}=360+{d};",
			$"if({x}<0){x}=0-{x};",
			$"if({y}<0){y}=0-{y};",
			$"if({d}>=180){d}={d}-180;",
			$"if({d}>=90){d}=90-({d}-90);",
		];
	}

	// What the last statement assigns the distance to, if it is `r=x/CosDegrees(d);` with parentheses: a variable or a
	// field of type double that does not use the computation's variables.
	private static ExpressionSyntax? Result(StatementSyntax statement, string x, string d, SemanticModel model, string[] variables)
	{
		if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
			|| !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
			|| model.GetTypeInfo(assignment.Left).Type?.SpecialType != SpecialType.System_Double
			|| assignment.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(i => variables.Contains(i.Identifier.ValueText)))
		{
			return null;
		}

		var value = Normalized(assignment.Right);
		string[] forms = [$"{x}/CosDegrees({d})", $"({x})/(CosDegrees({d}))", $"(({x})/(CosDegrees({d})))", $"({x})/CosDegrees({d})"];
		return forms.Contains(value) ? assignment.Left : null;
	}

	private static bool IsDouble(SemanticModel model, SyntaxNode method, string name)
	{
		var symbol = method.DescendantNodes().OfType<IdentifierNameSyntax>().Where(i => i.Identifier.ValueText == name)
			.Select(i => model.GetSymbolInfo(i).Symbol).FirstOrDefault(s => s is not null);
		return symbol switch
		{
			ILocalSymbol { IsRef: false } local => local.Type.SpecialType == SpecialType.System_Double,
			IParameterSymbol { RefKind: RefKind.None } parameter => parameter.Type.SpecialType == SpecialType.System_Double,
			_ => false,
		};
	}

	private static bool TakesAddress(SyntaxNode method, string name)
	{
		return method.DescendantNodes().OfType<PrefixUnaryExpressionSyntax>()
			.Any(p => p.IsKind(SyntaxKind.AddressOfExpression) && p.Operand is IdentifierNameSyntax { Identifier.ValueText: var operand } && operand == name);
	}

	// Whether a comment or a directive is between the first and the last statement.
	private static bool HasTriviaInside(IReadOnlyList<StatementSyntax> statements)
	{
		var inside = TextSpan.FromBounds(statements[0].SpanStart, statements[^1].Span.End);
		return statements[0].Parent!.DescendantTrivia(inside, descendIntoTrivia: true)
			.Any(t => inside.Contains(t.Span) && (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia) || t.IsDirective));
	}

	// The replacement of the computation that starts at statements[start], if it is one that can be replaced.
	private static (TextSpan Span, string Text)? Edit(IReadOnlyList<StatementSyntax> statements, int start, SemanticModel model)
	{
		if (start + 9 > statements.Count
			|| statements[start + 2] is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax direction, Right: BinaryExpressionSyntax { Left: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "atan2" }, ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax y }, { Expression: IdentifierNameSyntax x }] } } } })
		{
			return null;
		}

		var (xName, yName, dName) = (x.Identifier.ValueText, y.Identifier.ValueText, direction.Identifier.ValueText);
		var template = Template(xName, yName, dName);
		var range = statements.Skip(start).Take(9).ToList();
		string[] variables = [xName, yName, dName];
		if (!template.Select((t, i) => t == Normalized(range[i])).All(b => b) || Result(range[8], xName, dName, model, variables) is not { } result)
		{
			return null;
		}

		var method = range[0].Ancestors().OfType<MethodDeclarationSyntax>().First();
		if (variables.Distinct().Count() != 3
			|| !variables.All(v => IsDouble(model, method, v)) || variables.Any(v => TakesAddress(method, v)) || HasTriviaInside(range))
		{
			return null;
		}

		var flow = model.AnalyzeDataFlow(range[0], range[8])!;
		if (flow.DataFlowsOut.Any(s => variables.Contains(s.Name)))
		{
			return null;
		}

		return (TextSpan.FromBounds(range[0].SpanStart, range[8].Span.End), $"{result}=Distance({xName}, {yName});");
	}

	private static List<(TextSpan Span, string Text)> Edits(SyntaxTree tree, SemanticModel model)
	{
		var edits = new List<(TextSpan Span, string Text)>();
		var lists = tree.GetRoot().DescendantNodes().Select(n => n switch
		{
			BlockSyntax block => block.Statements,
			SwitchSectionSyntax section => section.Statements,
			_ => default(SyntaxList<StatementSyntax>?),
		}).OfType<SyntaxList<StatementSyntax>>();
		foreach (var list in lists)
		{
			for (var i = 0; i < list.Count; i++)
			{
				if (Edit(list, i, model) is { } edit)
				{
					edits.Add(edit);
					i += 8;
				}
			}
		}

		return edits;
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> edits)
	{
		foreach (var (span, replacement) in edits.OrderByDescending(e => e.Span.Start))
		{
			text = text[..span.Start] + replacement + text[span.End..];
		}

		return text;
	}

	// Replaces the computations in the project's files, and writes the files that changed. Returns their paths and the
	// number of replacements in each.
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
				written.Add($"{tree.FilePath}: {edits.Count}");
			}
		}

		return written;
	}
}
