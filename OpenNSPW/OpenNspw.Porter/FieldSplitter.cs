using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OpenNspw.Porter;

// Puts each field of a project on a line of its own: a declaration of several fields (`public double drctn,drctn_add;`)
// becomes one declaration per field, and declarations that share a line go on lines of their own. The comment at the
// end of the line stays on the first line. The fields keep their order, so the layout of the structs is unchanged.
// Fields in inactive #if blocks are left as they are.
internal sealed class FieldSplitter(CSharpProject project)
{
	private readonly CSharpProject _project = project;

	private static string Indentation(SourceText text, int position)
	{
		var line = text.Lines.GetLineFromPosition(position);
		var start = line.Start;
		var end = start;
		while (end < line.End && text[end] is ' ' or '\t')
		{
			end++;
		}

		return text.ToString(TextSpan.FromBounds(start, end));
	}

	// The comment after a declaration on its last line, if any.
	private static SyntaxTrivia? EndOfLineComment(FieldDeclarationSyntax field)
	{
		foreach (var trivia in field.GetTrailingTrivia())
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

	private static List<(TextSpan Span, string Text)> Edits(SyntaxTree tree)
	{
		var text = tree.GetText();
		var edits = new List<(TextSpan Span, string Text)>();
		foreach (var field in tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>())
		{
			var indentation = Indentation(text, field.SpanStart);

			// A declaration that follows another member on the same line goes on a line of its own.
			var previous = field.GetFirstToken().GetPreviousToken();
			if (previous.IsKind(SyntaxKind.SemicolonToken) || previous.IsKind(SyntaxKind.CloseBraceToken))
			{
				var between = TextSpan.FromBounds(previous.Span.End, field.SpanStart);
				if (text.ToString(between).All(c => c is ' ' or '\t'))
				{
					edits.Add((between, $"\n{indentation}"));
				}
			}

			var variables = field.Declaration.Variables;
			if (variables.Count < 2)
			{
				continue;
			}

			var prefix = text.ToString(TextSpan.FromBounds(field.SpanStart, variables[0].SpanStart));
			var comment = EndOfLineComment(field);
			var commentText = comment is { } c ? text.ToString(TextSpan.FromBounds(field.Span.End, c.Span.End)) : "";
			var declarations = variables.Select((v, i) => $"{prefix}{v};{(i == 0 ? commentText : "")}");
			var end = comment?.Span.End ?? field.Span.End;
			edits.Add((TextSpan.FromBounds(field.SpanStart, end), string.Join($"\n{indentation}", declarations)));
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

	// Splits the fields of the project's files, and writes the files that changed. Returns their paths.
	public IReadOnlyList<string> Run()
	{
		var written = new List<string>();
		foreach (var (path, text) in _project.ReadFiles())
		{
			var edits = Edits(CSharpSyntaxTree.ParseText(text, _project.ParseOptions(), path));
			if (edits.Count > 0)
			{
				File.WriteAllText(path, Apply(text, edits));
				written.Add(path);
			}
		}

		return written;
	}
}
