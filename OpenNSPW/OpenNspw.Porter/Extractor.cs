using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Extracts statements of a method into a new method (docs/Refactoring.md, Functions), so that a long function becomes
// smaller ones that do the same:
//
// - The statements are the ones of a block or a switch section that lie between two lines, and must follow each other.
// - A local of the method that the statements use becomes a parameter: by ref if the statements write it and it is used
//   outside them (Roslyn's flow analysis counts a value kept from one turn of a loop to the next), by value if they only
//   read it, and by ref if it is a struct other than a number, to avoid the copy. A ref local is passed by ref. A local
//   that only the statements use, and whose value does not flow into them, moves into the new method.
// - A jump out of the statements (return; in a void method, a return of a constant, continue; or break; of a loop around
//   them), all of one kind, makes the new method return false where it jumped and true at its end, and the call jump in
//   the same way when it returns false. Other jumps (goto, other returns, jumps of two kinds) are not extracted.
// - A local declared in the statements and used after them is not extracted.
// - Declarators keep their comments, such as the port's /* C4701 */ markers.
// - #if and #endif directives next to the statements move with them, as far as they need to balance.
// - A local passed by ref that is not definitely assigned before the statements gets `= default`, which changes no value:
//   the project zeroes its locals. The address of a local passed by ref, &x, becomes Unsafe.AsPointer(ref x), the same
//   address, cast back to x's type, since x is a local of the caller, which does not move.
//
// The new method is private and goes before the method it came from, and its comments.
internal sealed class Extractor(CSharpProject project, string file, int firstLine, int lastLine, string name)
{
	private readonly CSharpProject _project = project;
	private readonly string _file = file;
	private readonly int _firstLine = firstLine;
	private readonly int _lastLine = lastLine;
	private readonly string _name = name;

	// The lines of a span, with their line breaks, if nothing else is on them; otherwise the span itself.
	private static TextSpan WholeLines(SourceText text, TextSpan span)
	{
		var firstLine = text.Lines.GetLineFromPosition(span.Start);
		var lastLine = text.Lines.GetLineFromPosition(span.End);
		var before = text.ToString(TextSpan.FromBounds(firstLine.Start, span.Start));
		var after = text.ToString(TextSpan.FromBounds(span.End, lastLine.End));
		return string.IsNullOrWhiteSpace(before) && string.IsNullOrWhiteSpace(after)
			? TextSpan.FromBounds(firstLine.Start, lastLine.EndIncludingLineBreak)
			: span;
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

	private static SyntaxTrivia? EndOfLineComment(StatementSyntax statement)
	{
		foreach (var trivia in statement.GetTrailingTrivia())
		{
			if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
			{
				return null;
			}

			if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
			{
				return trivia;
			}
		}

		return null;
	}

	private static bool IsNumber(ITypeSymbol type)
	{
		return type.TypeKind == TypeKind.Enum || type.SpecialType is >= SpecialType.System_Boolean and <= SpecialType.System_Double;
	}

	// The statements of a block or of a switch section.
	private static SyntaxList<StatementSyntax>? StatementsOf(SyntaxNode node)
	{
		return node switch
		{
			BlockSyntax block => block.Statements,
			SwitchSectionSyntax section => section.Statements,
			_ => null,
		};
	}

	private (SyntaxNode Block, List<StatementSyntax> Statements) FindStatements(SyntaxTree tree)
	{
		var text = tree.GetText();
		var start = text.Lines[_firstLine - 1].Start;
		var end = text.Lines[_lastLine - 1].End;
		var region = TextSpan.FromBounds(start, end);
		var candidates = tree.GetRoot().DescendantNodes().Where(n => StatementsOf(n) is not null)
			.Select(b => (Block: b, Statements: StatementsOf(b)!.Value.Where(s => region.Contains(s.Span)).ToList()))
			.Where(c => c.Statements.Count > 0)
			.ToList();
		// The innermost block or switch section whose statements cover the lines.
		var best = candidates.OrderByDescending(c => c.Statements.Sum(s => s.Span.Length)).ThenByDescending(c => c.Block.SpanStart).First();
		var all = StatementsOf(best.Block)!.Value;
		var indexes = best.Statements.Select(s => all.IndexOf(s)).ToList();
		if (indexes.Zip(indexes.Skip(1)).Any(p => p.Second != p.First + 1))
		{
			throw new InvalidOperationException("The statements do not follow each other.");
		}

		return best;
	}

	public void Run()
	{
		var files = _project.ReadFiles();
		var path = files.Keys.Single(f => Path.GetFileName(f) == _file);
		var compilation = _project.Compile(files, CSharpProject.FrameworkReferences);
		var tree = compilation.SyntaxTrees.Single(t => t.FilePath == path);
		var model = compilation.GetSemanticModel(tree);
		var text = tree.GetText();
		var (block, statements) = FindStatements(tree);
		var first = statements[0];
		var last = statements[^1];
		var method = first.Ancestors().OfType<MethodDeclarationSyntax>().First();

		var control = model.AnalyzeControlFlow(first, last)!;
		var jumps = control.ExitPoints.ToList();
		// A return of a value is a jump of its own kind if every return returns the same constant (return false; of a
		// method extracted before); the call then returns it too.
		if (jumps.Any(j => j is GotoStatementSyntax || j is ReturnStatementSyntax { Expression: { } e } && !model.GetConstantValue(e).HasValue))
		{
			throw new InvalidOperationException("The statements jump out with goto or return a value that is not a constant.");
		}

		var kinds = jumps.Select(j => j is ReturnStatementSyntax r ? r.ToString() : j.Kind().ToString()).Distinct().ToList();
		if (kinds.Count > 1)
		{
			throw new InvalidOperationException("The statements jump out in more than one way.");
		}

		var flow = model.AnalyzeDataFlow(first, last)!;
		var declaredInside = flow.VariablesDeclared.ToHashSet(SymbolEqualityComparer.Default);
		if (declaredInside.Any(v => flow.ReadOutside.Contains(v) || flow.WrittenOutside.Contains(v)))
		{
			throw new InvalidOperationException("A local declared in the statements is used after them.");
		}

		var used = flow.ReadInside.Concat(flow.WrittenInside)
			.Where(v => (v is ILocalSymbol || v is IParameterSymbol { IsThis: false }) && !declaredInside.Contains(v))
			.Distinct(SymbolEqualityComparer.Default)
			.ToList();
		// In the order the statements first use them.
		var region = TextSpan.FromBounds(first.SpanStart, last.Span.End);
		var firstUse = new Dictionary<ISymbol, int>(SymbolEqualityComparer.Default);
		foreach (var identifier in tree.GetRoot().DescendantNodes(region).OfType<IdentifierNameSyntax>().Where(i => region.Contains(i.Span)))
		{
			if (model.GetSymbolInfo(identifier).Symbol is { } symbol && used.Contains(symbol, SymbolEqualityComparer.Default) && !firstUse.ContainsKey(symbol))
			{
				firstUse[symbol] = identifier.SpanStart;
			}
		}

		var assignedOnEntry = flow.DefinitelyAssignedOnEntry.ToHashSet(SymbolEqualityComparer.Default);
		var initialize = new List<VariableDeclaratorSyntax>();
		var byRefStructs = new Dictionary<string, string>();
		// Roslyn's flow analysis does not count &x as reading x, so a value that reaches a later turn of a loop through
		// &x is not seen to flow in. Inside a loop, a local whose address the statements take is therefore passed by ref
		// rather than moved. Outside a loop, the statements run at most once per call, and a moved local starts at zero
		// as it did.
		var inLoop = first.Ancestors().TakeWhile(a => a != method).Any(a => a is ForStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForEachStatementSyntax)
			|| method.DescendantNodes().OfType<GotoStatementSyntax>().Any();
		var addressTaken = tree.GetRoot().DescendantNodes(region).OfType<PrefixUnaryExpressionSyntax>()
			.Where(p => p.IsKind(SyntaxKind.AddressOfExpression) && region.Contains(p.Span) && p.Operand is IdentifierNameSyntax)
			.Select(p => model.GetSymbolInfo(p.Operand).Symbol)
			.OfType<ISymbol>()
			.ToHashSet(SymbolEqualityComparer.Default);
		var parameters = new List<string>();
		var arguments = new List<string>();
		var moved = new List<string>();
		foreach (var variable in used.OrderBy(v => firstUse.GetValueOrDefault(v, int.MaxValue)))
		{
			var type = variable is ILocalSymbol local ? local.Type : ((IParameterSymbol)variable).Type;
			var isRef = variable is ILocalSymbol { IsRef: true } || variable is IParameterSymbol { RefKind: not RefKind.None };
			var outside = flow.ReadOutside.Contains(variable) || flow.WrittenOutside.Contains(variable) || flow.DataFlowsIn.Contains(variable);
			var display = type.ToMinimalDisplayString(model, method.SpanStart);
			if (!outside && variable is ILocalSymbol { IsRef: false } && !flow.DataFlowsIn.Contains(variable) && !(inLoop && addressTaken.Contains(variable)))
			{
				// Only the statements use it: it moves into the new method, with its initializer if it has one.
				var declarator = (VariableDeclaratorSyntax)variable.DeclaringSyntaxReferences.Single().GetSyntax();
				moved.Add($"{display} {declarator.ToFullString().Trim()};");
				continue;
			}

			// A local that the statements write but whose value neither flows into them nor out of them is a local of
			// the new method too, even if the method uses it elsewhere: no value passes between the two. Not if its
			// address is taken, which the flow analysis does not see.
			if (variable is ILocalSymbol { IsRef: false } && flow.WrittenInside.Contains(variable) && !flow.DataFlowsIn.Contains(variable)
				&& !flow.DataFlowsOut.Contains(variable) && !addressTaken.Contains(variable))
			{
				var declarator = (VariableDeclaratorSyntax)variable.DeclaringSyntaxReferences.Single().GetSyntax();
				moved.Add($"{display} {declarator.Identifier.ValueText}{(declarator.Initializer is { } initializer ? $" {initializer.ToFullString().Trim()}" : "")};");
				continue;
			}

			var byRef = isRef || (flow.WrittenInside.Contains(variable) && outside) || (type.IsValueType && !IsNumber(type));
			if (byRef && variable is ILocalSymbol { IsRef: false } && !assignedOnEntry.Contains(variable)
				&& variable.DeclaringSyntaxReferences.Single().GetSyntax() is VariableDeclaratorSyntax { Initializer: null } uninitialized)
			{
				initialize.Add(uninitialized);
			}

			if (byRef && type.IsValueType)
			{
				byRefStructs[variable.Name] = display;
			}

			parameters.Add($"{(byRef ? "ref " : "")}{display} {variable.Name}");
			arguments.Add($"{(byRef ? "ref " : "")}{variable.Name}");
		}

		// The text that moves: the lines of the statements, widened so that the #if and #endif directives in it balance,
		// since a directive next to the statements belongs to the trivia of the tokens around them.
		var regionStart = text.Lines.GetLineFromPosition(first.SpanStart).Start;
		var regionEnd = last.FullSpan.End;
		var directives = tree.GetRoot().DescendantTrivia(descendIntoTrivia: true)
			.Where(t => t.IsKind(SyntaxKind.IfDirectiveTrivia) || t.IsKind(SyntaxKind.EndIfDirectiveTrivia))
			.OrderBy(t => t.SpanStart)
			.ToList();
		int Depth() => directives.Where(d => d.SpanStart >= regionStart && d.SpanStart < regionEnd).Sum(d => d.IsKind(SyntaxKind.IfDirectiveTrivia) ? 1 : -1);
		while (Depth() is var depth && depth != 0)
		{
			if (depth > 0)
			{
				regionEnd = text.Lines.GetLineFromPosition(directives.First(d => d.SpanStart >= regionEnd && d.IsKind(SyntaxKind.EndIfDirectiveTrivia)).SpanStart).EndIncludingLineBreak;
			}
			else
			{
				regionStart = text.Lines.GetLineFromPosition(directives.Last(d => d.SpanStart < regionStart && d.IsKind(SyntaxKind.IfDirectiveTrivia)).SpanStart).Start;
			}
		}

		// Only directives and inactive code may come along, not other statements.
		if (tree.GetRoot().DescendantTokens().Any(t => !region.Contains(t.Span) && TextSpan.FromBounds(regionStart, regionEnd).Contains(t.Span)))
		{
			throw new InvalidOperationException($"The #if and #endif directives around lines {_firstLine}-{_lastLine} enclose other statements.");
		}

		var widened = regionStart != text.Lines.GetLineFromPosition(first.SpanStart).Start || regionEnd != last.FullSpan.End;

		// The body, with the jumps out turned into return false.
		var bodyText = new StringBuilder(text.ToString(TextSpan.FromBounds(regionStart, regionEnd)));
		var bodyStart = regionStart;
		var bodyEdits = jumps.Select(j => (j.Span, "return false;"))
			.Concat(tree.GetRoot().DescendantNodes(region).OfType<PrefixUnaryExpressionSyntax>()
				.Where(p => p.IsKind(SyntaxKind.AddressOfExpression) && region.Contains(p.Span) && p.Operand is IdentifierNameSyntax { Identifier.ValueText: var operand } && byRefStructs.ContainsKey(operand))
				.Select(p => (p.Span, $"({byRefStructs[p.Operand.ToString()]}*)Unsafe.AsPointer(ref {p.Operand})")));
		foreach (var (span, replacement) in bodyEdits.OrderByDescending(e => e.Span.Start))
		{
			bodyText.Remove(span.Start - bodyStart, span.Length).Insert(span.Start - bodyStart, replacement);
		}

		var returnsBool = jumps.Count > 0;
		var methodIndentation = Indentation(text, method.SpanStart);
		var statementIndentation = Indentation(text, first.SpanStart);
		// The new method follows the previous member after a blank line, and keeps whatever was before the method.
		var newMethod = new StringBuilder("\n");
		newMethod.Append($"{methodIndentation}private {(returnsBool ? "bool" : "void")} {_name}({string.Join(", ", parameters)})\n");
		newMethod.Append($"{methodIndentation}\t{{\n");
		foreach (var declaration in moved)
		{
			newMethod.Append($"{methodIndentation}\t{declaration}\n");
		}

		// The statements keep their lines; their indentation becomes the new method's.
		foreach (var line in bodyText.ToString().TrimEnd().Split('\n'))
		{
			newMethod.Append(line.StartsWith(statementIndentation) ? $"{methodIndentation}\t{line[statementIndentation.Length..]}\n" : $"{line}\n");
		}

		if (returnsBool)
		{
			newMethod.Append($"{methodIndentation}\treturn true;\n");
		}

		newMethod.Append($"{methodIndentation}\t}}\n");

		var call = $"{_name}({string.Join(", ", arguments)})";
		var jumpText = jumps.FirstOrDefault()?.ToString();
		var callText = returnsBool ? $"if( !{call} )\n{statementIndentation}\t{jumpText}" : $"{call};";

		// Edits, from the end: the call replaces the statements, the moved locals leave the method's declarations, and
		// the new method goes before the method.
		var source = text.ToString();
		var edits = new List<(TextSpan Span, string Text)>
		{
			// The comment at the end of the last statement's line goes into the new method with it.
			widened
				? (TextSpan.FromBounds(regionStart, regionEnd), $"{statementIndentation}{callText}\n")
				: (TextSpan.FromBounds(first.SpanStart, EndOfLineComment(last)?.Span.End ?? last.Span.End), callText),
			(new TextSpan(method.FullSpan.Start, 0), newMethod.ToString()),
		};
		// The moved locals leave their declarations: each declaration is written again with the declarators it keeps,
		// or removed if it keeps none.
		var movedDeclarators = used
			.Where(v => v is ILocalSymbol { IsRef: false } && !flow.ReadOutside.Contains(v) && !flow.WrittenOutside.Contains(v) && !flow.DataFlowsIn.Contains(v) && !(inLoop && addressTaken.Contains(v)))
			.Select(v => (VariableDeclaratorSyntax)v.DeclaringSyntaxReferences.Single().GetSyntax())
			.ToHashSet();
		var rewritten = movedDeclarators.Select(d => (VariableDeclarationSyntax)d.Parent!).Distinct().ToList();
		foreach (var declaration in rewritten)
		{
			var kept = declaration.Variables.Where(v => !movedDeclarators.Contains(v)).ToList();
			edits.Add(kept.Count == 0
				? (WholeLines(text, declaration.Parent!.Span), "")
				: (TextSpan.FromBounds(declaration.Variables[0].SpanStart, declaration.Variables[^1].Span.End),
					string.Join(",", kept.Select(v => initialize.Contains(v) ? $"{v} = default" : v.ToFullString().Trim()))));
		}

		edits.AddRange(initialize.Where(d => !rewritten.Contains(d.Parent)).Select(d => (new TextSpan(d.Span.End, 0), " = default")));
		if (bodyText.ToString().Contains("Unsafe.AsPointer") && !source.Contains("using System.Runtime.CompilerServices;"))
		{
			var firstMember = tree.GetCompilationUnitRoot().Members.First();
			edits.Add((new TextSpan(firstMember.FullSpan.Start, 0), "using System.Runtime.CompilerServices;\n\n"));
		}

		foreach (var (span, replacement) in edits.OrderByDescending(e => e.Span.Start))
		{
			source = source[..span.Start] + replacement + source[span.End..];
		}

		File.WriteAllText(path, source);
	}
}
