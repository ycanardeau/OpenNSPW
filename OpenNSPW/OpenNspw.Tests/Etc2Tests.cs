using Xunit;

namespace OpenNspw.Tests;

// etc2.cpp
public class Etc2Tests
{
	private static RECT RectArg(FunctionCase c, int index)
	{
		return new RECT { left = c.IntArg(index), top = c.IntArg(index + 1), right = c.IntArg(index + 2), bottom = c.IntArg(index + 3) };
	}

	private static int[] Ints(params RECT[] rects)
	{
		return [.. rects.SelectMany(r => new[] { r.left, r.top, r.right, r.bottom })];
	}

	[Fact]
	public void find_out_size()
	{
		FunctionTest.Run("etc2", "find_out_size", (game, c) => game.GetDetectionSize(c.IntArg(0), c.IntArg(1)));
	}

	[Fact]
	public void pt_in_rect()
	{
		FunctionTest.Run("etc2", "pt_in_rect", (game, c) =>
		{
			var rect = RectArg(c, 0);
			return game.PointInRect(ref rect, c.IntArg(4), c.IntArg(5));
		});
	}

	[Fact]
	public void pt_in_rect2()
	{
		FunctionTest.Run("etc2", "pt_in_rect2", (game, c) =>
		{
			var rect = RectArg(c, 0);
			return game.PointInRect2(ref rect, c.IntArg(4), c.IntArg(5));
		});
	}

	[Fact]
	public void pt_in_rect3()
	{
		FunctionTest.Run("etc2", "pt_in_rect3", (game, c) =>
		{
			var rect = RectArg(c, 0);
			return game.PointInRect3(ref rect, c.IntArg(4), c.IntArg(5));
		});
	}

	[Fact]
	public void same_rect()
	{
		FunctionTest.Run("etc2", "same_rect", (game, c) =>
		{
			var dstn_rect = RectArg(c, 0);
			var src_rect = RectArg(c, 4);
			var field_rect = RectArg(c, 8);
			var result = game.ClipRects(ref dstn_rect, ref src_rect, ref field_rect);
			Assert.True(
				c.Outs.Select(o => o.GetInt32()).SequenceEqual(Ints(dstn_rect, src_rect, field_rect)),
				$"rects after the call: expected [{string.Join(", ", c.Outs)}], actual [{string.Join(", ", Ints(dstn_rect, src_rect, field_rect))}]");
			return result;
		});
	}

	[Fact]
	public void seek_parking_no()
	{
		FunctionTest.Run("etc2", "seek_parking_no", (game, c) => game.FindParkingNumber(c.IntArg(0)));
	}

	[Fact]
	public void plane_in_cv()
	{
		FunctionTest.Run("etc2", "plane_in_cv", (game, c) => game.CountPlanesIn(c.IntArg(0)));
	}

	[Fact]
	public void set_pos_of_parking()
	{
		FunctionTest.Run("etc2", "set_pos_of_parking", (game, c) =>
		{
			game.SetParkingPosition(c.IntArg(0));
			return null;
		});
	}

	[Fact]
	public void set_the_slct_unit()
	{
		FunctionTest.Run("etc2", "set_the_slct_unit", (game, c) =>
		{
			game.set_the_slct_unit(c.IntArg(0));
			return null;
		});
	}

	[Fact]
	public void cls_all_slct_unit()
	{
		FunctionTest.Run("etc2", "cls_all_slct_unit", (game, c) =>
		{
			game.ClearSelection();
			return null;
		});
	}

	[Fact]
	public void cls_all_slct_unit_p2()
	{
		FunctionTest.Run("etc2", "cls_all_slct_unit_p2", (game, c) =>
		{
			game.ClearSelection2(c.IntArg(0));
			return null;
		});
	}

	[Fact]
	public void seek_effect_no()
	{
		FunctionTest.Run("etc2", "seek_effect_no", (game, c) => game.FindFreeEffect());
	}

	[Fact]
	public void seek_fire_no()
	{
		FunctionTest.Run("etc2", "seek_fire_no", (game, c) => game.FindFreeFire());
	}

	[Fact]
	public void rtn_damage_pt()
	{
		FunctionTest.Run("etc2", "rtn_damage_pt", (game, c) => game.GetDamagePoints(c.IntArg(0)));
	}

	[Fact]
	public void drctn_for_8()
	{
		FunctionTest.Run("etc2", "drctn_for_8", (game, c) => game.ToEightDirections(c.IntArg(0)));
	}

	[Fact]
	public void set_frmtn_of_ships()
	{
		FunctionTest.Run("etc2", "set_frmtn_of_ships", (game, c) =>
		{
			game.SetShipFormation(c.IntArg(0));
			return null;
		});
	}

	[Fact]
	public void set_pos_of_take_down()
	{
		FunctionTest.Run("etc2", "set_pos_of_take_down", (game, c) =>
		{
			game.SetLandingDestination(c.IntArg(0));
			return null;
		});
	}

	[Fact]
	public void cont_pos_of_take_down()
	{
		FunctionTest.Run("etc2", "cont_pos_of_take_down", (game, c) =>
		{
			game.UpdateLanding(c.IntArg(0));
			return null;
		});
	}

	[Fact]
	public void find_out_ss()
	{
		FunctionTest.Run("etc2", "find_out_ss", (game, c) => game.DetectSubmarines(c.IntArg(0), c.IntArg(1)));
	}

	[Fact]
	public void find_out()
	{
		FunctionTest.Run("etc2", "find_out", (game, c) =>
		{
			game.Detect();
			return null;
		});
	}
}
