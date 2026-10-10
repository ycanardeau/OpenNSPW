using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Renames members of the port, and every use of them in the port and in the projects that use it, by their symbols, not
// their text (docs/Refactoring.md, Renaming). A renamed global, struct field, struct or function gets [Original] with
// its old name, unless it has one, so that the tests still find it by its C++ name. A field declaration that declares
// a renamed field among others is split first, one field per declaration, so that each can have its own [Original].
// Everything else in the files stays as it is.
//
// The renames are lines of a file: a documentation comment ID and the new name, such as
//
//   F:OpenNspw.Nspw.unit Units
//   T:OpenNspw.UNIT Unit
//   M:OpenNspw.Nspw.chara_cont UpdateBattle
//   M:OpenNspw.Nspw.fire_now FireWeapons    (all overloads of a method)
//
// A new name with dots moves the member into a field of a new type: `F:OpenNspw.UNIT.x Position.X` renames each use,
// `unit[m].x` to `unit[m].Position.X`, but leaves the declaration, which is replaced by hand with the new field.
//
// Code in inactive #if blocks and in comments is not renamed.
internal sealed class Renamer(CSharpProject port, IReadOnlyList<CSharpProject> users, IReadOnlyDictionary<string, string> renames)
{
	private readonly CSharpProject _port = port;
	private readonly IReadOnlyList<CSharpProject> _users = users;
	private readonly IReadOnlyDictionary<string, string> _renames = renames;

	public static Dictionary<string, string> ReadRenames(string path)
	{
		return File.ReadLines(path)
			.Select(l => l.Trim())
			.Where(l => l.Length > 0 && !l.StartsWith('#'))
			.Select(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
			.ToDictionary(p => p[0], p => p[1]);
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> edits)
	{
		foreach (var (span, replacement) in edits.OrderByDescending(e => e.Span.Start).ThenByDescending(e => e.Span.Length))
		{
			text = text[..span.Start] + replacement + text[span.End..];
		}

		return text;
	}

	// A method can also be named without its parameters (M:OpenNspw.Nspw.fire_now), which renames all its overloads.
	private string? NewName(ISymbol? symbol)
	{
		if (symbol?.OriginalDefinition.GetDocumentationCommentId() is not { } id)
		{
			return null;
		}

		var parameters = id.IndexOf('(');
		return _renames.TryGetValue(id, out var name) || (parameters > 0 && _renames.TryGetValue(id[..parameters], out name)) ? name : null;
	}

	// The new name of a symbol whose declaration is renamed too: not one that moves into a field of a new type.
	private string? NewDeclaredName(ISymbol? symbol)
	{
		return NewName(symbol) is { } name && !name.Contains('.') ? name : null;
	}

	// Whether a renamed symbol keeps its old name in [Original]: a member that the tests or the trace tools look up by
	// its C++ name.
	private static bool KeepsOriginalName(ISymbol symbol)
	{
		return symbol is IFieldSymbol { IsConst: false, IsStatic: false } or IMethodSymbol { MethodKind: MethodKind.Ordinary } or INamedTypeSymbol { TypeKind: TypeKind.Struct }
			&& !symbol.GetAttributes().Any(a => a.AttributeClass?.Name == "OriginalAttribute");
	}

	private static string Indentation(SyntaxNode node)
	{
		return string.Concat(node.GetLeadingTrivia().Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse().Select(t => t.ToFullString()));
	}

	// The first pass: splits each field declaration that declares a renamed field among others into one declaration per
	// field, on lines of their own. The comment at the end of the line stays after the last one.
	private Dictionary<string, string> Split(CSharpCompilation compilation, IReadOnlyDictionary<string, string> files)
	{
		var changed = new Dictionary<string, string>();
		foreach (var tree in compilation.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath)))
		{
			var model = compilation.GetSemanticModel(tree);
			var edits = new List<(TextSpan Span, string Text)>();
			foreach (var field in tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>())
			{
				var variables = field.Declaration.Variables;
				if (variables.Count < 2 || !variables.Any(v => NewDeclaredName(model.GetDeclaredSymbol(v)) is not null))
				{
					continue;
				}

				var prefix = files[tree.FilePath][field.SpanStart..variables[0].SpanStart];
				var separator = $"\n{Indentation(field)}";
				edits.Add((field.Span, string.Join(separator, variables.Select(v => $"{prefix}{v};"))));
			}

			if (edits.Count > 0)
			{
				changed[tree.FilePath] = Apply(files[tree.FilePath], edits);
			}
		}

		return changed;
	}

	private static (SyntaxToken Token, ISymbol? Symbol) Declared(SyntaxNode node, SemanticModel model)
	{
		return node switch
		{
			VariableDeclaratorSyntax declarator => (declarator.Identifier, model.GetDeclaredSymbol(declarator)),
			MethodDeclarationSyntax method => (method.Identifier, model.GetDeclaredSymbol(method)),
			BaseTypeDeclarationSyntax type => (type.Identifier, model.GetDeclaredSymbol(type)),
			ConstructorDeclarationSyntax constructor => (constructor.Identifier, model.GetDeclaredSymbol(constructor)?.ContainingType),
			PropertyDeclarationSyntax property => (property.Identifier, model.GetDeclaredSymbol(property)),
			EnumMemberDeclarationSyntax member => (member.Identifier, model.GetDeclaredSymbol(member)),
			ParameterSyntax parameter => (parameter.Identifier, model.GetDeclaredSymbol(parameter)),
			_ => (default, null),
		};
	}

	// [Original] before a declaration: on the field's line, or on a line of its own before a method or a struct.
	private static (TextSpan Span, string Text)? Original(SyntaxNode node, ISymbol symbol)
	{
		var attribute = $"[Original(\"{symbol.Name}\")]";
		return node switch
		{
			VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax field } => (new TextSpan(field.SpanStart, 0), $"{attribute} "),
			MethodDeclarationSyntax or StructDeclarationSyntax => (new TextSpan(node.SpanStart, 0), $"{attribute}\n{Indentation(node)}"),
			_ => null,
		};
	}

	// The second pass: renames the declarations and uses of the renamed symbols, and adds [Original].
	private Dictionary<string, string> Rename(CSharpCompilation compilation, IReadOnlyDictionary<string, string> files)
	{
		var changed = new Dictionary<string, string>();
		foreach (var tree in compilation.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath)))
		{
			var model = compilation.GetSemanticModel(tree);
			var edits = new List<(TextSpan Span, string Text)>();
			foreach (var node in tree.GetRoot().DescendantNodes())
			{
				if (node is SimpleNameSyntax name)
				{
					var info = model.GetSymbolInfo(name);
					if (NewName(info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is { } newName)
					{
						edits.Add((name.Identifier.Span, newName));
					}

					continue;
				}

				var (token, symbol) = Declared(node, model);
				if (symbol is not null && NewDeclaredName(symbol) is { } declaredName)
				{
					edits.Add((token.Span, declaredName));
					if (KeepsOriginalName(symbol) && Original(node, symbol) is { } original)
					{
						edits.Add(original);
					}
				}
			}

			if (edits.Count > 0)
			{
				changed[tree.FilePath] = Apply(files[tree.FilePath], edits);
			}
		}

		return changed;
	}

	private static void Write(Dictionary<string, string> files, Dictionary<string, string> changed, HashSet<string> written)
	{
		foreach (var (path, text) in changed)
		{
			files[path] = text;
			File.WriteAllText(path, text);
			written.Add(path);
		}
	}

	// Renames in the port and its users, and writes the files that changed. Returns their paths.
	public IReadOnlyList<string> Run()
	{
		var written = new HashSet<string>();
		var portFiles = _port.ReadFiles();
		Write(portFiles, Split(_port.Compile(portFiles, CSharpProject.FrameworkReferences), portFiles), written);

		// The users are compiled against the port before the renames, which is what their files refer to, and against
		// the users before them, which they may use.
		var portCompilation = _port.Compile(portFiles, CSharpProject.FrameworkReferences);
		var except = new HashSet<string>([_port.Name, .. _users.Select(u => u.Name)]);
		var compiled = new List<MetadataReference> { portCompilation.ToMetadataReference() };
		foreach (var user in _users)
		{
			var files = user.ReadFiles();
			var compilation = user.Compile(files, [.. CSharpProject.FrameworkReferences, .. user.OutputReferences(except), .. compiled]);
			compiled.Add(compilation.ToMetadataReference());
			Write(files, Rename(compilation, files), written);
		}

		Write(portFiles, Rename(portCompilation, portFiles), written);
		return [.. written];
	}
}
