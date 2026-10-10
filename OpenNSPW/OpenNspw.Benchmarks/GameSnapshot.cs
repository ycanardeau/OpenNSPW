using System.Collections.Immutable;
using System.Reflection;

namespace OpenNspw.Benchmarks;

// The values of a game's fields that are values: the globals of the original, the rand() state and the stand-ins' own
// values. Restoring them takes the game back to the same point of a battle; the objects of the stand-ins (surfaces,
// windows, DirectPlay) are not part of it, and must be in the same state, such as with no message waiting.
internal sealed class GameSnapshot(ImmutableArray<object?> values)
{
	private static readonly ImmutableArray<FieldInfo> Fields =
	[
		.. typeof(Nspw).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(f => f.FieldType.IsValueType && !f.IsInitOnly),
	];

	private readonly ImmutableArray<object?> _values = values;

	public static GameSnapshot Take(Nspw game)
	{
		return new GameSnapshot([.. Fields.Select(f => f.GetValue(game))]);
	}

	public void Restore(Nspw game)
	{
		for (var i = 0; i < Fields.Length; i++)
		{
			Fields[i].SetValue(game, _values[i]);
		}
	}
}
