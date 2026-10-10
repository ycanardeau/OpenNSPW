using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Gives a new effect or fire a ref local (docs/Refactoring.md, Tables and loops):
//
//   f=FindFreeEffect();                        f=FindFreeEffect();
//   Effects[f].Layer=EffectLayer.Upper;   =>   ref var effect = ref Effects[f];
//   Effects[f].TimeLeft=40;                    effect.Layer=EffectLayer.Upper;
//                                              effect.TimeLeft=40;
//
// The ref local refers to the same element as Effects[f] does in the statements that follow, up to the first one that
// assigns f again, so the code does the same. It is only given when those statements use the element at least twice.
// A later new element in the scope of the local reuses it (`effect = ref Effects[f];`). The result is
// compiled, and the methods where it does not compile (a name that is already used) are left as they were.
internal sealed class ElementRefs(CSharpProject project)
{
	private sealed record Table(string Finder, string Field, string Name);

	private static readonly Table[] Tables =
	[
		new("FindFreeEffect", "Effects", "effect"),
		new("FindFreeFire", "Fires", "fire"),
	];

	private readonly CSharpProject _project = project;

	private static bool Assigns(SyntaxNode node, string variable)
	{
		return node.DescendantNodesAndSelf().Any(n => n switch
		{
			AssignmentExpressionSyntax a => a.Left is IdentifierNameSyntax { Identifier.ValueText: var name } && name == variable,
			PostfixUnaryExpressionSyntax p => p.Operand is IdentifierNameSyntax { Identifier.ValueText: var name } && name == variable,
			PrefixUnaryExpressionSyntax p => (p.IsKind(SyntaxKind.PreIncrementExpression) || p.IsKind(SyntaxKind.PreDecrementExpression))
				&& p.Operand is IdentifierNameSyntax { Identifier.ValueText: var name } && name == variable,
			ArgumentSyntax a => !a.RefKindKeyword.IsKind(SyntaxKind.None) && a.Expression is IdentifierNameSyntax { Identifier.ValueText: var name } && name == variable,
			_ => false,
		});
	}

	private static string Indentation(SourceText text, int position)
	{
		var line = text.Lines.GetLineFromPosition(position);
		var end = line.Start;
		while (end < line.End && text[end] is ' ' or '\t')
		{
			end++;
		}

		return text.ToString(TextSpan.FromBounds(line.Start, end));
	}

	// The edits of one method, as one group, so that they can be left out together.
	private static List<List<(TextSpan Span, string Text)>> Edits(SyntaxTree tree)
	{
		var text = tree.GetText();
		var groups = new List<List<(TextSpan Span, string Text)>>();
		foreach (var method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
		{
			var edits = new List<(TextSpan Span, string Text)>();
			// The ref locals declared so far: the statement that declares each, whose scope is the statements after it in
			// its list, and their descendants.
			var declarations = new List<(StatementSyntax Statement, string Name)>();
			var lists = method.DescendantNodes().Select(n => n switch
			{
				BlockSyntax block => block.Statements,
				SwitchSectionSyntax section => section.Statements,
				_ => default(SyntaxList<StatementSyntax>?),
			}).OfType<SyntaxList<StatementSyntax>>();
			foreach (var list in lists)
			{
				for (var i = 0; i < list.Count; i++)
				{
					if (list[i] is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax index, Right: InvocationExpressionSyntax { Expression: IdentifierNameSyntax finder, ArgumentList.Arguments.Count: 0 } } }
						|| Tables.FirstOrDefault(t => t.Finder == finder.Identifier.ValueText) is not { } table)
					{
						continue;
					}

					var variable = index.Identifier.ValueText;
					var run = list.Skip(i + 1).TakeWhile(s => !Assigns(s, variable)).ToList();
					var uses = run.SelectMany(s => s.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
						.Where(e => e.Expression is IdentifierNameSyntax { Identifier.ValueText: var field } && field == table.Field
							&& e.ArgumentList.Arguments is [{ Expression: IdentifierNameSyntax { Identifier.ValueText: var argument } }] && argument == variable)
						.ToList();
					if (uses.Count < 2)
					{
						continue;
					}

					var indentation = Indentation(text, list[i].SpanStart);
					var site = list[i];
					var inScope = declarations.Any(d => d.Name == table.Name && d.Statement.Parent is { } scope
						&& site.Ancestors().Contains(scope) && d.Statement.Span.End <= site.SpanStart);
					var reference = inScope ? $"{table.Name} = ref {table.Field}[{variable}];" : $"ref var {table.Name} = ref {table.Field}[{variable}];";
					if (!inScope)
					{
						declarations.Add((site, table.Name));
					}

					edits.Add((new TextSpan(list[i].FullSpan.End, 0), $"{indentation}{reference}\n"));
					edits.AddRange(uses.Select(u => (u.Span, table.Name)));
				}
			}

			if (edits.Count > 0)
			{
				groups.Add(edits);
			}
		}

		return groups;
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> edits)
	{
		foreach (var (span, replacement) in edits.OrderByDescending(e => e.Span.Start).ThenByDescending(e => e.Span.Length))
		{
			text = text[..span.Start] + replacement + text[span.End..];
		}

		return text;
	}

	// Gives the new elements of the project's files ref locals, and writes the files that changed. Returns their paths
	// and the number of methods changed in each.
	public IReadOnlyList<string> Run()
	{
		var files = _project.ReadFiles();
		var compilation = _project.Compile(files, CSharpProject.FrameworkReferences);
		var written = new List<string>();
		foreach (var tree in compilation.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath)).ToList())
		{
			var groups = Edits(tree);
			while (groups.Count > 0)
			{
				var newText = Apply(files[tree.FilePath], groups.SelectMany(g => g));
				var newTree = CSharpSyntaxTree.ParseText(newText, _project.ParseOptions(), tree.FilePath);
				var newCompilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(t => t.FilePath == tree.FilePath), newTree);
				var errors = newCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree == newTree).ToList();
				if (errors.Count == 0)
				{
					File.WriteAllText(tree.FilePath, newText);
					files[tree.FilePath] = newText;
					compilation = newCompilation;
					written.Add($"{tree.FilePath}: {groups.Count}");
					break;
				}

				// Leave out the methods with errors, found by their position in the new text.
				var newMethods = newTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
				var failing = errors.Select(e => newMethods.FirstOrDefault(m => m.Span.Contains(e.Location.SourceSpan))?.Identifier.ValueText).ToHashSet();
				var oldMethods = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
				var kept = groups.Where(g => !failing.Contains(oldMethods.First(m => m.FullSpan.Contains(g[0].Span.Start)).Identifier.ValueText)).ToList();
				if (kept.Count == groups.Count)
				{
					throw new InvalidOperationException($"Errors outside the changed methods in {tree.FilePath}: {errors[0]}");
				}

				groups = kept;
			}
		}

		return written;
	}
}
