using System.Reflection;

namespace OpenNspw.Tests;

// A C++ variable or struct member, and where its bytes are in the C# field that holds it. For a global, Owners are the
// fields that lead from Nspw to the object that holds the field, such as Nspw.Sound, none if Nspw holds it itself.
internal sealed record OriginalSlice(string Name, FieldInfo Field, int Offset, int Size)
{
	public IReadOnlyList<FieldInfo> Owners { get; init; } = [];
}

// The C++ names of the port's members: the names in [Original], or the members' own names (docs/Refactoring.md, Name
// binding).
internal static class OriginalNames
{
	public static IReadOnlyList<string> Of(MemberInfo member)
	{
		return member.GetCustomAttribute<OriginalAttribute>()?.Names ?? [member.Name];
	}

	// The C++ variables that a field holds, with their offsets from `offset`, where the field starts: the field itself,
	// or, for a field with several names, each field of its type.
	public static IEnumerable<OriginalSlice> Slices(FieldInfo field, int offset)
	{
		var names = Of(field);
		if (names.Count == 1)
		{
			yield return new OriginalSlice(
				names[0],
				field,
				offset,
				TypeLayout.SizeOf(field.FieldType)
			);
			yield break;
		}

		var parts = TypeLayout.FieldsOf(field.FieldType);
		if (parts.Count != names.Count)
		{
			throw new InvalidOperationException(
				$"{field.DeclaringType?.Name}.{field.Name} has {names.Count} original names, but its type has {parts.Count} fields."
			);
		}

		for (var i = 0; i < parts.Count; i++)
		{
			yield return new OriginalSlice(
				names[i],
				field,
				offset + parts[i].Offset,
				parts[i].Size
			);
		}
	}

	// The struct of the port that has a C++ name.
	public static Type FindStruct(string name)
	{
		return typeof(Nspw)
				.Assembly.GetTypes()
				.SingleOrDefault(t =>
					t.IsValueType && t.Namespace == "OpenNspw" && Of(t).Contains(name)
				)
			?? throw new InvalidOperationException($"The port has no struct named {name}.");
	}

	// The fields that hold globals: Nspw's own, and those of the objects of [Globals] classes that its fields hold, with
	// the fields that lead to them.
	public static IEnumerable<(IReadOnlyList<FieldInfo> Owners, FieldInfo Field)> GlobalFields(
		Type type,
		IReadOnlyList<FieldInfo> owners
	)
	{
		foreach (
			var field in type.GetFields(
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
			)
		)
		{
			if (field.FieldType.GetCustomAttribute<GlobalsAttribute>() is not null)
			{
				foreach (var global in GlobalFields(field.FieldType, [.. owners, field]))
				{
					yield return global;
				}
			}
			else
			{
				yield return (owners, field);
			}
		}
	}

	// The global of the port that has a C++ name.
	public static OriginalSlice FindGlobal(string name)
	{
		var (owners, field) = GlobalFields(typeof(Nspw), [])
			.SingleOrDefault(g => Of(g.Field).Contains(name));
		if (field is null)
		{
			throw new InvalidOperationException($"The port has no global named {name}.");
		}

		return Slices(field, 0).Single(s => s.Name == name) with
		{
			Owners = owners,
		};
	}
}
