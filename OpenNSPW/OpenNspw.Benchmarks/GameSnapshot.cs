using System.Collections.Immutable;
using System.Reflection;

namespace OpenNspw.Benchmarks;

// The values of a game's fields that are values: the globals of the original, the rand() state and the stand-ins' own
// values. Restoring them takes the game back to the same point of a battle; the objects of the stand-ins (surfaces,
// windows, DirectPlay) are not part of it, and must be in the same state, such as with no message waiting.
internal sealed class GameSnapshot(ImmutableArray<object?> values)
{
	// The value fields of a type, and those of the objects of [Globals] classes that its fields hold, with the field that
	// holds that object.
	private static IEnumerable<(FieldInfo? Owner, FieldInfo Field)> ValueFields(
		Type type,
		FieldInfo? owner
	)
	{
		foreach (
			var field in type.GetFields(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
			)
		)
		{
			if (owner is null && field.FieldType.GetCustomAttribute<GlobalsAttribute>() is not null)
			{
				foreach (var inner in ValueFields(field.FieldType, field))
				{
					yield return inner;
				}
			}
			else if (field.FieldType.IsValueType && !field.IsInitOnly)
			{
				yield return (owner, field);
			}
		}
	}

	private static readonly ImmutableArray<(FieldInfo? Owner, FieldInfo Field)> Fields =
	[
		.. ValueFields(typeof(Nspw), null),
	];

	private readonly ImmutableArray<object?> _values = values;

	private static object Holder(Nspw game, FieldInfo? owner)
	{
		return owner is null ? game : owner.GetValue(game)!;
	}

	public static GameSnapshot Take(Nspw game)
	{
		return new GameSnapshot([.. Fields.Select(f => f.Field.GetValue(Holder(game, f.Owner)))]);
	}

	public void Restore(Nspw game)
	{
		for (var i = 0; i < Fields.Length; i++)
		{
			Fields[i].Field.SetValue(Holder(game, Fields[i].Owner), _values[i]);
		}
	}
}
