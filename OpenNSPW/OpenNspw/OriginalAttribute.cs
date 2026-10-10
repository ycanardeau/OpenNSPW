namespace OpenNspw;

// The name in the C++ source (Original/NSPW_NET) of a global, struct, struct field or function that has a new name in
// C#, so that the tests and the trace tools find it by the name that the reference records (docs/Refactoring.md, Name
// binding). A field that holds several C++ variables lists their names in order, one for each field of its type.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OriginalAttribute(params string[] names) : Attribute
{
	public IReadOnlyList<string> Names { get; } = names;
}
