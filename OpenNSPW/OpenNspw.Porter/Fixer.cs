using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// The second stage of porting: compiles the port and fixes the errors that have a mechanical answer, the same way every
// time (OpenNSPW/README.md, porting conventions), until none is left. The other errors are reported for porting by hand.
internal sealed partial class Fixer(string projectDirectory, IReadOnlySet<string> targets)
{
	private readonly string _projectDirectory = projectDirectory;
	private readonly IReadOnlySet<string> _targets = targets;

	private static readonly string[] ImplicitUsings =
	[
		"System", "System.IO", "System.Linq", "System.Collections.Generic", "System.Threading", "System.Threading.Tasks",
	];

	private string GlobalUsings()
	{
		var project = File.ReadAllText(Directory.GetFiles(_projectDirectory, "*.csproj").Single());
		var lines = ImplicitUsings.Select(u => $"global using {u};").ToList();
		foreach (Match m in Regex.Matches(project, @"<Using Include=""([^""]+)""( Static=""true"")?( Alias=""([^""]+)"")? />"))
		{
			lines.Add(m.Groups[4].Success
				? $"global using {m.Groups[4].Value} = {m.Groups[1].Value};"
				: m.Groups[2].Success ? $"global using static {m.Groups[1].Value};" : $"global using {m.Groups[1].Value};");
		}

		return string.Join('\n', lines);
	}

	private IEnumerable<string> Symbols()
	{
		var project = File.ReadAllText(Directory.GetFiles(_projectDirectory, "*.csproj").Single());
		var defines = Regex.Match(project, @"<DefineConstants>([^<]*)</DefineConstants>").Groups[1].Value;
		return defines.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0 && !s.StartsWith('$'));
	}

	private static readonly ImmutableArray<MetadataReference> References =
	[
		.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p)),
	];

	private CSharpCompilation Compile(IReadOnlyDictionary<string, string> files)
	{
		var options = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: Symbols());
		var trees = files.Select(f => CSharpSyntaxTree.ParseText(f.Value, options, f.Key)).ToList();
		trees.Add(CSharpSyntaxTree.ParseText(GlobalUsings(), options, "GlobalUsings.cs"));
		return CSharpCompilation.Create(
			"OpenNspw",
			trees,
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Annotations));
	}

	private static bool IsNumeric(ITypeSymbol? type)
	{
		return type?.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16
			or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64
			or SpecialType.System_UInt64 or SpecialType.System_Double or SpecialType.System_Single or SpecialType.System_Char
			or SpecialType.System_IntPtr or SpecialType.System_UIntPtr;
	}

	private static bool IsFloating(ITypeSymbol? type)
	{
		return type?.SpecialType is SpecialType.System_Double or SpecialType.System_Single;
	}

	private static bool IsBool(ITypeSymbol? type)
	{
		return type?.SpecialType == SpecialType.System_Boolean;
	}

	// Wraps an expression in parentheses unless it is a primary expression.
	private static string Wrap(ExpressionSyntax e)
	{
		return e is IdentifierNameSyntax or LiteralExpressionSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax
			or InvocationExpressionSyntax or ParenthesizedExpressionSyntax or MemberBindingExpressionSyntax
			? e.ToString()
			: $"({e})";
	}

	// An integer, pointer or reference used as a condition: x!=0 or x!=null, as in C.
	private static string AsCondition(ExpressionSyntax e, ITypeSymbol? type)
	{
		return type is IPointerTypeSymbol || type?.IsReferenceType == true ? $"{Wrap(e)}!=null" : $"{Wrap(e)}!=0";
	}

	// A conversion that C++ does implicitly: a cast, or a bool as 0 or 1. Out-of-range constants truncate, as in C++.
	private static string? Convert(ExpressionSyntax e, ITypeSymbol? from, ITypeSymbol? to, SemanticModel model)
	{
		if (to is null)
		{
			return null;
		}

		var target = to.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

		// A char array initialized with TEXT("...").
		if (from?.SpecialType == SpecialType.System_String && to.Name.StartsWith("Array", StringComparison.Ordinal) && e is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "TEXT" } } text)
		{
			return $"TEXT<{target}>{text.ArgumentList}";
		}

		if (IsBool(to) && from is not null && !IsBool(from))
		{
			return AsCondition(e, from);
		}

		if (IsBool(from) && IsNumeric(to))
		{
			return to.SpecialType == SpecialType.System_Int32 ? $"({e} ? 1 : 0)" : $"({target})({e} ? 1 : 0)";
		}

		if (IsNumeric(from) && IsNumeric(to) || from is IPointerTypeSymbol && to is IPointerTypeSymbol)
		{
			// double to short goes through int, as MSVC converts it.
			var cast = IsFloating(from) && !IsFloating(to) && to.SpecialType != SpecialType.System_Int32
				? $"({target})(int){Wrap(e)}"
				: $"({target}){Wrap(e)}";
			return model.GetConstantValue(e).HasValue ? $"unchecked({cast})" : cast;
		}

		return null;
	}

	private static string? DefaultFor(ITypeSymbol? type)
	{
		return IsNumeric(type) ? "0" : type?.IsValueType == true ? "default" : null;
	}

	// The fix for one error, as a replacement of a span, or null if it has no mechanical fix.
	private static (TextSpan Span, string Text)? Fix(Diagnostic d, SemanticModel model, SyntaxNode root)
	{
		var node = root.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true);
		var expression = node as ExpressionSyntax ?? (node as ArgumentSyntax)?.Expression;
		switch (d.Id)
		{
			// Cannot implicitly convert type.
			case "CS0029" or "CS0266" when expression is not null:
				{
					var info = model.GetTypeInfo(expression);

					// The target type, from the message: for a condition, ConvertedType is not bool.
					var to = Regex.Match(d.GetMessage(), "to '([^']+)'").Groups[1].Value switch
					{
						"bool" => model.Compilation.GetSpecialType(SpecialType.System_Boolean),
						_ => info.ConvertedType,
					};
					if (SymbolEqualityComparer.Default.Equals(to, info.Type))
					{
						return null;
					}

					var text = Convert(expression, info.Type, to, model);
					if (text is null && info.Type is null && expression is LiteralExpressionSyntax { Token.ValueText: "null" })
					{
						text = DefaultFor(info.ConvertedType);
					}

					return text is null ? null : (expression.Span, text);
				}

			// Operator cannot be applied to operand: ! on an integer, or . on a pointer.
			case "CS0023" when node is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } not:
				{
					var type = model.GetTypeInfo(not.Operand).Type;
					var test = type is IPointerTypeSymbol || type?.IsReferenceType == true ? "==null" : "==0";
					return (not.Span, $"{Wrap(not.Operand)}{test}");
				}

			case "CS0023" or "CS1061" when node.FirstAncestorOrSelf<MemberAccessExpressionSyntax>() is { } access
				&& model.GetTypeInfo(access.Expression).Type is IPointerTypeSymbol:
				return (access.OperatorToken.Span, "->");

			// Operator cannot be applied to operands: && or || with an integer.
			case "CS0019" when node is BinaryExpressionSyntax binary
				&& binary.Kind() is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression:
				{
					var left = model.GetTypeInfo(binary.Left).Type;
					var right = model.GetTypeInfo(binary.Right).Type;
					var leftText = IsBool(left) ? binary.Left.ToString() : AsCondition(binary.Left, left);
					var rightText = IsBool(right) ? binary.Right.ToString() : AsCondition(binary.Right, right);
					return (binary.Span, $"{leftText} {binary.OperatorToken.Text} {rightText}");
				}

			// Cannot convert null to a value type.
			case "CS0037" when expression is not null:
				{
					var text = DefaultFor(model.GetTypeInfo(expression).ConvertedType);
					return text is null ? null : (expression.Span, text);
				}

			// Constant value cannot be converted.
			case "CS0031" or "CS0221" when expression is not null:
				{
					var info = model.GetTypeInfo(expression);
					var target = info.ConvertedType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
					return node.Parent is CastExpressionSyntax cast
						? (cast.Span, $"unchecked({cast})")
						: target is null ? null : (expression.Span, $"unchecked(({target}){Wrap(expression)})");
				}

			// The address of a field: passed by ref instead.
			case "CS0212" when node.FirstAncestorOrSelf<PrefixUnaryExpressionSyntax>() is { RawKind: (int)SyntaxKind.AddressOfExpression } address
				&& address.Parent is ArgumentSyntax:
				return (address.Span, $"ref {address.Operand}");

			// Argument must be passed with ref or out.
			case "CS1620" when node.FirstAncestorOrSelf<ArgumentSyntax>() is { } argument:
				{
					var keyword = Regex.Match(d.GetMessage(), "'(ref|out|in)'").Groups[1].Value;
					var value = argument.Expression is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.AddressOfExpression } a ? a.Operand : argument.Expression;
					return keyword.Length == 0 ? null : (argument.Span, $"{keyword} {value}");
				}

			// Argument cannot convert.
			case "CS1503" when node.FirstAncestorOrSelf<ArgumentSyntax>() is { RefKindKeyword.RawKind: 0 } argument:
				{
					var parameter = (model.GetSymbolInfo(argument.Parent!.Parent!).CandidateSymbols.FirstOrDefault() as IMethodSymbol)?.Parameters
						.ElementAtOrDefault(((ArgumentListSyntax)argument.Parent).Arguments.IndexOf(argument));
					var from = model.GetTypeInfo(argument.Expression).Type;
					if (argument.Expression is LiteralExpressionSyntax { Token.ValueText: "null" })
					{
						var text = DefaultFor(parameter?.Type);
						return text is null ? null : (argument.Expression.Span, text);
					}

					var converted = Convert(argument.Expression, from, parameter?.Type, model);
					return converted is null ? null : (argument.Expression.Span, converted);
				}

			// Use of an unassigned local: C++ leaves it uninitialized (C4700, C4701); zero here.
			case "CS0165" when node is IdentifierNameSyntax name
				&& model.GetSymbolInfo(name).Symbol is ILocalSymbol local
				&& local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is VariableDeclaratorSyntax { Initializer: null } declarator
				&& declarator.SyntaxTree == root.SyntaxTree:
				{
					var marker = local.Type.IsValueType && local.Type.TypeKind != TypeKind.Struct || local.Type.SpecialType != SpecialType.None ? " /* C4701 */" : "";
					return (new TextSpan(declarator.Identifier.Span.End, 0), $"=default{marker}");
				}

			// A variable used like a type: sizeof(variable), which C# does not allow.
			case "CS0118" or "CS0246" when node.FirstAncestorOrSelf<SizeOfExpressionSyntax>() is { } size:
				{
					var symbol = model.LookupSymbols(size.SpanStart, name: size.Type.ToString()).FirstOrDefault();
					var type = symbol switch
					{
						ILocalSymbol l => l.Type,
						IFieldSymbol f => f.Type,
						IParameterSymbol p => p.Type,
						_ => null,
					};
					return type is null ? null : (size.Type.Span, type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
				}

			default:
				return null;
		}
	}

	private static string Apply(string text, IEnumerable<(TextSpan Span, string Text)> fixes)
	{
		var applied = new List<(TextSpan Span, string Text)>();
		foreach (var fix in fixes.OrderBy(f => f.Span.Start).ThenByDescending(f => f.Span.Length))
		{
			if (applied.Count == 0 || fix.Span.Start >= applied[^1].Span.End && fix.Span.Start != applied[^1].Span.Start)
			{
				applied.Add(fix);
			}
		}

		foreach (var (span, replacement) in applied.AsEnumerable().Reverse())
		{
			text = text[..span.Start] + replacement + text[span.End..];
		}

		return text;
	}

	// Fixes the targets until no error has a mechanical fix. Returns the errors left.
	public IReadOnlyList<Diagnostic> Run(int maxRounds = 60)
	{
		var files = Directory.GetFiles(_projectDirectory, "*.cs", SearchOption.AllDirectories)
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
			.ToDictionary(f => f, File.ReadAllText);
		IReadOnlyList<Diagnostic> errors = [];
		for (var round = 0; round < maxRounds; round++)
		{
			var compilation = Compile(files);
			errors = [.. compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];
			var changed = false;
			foreach (var tree in compilation.SyntaxTrees.Where(t => _targets.Contains(Path.GetFileName(t.FilePath))))
			{
				var model = compilation.GetSemanticModel(tree);
				var root = tree.GetRoot();
				var fixes = errors.Where(d => d.Location.SourceTree == tree)
					.Select(d => Fix(d, model, root))
					.OfType<(TextSpan Span, string Text)>()
					.ToList();
				var fixedText = Apply(files[tree.FilePath], fixes);
				if (fixedText != files[tree.FilePath])
				{
					files[tree.FilePath] = fixedText;
					changed = true;
				}
			}

			Console.WriteLine($"Round {round + 1}: {errors.Count} errors.");
			if (!changed)
			{
				break;
			}
		}

		foreach (var (path, text) in files.Where(f => _targets.Contains(Path.GetFileName(f.Key))))
		{
			File.WriteAllText(path, text);
		}

		return errors;
	}
}
