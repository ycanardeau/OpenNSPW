using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.Tests.Lockstep.BattleScripts;

namespace OpenNspw.Tests;

// The game's own frames drawn from the desktop app's sprite files instead of the sprite sheet (docs/GameSprites.md):
// each piece holds what the sheet holds there, every blit falls in one piece, and the frames are the same.
public class GameSpritesTests
{
	// The rectangle of an effect's frame on the sheet (SubUnits: 12 a row of 40 by 40).
	private static RECT Effect(int frame)
	{
		var (x, y) = (frame % 12 * 40, 3520 + frame / 12 * 40);
		return new RECT
		{
			left = x,
			top = y,
			right = x + 40,
			bottom = y + 40,
		};
	}

	// The regions of 1.22's sheet that its pieces do not have as it does: the title, which a single-player game does not
	// draw, and the effects 16, 26 and 27, blank on 1.22's sheet, which only NSPW NET's rules draw.
	private static readonly RECT[] DifferentFrom122 =
	[
		new()
		{
			left = 0,
			top = 3940,
			right = 699,
			bottom = 3940 + 387,
		},
		Effect(16),
		Effect(26),
		Effect(27),
	];

	private static bool IsIn(RECT r, int x, int y) =>
		x >= r.left && x < r.right && y >= r.top && y < r.bottom;

	private static void AssertPiecesAreTheSheets(
		SpriteSheetSurface pieces,
		DirectDrawSurface7 sheet,
		IReadOnlyList<RECT> excepted
	)
	{
		foreach (var (piece, x, y) in pieces.Pieces)
		{
			var differing = 0;
			for (var j = 0; j < piece.Height; j++)
			{
				for (var i = 0; i < piece.Width; i++)
				{
					if (
						!excepted.Any(r => IsIn(r, x + i, y + j))
						&& piece.Pixels[j * piece.Width + i]
							!= sheet.Pixels[(y + j) * sheet.Width + x + i]
					)
					{
						differing++;
					}
				}
			}

			Assert.True(differing == 0, $"The piece at {x}, {y} differs in {differing} pixels.");
		}
	}

	private static void AssertNoMisses(LockstepGame game)
	{
		var sheet = Assert.IsType<SpriteSheetSurface>(game.Game.Renderer.OffscreenSurface);
		Assert.Empty(
			sheet.Misses.Select(r => $"({r.left}, {r.top}, {r.right}, {r.bottom})").Order()
		);
	}

	[Fact]
	public void NSPW_NETs_pieces_hold_what_its_sheet_holds()
	{
		using var fromFiles = new LockstepRun(drawsFromSpriteFiles: true);
		using var fromSheet = new LockstepRun();
		Connect(fromFiles);
		Connect(fromSheet);

		AssertPiecesAreTheSheets(
			Assert.IsType<SpriteSheetSurface>(fromFiles.Host.Game.Renderer.OffscreenSurface),
			Assert.IsType<DirectDrawSurface7>(fromSheet.Host.Game.Renderer.OffscreenSurface),
			[]
		);
	}

	[Fact]
	public void The_pieces_of_122_hold_what_its_sheet_holds()
	{
		using var fromFiles = new SingleplayerRun(202, drawsFromSpriteFiles: true);
		using var fromSheet = new SingleplayerRun(202);
		fromFiles.Frames(2);
		fromSheet.Frames(2);

		AssertPiecesAreTheSheets(
			Assert.IsType<SpriteSheetSurface>(fromFiles.Game.Renderer.OffscreenSurface),
			Assert.IsType<DirectDrawSurface7>(fromSheet.Game.Renderer.OffscreenSurface),
			DifferentFrom122
		);
	}

	// A single-player game shows the same frames, writes the same texts and plays the same sounds from the files as
	// from 1.22's sheet.
	[Theory]
	[InlineData(202)]
	[InlineData(303)]
	public void A_single_player_game_draws_the_same_from_the_files(int scenario)
	{
		using var fromFiles = new SingleplayerRun(scenario, drawsFromSpriteFiles: true);
		using var fromSheet = new SingleplayerRun(scenario);
		for (var i = 0; i < 20; i++)
		{
			fromFiles.Frames(25);
			fromSheet.Frames(25);
			Assert.Equal(fromSheet.Player.TakeOutput(), fromFiles.Player.TakeOutput());
		}

		AssertNoMisses(fromFiles.Player);
	}

	// A network game's committed traces, which hash every frame, stay as they are from the files: the title screen, and
	// a battle of the setting screen's first scenario.
	[Fact]
	public void A_network_game_draws_the_same_from_the_files()
	{
		using var run = new LockstepRun(drawsFromSpriteFiles: true);
		Connect(run);
		run.Frames(run.Host, 100);

		Traces.Check("title", run.FinishTrace(), null);
		AssertNoMisses(run.Host);
		AssertNoMisses(run.Guest);
	}

	[Fact]
	public void A_network_battle_draws_the_same_from_the_files()
	{
		using var run = new LockstepRun(drawsFromSpriteFiles: true);
		StartBattle(run, 0);
		Battle(run, 1, 2000);

		Traces.Check("scenario-1", run.FinishTrace(), null);
		AssertNoMisses(run.Host);
		AssertNoMisses(run.Guest);
	}
}
