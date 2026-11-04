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
// commands queued on it, are left behind, as they act on it; the copy takes its own orders as the clicks would run them,
// at once (ClickUnit, SetDestination, RightClick). It plays the same as the original only if what the drawing and the
// timer do changes nothing of the battle, which the arena checks (--fork-check).
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

	private delegate int SightOfDelegate(Nspw game, ref Unit unit);
	private delegate WorldPosition EyeOfDelegate(Nspw game, ref Unit unit);

	// How far a unit sees an effect, by the game's own ranges (EffectSightOf), and where it looks from (EyeOf).
	private static readonly SightOfDelegate EffectSightOf = typeof(Nspw)
		.GetMethod("EffectSightOf", InstanceFields)!
		.CreateDelegate<SightOfDelegate>();
	private static readonly EyeOfDelegate EyeOf = typeof(Nspw)
		.GetMethod("EyeOf", InstanceFields)!
		.CreateDelegate<EyeOfDelegate>();

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

	// Whether a click on the battlefield counts now: not while an order waits for its turn, as the drawing takes them
	// (DrawBattleArea).
	private bool TakesClicks => !(Game.HasOrdered && Game.Result == GameResult.None);

	// Runs the commands of the host's UI given for a site of the copy, or drops them (IGameExtension.InputSite), as the
	// drawing has the game do: a player's that plays on in it.
	private static void InputSite(Nspw game, ActionSite site, bool counts)
	{
		foreach (var extension in game.Extensions.All)
			extension.InputSite(game, site, counts);
	}

	// A click on unit m, run at once: in the game, the drawing takes the battlefield's clicks (DrawBattleArea), which a
	// copy does not do, so a copy's orders run what the clicks would, when they are given.
	public void ClickUnit(int m)
	{
		Game.Commands.ClickUnit(m);
		InputSite(Game, ActionSite.Battlefield, TakesClicks);
	}

	// A click on the battlefield that gives the selected unit a destination, run at once.
	public void SetDestination(WorldPosition destination)
	{
		Game.Commands.SetDestination(destination);
		InputSite(Game, ActionSite.Battlefield, TakesClicks);
	}

	// A right click on the battlefield, run at once.
	public void RightClick()
	{
		Game.Commands.RightClick();
		InputSite(Game, ActionSite.Battlefield, TakesClicks);
	}

	// Gives the copy random numbers of its own from here on, so that what it plays out is one of the ways the battle may
	// go, not the way it will: the game's own numbers to come are not the player's to know.
	public void Reseed(uint seed)
	{
		Game.srand(seed);
	}

	// Whether one of the player's units, but parked planes, sees an effect at a point, as the drawing tests it
	// (UpdateLowerEffects, UpdateUpperEffects): by 1.22's ranges under its rules (Rules.HasSightRangesOf122), as the drawing
	// does, and the game's own test for the host's UI (IsEffectSeen) does not.
	private static bool IsEffectSeen(Nspw game, WorldPosition position)
	{
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var unit = ref game.Units[n];
			if (!unit.IsUsed || unit.Side != game.LocalSide || unit.PlaneState == UnitState.Parked)
				continue;

			var sight = game.Rules.HasSightRangesOf122
				? Nspw.GetSightRangeAs122(unit.Kind)
				: EffectSightOf(game, ref unit);
			if ((EyeOf(game, ref unit) - position).Length <= sight)
				return true;
		}

		return false;
	}

	// The random numbers the drawing draws for the effects it shows (UpdateLowerEffects, then UpdateUpperEffects), from
	// the game's own, as 1.22 has its effects do in single-player (EffectRandom, Rules.EffectsDrawFromRand): three for a
	// plane's smoke, one for a wake, each effect of a layer in turn that one of the player's units sees, or every one once
	// the battle has a result. The drawing changes nothing else of the battle, so a copy that draws these plays on as the
	// game does.
	private static void DrawEffectRandoms(Nspw game, EffectLayer layer)
	{
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

	// Checks the battle's result (decision): in single-player, which also draws the result once there is one, on a
	// screen a copy does not have. A copy needs the result, not its drawing, and plays no further once it has one.
	private static void CheckResult(Nspw game)
	{
		try
		{
			game.CheckResult();
		}
		catch (NullReferenceException) when (game.Result != GameResult.None) { }
	}

	// Plays a frame of the battle, as UpdateFrame does, but for the drawing and its timer: the speed's ticks, the result
	// checked after each (but the drawn one, in single-player, as 1.22 does) and after the frame, the input and the
	// commands given, the mouse buttons released, and the frame counted; of the drawing, only what it does to the battle:
	// the random numbers it draws for the effects (DrawEffectRandoms), and between the layers, the orders cleared and the
	// battlefield's clicks taken, which are the frame's `clicks` (ClickUnit, SetDestination, RightClick).
	public void Step(Action<GameFork>? clicks = null)
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
				CheckResult(game);
		}

		DrawEffectRandoms(game, EffectLayer.Lower);
		game.MoveOrders[0].Unit = 0;
		game.SelectOrders[0].IsSet = false;
		game.MoveOrders[1].Unit = 0;
		game.SelectOrders[1].IsSet = false;
		clicks?.Invoke(this);
		DrawEffectRandoms(game, EffectLayer.Upper);
		if (!game.IsEditingMap)
		{
			CheckResult(game);
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
