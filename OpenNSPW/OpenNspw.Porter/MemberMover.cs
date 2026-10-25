using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Moves members of Nspw into a class that Nspw holds (docs/Refactoring.md, Splitting the partial class): the parts of
// Nspw in some files, which become parts of the class, and other members by name. A field given by name goes to a new
// file that declares the class; a method or property stays in its file, the port of its C++ file, in a part of the class
// after what is left of Nspw:
//
//   [Globals]
//   public sealed unsafe partial class Sound(Nspw game)
//   {
//       private readonly Nspw _game = game;
//       ...
//   }
//
// The uses are rewritten by their symbols: a moved instance member is reached through the member of Nspw that holds
// the object (`PlaySoundEffect(...)` and `game.PlaySoundEffect(...)` become `Sound.PlaySoundEffect(...)` and
// `game.Sound.PlaySoundEffect(...)`), a moved static member through the class, and a member of Nspw that the moved code
// uses through `_game` (`Units[n]` becomes `_game.Units[n]`); Nspw's static members stay reachable as they were, by a
// `using static` in the moved files. Nspw's member that holds the object is not added: Nspw creates it.
internal sealed class MemberMover(CSharpProject port, IReadOnlyList<CSharpProject> users, string className, string owner, string newFile, IReadOnlySet<string> files, IReadOnlySet<string> members)
{
	private const string Game = "_game";
	private const string UsingNspw = "using static OpenNspw.Nspw;\n";

	private readonly CSharpProject _port = port;
	private readonly IReadOnlyList<CSharpProject> _users = users;
	private readonly string _className = className;
	private readonly string _owner = owner;
	private readonly string _newFile = newFile;
	private readonly IReadOnlySet<string> _files = files;
	private readonly IReadOnlySet<string> _members = members;

	private static bool IsNspw(ISymbol? type, INamedTypeSymbol nspw)
	{
		return SymbolEqualityComparer.Default.Equals(type?.OriginalDefinition, nspw);
	}

	// The name a member is declared by, for the members given by name.
	private static string? DeclaredName(MemberDeclarationSyntax member)
	{
		return member switch
		{
			MethodDeclarationSyntax m => m.Identifier.ValueText,
			PropertyDeclarationSyntax p => p.Identifier.ValueText,
			FieldDeclarationSyntax { Declaration.Variables: [var variable] } => variable.Identifier.ValueText,
			_ => null,
		};
	}

	// The receiver that qualifies a name, if any: `game` in `game.Units`, `this` in `this.Units`.
	private static ExpressionSyntax? Receiver(SimpleNameSyntax name)
	{
		return name.Parent switch
		{
			MemberAccessExpressionSyntax access when access.Name == name => access.Expression,
			MemberBindingExpressionSyntax binding => binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>().First().Expression,
			_ => null,
		};
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> edits)
	{
		foreach (var (span, replacement) in edits.OrderByDescending(e => e.Span.Start).ThenByDescending(e => e.Span.End))
		{
			text = text[..span.Start] + replacement + text[span.End..];
		}

		return text;
	}

	private static string WithUsingNspw(string text)
	{
		if (text.Contains(UsingNspw, StringComparison.Ordinal))
		{
			return text;
		}

		var index = text.IndexOf("\nnamespace ", StringComparison.Ordinal) + 1;
		return text[..index] + UsingNspw + "\n" + text[index..];
	}

	// The declarations that move: the parts of Nspw in the files, and the members given by name.
	private List<SyntaxNode> Moved(INamedTypeSymbol nspw)
	{
		var moved = new List<SyntaxNode>();
		foreach (var reference in nspw.DeclaringSyntaxReferences)
		{
			var declaration = (TypeDeclarationSyntax)reference.GetSyntax();
			if (_files.Contains(Path.GetFullPath(declaration.SyntaxTree.FilePath)))
			{
				moved.Add(declaration);
				continue;
			}

			moved.AddRange(declaration.Members.Where(m => DeclaredName(m) is { } name && _members.Contains(name)));
		}

		return moved;
	}

	// The edits of the uses of the members in one tree. The private members used across the two classes are added to
	// widen.
	private List<(TextSpan Span, string Text)> UseEdits(SyntaxTree tree, SemanticModel model, INamedTypeSymbol nspw, HashSet<ISymbol> movedSymbols, IReadOnlyList<SyntaxNode> moved, HashSet<ISymbol> widen, List<string> report)
	{
		var edits = new List<(TextSpan Span, string Text)>();
		foreach (var name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
		{
			var info = model.GetSymbolInfo(name);
			var symbol = (info.Symbol ?? info.CandidateSymbols.FirstOrDefault())?.OriginalDefinition;
			if (symbol is not (IFieldSymbol or IMethodSymbol or IPropertySymbol or IEventSymbol) || !IsNspw(symbol.ContainingType, nspw)
				|| name.Parent is NameColonSyntax or NameEqualsSyntax)
			{
				continue;
			}

			var isInMoved = moved.Any(m => m.Span.Contains(name.Span));
			if (symbol.DeclaredAccessibility == Accessibility.Private && isInMoved != movedSymbols.Contains(symbol))
			{
				widen.Add(symbol);
			}

			var receiver = Receiver(name);
			var receiverIsType = receiver is not null && model.GetSymbolInfo(receiver).Symbol is ITypeSymbol;
			if (movedSymbols.Contains(symbol))
			{
				if (symbol.IsStatic)
				{
					if (receiverIsType)
					{
						edits.Add((receiver!.Span, _className));
					}
					else if (receiver is null && !isInMoved)
					{
						edits.Add((new TextSpan(name.SpanStart, 0), _className + "."));
					}
				}
				else if (receiver is ThisExpressionSyntax && isInMoved)
				{
				}
				else if (receiver is not null || !isInMoved)
				{
					edits.Add((new TextSpan(name.SpanStart, 0), _owner + "."));
				}
			}
			else if (isInMoved && !symbol.IsStatic)
			{
				if (receiver is ThisExpressionSyntax)
				{
					edits.Add((receiver.Span, Game));
				}
				else if (receiver is null)
				{
					edits.Add((new TextSpan(name.SpanStart, 0), Game + "."));
				}
			}
		}

		foreach (var self in tree.GetRoot().DescendantNodes().OfType<ThisExpressionSyntax>())
		{
			if (moved.Any(m => m.Span.Contains(self.Span)) && self.Parent is not MemberAccessExpressionSyntax)
			{
				report.Add($"{tree.FilePath}({self.GetLocation().GetLineSpan().StartLinePosition.Line + 1}): `this` in moved code");
			}
		}

		return edits;
	}

	// The member of a user compilation's Nspw that is the same as one of the port's compilation.
	private static ISymbol? FindIn(INamedTypeSymbol nspw, ISymbol symbol)
	{
		var id = symbol.GetDocumentationCommentId();
		return nspw.GetMembers(symbol.Name).FirstOrDefault(m => m.GetDocumentationCommentId() == id);
	}

	// Moves the members, and writes the files that changed. Returns their paths, and what needs a look.
	public IReadOnlyList<string> Run()
	{
		var portFiles = _port.ReadFiles();
		var portCompilation = _port.Compile(portFiles, CSharpProject.FrameworkReferences);
		var nspw = portCompilation.GetTypeByMetadataName("OpenNspw.Nspw") ?? throw new InvalidOperationException("No Nspw.");
		var moved = Moved(nspw);
		var movedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		foreach (var declaration in moved)
		{
			var model = portCompilation.GetSemanticModel(declaration.SyntaxTree);
			var members = declaration is TypeDeclarationSyntax type ? type.Members.AsEnumerable() : [(MemberDeclarationSyntax)declaration];
			foreach (var member in members)
			{
				var symbols = member is FieldDeclarationSyntax field
					? field.Declaration.Variables.Select(v => model.GetDeclaredSymbol(v))
					: [model.GetDeclaredSymbol(member)];
				movedSymbols.UnionWith(symbols.OfType<ISymbol>());
			}
		}

		var compilations = new List<(CSharpCompilation Compilation, Dictionary<string, string> Files)> { (portCompilation, portFiles) };
		var except = new HashSet<string>([_port.Name, .. _users.Select(u => u.Name)]);
		var compiled = new List<MetadataReference> { portCompilation.ToMetadataReference() };
		foreach (var user in _users)
		{
			var files = user.ReadFiles();
			var compilation = user.Compile(files, [.. CSharpProject.FrameworkReferences, .. user.OutputReferences(except), .. compiled]);
			compiled.Add(compilation.ToMetadataReference());
			compilations.Add((compilation, files));
		}

		var report = new List<string>();

		// The private members used across the classes become internal.
		var widen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		foreach (var tree in portCompilation.SyntaxTrees.Where(t => portFiles.ContainsKey(t.FilePath)))
		{
			UseEdits(tree, portCompilation.GetSemanticModel(tree), nspw, movedSymbols, [.. moved.Where(m => m.SyntaxTree == tree)], widen, []);
		}

		var written = new List<string>();
		var extra = new List<string>();
		foreach (var (compilation, files) in compilations)
		{
			var userNspw = compilation == portCompilation ? nspw : compilation.GetTypeByMetadataName("OpenNspw.Nspw")!;
			var userMoved = compilation == portCompilation ? movedSymbols : new HashSet<ISymbol>(movedSymbols.Select(s => FindIn(userNspw, s)).OfType<ISymbol>(), SymbolEqualityComparer.Default);
			foreach (var tree in compilation.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath)))
			{
				var model = compilation.GetSemanticModel(tree);
				var treeMoved = compilation == portCompilation ? moved.Where(m => m.SyntaxTree == tree).ToList() : [];
				var edits = UseEdits(tree, model, userNspw, userMoved, treeMoved, [], report);
				if (compilation == portCompilation)
				{
					edits.AddRange(widen.SelectMany(w => w.DeclaringSyntaxReferences)
						.Where(r => r.SyntaxTree == tree)
						.Select(r => r.GetSyntax().AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First())
						.SelectMany(m => m.Modifiers.Where(t => t.IsKind(SyntaxKind.PrivateKeyword)))
						.Select(t => (t.Span, "internal"))
						.Distinct());
				}

				var isMovedFile = _files.Contains(Path.GetFullPath(tree.FilePath));
				var local = new List<string>();
				foreach (var declaration in treeMoved)
				{
					if (declaration is TypeDeclarationSyntax type)
					{
						if (type.ParameterList is not null)
						{
							report.Add($"{tree.FilePath}: the part with the primary constructor cannot move");
						}

						edits.Add((type.Identifier.Span, _className));
					}
					else
					{
						// The edits inside the member go with it.
						var span = declaration.FullSpan;
						var inner = edits.Where(e => span.Contains(e.Span)).ToList();
						edits.RemoveAll(inner.Contains);
						var member = Apply(tree.GetText().ToString(span), inner.Select(e => (new TextSpan(e.Span.Start - span.Start, e.Span.Length), e.Text))).TrimEnd() + "\n";
						(declaration is FieldDeclarationSyntax ? extra : local).Add(member);
						edits.Add((span, ""));
					}
				}

				if (edits.Count == 0)
				{
					continue;
				}

				var text = Apply(tree.GetText().ToString(), edits);
				if (local.Count > 0)
				{
					text = text.TrimEnd() + $"\n\npublic unsafe partial class {_className}\n{{\n" + string.Join("", local) + "}\n";
				}

				if (isMovedFile || local.Count > 0)
				{
					text = WithUsingNspw(text);
				}

				File.WriteAllText(tree.FilePath, text);
				written.Add(tree.FilePath);
			}
		}

		var header = $"{UsingNspw}\nnamespace OpenNspw;\n\n[Globals]\npublic sealed unsafe partial class {_className}(Nspw game)\n{{\n\tprivate readonly Nspw {Game} = game;\n";
		File.WriteAllText(_newFile, header + (extra.Count > 0 ? "\n" + string.Join("", extra) : "") + "}\n");
		written.Add(_newFile);
		return [.. report, .. written];
	}
}
