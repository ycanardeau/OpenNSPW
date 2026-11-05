using OpenNspw.Replay;
using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// OpenNSPW's own options for a single-player battle (docs/Options.md).
public sealed class BattleOptionsTests
{
	private const int Frames = 150;

	private static readonly BattleOptions SharedEffectRandom = new(SharedEffectRandom: true);

	// The battle's hash as each frame ends, by its tick.
	private sealed class Hashes : IGameExtension
	{
		public Dictionary<int, ulong> ByTick { get; } = [];

		public void FrameEnded(Nspw game) => ByTick[game.Tick] = ReplayFormat.HashBattle(game);
	}

	// Shows every other frame of the battle after the first 10, and counts the frames whose drawing drew random numbers,
	// from rand or from SharedRandom, by whether they were shown.
	private sealed class EveryOtherFrame : IGameExtension
	{
		private int _frames;
		private (int Count, short Index)? _beforeDrawing;

		public int ShownDrawingFromRand { get; private set; }

		public int ShownDrawingFromTable { get; private set; }

		public int NotShownDrawingFromRand { get; private set; }

		public int NotShownDrawingFromTable { get; private set; }

		public bool? ShowsFrame(Nspw game) => _frames++ < 10 || _frames % 2 == 0;

		public void Drawing(Nspw game) =>
			_beforeDrawing = (game.RandomCount, game.SharedRandomIndex);

		// The first check of the result after the drawing.
		public void ResultChecking(Nspw game)
		{
			if (_beforeDrawing is not var (count, index))
				return;

			_beforeDrawing = null;
			var (fromRand, fromTable) = (
				game.RandomCount != count,
				game.SharedRandomIndex != index
			);
			if (game.Renderer.DrawsNothing)
			{
				NotShownDrawingFromRand += fromRand ? 1 : 0;
				NotShownDrawingFromTable += fromTable ? 1 : 0;
			}
			else
			{
				ShownDrawingFromRand += fromRand ? 1 : 0;
				ShownDrawingFromTable += fromTable ? 1 : 0;
			}
		}
	}

	// The battles are the same at every tick that both ran.
	private static void AssertSameBattle(Hashes expected, Hashes actual)
	{
		var common = expected.ByTick.Keys.Intersect(actual.ByTick.Keys).ToList();
		Assert.NotEmpty(common);
		Assert.All(common, tick => Assert.Equal(expected.ByTick[tick], actual.ByTick[tick]));
	}

	// Plays scenario 7, whose drawing draws random numbers for the effects in sight from its first frames, for Frames
	// frames shown, with `options` and the extension `added`, if any.
	private static Hashes Play(BattleOptions options, IGameExtension? added = null)
	{
		var hashes = new Hashes();
		using var run = new SingleplayerRun(
			7,
			game =>
			{
				game.Options = options;
				if (added is not null)
					game.Extensions.Add(added);
				game.Extensions.Add(hashes);
			}
		);
		run.Frames(Frames);
		return hashes;
	}

	// With every option off, the battle plays by 1.22's rules; with SharedEffectRandom, its effects draw from a table of
	// its own, made without moving rand.
	[Fact]
	public void TheOptionsChangeTheRules()
	{
		using (var off = new SingleplayerRun(4))
		{
			off.Frames(2);

			Assert.Same(BattleRules.Nspw122, off.Game.Rules);
		}

		using var on = new SingleplayerRun(4, game => game.Options = SharedEffectRandom);
		on.Frames(2);

		Assert.Equal(BattleRules.Nspw122 with { EffectsDrawFromRand = false }, on.Game.Rules);
		Assert.Contains(
			((ReadOnlySpan<int>)on.Game.SharedRandomTable).ToArray(),
			value => value != 0
		);
	}

	// With SharedEffectRandom, a frame that is not shown skips the effects (Renderer.SkipsEffects), which draw from
	// SharedRandom in a frame shown: the battle is the same.
	[Fact]
	public void WithSharedEffectRandomAFrameNotShownSkipsTheEffects()
	{
		var shown = Play(SharedEffectRandom);

		var everyOther = new EveryOtherFrame();
		var notAllShown = Play(SharedEffectRandom, everyOther);

		Assert.True(everyOther.ShownDrawingFromTable > 0);
		Assert.Equal(0, everyOther.NotShownDrawingFromTable);
		Assert.Equal(0, everyOther.NotShownDrawingFromRand + everyOther.ShownDrawingFromRand);
		AssertSameBattle(shown, notAllShown);
	}

	// Without it, as 1.22 plays, a frame that is not shown runs the effects, which draw from rand, as the battle does: the
	// battle is the same.
	[Fact]
	public void WithoutItAFrameNotShownRunsTheEffects()
	{
		var shown = Play(BattleOptions.None);

		var everyOther = new EveryOtherFrame();
		var notAllShown = Play(BattleOptions.None, everyOther);

		Assert.True(everyOther.NotShownDrawingFromRand > 0);
		AssertSameBattle(shown, notAllShown);
	}
}
