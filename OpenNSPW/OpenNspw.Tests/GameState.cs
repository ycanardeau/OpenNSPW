using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace OpenNspw.Tests;

// The bytes of a global of the port, in the game.
internal delegate Span<byte> GlobalBytes(Nspw game);

internal delegate ref T FieldReference<T>(Nspw game);

// A global of the reference (Layout.json), and the field of the port that holds it, at an offset (see OriginalNames).
internal sealed record Global(string Name, int Size, FieldInfo Field, int Offset, GlobalBytes Bytes)
{
	private static GlobalBytes Wrap<T>(FieldReference<T> reference) where T : struct
	{
		return game => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref reference(game), 1));
	}

	private static readonly MethodInfo WrapMethod =
		typeof(Global).GetMethod(nameof(Wrap), BindingFlags.NonPublic | BindingFlags.Static)!;

	// Accesses the field in place, without copying it, as `ref game.field`.
	private static GlobalBytes CreateBytes(FieldInfo field)
	{
		var method = new DynamicMethod($"ref_{field.Name}", field.FieldType.MakeByRefType(), [typeof(Nspw)], typeof(Nspw).Module, skipVisibility: true);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldflda, field);
		il.Emit(OpCodes.Ret);
		var reference = method.CreateDelegate(typeof(FieldReference<>).MakeGenericType(field.FieldType));
		return (GlobalBytes)WrapMethod.MakeGenericMethod(field.FieldType).Invoke(null, [reference])!;
	}

	private static Global Create(ReferenceGlobal global)
	{
		var slice = OriginalNames.FindGlobal(global.Name);
		var bytes = CreateBytes(slice.Field);
		return new Global(global.Name, global.Size, slice.Field, slice.Offset, game => bytes(game).Slice(slice.Offset, slice.Size));
	}

	public static ImmutableArray<Global> All { get; } = [.. ReferenceLayout.Instance.Globals.Select(Create)];

	public static ImmutableDictionary<string, int> Indexes { get; } = All.Select((g, i) => (g.Name, i)).ToImmutableDictionary(g => g.Name, g => g.i);

	// The global's bytes in the game, which must have the size they have in the reference.
	public Span<byte> BytesIn(Nspw game)
	{
		var bytes = Bytes(game);
		if (bytes.Length != Size)
		{
			throw new InvalidOperationException($"{Name} has {bytes.Length} bytes in the port, but {Size} in the reference.");
		}

		return bytes;
	}
}

// The values of all the globals of the reference, as bytes. Immutable: With() copies only the globals it changes.
internal sealed class GameState(ImmutableArray<byte[]> bytes)
{
	private readonly ImmutableArray<byte[]> _bytes = bytes;

	private static GameState Zero()
	{
		return new GameState([.. Global.All.Select(g => new byte[g.Size])]);
	}

	public GameState With(IEnumerable<Run> runs)
	{
		var bytes = _bytes.ToBuilder();
		var copied = new HashSet<int>();
		foreach (var run in runs)
		{
			var index = Global.Indexes[run.Global];
			if (copied.Add(index))
			{
				bytes[index] = [.. bytes[index]];
			}

			run.Bytes.CopyTo(bytes[index], run.Offset);
		}

		return new GameState(bytes.ToImmutable());
	}

	// The state of the reference at startup.
	public static GameState Initial { get; } = Zero().With(ReferenceLayout.Instance.Initial);

	public void Restore(Nspw game)
	{
		for (var i = 0; i < Global.All.Length; i++)
		{
			_bytes[i].CopyTo(Global.All[i].BytesIn(game));
		}
	}

	// The numbers in the game's globals that differ from this state, at most `limit` of them.
	public IReadOnlyList<string> Differences(Nspw game, int limit = 8)
	{
		var differences = new List<string>();
		for (var i = 0; i < Global.All.Length && differences.Count < limit; i++)
		{
			var global = Global.All[i];
			ReadOnlySpan<byte> expected = _bytes[i];
			ReadOnlySpan<byte> actual = global.BytesIn(game);
			var offset = 0;
			while (differences.Count < limit)
			{
				var mismatch = expected[offset..].CommonPrefixLength(actual[offset..]);
				if (offset + mismatch >= expected.Length)
				{
					break;
				}

				var location = TypeLayout.Locate(global.Field.FieldType, global.Offset + offset + mismatch);
				var start = location.Start - global.Offset;
				var size = TypeLayout.IsScalar(location.Type) ? TypeLayout.SizeOf(location.Type) : 1;
				differences.Add(
					$"{global.Name}{location.Path}: expected {TypeLayout.Format(location.Type, expected.Slice(start, size))}, " +
					$"actual {TypeLayout.Format(location.Type, actual.Slice(start, size))}");
				offset = start + size;
			}
		}

		return differences;
	}
}
