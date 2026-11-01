using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenNspw.AI;

// A copy of a single-player battle as it stands, which plays on by itself, headless, as the game would from there: to
// look ahead, by playing candidate orders forward in copies and keeping the one that turns out best
// (docs/StrongAIResearch.md). The game knows nothing of it: the copy takes every field of the game that holds a value
// (the units, fires, effects, map, random numbers, the computer's state, the renderer's frame count and sprites) by
// reflection, and the one reference that holds state of the battle (the key state), and steps a frame as UpdateFrame
// does, without the drawing and the timer, and with sounds that play nothing. The original's extensions, and the
// commands queued on it, are left behind, as they act on it; the copy takes its own, as commands of the host's UI of its own
// (GameCommands). It plays the same as the original only if what the drawing and the timer do changes nothing of the
// battle, which the arena checks (--fork-check).
public sealed class GameFork
{
	// A sound buffer that plays nothing, for every sound of a copy: a copy's sounds are never heard.
	private sealed unsafe class SilentSoundBuffer : IDirectSoundBuffer
	{
		public static SilentSoundBuffer Instance { get; } = new();

		public int Play(uint dwReserved1, uint dwPriority, uint dwFlags) => 0;

		public int Stop() => 0;

		public int SetVolume(int lVolume) => 0;

		public int SetCurrentPosition(uint dwNewPosition) => 0;

		public int Lock(
			uint dwOffset,
			uint dwBytes,
			void** ppvAudioPtr1,
			uint* pdwAudioBytes1,
			void** ppvAudioPtr2,
			uint* pdwAudioBytes2,
			uint dwFlags
		) => 0;

		public int Unlock(
			void* pvAudioPtr1,
			uint dwAudioBytes1,
			void* pvAudioPtr2,
			uint dwAudioBytes2
		) => 0;

		public uint Release() => 0;
	}

	private const BindingFlags InstanceFields =
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly FieldInfo[] GameValues = ValueFields(typeof(Nspw));
	private static readonly FieldInfo[] RendererValues = ValueFields(typeof(Renderer));

	private static readonly FieldInfo KeyState = typeof(Nspw).GetField(
		"_keyState",
		InstanceFields
	)!;

	// Whether one of the player's units sees an effect at a point, as the drawing tests it (UpdateLowerEffects).
	private static readonly Func<Nspw, WorldPosition, bool> IsEffectSeen = typeof(Nspw)
		.GetMethod("IsEffectSeen", InstanceFields)!
		.CreateDelegate<Func<Nspw, WorldPosition, bool>>();

	// The copy, to read and to give orders to.
	public Nspw Game { get; }

	private static FieldInfo[] ValueFields(Type type) =>
		[.. type.GetFields(InstanceFields).Where(f => f.FieldType.IsValueType)];

	private static void CopyValues(FieldInfo[] fields, object from, object to)
	{
		foreach (var field in fields)
			field.SetValue(to, field.GetValue(from));
	}

	private static void CopyCollections(Nspw from, Nspw to)
	{
		((byte[])KeyState.GetValue(from)!).CopyTo((byte[])KeyState.GetValue(to)!, 0);
	}

	// A copy of the game as it stands, on a platform with nothing shown or played. Call it on the game's thread, between
	// frames.
	public GameFork(Nspw game)
	{
		Game = new Nspw(NullPlatform.Instance) { Rules = game.Rules };
		CopyValues(GameValues, game, Game);
		CopyValues(RendererValues, game.Renderer, Game.Renderer);
		CopyCollections(game, Game);
		Game.Sound.NextSoundBuffers = game.Sound.NextSoundBuffers;
		for (var sound = 0; sound < 35; sound++)
		{
			for (var i = 0; i < 6; i++)
				Game.Sound.SoundBuffers[sound][i] = SilentSoundBuffer.Instance;
		}
	}

	// The random numbers the drawing draws for the effects it shows (UpdateLowerEffects, then UpdateUpperEffects), from
	// the game's own, as 1.22 has its effects do in single-player (EffectRandom, Rules.EffectsDrawFromRand): three for a
	// plane's smoke, one for a wake, each effect of a layer in turn that one of the player's units sees, or every one once
	// the battle has a result. The drawing changes nothing else of the battle, so a copy that draws these plays on as the
	// game does.
	private static void DrawEffectRandoms(Nspw game)
	{
		foreach (var layer in (ReadOnlySpan<EffectLayer>)[EffectLayer.Lower, EffectLayer.Upper])
		{
			for (var m = 1; m < all_head.EFFECT_MAX; m++)
			{
				ref var effect = ref game.Effects[m];
				if (effect.Layer != layer)
					continue;

				var isSeen =
					layer == EffectLayer.Lower
						? (effect.Animation == 3 && game.Result == GameResult.None)
							|| (
								game.Result != GameResult.None
								&& game.Rules.ResultShowsEveryLowerEffect
							)
							|| IsEffectSeen(game, effect.Position)
						: game.Result != GameResult.None || IsEffectSeen(game, effect.Position);
				if (!isSeen)
					continue;

				if (effect.Animation == 2)
				{
					game.EffectRandom(2);
					game.EffectRandom(10);
					game.EffectRandom(10);
				}
				else if (effect.Animation == 4)
					game.EffectRandom(2);
			}
		}
	}

	// Plays a frame of the battle, as UpdateFrame does, but for the drawing and its timer: the speed's ticks, the result
	// checked after each (but the drawn one, in single-player, as 1.22 does) and after the frame, the input and the
	// commands given, the mouse buttons released, and the frame counted; of the drawing, only the random numbers it draws
	// (DrawEffectRandoms).
	public void Step()
	{
		var game = Game;
		var ticks =
			game.Buttons.HasFlag(InputButtons.Space) ? 20
			: game.GameSpeed == 0 ? 0
			: game.GameSpeed + 1;
		game.ReadInput();
		for (var i = ticks; i >= 1; i--)
		{
			game.Renderer.IsDrawnTick = i == 1;
			game.UpdateBattle();
			if (!game.IsEditingMap && !(game.IsSingleplayer && game.Renderer.IsDrawnTick))
				game.CheckResult();
		}

		DrawEffectRandoms(game);
		if (!game.IsEditingMap)
		{
			game.CheckResult();
			game.HandleInput();
		}

		if (game.RightButton == 1)
			game.RightButton = 2;
		if (game.RightButton == 3)
			game.RightButton = 0;
		if (game.LeftButton == 1)
			game.LeftButton = 2;
		if (game.LeftButton == 3)
			game.LeftButton = 0;
		game.Renderer.FrameCount++;
	}

	// The bytes of a value with no references in it, boxed.
	private static byte[] Bytes<T>(object boxed)
		where T : struct =>
		MemoryMarshal.AsBytes(new Span<T>(ref Unsafe.Unbox<T>(boxed))).ToArray();

	// Whether two boxed values of a type are the same, byte for byte where they hold no references.
	private static bool IsSame(Type type, object x, object y)
	{
		if (
			Nullable.GetUnderlyingType(type) is not null
			|| (bool)
				typeof(RuntimeHelpers)
					.GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences))!
					.MakeGenericMethod(type)
					.Invoke(null, null)!
		)
			return Equals(x, y);

		var toBytes = typeof(GameFork)
			.GetMethod(nameof(Bytes), BindingFlags.Static | BindingFlags.NonPublic)!
			.MakeGenericMethod(type);
		return ((byte[])toBytes.Invoke(null, [x])!)
			.AsSpan()
			.SequenceEqual((byte[])toBytes.Invoke(null, [y])!);
	}

	// The fields of the frame's timer, which a copy does not run.
	private static readonly HashSet<string> TimerFields = ["Now", "LastTime2", "Elapsed"];

	// The names of the game's fields of values, with no references in them, that differ between two games, but the
	// timer's: none, if a copy has played as the original.
	public static List<string> Differences(Nspw a, Nspw b)
	{
		var bytes = typeof(GameFork).GetMethod(
			nameof(Bytes),
			BindingFlags.Static | BindingFlags.NonPublic
		)!;
		var differences = new List<string>();
		foreach (
			var (fields, x, y) in new[]
			{
				(GameValues, (object)a, (object)b),
				(RendererValues, a.Renderer, b.Renderer),
			}
		)
		{
			foreach (var field in fields)
			{
				if (TimerFields.Contains(field.Name))
					continue;

				// The sprites' frames and places are the drawing's, which a copy does not do; their sizes, which the battle
				// reads (TileAt), are compared.
				if (field.FieldType == typeof(SpriteArray))
				{
					for (var i = 0; i < 25; i++)
					{
						if (
							((Renderer)x).Sprites[i].Size.Width
								!= ((Renderer)y).Sprites[i].Size.Width
							|| ((Renderer)x).Sprites[i].Size.Height
								!= ((Renderer)y).Sprites[i].Size.Height
						)
							differences.Add($"Sprites[{(SpriteId)i}]");
					}

					continue;
				}

				if (
					(bool)
						typeof(RuntimeHelpers)
							.GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences))!
							.MakeGenericMethod(field.FieldType)
							.Invoke(null, null)!
				)
					continue;

				// The units, each by its number and the fields of it that differ, but the column of its sprite, which the
				// drawing sets as it draws it (DrawUnit) and nothing else reads.
				if (field.Name == nameof(Nspw.Units))
				{
					for (var m = 0; m < 256; m++)
					{
						var (unitA, unitB) = (a.Units[m], b.Units[m]);
						if (
							!MemoryMarshal
								.AsBytes(new Span<Unit>(ref unitA))
								.SequenceEqual(MemoryMarshal.AsBytes(new Span<Unit>(ref unitB)))
						)
						{
							var names = typeof(Unit)
								.GetFields(InstanceFields)
								.Where(f =>
									f.Name != nameof(Unit.SpriteColumn)
									&& !IsSame(f.FieldType, f.GetValue(unitA)!, f.GetValue(unitB)!)
								)
								.Select(f => f.Name)
								.ToList();
							if (names.Count > 0)
								differences.Add(
									$"Units[{m}] ({a.Units[m].Kind}: {string.Join(", ", names)})"
								);
						}
					}

					continue;
				}

				if (Nullable.GetUnderlyingType(field.FieldType) is not null)
				{
					if (!Equals(field.GetValue(x), field.GetValue(y)))
						differences.Add(field.Name);
					continue;
				}

				var toBytes = bytes.MakeGenericMethod(field.FieldType);
				var left = (byte[])toBytes.Invoke(null, [field.GetValue(x)])!;
				var right = (byte[])toBytes.Invoke(null, [field.GetValue(y)])!;
				if (!left.AsSpan().SequenceEqual(right))
					differences.Add(field.Name);
			}
		}

		return differences;
	}
}
