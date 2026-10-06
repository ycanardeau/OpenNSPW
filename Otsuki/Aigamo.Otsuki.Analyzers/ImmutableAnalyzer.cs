using System.Collections.Immutable;
using Aigamo.Otsuki.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aigamo.Otsuki.Analyzers;

/// <summary>
/// Enforces the <c>[Immutable]</c> contract on the properties of types marked with it:
/// every property must be get-only or init-only (<see cref="MutableSetterDiagnosticId"/>),
/// and every property type must itself be immutable (<see cref="MutableTypeDiagnosticId"/>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ImmutableAnalyzer : DiagnosticAnalyzer
{
	public const string MutableSetterDiagnosticId = "OTSUKI0001";

	public const string MutableTypeDiagnosticId = "OTSUKI0002";

	// ImmutableAttribute.cs is linked in from Aigamo.Otsuki.Messages, so renaming or moving the
	// attribute keeps this name in sync.
	private static readonly string ImmutableAttributeMetadataName =
		typeof(ImmutableAttribute).FullName;

	private static readonly string ImmutableCollectionsNamespace = typeof(ImmutableArray).Namespace;

	// Immutable types that are not marked `readonly` in the netstandard2.0 reference assemblies.
	private static readonly ImmutableHashSet<string> KnownImmutableTypeNames =
		ImmutableHashSet.Create(
			typeof(DateTime).FullName,
			typeof(DateTimeOffset).FullName,
			typeof(Guid).FullName,
			typeof(TimeSpan).FullName
		);

	private static readonly DiagnosticDescriptor MutableSetterRule = new(
		id: MutableSetterDiagnosticId,
		title: "Properties of an immutable type must be get-only or init-only",
		messageFormat: "Property '{0}' of immutable type '{1}' must be get-only or init-only",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	private static readonly DiagnosticDescriptor MutableTypeRule = new(
		id: MutableTypeDiagnosticId,
		title: "Properties of an immutable type must have an immutable type",
		messageFormat: "Property '{0}' of immutable type '{1}' uses mutable type '{2}'",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(MutableSetterRule, MutableTypeRule);

	private static bool IsImmutable(ITypeSymbol type, INamedTypeSymbol immutableAttribute) =>
		type.GetAttributes()
			.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, immutableAttribute));

	private static bool HasMutableSetter(IPropertySymbol property) =>
		property.SetMethod is { IsInitOnly: false };

	// Points at the `set` accessor when it is written out, or at the property itself when the
	// setter is compiler-generated (e.g. a positional parameter of a non-readonly record struct).
	private static Location GetSetterLocation(IPropertySymbol property) =>
		property.SetMethod!.Locations.FirstOrDefault(l => l.IsInSource) ?? property.Locations[0];

	private static bool IsImmutableCollection(INamedTypeSymbol type) =>
		type.ContainingType is null
		&& type.ContainingNamespace.ToDisplayString() == ImmutableCollectionsNamespace;

	private static bool IsShallowlyImmutable(
		ITypeSymbol type,
		INamedTypeSymbol immutableAttribute
	) =>
		type.OriginalDefinition.SpecialType
			is SpecialType.System_Boolean
				or SpecialType.System_Char
				or SpecialType.System_SByte
				or SpecialType.System_Byte
				or SpecialType.System_Int16
				or SpecialType.System_UInt16
				or SpecialType.System_Int32
				or SpecialType.System_UInt32
				or SpecialType.System_Int64
				or SpecialType.System_UInt64
				or SpecialType.System_Decimal
				or SpecialType.System_Single
				or SpecialType.System_Double
				or SpecialType.System_String
				or SpecialType.System_IntPtr
				or SpecialType.System_UIntPtr
				or SpecialType.System_Nullable_T
		|| type.TypeKind is TypeKind.Enum or TypeKind.TypeParameter
		|| type is { IsValueType: true, IsReadOnly: true }
		|| KnownImmutableTypeNames.Contains(type.OriginalDefinition.ToDisplayString())
		|| IsImmutable(type, immutableAttribute)
		|| type is INamedTypeSymbol named && IsImmutableCollection(named.OriginalDefinition);

	// Returns the first type (the type itself or one of its type arguments, recursively) that is
	// not immutable, or null when the whole type is immutable. Type parameters are accepted here;
	// they are checked against their type arguments wherever the generic type is used.
	private static ITypeSymbol? FindMutableType(
		ITypeSymbol type,
		INamedTypeSymbol immutableAttribute
	)
	{
		if (!IsShallowlyImmutable(type, immutableAttribute))
			return type;

		if (type is not INamedTypeSymbol named)
			return null;

		return named
			.TypeArguments.Select(typeArgument => FindMutableType(typeArgument, immutableAttribute))
			.FirstOrDefault(mutableType => mutableType is not null);
	}

	// Points at the property's declared type, falling back to the property name.
	private static Location GetTypeLocation(
		IPropertySymbol property,
		CancellationToken cancellationToken
	) =>
		property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) switch
		{
			BasePropertyDeclarationSyntax declaration => declaration.Type.GetLocation(),
			ParameterSyntax { Type: { } parameterType } => parameterType.GetLocation(),
			_ => property.Locations[0],
		};

	private static void AnalyzeProperty(
		SymbolAnalysisContext context,
		INamedTypeSymbol type,
		IPropertySymbol property,
		INamedTypeSymbol immutableAttribute
	)
	{
		if (HasMutableSetter(property))
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					MutableSetterRule,
					GetSetterLocation(property),
					property.Name,
					type.Name
				)
			);
		}

		// Skips compiler-generated properties such as a record's `EqualityContract`.
		if (property.IsImplicitlyDeclared)
			return;

		if (FindMutableType(property.Type, immutableAttribute) is { } mutableType)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					MutableTypeRule,
					GetTypeLocation(property, context.CancellationToken),
					property.Name,
					type.Name,
					mutableType.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
				)
			);
		}
	}

	private static void AnalyzeNamedType(
		SymbolAnalysisContext context,
		INamedTypeSymbol immutableAttribute
	)
	{
		var type = (INamedTypeSymbol)context.Symbol;
		if (!IsImmutable(type, immutableAttribute))
			return;

		foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
			AnalyzeProperty(context, type, property, immutableAttribute);
	}

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(startContext =>
		{
			var immutableAttribute = startContext.Compilation.GetTypeByMetadataName(
				ImmutableAttributeMetadataName
			);
			if (immutableAttribute is null)
				return;

			startContext.RegisterSymbolAction(
				symbolContext => AnalyzeNamedType(symbolContext, immutableAttribute),
				SymbolKind.NamedType
			);
		});
	}
}
