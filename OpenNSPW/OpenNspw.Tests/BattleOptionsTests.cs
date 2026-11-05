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

	// Shows every other frame of the battle after the first 10, and skips the effects of those it does not show: from the
	// drawing to the check of the result, there are none, and then they are as they were, as if the effect loops of
	// UpdateLowerEffects and UpdateUpperEffects had not run.
	private sealed class SkipsEffectsNotShown : IGameExtension
	{
		private int _frames;
		private Array1024<Effect>? _effects;

		// The frames that skipped an effect.
		public int Skipped { get; private set; }

		public bool? ShowsFrame(Nspw game) => _frames++ < 10 || _frames % 2 == 0;

		public void Drawing(Nspw game)
		{
			if (!game.Renderer.DrawsNothing)
				return;

			_effects = game.Effects;
			if (
				((ReadOnlySpan<Effect>)game.Effects)
					.ToArray()
					.Any(effect => effect.Layer != EffectLayer.None)
			)
				Skipped++;
			game.Effects = default;
		}

		public void ResultChecking(Nspw game)
		{
			if (_effects is not { } effects)
				return;

			game.Effects = effects;
			_effects = null;
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

	// With SharedEffectRandom, a frame that is not shown can skip the effects: the battle draws no random number for them,
	// and is the same.
	[Fact]
	public void WithSharedEffectRandomAFrameNotShownCanSkipTheEffects()
	{
		var shown = Play(SharedEffectRandom);

		var skips = new SkipsEffectsNotShown();
		var skipped = Play(SharedEffectRandom, skips);

		Assert.True(skips.Skipped > 0);
		AssertSameBattle(shown, skipped);
	}

	// Without it, as 1.22 plays, skipping the effects plays another battle: they draw from rand, as the battle does.
	[Fact]
	public void WithoutItSkippingTheEffectsPlaysAnotherBattle()
	{
		var shown = Play(BattleOptions.None);

		var skips = new SkipsEffectsNotShown();
		var skipped = Play(BattleOptions.None, skips);

		Assert.True(skips.Skipped > 0);
		var common = shown.ByTick.Keys.Intersect(skipped.ByTick.Keys).ToList();
		Assert.Contains(common, tick => shown.ByTick[tick] != skipped.ByTick[tick]);
	}
}
