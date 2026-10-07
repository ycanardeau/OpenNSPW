using System.Text;
using System.Text.RegularExpressions;

namespace OpenNspw.Porter;

// The first stage of porting a C++ file: text rewrites that turn it into C# that reads like the original, with the
// conventions in OpenNSPW/README.md. Comments, strings and #if 0 sections are left as they are. What this stage does not
// know how to convert is left for the second stage (Fixer) and for porting by hand.
internal sealed partial class CppToCs(IReadOnlyDictionary<string, long> constants)
{
	private readonly IReadOnlyDictionary<string, long> _constants = constants;

	// The array lengths that the ported file uses, for InlineArrays.cs.
	public SortedSet<long> ArrayLengths { get; } = [];

	// C++ types and the C# types they become.
	private static readonly (string Cpp, string Cs)[] TypeMap =
	[
		("unsigned short", "ushort"),
		("unsigned char", "byte"),
		("unsigned int", "uint"),
		("unsigned long", "uint"),
		("unsigned", "uint"),
		("BOOL", "int"),
		("DWORD", "uint"),
		("WORD", "ushort"),
		("BYTE", "byte"),
		("UINT", "uint"),
		("LONG", "int"),
		("long", "int"),
		("HRESULT", "int"),
		("CHAR", "byte"),
		("TCHAR", "byte"),
		("char", "byte"),
		("WCHAR", "char"),
		("VOID", "void"),
		("FOURCC", "uint"),
		("DPNID", "uint"),
		("DPNHANDLE", "uint"),
		("COLORREF", "uint"),
		("WPARAM", "nint"),
		("LPARAM", "nint"),
		("LRESULT", "nint"),
		("INT_PTR", "nint"),
		("HPSTR", "byte*"),
		("LPSTR", "byte*"),
		("LPVOID", "void*"),
		("PVOID", "void*"),
		("HINSTANCE", "object?"),
		("HICON", "object?"),
		("HCURSOR", "object?"),
		("LPDIRECTDRAW7", "IDirectDraw7?"),
		("LPDIRECTDRAWSURFACE7", "IDirectDrawSurface7?"),
		("LPDIRECTDRAWCLIPPER", "IDirectDrawClipper?"),
		("LPDIRECTSOUND8", "IDirectSound8?"),
		("LPDIRECTSOUNDBUFFER", "IDirectSoundBuffer?"),
		("LPDIRECTINPUT8", "IDirectInput8?"),
		("LPDIRECTINPUTDEVICE8", "IDirectInputDevice8?"),
		("_GENERICMSG", "GENERICMSG"),
	];

	// Types that a declaration can start with, besides those of TypeMap.
	private static readonly string[] DeclarationTypes =
	[
		"int", "short", "double", "float", "bool", "void", "HWND", "HDC", "HFONT", "HBITMAP", "HBRUSH", "HGDIOBJ",
		"HANDLE", "HMMIO", "HKEY", "RECT", "POINT", "MSG", "WNDCLASS", "BITMAP", "DDSURFACEDESC2", "DDSCAPS2",
		"DDPIXELFORMAT", "DDCOLORKEY", "DDBLTFX", "DSBUFFERDESC", "WAVEFORMATEX", "PCMWAVEFORMAT", "MMCKINFO", "MMIOINFO",
		"DIDEVICEOBJECTDATA", "DIPROPDWORD", "DIMOUSESTATE2", "UNIT", "FIRE", "EFFECT", "KUMO", "SPRT", "NEW_PP",
		"NEW_SLCT", "NEW_MENU", "GENERICMSG", "UNIT_MSG", "_DP_DATA_1", "_DP_NEW_PP", "_DP_NEW_PP_SHIP",
		"_DP_NEW_PP_PLANE", "_DP_NEW_SLCT", "_DP_NEW_SLCT_SHIP", "_DP_NEW_SLCT_PLANE", "_DP_NEW_SLCT_LAND", "_DP_NEW_MENU",
		"_DP_FLAG", "_DP_DATA_20", "WIN32_FIND_DATA",
	];

	private static readonly HashSet<string> InterfaceTypes =
	[
		"IDirectDraw7?", "IDirectDrawSurface7?", "IDirectDrawClipper?", "IDirectSound8?", "IDirectSoundBuffer?",
		"IDirectInput8?", "IDirectInputDevice8?",
	];

	// Comments, strings, chars and #if 0 sections, replaced by placeholders while the code is rewritten.
	private readonly List<string> _masked = [];

	private string Mask(string text)
	{
		_masked.Add(text);
		return $"\u0001{_masked.Count - 1}\u0002";
	}

	private string Unmask(string text)
	{
		// Masked text can contain placeholders of its own (a comment inside an #if 0 section), so repeat.
		string previous;
		do
		{
			previous = text;
			text = PlaceholderRegex().Replace(text, m => _masked[int.Parse(m.Groups[1].Value)]);
		}
		while (text != previous);

		return text;
	}

	[GeneratedRegex("\u0001(\\d+)\u0002")]
	private static partial Regex PlaceholderRegex();

	// Masks comments, string and char literals, and preprocessor lines (marked so that they can be rewritten).
	private string MaskLexically(string source)
	{
		var output = new StringBuilder();
		var i = 0;
		var lineStart = true;
		while (i < source.Length)
		{
			var c = source[i];
			if (lineStart && c is ' ' or '\t')
			{
				output.Append(c);
				i++;
				continue;
			}

			if (lineStart && c == '#')
			{
				var end = source.IndexOf('\n', i);
				end = end < 0 ? source.Length : end;
				output.Append('\u0003').Append(Mask(source[i..end]));
				i = end;
				continue;
			}

			lineStart = c == '\n';
			if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
			{
				var end = source.IndexOf('\n', i);
				end = end < 0 ? source.Length : end;
				output.Append(Mask(source[i..end]));
				i = end;
			}
			else if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
			{
				var end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
				end = end < 0 ? source.Length : end + 2;
				output.Append(Mask(source[i..end]));
				i = end;
			}
			else if (c is '"' or '\'')
			{
				var j = i + 1;
				while (j < source.Length && source[j] != c)
				{
					j += source[j] == '\\' ? 2 : 1;
				}

				output.Append(Mask(source[i..Math.Min(j + 1, source.Length)]));
				i = j + 1;
			}
			else
			{
				output.Append(c);
				i++;
			}
		}

		return output.ToString();
	}

	[GeneratedRegex("^\\s*#\\s*(\\w+)(.*)$")]
	private static partial Regex DirectiveRegex();

	private static string RewriteDirective(string directive)
	{
		var m = DirectiveRegex().Match(directive);
		if (!m.Success)
		{
			return directive;
		}

		var name = m.Groups[1].Value;
		var rest = m.Groups[2].Value.Trim();
		rest = Regex.Replace(rest, @"\b(\w+)\s*==\s*0\b", "!$1");
		rest = Regex.Replace(rest, @"^0\b", "false");
		rest = Regex.Replace(rest, @"^1\b", "true");
		return name switch
		{
			"if" or "elif" => $"#{name} {rest}",
			"else" or "endif" => directive,
			_ => "//" + directive,
		};
	}

	// Masks the sections that #if 0 (and #elif 0) excludes, so that they stay as they are, and rewrites the directives.
	private string MaskDirectives(string text)
	{
		var lines = text.Split('\n');
		var output = new List<string>();
		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			var marker = line.IndexOf('\u0003');
			if (marker < 0)
			{
				output.Add(line);
				continue;
			}

			var directive = Unmask(line[(marker + 1)..]);
			var rewritten = line[..marker] + RewriteDirective(directive);
			output.Add(rewritten);
			if (!Regex.IsMatch(rewritten, @"#(if|elif) false\b"))
			{
				continue;
			}

			// Skip to the #else, #elif or #endif that ends this section, at the same nesting.
			var depth = 0;
			var skipped = new List<string>();
			var j = i + 1;
			for (; j < lines.Length; j++)
			{
				var inner = lines[j].IndexOf('\u0003') is var k and >= 0 ? Unmask(lines[j][(k + 1)..]).Trim() : "";
				if (inner.StartsWith("#if", StringComparison.Ordinal))
				{
					depth++;
				}
				else if (inner.StartsWith("#endif", StringComparison.Ordinal) && depth-- == 0)
				{
					break;
				}
				else if (depth == 0 && (inner.StartsWith("#else", StringComparison.Ordinal) || inner.StartsWith("#elif", StringComparison.Ordinal)))
				{
					break;
				}

				// C# checks the #if expressions of skipped sections too.
				skipped.Add(inner.StartsWith('#') ? lines[j][..lines[j].IndexOf('\u0003')] + RewriteDirective(inner) : Unmask(lines[j]));
			}

			if (skipped.Count > 0)
			{
				output.Add(Mask(string.Join('\n', skipped)));
			}

			i = j - 1;
		}

		return string.Join('\n', output);
	}

	private long Evaluate(string expression)
	{
		var tokens = Regex.Matches(expression, @"\w+|[-+*/()]").Select(m => m.Value).ToList();
		var position = 0;

		long Primary()
		{
			var token = tokens[position++];
			if (token == "(")
			{
				var value = Sum();
				position++;
				return value;
			}

			if (token == "-")
			{
				return -Primary();
			}

			return long.TryParse(token, out var number) ? number
				: _constants.TryGetValue(token, out var constant) ? constant
				: throw new InvalidOperationException($"Unknown constant {token} in {expression}.");
		}

		long Product()
		{
			var value = Primary();
			while (position < tokens.Count && tokens[position] is "*" or "/")
			{
				value = tokens[position++] == "*" ? value * Primary() : value / Primary();
			}

			return value;
		}

		long Sum()
		{
			var value = Product();
			while (position < tokens.Count && tokens[position] is "+" or "-")
			{
				value = tokens[position++] == "+" ? value + Product() : value - Product();
			}

			return value;
		}

		return Sum();
	}

	private static string MapType(string cppType)
	{
		var type = Regex.Replace(cppType.Trim(), @"\s+", " ");
		foreach (var (cpp, cs) in TypeMap)
		{
			type = Regex.Replace(type, $@"(?<![\w]){Regex.Escape(cpp)}(?![\w])", cs);
		}

		return type.Replace(" *", "*").Replace("* ", "*");
	}

	// An array declarator's type: ArrayN<...> for each dimension, outermost first.
	private string ArrayType(string elementType, IEnumerable<string> lengths)
	{
		var type = elementType;
		foreach (var length in lengths.Reverse())
		{
			var n = Evaluate(length);
			ArrayLengths.Add(n);
			type = $"Array{n}<{type}>";
		}

		return type;
	}

	[GeneratedRegex(@"^(?<indent>[ \t]*)(?<static>static[ \t]+)?(?<type>(?:const[ \t]+)?(?:unsigned[ \t]+(?:short|char|int|long)|unsigned|[A-Za-z_]\w*))(?<ptr>[ \t]*\*+)?[ \t]+(?<decls>[*\w][^;(){}=]*(?:=[^;{}]*)?);(?<tail>.*)$", RegexOptions.Multiline)]
	private static partial Regex DeclarationRegex();

	[GeneratedRegex(@"^(?<ptr>\*+)?\s*(?<name>[A-Za-z_]\w*)\s*(?<dims>(?:\[[^\]]*\]\s*)*)(?<init>=.*)?$", RegexOptions.Singleline)]
	private static partial Regex DeclaratorRegex();

	// A COM interface (IDirectPlay8Peer), whose pointers are references in C#.
	private static bool IsInterface(string csType)
	{
		return InterfaceTypes.Contains(csType) || System.Text.RegularExpressions.Regex.IsMatch(csType, @"^I[A-Z]\w+$");
	}

	// The C# type of a pointer to a C++ type: a reference for COM interfaces and GUIDs, an unsafe pointer otherwise.
	private static string PointerType(string csType, string pointer)
	{
		if (pointer.Length == 0)
		{
			return csType;
		}

		if (csType == "GUID")
		{
			return "Guid?";
		}

		return IsInterface(csType) ? csType.TrimEnd('?') + "?" : csType + pointer;
	}

	private static bool IsDeclarationType(string type)
	{
		var bare = Regex.Replace(type, @"^const\s+", "");
		return DeclarationTypes.Contains(bare) || TypeMap.Any(t => t.Cpp == bare) || bare.StartsWith("unsigned", StringComparison.Ordinal);
	}

	// Rewrites the declarations of a statement: arrays become ArrayN<T> (one statement each), pointer declarators get
	// their own statement (C# puts the * on the type), and declarations outside functions become public fields.
	private string RewriteDeclaration(Match m, bool global)
	{
		var type = m.Groups["type"].Value;
		if (!(global || IsDeclarationType(type))
			|| Regex.IsMatch(type, @"^(return|else|case|goto|delete|new|typedef|struct|extern)$")
			|| Regex.IsMatch(m.Groups["decls"].Value, @"\breturn\b"))
		{
			return m.Value;
		}

		var indent = m.Groups["indent"].Value;
		var csType = MapType(type);
		var basePointer = m.Groups["ptr"].Value.Trim();
		var declarators = SplitTopLevel(m.Groups["decls"].Value);
		var plain = new List<string>();
		var statements = new List<string>();
		var prefix = global ? "public " : "";
		foreach (var declarator in declarators)
		{
			var d = DeclaratorRegex().Match(declarator.Trim());
			if (!d.Success)
			{
				return m.Value;
			}

			var pointer = basePointer + d.Groups["ptr"].Value;
			var name = d.Groups["name"].Value;
			var init = d.Groups["init"].Value.Trim();
			var dims = Regex.Matches(d.Groups["dims"].Value, @"\[([^\]]*)\]").Select(x => x.Groups[1].Value).ToList();
			var elementType = PointerType(csType, pointer);
			if (dims.Count > 0)
			{
				var arrayInit = init.Length > 0 ? $" {init}" : global ? "" : " = default";
				statements.Add($"{prefix}{ArrayType(elementType, dims)} {name}{arrayInit};");
			}
			else if (pointer.Length > 0)
			{
				statements.Add($"{prefix}{elementType} {name}{(init.Length > 0 ? " " + init : "")};");
			}
			else
			{
				plain.Add(declarator.Trim());
			}
		}

		// A declaration of plain variables keeps its spacing, with the type renamed.
		if (statements.Count == 0)
		{
			var typeGroup = m.Groups["type"];
			return m.Value[..(typeGroup.Index - m.Index)].Insert(indent.Length, prefix) + csType + m.Value[(typeGroup.Index - m.Index + typeGroup.Length)..];
		}

		if (plain.Count > 0)
		{
			statements.Insert(0, $"{prefix}{csType}\t{string.Join(",", plain)};");
		}

		return indent + string.Join(" ", statements) + m.Groups["tail"].Value;
	}

	private static List<string> SplitTopLevel(string declarators)
	{
		var parts = new List<string>();
		var depth = 0;
		var start = 0;
		for (var i = 0; i < declarators.Length; i++)
		{
			switch (declarators[i])
			{
				case '(' or '[' or '{':
					depth++;
					break;
				case ')' or ']' or '}':
					depth--;
					break;
				case ',' when depth == 0:
					parts.Add(declarators[start..i]);
					start = i + 1;
					break;
			}
		}

		parts.Add(declarators[start..]);
		return parts;
	}

	private static string MapParameter(string parameter, bool callback, int index)
	{
		var p = parameter.Trim();
		if (p is "" or "void")
		{
			return "";
		}

		if (callback)
		{
			var name = Regex.Match(p, @"(\w+)\s*$").Groups[1].Value;
			return index switch
			{
				0 => $"HWND {name}",
				1 => $"uint {name}",
				_ => $"nint {name}",
			};
		}

		var m = Regex.Match(p, @"^(?<type>.*?)(?<ptr>[\s\*]*\*)?\s*(?<name>\w+)\s*(?<dims>(\[\s*\])*)$");
		var type = m.Groups["type"].Value.Trim();
		var pointer = m.Groups["ptr"].Value.Replace(" ", "").Replace("\t", "");
		var paramName = m.Groups["name"].Value;
		if (type is "char" or "CHAR" or "TCHAR" or "LPCTSTR" or "LPCSTR" or "LPSTR" && (pointer == "*" || type.StartsWith("LP", StringComparison.Ordinal)))
		{
			return $"string {paramName}";
		}

		var csType = MapType(type);
		return $"{PointerType(csType, pointer)} {paramName}";
	}

	[GeneratedRegex(@"^(?<ret>[A-Za-z_][\w \t\*]*?)[ \t\*]+(?<name>\w+)[ \t]*\((?<params>[^()]*)\)[ \t]*(?=\r?\n[ \t]*\{)", RegexOptions.Multiline)]
	private static partial Regex FunctionRegex();

	[GeneratedRegex(@"^(?<indent>[ \t]*)(?<prototype>[A-Za-z_][\w \t\*]*[ \t\*]\w+[ \t]*\([^;{}()]*\)[ \t]*;)", RegexOptions.Multiline)]
	private static partial Regex PrototypeRegex();

	private static string RewriteFunction(Match m)
	{
		var ret = m.Groups["ret"].Value;
		var callback = Regex.IsMatch(ret, @"\b(CALLBACK)\b");
		ret = Regex.Replace(ret, @"\b(CALLBACK|WINAPI|static)\b", "").Trim();
		var parameters = m.Groups["params"].Value.Split(',');
		var mapped = parameters.Select((p, i) => MapParameter(p, callback, i)).Where(p => p.Length > 0);
		var csRet = callback ? "nint" : MapType(ret);
		var pointer = Regex.Match(m.Value[..(m.Groups["name"].Index - m.Index)], @"\*+\s*$").Value.Trim();
		csRet = PointerType(csRet, pointer);

		var separator = m.Value[(m.Groups["ret"].Index + m.Groups["ret"].Length - m.Index)..(m.Groups["name"].Index - m.Index)].Replace("*", "");
		return $"public {csRet}{(separator.Length > 0 ? separator : " ")}{m.Groups["name"].Value}({string.Join(",", mapped)})";
	}

	// Splits the text into the parts outside functions (brace depth 0) and inside them.
	private static IEnumerable<(string Text, bool Global)> SplitByDepth(string text)
	{
		var depth = 0;
		var start = 0;
		for (var i = 0; i < text.Length; i++)
		{
			if (text[i] == '{')
			{
				if (depth++ == 0)
				{
					yield return (text[start..(i + 1)], true);
					start = i + 1;
				}
			}
			else if (text[i] == '}' && --depth == 0)
			{
				yield return (text[start..i], false);
				start = i;
			}
		}

		yield return (text[start..], true);
	}

	private static string RewriteCode(string code)
	{
		code = Regex.Replace(code, @"\(\s*(?:void|LPVOID|VOID)\s*\*\s*\*?\s*\)\s*&\s*", "out ");
		code = Regex.Replace(code, @"\bwhile\s*\(\s*1\s*\)", "while(true)");
		code = Regex.Replace(code, @"\(\s*(?:LPARAM|WPARAM)\s*\)\s*", "");
		code = Regex.Replace(code, @"->", ".");
		code = Regex.Replace(code, @"\bNULL\b", "null");
		foreach (var (cpp, cs) in TypeMap)
		{
			if (cpp.Contains(' ') || cs.EndsWith('?'))
			{
				continue;
			}

			code = Regex.Replace(code, $@"\(\s*{Regex.Escape(cpp)}\s*(\*?)\s*\)", $"({cs}$1)");
			code = Regex.Replace(code, $@"\bsizeof\(\s*{Regex.Escape(cpp)}\s*\)", $"sizeof({cs})");
		}

		return code;
	}

	// Ports the C++ source of `fileName` (e.g. "win_main.cpp").
	public string Port(string source, string fileName)
	{
		_masked.Clear();
		var text = source.TrimStart('﻿').Replace("\r\n", "\n");
		text = MaskLexically(text);
		text = MaskDirectives(text);

		var output = new StringBuilder();
		foreach (var (part, global) in SplitByDepth(text))
		{
			// Prototypes are commented out: C# needs none.
			var rewritten = global ? PrototypeRegex().Replace(part, "${indent}//${prototype}") : part;
			rewritten = global ? FunctionRegex().Replace(rewritten, RewriteFunction) : rewritten;
			rewritten = DeclarationRegex().Replace(rewritten, m => RewriteDeclaration(m, global));
			output.Append(RewriteCode(rewritten));
		}

		var ported = Unmask(output.ToString());

		// The class, after the #include lines.
		var includes = Regex.Matches(ported, @"^//#include.*$", RegexOptions.Multiline);
		var insertAt = includes.Count > 0 ? includes[^1].Index + includes[^1].Length : 0;
		var header = $"\n\n// Port of {fileName}.\n\nnamespace OpenNspw;\n\npublic unsafe partial class Nspw\n{{";
		return ported[..insertAt] + header + ported[insertAt..].TrimEnd() + "\n}\n";
	}
}
