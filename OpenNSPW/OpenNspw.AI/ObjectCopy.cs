using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OpenNspw.AI;

// A deep copy of an object and of everything it holds, by reflection, field by field, as GameFork copies the game: to
// copy a player into a fork of the battle, with its dictionaries, its plans and the clicks it has queued. The objects
// `mapped` names stand for others in the copy, or for themselves, if they are to be shared: the game, for the fork's,
// and what never changes. A delegate whose target is copied, such as a lambda of the copied object or one capturing
// its locals, is made again on the copy of its target; another is shared. Each object is copied once, however many
// fields hold it, so the copy is held as the original is. Call it on the thread that changes the original.
public sealed class ObjectCopy(IReadOnlyDictionary<object, object?> mapped)
{
	private const BindingFlags InstanceFields =
		BindingFlags.Instance
		| BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.DeclaredOnly;

	private static readonly ConcurrentDictionary<Type, FieldInfo[]> Fields = new();
	private static readonly ConcurrentDictionary<Type, bool> ReferenceHolders = new();

	private readonly Dictionary<object, object?> _copies = new(
		mapped,
		ReferenceEqualityComparer.Instance
	);

	// The instance fields of a type and of the types it derives from.
	private static FieldInfo[] FieldsOf(Type type) =>
		Fields.GetOrAdd(
			type,
			t =>
			{
				var fields = new List<FieldInfo>();
				for (var declaring = t; declaring is not null; declaring = declaring.BaseType)
					fields.AddRange(declaring.GetFields(InstanceFields));
				return [.. fields];
			}
		);

	private static bool HoldsReferences(Type type) =>
		ReferenceHolders.GetOrAdd(
			type,
			t =>
				(bool)
					typeof(RuntimeHelpers)
						.GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences))!
						.MakeGenericMethod(t)
						.Invoke(null, null)!
		);

	// What is shared, not copied: what holds no state, such as the comparers of the collections, and what reflection
	// describes.
	private static bool IsShared(object value) =>
		value is string or MemberInfo or Pointer
		|| value.GetType() is { Namespace: "System.Collections.Generic" } type
			&& type.Name.Contains("Comparer", StringComparison.Ordinal);

	// The fields of an object copied into another, of the same type.
	private void CopyFields(object from, object to)
	{
		foreach (var field in FieldsOf(from.GetType()))
		{
			var value = field.GetValue(from);
			field.SetValue(to, HoldsReferences(field.FieldType) ? Copy(value) : value);
		}
	}

	private Delegate CopyDelegate(Delegate value)
	{
		if (value.GetInvocationList() is { Length: > 1 } list)
			return Delegate.Combine([.. list.Select(CopyDelegate)])!;

		if (value.Target is null)
			return value;

		var target = Copy(value.Target);
		return ReferenceEquals(target, value.Target)
			? value
			: Delegate.CreateDelegate(value.GetType(), target, value.Method);
	}

	private Array CopyArray(Array value)
	{
		var elementType = value.GetType().GetElementType()!;
		if (!HoldsReferences(elementType))
			return (Array)value.Clone();
		if (value.Rank != 1)
			throw new NotSupportedException($"A {value.GetType()} is not copied.");

		var copy = Array.CreateInstance(elementType, value.Length);
		_copies[value] = copy;
		for (var i = 0; i < value.Length; i++)
			copy.SetValue(Copy(value.GetValue(i)), i);
		return copy;
	}

	// A copy of a value: itself, if it holds no references; a value with references in it boxed afresh, its fields
	// copied; an object copied once.
	public object? Copy(object? value)
	{
		if (value is null)
			return null;

		var type = value.GetType();
		if (type.IsValueType)
		{
			if (!HoldsReferences(type))
				return value;

			// A boxed value is the box's own: GetValue boxes it afresh, so its fields can be set.
			CopyFields(value, value);
			return value;
		}

		if (_copies.TryGetValue(value, out var copied))
			return copied;
		if (IsShared(value))
			return value;
		if (value is Delegate function)
			return _copies[value] = CopyDelegate(function);
		if (value is Array array)
			return CopyArray(array);

		var copy = RuntimeHelpers.GetUninitializedObject(type);
		_copies[value] = copy;
		CopyFields(value, copy);
		return copy;
	}
}
