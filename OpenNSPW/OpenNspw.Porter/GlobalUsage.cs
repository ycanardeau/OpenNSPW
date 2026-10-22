using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace OpenNspw.Porter;

// Reports which members read and write each global, the instance fields of Nspw, and which of its methods call which,
// for choosing the classes that the partial class is split into (docs/Refactoring.md, Splitting the partial class).
// Tab-separated lines:
//
//   global <name> <type> <file>
//   use <global> <file> <type> <member> <reads> <writes>
//   call <file> <caller> <callee>
//
// A use counts as a write when it assigns the global or any part of it (`Units[n].Speed=0`, `Tick++`), passes it or a
// part by ref or out, takes its address or a ref to it, or calls a method that may change it (a non-readonly method of
// a struct); `ref` and the increments count as a read and a write.
internal sealed class GlobalUsage(CSharpProject project)
{
	private readonly CSharpProject _project = project;

	// The operation that a use of a location ends in: the outermost part of it that is still a location, such as
	// `Units[n].Speed` for `Units`.
	private static IOperation Location(IOperation operation)
	{
		var current = operation;
		while (current.Parent is { } parent && parent switch
		{
			IFieldReferenceOperation f => f.Instance == current,
			IArrayElementReferenceOperation a => a.ArrayReference == current,
			IInlineArrayAccessOperation a => a.Instance == current,
			IPropertyReferenceOperation p => p.Instance == current && p.Property.ReturnsByRef,
			IParenthesizedOperation or IConversionOperation { Conversion.IsIdentity: true } => true,
			_ => false,
		})
		{
			current = parent;
		}

		return current;
	}

	private static bool IsRefReturning(IOperation operation)
	{
		return operation.Syntax.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.ReturnType is RefTypeSyntax;
	}

	// The operations of a member's body that refer to a symbol, a ref local or parameter, or `this`.
	private static IEnumerable<IOperation> References(ISymbol symbol, Compilation compilation)
	{
		var member = symbol is IMethodSymbol ? symbol : symbol.ContainingSymbol;
		foreach (var reference in member.DeclaringSyntaxReferences)
		{
			var model = compilation.GetSemanticModel(reference.SyntaxTree);
			foreach (var operation in reference.GetSyntax().DescendantNodes().Select(n => model.GetOperation(n)).OfType<IOperation>())
			{
				switch (operation)
				{
					case ILocalReferenceOperation l when SymbolEqualityComparer.Default.Equals(l.Local, symbol):
					case IParameterReferenceOperation p when SymbolEqualityComparer.Default.Equals(p.Parameter, symbol):
					case IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } when symbol is IMethodSymbol:
						yield return operation;
						break;
				}
			}
		}
	}

	// Whether a member writes through a ref local or ref parameter, or a struct method through `this`, looking at most
	// a few calls deep; deeper, it counts as a write.
	private static bool WritesThrough(ISymbol symbol, Compilation compilation, int depth)
	{
		return depth == 0 || symbol.DeclaringSyntaxReferences.Length == 0
			|| References(symbol, compilation).Any(r => Access(Location(r), compilation, depth - 1).Writes > 0);
	}

	private static (int Reads, int Writes) Access(IOperation location, Compilation compilation, int depth = 4)
	{
		static (int, int) Through(bool writes)
		{
			return writes ? (1, 1) : (1, 0);
		}

		return location.Parent switch
		{
			ISimpleAssignmentOperation { IsRef: false } a when a.Target == location => (0, 1),
			ISimpleAssignmentOperation { IsRef: true } a when a.Value == location => (1, 1),
			ICompoundAssignmentOperation a when a.Target == location => (1, 1),
			ICoalesceAssignmentOperation a when a.Target == location => (1, 1),
			IIncrementOrDecrementOperation => (1, 1),
			IArgumentOperation { Parameter.RefKind: RefKind.Out } => (0, 1),
			IArgumentOperation { Parameter: { RefKind: RefKind.Ref } parameter } => Through(WritesThrough(parameter.OriginalDefinition, compilation, depth)),
			IAddressOfOperation => (1, 1),
			IVariableInitializerOperation { Parent: IVariableDeclaratorOperation { Symbol: { RefKind: RefKind.Ref } local } } => Through(WritesThrough(local, compilation, depth)),
			IReturnOperation when IsRefReturning(location.Parent) => (1, 1),
			IInvocationOperation i when i.Instance == location && i.TargetMethod is { IsReadOnly: false, ContainingType.IsValueType: true } method =>
				Through(WritesThrough(method.OriginalDefinition, compilation, depth)),
			IPropertyReferenceOperation p when p.Instance == location && p.Property is { ContainingType.IsValueType: true, GetMethod: { IsReadOnly: false } getter } =>
				Through(WritesThrough(getter.OriginalDefinition, compilation, depth)),
			ITupleOperation { Parent: IDeconstructionAssignmentOperation d } t when d.Target == t => (0, 1),
			_ => (1, 0),
		};
	}

	private static string MemberName(SyntaxNode node)
	{
		return node.AncestorsAndSelf().FirstOrDefault(n => n is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or IndexerDeclarationSyntax or FieldDeclarationSyntax) switch
		{
			MethodDeclarationSyntax m => m.Identifier.ValueText,
			ConstructorDeclarationSyntax c => ".ctor",
			OperatorDeclarationSyntax o => "operator " + o.OperatorToken.ValueText,
			ConversionOperatorDeclarationSyntax => "operator conversion",
			PropertyDeclarationSyntax p => p.Identifier.ValueText,
			IndexerDeclarationSyntax => "this[]",
			FieldDeclarationSyntax f => "<init " + f.Declaration.Variables[0].Identifier.ValueText + ">",
			_ => "?",
		};
	}

	public IReadOnlyList<string> Run()
	{
		var files = _project.ReadFiles();
		var compilation = _project.Compile(files, CSharpProject.FrameworkReferences);
		var nspw = compilation.GetTypeByMetadataName("OpenNspw.Nspw") ?? throw new InvalidOperationException("No Nspw.");
		var lines = new List<string>();
		foreach (var field in nspw.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsStatic && !f.IsConst && !f.IsImplicitlyDeclared))
		{
			var file = field.Locations.FirstOrDefault()?.SourceTree?.FilePath;
			lines.Add($"global\t{field.Name}\t{field.Type.ToDisplayString()}\t{Path.GetFileName(file)}");
		}

		var uses = new Dictionary<(string Global, string File, string Type, string Member), (int Reads, int Writes)>();
		var calls = new HashSet<(string File, string Caller, string Callee)>();
		foreach (var tree in compilation.SyntaxTrees.Where(t => files.ContainsKey(t.FilePath)))
		{
			var model = compilation.GetSemanticModel(tree);
			var file = Path.GetFileName(tree.FilePath);
			foreach (var node in tree.GetRoot().DescendantNodes().Where(n => n is IdentifierNameSyntax or GenericNameSyntax))
			{
				// A name's operation is on the member access it ends, and a method's on the call of it.
				var expression = node.Parent is MemberAccessExpressionSyntax access && access.Name == node ? access : node;
				var operation = expression.Parent is InvocationExpressionSyntax invocation && invocation.Expression == expression
					? model.GetOperation(invocation)
					: model.GetOperation(expression);
				var type = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ?? "?";
				switch (operation)
				{
					case IFieldReferenceOperation { Field: { IsStatic: false, IsConst: false } field } reference
						when SymbolEqualityComparer.Default.Equals(field.ContainingType, nspw):
					{
						var (reads, writes) = Access(Location(reference), compilation);
						var key = (field.Name, file, type, MemberName(node));
						var (r, w) = uses.GetValueOrDefault(key);
						uses[key] = (r + reads, w + writes);
						break;
					}
					case IInvocationOperation { TargetMethod: var method } when SymbolEqualityComparer.Default.Equals(method.ContainingType, nspw):
						calls.Add((file, MemberName(node), method.Name));
						break;
					case IMethodReferenceOperation { Method: var method } when SymbolEqualityComparer.Default.Equals(method.ContainingType, nspw):
						calls.Add((file, MemberName(node), method.Name));
						break;
				}
			}
		}

		lines.AddRange(uses.OrderBy(u => u.Key).Select(u => $"use\t{u.Key.Global}\t{u.Key.File}\t{u.Key.Type}\t{u.Key.Member}\t{u.Value.Reads}\t{u.Value.Writes}"));
		lines.AddRange(calls.Order().Select(c => $"call\t{c.File}\t{c.Caller}\t{c.Callee}"));
		return lines;
	}
}
