using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Renames the locals and parameters of methods, by their symbols, not their text (docs/Refactoring.md, Renaming:
// locals are renamed per function). The renames are lines of a file: a method, an old name and a new name, such as
//
//   CheckHit n target
//
// which renames every local and parameter named n in the methods named CheckHit. The result is compiled, and a rename
// that does not compile (a name that is already used) is reported and left out.
internal sealed class LocalRenamer(
	CSharpProject project,
	IReadOnlyList<(string Method, string Old, string New)> renames
)
{
	private readonly CSharpProject _project = project;
	private readonly IReadOnlyList<(string Method, string Old, string New)> _renames = renames;

	public static List<(string Method, string Old, string New)> ReadRenames(string path)
	{
		return
		[
			.. File.ReadLines(path)
				.Select(l => l.Trim())
				.Where(l => l.Length > 0 && !l.StartsWith('#'))
				.Select(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
				.Select(p => (p[0], p[1], p[2])),
		];
	}

	private static string? MethodName(SyntaxNode member)
	{
		return member switch
		{
			MethodDeclarationSyntax m => m.Identifier.ValueText,
			ConstructorDeclarationSyntax c => c.Identifier.ValueText,
			_ => null,
		};
	}

	private static IEnumerable<BaseMethodDeclarationSyntax> Methods(SyntaxTree tree)
	{
		return tree.GetRoot()
			.DescendantNodes()
			.OfType<BaseMethodDeclarationSyntax>()
			.Where(m => MethodName(m) is not null);
	}

	private static bool IsLocal(ISymbol? symbol)
	{
		return symbol is ILocalSymbol or IParameterSymbol;
	}

	// The edits of one rename in one method: its declarations and uses.
	private static List<(TextSpan Span, string Text)> Edits(
		BaseMethodDeclarationSyntax method,
		SemanticModel model,
		string oldName,
		string newName
	)
	{
		var edits = new List<(TextSpan Span, string Text)>();
		foreach (var node in method.DescendantNodes())
		{
			var (token, symbol) = node switch
			{
				VariableDeclaratorSyntax d => (d.Identifier, model.GetDeclaredSymbol(d)),
				ParameterSyntax p => (p.Identifier, model.GetDeclaredSymbol(p)),
				ForEachStatementSyntax f => (f.Identifier, model.GetDeclaredSymbol(f)),
				SingleVariableDesignationSyntax s => (s.Identifier, model.GetDeclaredSymbol(s)),
				IdentifierNameSyntax n => (n.Identifier, model.GetSymbolInfo(n).Symbol),
				_ => (default, null),
			};
			if (token.ValueText == oldName && IsLocal(symbol))
			{
				edits.Add((token.Span, newName));
			}
		}

		return edits;
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> edits)
	{
		foreach (
			var (span, replacement) in edits
				.OrderByDescending(e => e.Span.Start)
				.ThenByDescending(e => e.Span.Length)
		)
		{
			text = text[..span.Start] + replacement + text[span.End..];
		}

		return text;
	}

	// The locals and parameters of each method of the project, by file, for writing the renames.
	public IReadOnlyList<string> List()
	{
		var files = _project.ReadFiles();
		var compilation = _project.Compile(files, CSharpProject.FrameworkReferences);
		var lines = new List<string>();
		foreach (
			var tree in compilation
				.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath))
				.OrderBy(t => t.FilePath)
		)
		{
			foreach (var method in Methods(tree))
			{
				var names = method
					.DescendantNodes()
					.Select(n =>
						n switch
						{
							VariableDeclaratorSyntax
							{
								Parent.Parent: LocalDeclarationStatementSyntax or ForStatementSyntax
							} d => d.Identifier.ValueText,
							ParameterSyntax p => p.Identifier.ValueText,
							ForEachStatementSyntax f => f.Identifier.ValueText,
							_ => null,
						}
					)
					.OfType<string>()
					.Distinct();
				lines.Add(
					$"{Path.GetFileName(tree.FilePath)} {MethodName(method)}: {string.Join(' ', names)}"
				);
			}
		}

		return lines;
	}

	// Renames the locals, and writes the files that changed. Returns the renames left out, with the first error of each.
	public IReadOnlyList<string> Run()
	{
		var files = _project.ReadFiles();
		var compilation = _project.Compile(files, CSharpProject.FrameworkReferences);
		var report = new List<string>();
		foreach (
			var tree in compilation.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath)).ToList()
		)
		{
			var current = compilation.SyntaxTrees.Single(t => t.FilePath == tree.FilePath);
			var baseline = compilation
				.GetDiagnostics()
				.Where(d =>
					d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree == current
				)
				.Select(d => d.GetMessage())
				.ToHashSet();
			foreach (var (methodName, oldName, newName) in _renames)
			{
				var model = compilation.GetSemanticModel(current);
				var edits = Methods(current)
					.Where(m => MethodName(m) == methodName)
					.SelectMany(m => Edits(m, model, oldName, newName))
					.ToList();
				if (edits.Count == 0)
				{
					continue;
				}

				var newText = Apply(current.GetText().ToString(), edits);
				var newTree = CSharpSyntaxTree.ParseText(
					newText,
					_project.ParseOptions(),
					tree.FilePath
				);
				var newCompilation = compilation.ReplaceSyntaxTree(current, newTree);
				var error = newCompilation
					.GetDiagnostics()
					.FirstOrDefault(d =>
						d.Severity == DiagnosticSeverity.Error
						&& d.Location.SourceTree == newTree
						&& !baseline.Contains(d.GetMessage())
					);
				if (error is not null)
				{
					report.Add($"{methodName} {oldName} {newName}: {error}");
					continue;
				}

				compilation = newCompilation;
				current = newTree;
			}

			if (current != tree)
			{
				var text = current.GetText().ToString();
				files[tree.FilePath] = text;
				File.WriteAllText(tree.FilePath, text);
			}
		}

		return report;
	}
}
