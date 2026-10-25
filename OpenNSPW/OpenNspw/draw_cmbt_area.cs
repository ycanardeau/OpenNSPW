using System.Runtime.CompilerServices;
//
//                                    **
//                                   *  *
//                                    **  *
//                                    **   *
//                                    **  *
//                            *     ******     *
//                            ***   * **     ***
//                              ****  **  ****
//                             **  ********
//
//                 Ｎａｖａｌ Ｓｏｕｔｈ Ｐａｃｉｆｉｃ Ｗａｒ
//                             Ｏｎ　ｔｈｅ　Ｎｅｔ

// Port of draw_cmbt_area.cpp.

using static OpenNspw.Nspw;

namespace OpenNspw;

public unsafe partial class Nspw
{
	// Where a position in the world is on the screen, in whole pixels: its offset from CameraPosition, with Y growing
	// downward, as `(int)(x-cmbt_x)` and `(int)(cmbt_y-y)` compute it.
	public PixelPoint WorldToScreen(WorldPosition position)
	{
		return new PixelPoint(
			(int)(position.X - CameraPosition.X),
			(int)(CameraPosition.Y - position.Y)
		);
	}

	// A left click on unit m, that counts (CanClickUnit): it selects the unit, or the selected unit's target, or, for m 0,
	// the land at `ground` where the selected transport lands.
	private void ClickUnit(int m, WorldPosition ground)
	{
		MoveOrders[1].Unit = 0;

		if (SelectedUnit == m)
		{
			SelectedUnit = 0;
			CombatMenuKind = 0;
			CombatMenuSelection = CombatMenuItem.None;
			ClearSelection2(1);
		}
		else
		{
			if (SelectedUnit == 0)
			{
				if (
					!(
						Units[m].Category == UnitCategory.Plane
						&& Units[m].PlaneState == UnitState.Parked
					)
				)
					PreviousSelectedUnit = (short)m;

				SelectUnit(m);
			}
			else if (!IsEditingMap)
			{
				if (m != 0)
				{
					// 陸地以外、普通の場合
					SelectOrders[1].IsSet = true;
					SelectOrders[1].SelectedUnit = SelectedUnit;
					SelectOrders[1].Unit = (short)m;
				}
				else
				{
					// 陸地指定
					SelectOrders[1].IsSet = true;
					SelectOrders[1].SelectedUnit = SelectedUnit;
					SelectOrders[1].Unit = 0;
					SelectOrders[1].GroundPosition = ground;
				}
			}
		}
	}

	internal void HandleUnitClick(
		ref Unit unit,
		ref RECT destination,
		ref int leftButton,
		int m,
		SpriteId sprite
	)
	{
		int right;
		int bottom;
		if ((Renderer.FrameCount % 2) != 0)
		{
			if (unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
			{
				right = SCRN_WIDTH - 1;
				bottom = SCRN_HEIGHT - 1;
			}
			else
			{
				right = CMBT_WIDTH - 1;
				bottom = CMBT_HEIGHT - 1;
			}
			var n = 15;
			Renderer.DrawLine4(
				destination.left + n,
				destination.top + n,
				destination.right - n,
				destination.top + n,
				right,
				bottom
			);
			Renderer.DrawLine4(
				destination.right - n,
				destination.top + n,
				destination.right - n,
				destination.bottom - n,
				right,
				bottom
			);
			Renderer.DrawLine4(
				destination.right - n,
				destination.bottom - n,
				destination.left + n,
				destination.bottom - n,
				right,
				bottom
			);
			Renderer.DrawLine4(
				destination.left + n,
				destination.bottom - n,
				destination.left + n,
				destination.top + n,
				right,
				bottom
			);
		}

		if (leftButton == 1)
		{
			leftButton = 0;
			ClickUnit(
				m,
				new WorldPosition(
					CameraPosition.X + Renderer.Sprites[sprite].Position.X,
					CameraPosition.Y - Renderer.Sprites[sprite].Position.Y
				)
			);
		}
	}

	// Whether the selected unit can be given a destination: it is the player's, it is not a parked plane that cannot take
	// off yet, and the map is not being edited.
	private bool CanSetDestination(ref Unit unit)
	{
		return unit.Side == LocalSide
			&&
			// 発進チェック
			!(
				unit.Category == UnitCategory.Plane
				&& unit.PlaneState == UnitState.Parked
				&& (
					Units[unit.Carrier].PlanesToLaunch != 0
					|| Units[unit.Carrier].LaunchLock != 0
					|| unit.ReloadTime > 0
					|| Units[unit.Carrier].IsSupplying
				)
			)
			&& !IsEditingMap;
	}

	// A left click on the battlefield, that gives the selected unit (CanSetDestination) a destination.
	private void SetDestination(WorldPosition destination)
	{
		MoveOrders[1].Unit = SelectedUnit;
		MoveOrders[1].Destination = destination;
	}

	// A right click while unit m is selected: nothing is selected any more.
	private void CancelSelection(int m)
	{
		SelectedUnit = 0;
		Selections[1][m] = 0;
		CombatMenuKind = 0;
		CombatMenuSelection = CombatMenuItem.None;
		ClearSelection2(1);

		BufferedMoveOrders[1].ClearsPath = 0;
	}

	internal void DrawPathAndSetDestination(
		ref Unit unit,
		SpriteId sprite,
		int leftButton,
		ref int rightButton,
		int m
	)
	{
		int n;
		if (unit.EmergencyFlags[0] != 0)
		{
			Renderer.DrawLine5(
				WorldToScreen(unit.Position),
				WorldToScreen(unit.EmergencyDestination),
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1,
				PALT_RED
			);
		}

		for (n = 0; !unit.IsPathEnd(n); n++)
		{
			//cl=(31<<7)|(0); // Ｇ 各値最大３１
			Renderer.DrawLine4(
				WorldToScreen(unit.GetPathPoint(n)) + new PixelPoint(-10, -10),
				WorldToScreen(unit.GetPathPoint(n)) + new PixelPoint(10, -10),
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
			Renderer.DrawLine4(
				WorldToScreen(unit.GetPathPoint(n)) + new PixelPoint(10, -10),
				WorldToScreen(unit.GetPathPoint(n)) + new PixelPoint(10, 10),
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
			Renderer.DrawLine4(
				WorldToScreen(unit.GetPathPoint(n)) + new PixelPoint(10, 10),
				WorldToScreen(unit.GetPathPoint(n)) + new PixelPoint(-10, 10),
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
			Renderer.DrawLine4(
				WorldToScreen(unit.GetPathPoint(n)) + new PixelPoint(-10, 10),
				WorldToScreen(unit.GetPathPoint(n)) + new PixelPoint(-10, -10),
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);

			if (n == 0)
			{
				if (unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
				{
					Renderer.DrawLine4(
						WorldToScreen(Units[unit.Carrier].Position),
						WorldToScreen(unit.GetPathPoint(n)),
						CMBT_WIDTH - 1,
						CMBT_HEIGHT - 1
					);
				}
				else
				{
					Renderer.DrawLine4(
						Renderer.Sprites[sprite].Position,
						WorldToScreen(unit.GetPathPoint(n)),
						CMBT_WIDTH - 1,
						CMBT_HEIGHT - 1
					);
				}
			}
			else
			{
				Renderer.DrawLine4(
					WorldToScreen(unit.GetPathPoint(n - 1)),
					WorldToScreen(unit.GetPathPoint(n)),
					CMBT_WIDTH - 1,
					CMBT_HEIGHT - 1
				);
			}
		}

		// 次の定点なるか までの線引き
		if (CursorPosition.x <= CMBT_WIDTH)
		{
			if (MoveOrders[1].ClearsPath != 0)
			{
				if (unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
					Renderer.DrawLine4(
						WorldToScreen(Units[unit.Carrier].Position),
						new PixelPoint(CursorPosition.x, CursorPosition.y),
						CMBT_WIDTH - 1,
						CMBT_HEIGHT - 1
					);
				else
					Renderer.DrawLine4(
						WorldToScreen(unit.Position),
						new PixelPoint(CursorPosition.x, CursorPosition.y),
						CMBT_WIDTH - 1,
						CMBT_HEIGHT - 1
					);
			}
			else
				Renderer.DrawLine4(
					WorldToScreen(unit.GetPathPoint(n - 1)),
					new PixelPoint(CursorPosition.x, CursorPosition.y),
					CMBT_WIDTH - 1,
					CMBT_HEIGHT - 1
				);
		}

		// 定点設定
		if (leftButton == 1 && CursorPosition.x < CMBT_WIDTH && CanSetDestination(ref unit))
		{
			SetDestination(
				new WorldPosition(
					CursorPosition.x + CameraPosition.X,
					CameraPosition.Y - CursorPosition.y
				)
			);
		}

		if (1 != 0)
		{
			if (rightButton == 1)
			{
				rightButton = 0;
				CancelSelection(m);
			}
		}
	}

	// Whether a left click on the unit counts, as it and the selected unit are: the conditions of DrawUnitMarks's click
	// that are not about where the cursor is. The unit is Units[0] for the land, where the selected transport lands.
	internal bool CanClickUnit(ref Unit unit)
	{
		return !(
				Units[SelectedUnit].Category == UnitCategory.Plane
				&& unit.Category == UnitCategory.Ship
				&& !(
					unit.Kind == UnitKind.Carrier
					|| unit.Kind == UnitKind.LightCarrier
					|| unit.Kind == UnitKind.AirBase
					|| Units[SelectedUnit].Side != unit.Side
				)
			)
			&& !(
				Units[SelectedUnit].Category == UnitCategory.Plane
				&& Units[SelectedUnit].PlaneState == UnitState.Parked
				&& unit.Category == UnitCategory.Plane
				&& unit.PlaneState == UnitState.Flying
			)
			&& !(
				unit.Category == UnitCategory.Plane
				&& unit.PlaneState == UnitState.Parked
				&& !unit.IsStopping
			)
			&& !(
				Units[SelectedUnit].Category == UnitCategory.Plane
				&& Units[SelectedUnit].PlaneState == UnitState.Parked
				&& !Units[SelectedUnit].IsStopping
			)
			&& !(
				Units[SelectedUnit].Category == UnitCategory.Plane
				&& Units[SelectedUnit].PlaneState == UnitState.Parked
				&& unit.Category == UnitCategory.Ship
			)
			&& !(
				Units[SelectedUnit].Category == UnitCategory.Ship
				&& unit.Category == UnitCategory.Plane
				&& unit.PlaneState == UnitState.Parked
			)
			&& !(SelectedUnit != 0 && Units[SelectedUnit].Side != LocalSide)
			&& !(
				SelectedUnit != 0
				&& Units[SelectedUnit].Category == UnitCategory.Plane
				&& unit.Weapon == FireKind.Maintenance
			)
			&& !(unit.Side != LocalSide && unit.Kind == UnitKind.Submarine && unit.IsSubmerged);
	}

	// Whether the selected transport can land on the land at point (x, y) of the battlefield; if so, the landing mark (the
	// sprite's position) is put on the land's tile.
	internal bool AimLanding(SpriteId sprite, int x, int y)
	{
		// カーソルの示す、マップチップの場所
		// BUG(NSPW NET): a cursor off the map gives a tile outside it, which NSPW NET reads out of bounds; the port throws
		// instead (docs/KnownIssues.md).
		var column = (int)(
			((x + 40 + (int)CameraPosition.X) - World.Left)
			/ Renderer.Sprites[SpriteId.MapTiles].Size.Width
		);
		var row = (int)(
			(World.Top - ((int)CameraPosition.Y - y - 40))
			/ Renderer.Sprites[SpriteId.MapTiles].Size.Height
		);
		if (
			MapTiles[row][column] == 9
			|| MapTiles[row][column] == 8
			|| MapTiles[row][column] == 7
			|| MapTiles[row][column] == 5
			|| MapTiles[row][column] == 2
		)
		{
			Renderer.Sprites[sprite].Position = new PixelPoint(
				(
					(
						(
							x
							+ 40
							+ (int)(CameraPosition.X - World.Left)
								% Renderer.Sprites[SpriteId.MapTiles].Size.Width
						) / 80
					) * 80
				)
					- (int)(CameraPosition.X - World.Left)
						% Renderer.Sprites[SpriteId.MapTiles].Size.Width,
				(
					(
						(
							y
							+ 40
							+ (int)(World.Top - CameraPosition.Y)
								% Renderer.Sprites[SpriteId.MapTiles].Size.Height
						) / 80
					) * 80
				)
					- (int)(World.Top - CameraPosition.Y)
						% Renderer.Sprites[SpriteId.MapTiles].Size.Height
			);
			return true;
		}

		return false;
	}

	// A right click that no selected unit took: the unit selected before is selected again, if nothing is selected and it
	// is still there.
	internal void RestoreSelection()
	{
		if (SelectedUnit == 0 && PreviousSelectedUnit != 0 && Units[PreviousSelectedUnit].IsUsed)
		{
			SelectUnit(PreviousSelectedUnit);
		}
	}
}

public unsafe partial class Renderer
{
	// Outlines an area of the world in the battle area: its top edge from left to right, then its right, bottom and left
	// edges, as the original draws its hit areas.
	public void DrawWorldRect(WorldRect area)
	{
		var topLeft = _game.WorldToScreen(new WorldPosition(area.Left, area.Top));
		var topRight = _game.WorldToScreen(new WorldPosition(area.Right, area.Top));
		var bottomRight = _game.WorldToScreen(new WorldPosition(area.Right, area.Bottom));
		var bottomLeft = _game.WorldToScreen(new WorldPosition(area.Left, area.Bottom));
		DrawLine4(topLeft, topRight, CMBT_WIDTH - 1, CMBT_HEIGHT - 1);
		DrawLine4(topRight, bottomRight, CMBT_WIDTH - 1, CMBT_HEIGHT - 1);
		DrawLine4(bottomRight, bottomLeft, CMBT_WIDTH - 1, CMBT_HEIGHT - 1);
		DrawLine4(bottomLeft, topLeft, CMBT_WIDTH - 1, CMBT_HEIGHT - 1);
	}

	//============================================================================
	//
	//----------------------------------------------------------------------------
	[Original("cont_upper_effect")]
	public void UpdateUpperEffects(RECT* field, RECT* infoArea)
	{
		RECT source,
			destination;
		int n,
			size =
				default /* C4701 */ /* C4701 */
		;

		//=========		 Ｕｐｐｅｒのエフェクト描画		=========//
		for (var m = 1; m < EFFECT_MAX; m++)
		{
			ref var effect = ref _game.Effects[m];
			if (effect.Layer == EffectLayer.Upper)
			{
				var isSeen = false;
				if (_game.Result != GameResult.None)
					isSeen = true;
				for (n = 1; n <= _game.MaxUnitId && !isSeen; n++)
				{
					if (
						_game.Units[n].IsUsed
						&& _game.Units[n].Side == _game.LocalSide
						&& _game.Units[n].PlaneState != UnitState.Parked
					)
					{
						// マイユニットからこのエフェクトが見えるか
						// 現地点からユニット地点への距離
						var eye = _game.EyeOf(ref _game.Units[n]); // 航空機の場合はちょっと前へ

						var distance = (eye - effect.Position).Length;

						switch (_game.Units[n].Kind)
						{
							case UnitKind.Battleship:
								size = BB1_SIGHT;
								break;
							case UnitKind.Cruiser:
								size = CA1_SIGHT;
								break;
							case UnitKind.Destroyer:
								size = DD1_SIGHT;
								break;
							case UnitKind.Submarine:
								size = SS1_SIGHT;
								break;
							case UnitKind.Carrier:
								size = CV1_SIGHT;
								break;
							case UnitKind.LightCarrier:
								size = CVL1_SIGHT;
								break;
							case UnitKind.Transport:
								size = TR1_SIGHT;
								break;
							case UnitKind.Fighter:
								size = FT1_SIGHT;
								break;
							case UnitKind.Attacker:
								size = AT1_SIGHT;
								break;
							case UnitKind.Bomber:
								size = BM1_SIGHT;
								break;
							case UnitKind.AirBase:
							case UnitKind.NavalBase:
								size = AP_SIGHT;
								break;
							case UnitKind.City:
								size = CT1_SIGHT;
								break;
							case UnitKind.Mine:
								size = MN1_SIGHT;
								break;
							case UnitKind.InfantryBase:
								size = GF1_SIGHT;
								break;
							case UnitKind.Pillboxes:
								size = GF2_SIGHT;
								break;
							case UnitKind.Fortress:
								size = GF3_SIGHT;
								break;
						}

						if (
							_game.Units[n].Kind >= UnitKind.AirBase
							&& _game.Units[n].Kind <= UnitKind.Fortress
							&& _game.Units[n].BuildTime != 0
						)
						{
							// 工事中は視界を制限
							size = FT1_SIGHT / 2;
						}

						// 1.22's ranges, none limited while a base is built (0x40C919).
						if (_game.Rules.HasSightRangesOf122)
							size = GetSightRangeAs122(_game.Units[n].Kind);

						if (distance <= size)
						{
							isSeen = true;
						}
					}
				}

				if (isSeen)
				{
					// マイユニットから見えるので表示
					switch (effect.Animation)
					{
						case 0: //
							Sprites[SpriteId.SubUnits].Frame = effect.SpriteNumber;
							Sprites[SpriteId.SubUnits].Position = _game.WorldToScreen(
								effect.Position
							);
							n = 1;
							break;
						case 1: // 対空機関砲弾がヒット
							Sprites[SpriteId.SubUnits].Frame =
								effect.SpriteNumber
								+ (Sprites[SpriteId.SubUnits].FramesPerRow * (effect.TimeLeft % 2));
							Sprites[SpriteId.SubUnits].Position = _game.WorldToScreen(
								effect.Position
							);
							n = 1;
							break;
						case 2: //	飛行機からの煙
							Sprites[SpriteId.SubUnits].Frame =
								effect.SpriteNumber
								+ (
									Sprites[SpriteId.SubUnits].FramesPerRow
									* (_game.EffectRandom(2))
								);
							Sprites[SpriteId.SubUnits].Position =
								_game.WorldToScreen(effect.Position)
								+ new PixelPoint(
									_game.EffectRandom(10) - 5,
									_game.EffectRandom(10) - 5
								);
							n = 1;
							break;
						case 3: //	駐機場の飛行機用
							Sprites[SpriteId.SubUnits].Frame = effect.SpriteNumber;
							Sprites[SpriteId.SubUnits].Position = effect.Position.ToPixelPoint();
							n = 0;
							break;
						case 4: // 雷跡
							Sprites[SpriteId.SubUnits].Frame =
								effect.SpriteNumber
								+ (
									Sprites[SpriteId.SubUnits].FramesPerRow
									* (_game.EffectRandom(2))
								);
							Sprites[SpriteId.SubUnits].Position = _game.WorldToScreen(
								effect.Position
							);
							n = 1;
							break;
						case 10: // 対空機関砲
							DrawLine5(
								_game.WorldToScreen(effect.Position),
								_game.WorldToScreen(effect.EndPosition),
								CMBT_WIDTH - 1,
								CMBT_HEIGHT - 1,
								255
							);
							DrawLine5(
								_game.WorldToScreen(effect.Position) + new PixelPoint(1, 0),
								_game.WorldToScreen(effect.EndPosition) + new PixelPoint(1, 0),
								CMBT_WIDTH - 1,
								CMBT_HEIGHT - 1,
								255
							);
							DrawLine5(
								_game.WorldToScreen(effect.Position) + new PixelPoint(0, -1),
								_game.WorldToScreen(effect.EndPosition) + new PixelPoint(0, -1),
								CMBT_WIDTH - 1,
								CMBT_HEIGHT - 1,
								255
							);
							continue;
						case 11: // 弾丸
							DrawLine5(
								_game.WorldToScreen(effect.Position),
								_game.WorldToScreen(effect.EndPosition),
								CMBT_WIDTH - 1,
								CMBT_HEIGHT - 1,
								255
							);
							continue;
					}
					// src_rect は ソースサーフェスのレクタングルです。
					source.left =
						Sprites[SpriteId.SubUnits].SheetPosition.X
						+ (
							Sprites[SpriteId.SubUnits].Size.Width
							* (
								Sprites[SpriteId.SubUnits].Frame
								% Sprites[SpriteId.SubUnits].FramesPerRow
							)
						)
						+ 1;
					source.top =
						Sprites[SpriteId.SubUnits].SheetPosition.Y
						+ (
							Sprites[SpriteId.SubUnits].Size.Height
							* (
								Sprites[SpriteId.SubUnits].Frame
								/ Sprites[SpriteId.SubUnits].FramesPerRow
							)
						)
						+ 1;
					source.right = (source.left + Sprites[SpriteId.SubUnits].Size.Width) - 1;
					source.bottom = (source.top + Sprites[SpriteId.SubUnits].Size.Height) - 1;
					// dstn_rect は ディスティネーションレクタングルです。
					destination.left =
						Sprites[SpriteId.SubUnits].Position.X - Sprites[SpriteId.SubUnits].Center.X;
					destination.top =
						Sprites[SpriteId.SubUnits].Position.Y - Sprites[SpriteId.SubUnits].Center.Y;
					destination.right =
						destination.left + Sprites[SpriteId.SubUnits].Size.Width - 1;
					destination.bottom =
						destination.top + Sprites[SpriteId.SubUnits].Size.Height - 1;

					if (n != 0)
					{
						if (_game.ClipRects(ref destination, ref source, ref *field))
						{
							if (
								DDERR_SURFACELOST
								== IDirectDrawSurface_BltFast(
									lpDDSBack,
									destination.left,
									destination.top,
									OffscreenSurface,
									&source,
									DDBLTFAST_SRCCOLORKEY
								)
							)
							{
								RestoreSurfaces();
							}
						}
					}
					else
					{
						if (_game.ClipRects(ref destination, ref source, ref *infoArea))
						{
							if (
								DDERR_SURFACELOST
								== IDirectDrawSurface_BltFast(
									lpDDSBack,
									destination.left,
									destination.top,
									OffscreenSurface,
									&source,
									DDBLTFAST_SRCCOLORKEY
								)
							)
							{
								RestoreSurfaces();
							}
						}
					}
				}
			}
		}
	}

	//============================================================================
	//
	//----------------------------------------------------------------------------
	[Original("cont_lower_effect")]
	public void UpdateLowerEffects(RECT* field, RECT* infoArea)
	{
		RECT source,
			destination;
		int n =
				default /* C4701 */
			,
			size =
				default /* C4701 */
			,
			isSeen;

		//=========		 Ｌｏｗｅｒのエフェクト描画		=========//
		for (var m = 1; m < EFFECT_MAX; m++)
		{
			ref var effect = ref _game.Effects[m];
			if (effect.Layer == EffectLayer.Lower)
			{
				if (effect.Animation != 3 || _game.Result != GameResult.None)
				{
					// ユニットインフォ画面以外のエフェクト表示は見える見えないのテストをします。
					// 1.22 still tests them once the battle has a result (0x40CDEC).
					isSeen = 0;
					if (_game.Result != GameResult.None && _game.Rules.ResultShowsEveryLowerEffect)
						isSeen = 1;
					for (n = 1; n <= _game.MaxUnitId; n++)
					{
						ref var unit = ref _game.Units[n];
						if (
							unit.IsUsed
							&& unit.Side == _game.LocalSide
							&& unit.PlaneState != UnitState.Parked
						)
						{
							// マイユニットからこのエフェクトが見えるか
							// 現地点からユニット地点への距離
							var eye = _game.EyeOf(ref unit); // 航空機の場合はちょっと前へ

							var distance = (eye - effect.Position).Length;

							switch (unit.Kind)
							{
								case UnitKind.Battleship:
									size = BB1_SIGHT;
									break;
								case UnitKind.Cruiser:
									size = CA1_SIGHT;
									break;
								case UnitKind.Destroyer:
									size = DD1_SIGHT;
									break;
								case UnitKind.Submarine:
									size = SS1_SIGHT;
									break;
								case UnitKind.Carrier:
									size = CV1_SIGHT;
									break;
								case UnitKind.LightCarrier:
									size = CVL1_SIGHT;
									break;
								case UnitKind.Transport:
									size = TR1_SIGHT;
									break;
								case UnitKind.Fighter:
									size = FT1_SIGHT;
									break;
								case UnitKind.Attacker:
									size = AT1_SIGHT;
									break;
								case UnitKind.Bomber:
									size = BM1_SIGHT;
									break;
								case UnitKind.AirBase:
								case UnitKind.NavalBase:
									size = AP_SIGHT;
									break;
								case UnitKind.City:
									size = CT1_SIGHT;
									break;
								case UnitKind.Mine:
									size = MN1_SIGHT;
									break;
								case UnitKind.InfantryBase:
									size = GF1_SIGHT;
									break;
								case UnitKind.Pillboxes:
									size = GF2_SIGHT;
									break;
								case UnitKind.Fortress:
									size = GF3_SIGHT;
									break;
							}

							if (
								unit.Kind >= UnitKind.AirBase
								&& unit.Kind <= UnitKind.Fortress
								&& unit.BuildTime != 0
							)
							{
								// 工事中は視界を制限
								size = FT1_SIGHT / 2;
							}

							// 1.22's ranges, none limited while a base is built (0x40CF46).
							if (_game.Rules.HasSightRangesOf122)
								size = GetSightRangeAs122(unit.Kind);

							if (distance <= size)
							{
								isSeen = 1;
							}
						}
					}
				}
				else
				{
					// ユニットインフォ内のエフェクトは必ず表示します。
					isSeen = 1;
				}

				if (isSeen != 0)
				{
					// マイユニットからの見える
					switch (effect.Animation)
					{
						case 0: //
							Sprites[SpriteId.SubUnits].Frame = effect.SpriteNumber;
							Sprites[SpriteId.SubUnits].Position = _game.WorldToScreen(
								effect.Position
							);
							n = 1;
							break;
						case 1: // 対空機関砲弾がヒット
							Sprites[SpriteId.SubUnits].Frame =
								effect.SpriteNumber
								+ (Sprites[SpriteId.SubUnits].FramesPerRow * (effect.TimeLeft % 2));
							Sprites[SpriteId.SubUnits].Position = _game.WorldToScreen(
								effect.Position
							);
							n = 1;
							break;
						case 2: //	飛行機からの煙
							Sprites[SpriteId.SubUnits].Frame =
								effect.SpriteNumber
								+ (
									Sprites[SpriteId.SubUnits].FramesPerRow
									* (_game.EffectRandom(2))
								);
							Sprites[SpriteId.SubUnits].Position =
								_game.WorldToScreen(effect.Position)
								+ new PixelPoint(
									_game.EffectRandom(10) - 5,
									_game.EffectRandom(10) - 5
								);
							n = 1;
							break;
						case 3: //	駐機場の飛行機用
							Sprites[SpriteId.SubUnits].Frame = effect.SpriteNumber;
							Sprites[SpriteId.SubUnits].Position = effect.Position.ToPixelPoint();
							n = 0;
							break;
						case 4: // 雷跡、航跡
							Sprites[SpriteId.SubUnits].Frame =
								effect.SpriteNumber
								+ (
									Sprites[SpriteId.SubUnits].FramesPerRow
									* (_game.EffectRandom(2))
								);
							Sprites[SpriteId.SubUnits].Position = _game.WorldToScreen(
								effect.Position
							);
							n = 1;
							break;

						case 12: // 矩形
							DrawLine5(
								(int)(
									effect.Position.X
									- effect.EndPosition.X
									- _game.CameraPosition.X
								),
								(int)(
									_game.CameraPosition.Y
									- effect.Position.Y
									- effect.EndPosition.Y
								),
								(int)(
									effect.Position.X
									+ effect.EndPosition.X
									- _game.CameraPosition.X
								),
								(int)(
									_game.CameraPosition.Y
									- effect.Position.Y
									- effect.EndPosition.Y
								),
								CMBT_WIDTH - 1,
								CMBT_HEIGHT - 1,
								255
							);
							DrawLine5(
								(int)(
									effect.Position.X
									+ effect.EndPosition.X
									- _game.CameraPosition.X
								),
								(int)(
									_game.CameraPosition.Y
									- effect.Position.Y
									- effect.EndPosition.Y
								),
								(int)(
									effect.Position.X
									+ effect.EndPosition.X
									- _game.CameraPosition.X
								),
								(int)(
									_game.CameraPosition.Y
									- effect.Position.Y
									+ effect.EndPosition.Y
								),
								CMBT_WIDTH - 1,
								CMBT_HEIGHT - 1,
								255
							);
							DrawLine5(
								(int)(
									effect.Position.X
									- effect.EndPosition.X
									- _game.CameraPosition.X
								),
								(int)(
									_game.CameraPosition.Y
									- effect.Position.Y
									+ effect.EndPosition.Y
								),
								(int)(
									effect.Position.X
									+ effect.EndPosition.X
									- _game.CameraPosition.X
								),
								(int)(
									_game.CameraPosition.Y
									- effect.Position.Y
									+ effect.EndPosition.Y
								),
								CMBT_WIDTH - 1,
								CMBT_HEIGHT - 1,
								255
							);
							DrawLine5(
								(int)(
									effect.Position.X
									- effect.EndPosition.X
									- _game.CameraPosition.X
								),
								(int)(
									_game.CameraPosition.Y
									- effect.Position.Y
									+ effect.EndPosition.Y
								),
								(int)(
									effect.Position.X
									- effect.EndPosition.X
									- _game.CameraPosition.X
								),
								(int)(
									_game.CameraPosition.Y
									- effect.Position.Y
									- effect.EndPosition.Y
								),
								CMBT_WIDTH - 1,
								CMBT_HEIGHT - 1,
								255
							);
							continue;
					}
					// src_rect は ソースサーフェスのレクタングルです。
					source.left =
						Sprites[SpriteId.SubUnits].SheetPosition.X
						+ (
							Sprites[SpriteId.SubUnits].Size.Width
							* (
								Sprites[SpriteId.SubUnits].Frame
								% Sprites[SpriteId.SubUnits].FramesPerRow
							)
						)
						+ 1;
					source.top =
						Sprites[SpriteId.SubUnits].SheetPosition.Y
						+ (
							Sprites[SpriteId.SubUnits].Size.Height
							* (
								Sprites[SpriteId.SubUnits].Frame
								/ Sprites[SpriteId.SubUnits].FramesPerRow
							)
						)
						+ 1;
					source.right = (source.left + Sprites[SpriteId.SubUnits].Size.Width) - 1;
					source.bottom = (source.top + Sprites[SpriteId.SubUnits].Size.Height) - 1;
					// dstn_rect は ディスティネーションレクタングルです。
					destination.left =
						Sprites[SpriteId.SubUnits].Position.X - Sprites[SpriteId.SubUnits].Center.X;
					destination.top =
						Sprites[SpriteId.SubUnits].Position.Y - Sprites[SpriteId.SubUnits].Center.Y;
					destination.right =
						destination.left + Sprites[SpriteId.SubUnits].Size.Width - 1;
					destination.bottom =
						destination.top + Sprites[SpriteId.SubUnits].Size.Height - 1;

					if (n != 0)
					{
						if (_game.ClipRects(ref destination, ref source, ref *field))
						{
							if (
								DDERR_SURFACELOST
								== IDirectDrawSurface_BltFast(
									lpDDSBack,
									destination.left,
									destination.top,
									OffscreenSurface,
									&source,
									DDBLTFAST_SRCCOLORKEY
								)
							)
							{
								RestoreSurfaces();
							}
						}
					}
					else
					{
						if (_game.ClipRects(ref destination, ref source, ref *infoArea))
						{
							if (
								DDERR_SURFACELOST
								== IDirectDrawSurface_BltFast(
									lpDDSBack,
									destination.left,
									destination.top,
									OffscreenSurface,
									&source,
									DDBLTFAST_SRCCOLORKEY
								)
							)
							{
								RestoreSurfaces();
							}
						}
					}
				}
			}
		}
	}

	//============================================================================
	//
	//----------------------------------------------------------------------------
	[Original("draw_cloud")]
	public void DrawClouds(RECT* field)
	{
		RECT source,
			destination;

		for (var n = 0; n < KUMO_MAX; n++)
		{
			ref var cloud = ref _game.Clouds[n];
			if (cloud.Used != 0)
			{
				Sprites[SpriteId.MapTiles].Position = _game.WorldToScreen(cloud.Position);
				Sprites[SpriteId.MapTiles].Frame = 2;
				// src_rect は ソースサーフェスのレクタングルです。
				source.left =
					Sprites[SpriteId.MapTiles].SheetPosition.X
					+ (
						Sprites[SpriteId.MapTiles].Size.Width
						* (
							Sprites[SpriteId.MapTiles].Frame
							% Sprites[SpriteId.MapTiles].FramesPerRow
						)
					);
				source.top =
					Sprites[SpriteId.MapTiles].SheetPosition.Y
					+ (
						Sprites[SpriteId.MapTiles].Size.Height
						* (
							Sprites[SpriteId.MapTiles].Frame
							/ Sprites[SpriteId.MapTiles].FramesPerRow
						)
					);
				source.right = (source.left + Sprites[SpriteId.MapTiles].Size.Width);
				source.bottom = (source.top + Sprites[SpriteId.MapTiles].Size.Height);

				// dstn_rect は ディスティネーションレクタングルです。
				destination.left =
					Sprites[SpriteId.MapTiles].Position.X
					- (Sprites[SpriteId.MapTiles].Size.Width / 2);
				destination.top =
					Sprites[SpriteId.MapTiles].Position.Y
					- (Sprites[SpriteId.MapTiles].Size.Height / 2);
				destination.right = destination.left + Sprites[SpriteId.MapTiles].Size.Width;
				destination.bottom = destination.top + Sprites[SpriteId.MapTiles].Size.Height;

				if (_game.ClipRects(ref destination, ref source, ref *field))
				{
					if (
						DDERR_SURFACELOST
						== IDirectDrawSurface_BltFast(
							lpDDSBack,
							destination.left,
							destination.top,
							OffscreenSurface,
							&source,
							DDBLTFAST_SRCCOLORKEY
						)
					)
					{
						RestoreSurfaces();
					}
				}
			}
		}
	}

	//============================================================================
	//
	//----------------------------------------------------------------------------
	[Original("be_dstryd")]
	public void DrawDestruction(int m)
	{
		if (!(_game.Units[m].Kind >= UnitKind.AirBase && _game.Units[m].Kind <= UnitKind.Fortress))
		{
			// 沈没の水門
			var f = _game.FindFreeEffect();
			ref var effect = ref _game.Effects[f];
			effect.Layer = EffectLayer.Lower;
			effect.TimeLeft = 220;
			effect.Animation = 0;
			effect.Position = _game.Units[m].Position;
			effect.SpriteNumber = 7; // ソースファイル上の番号
		}

		if (
			_game.Units[m].Kind == UnitKind.Battleship
			|| _game.Units[m].Kind == UnitKind.Carrier
			|| _game.Units[m].Kind == UnitKind.LightCarrier
			|| _game.Units[m].Kind == UnitKind.NavalBase
			|| _game.Units[m].Kind == UnitKind.AirBase
			|| _game.Units[m].Kind == UnitKind.Fortress
		)
		{
			// 大型艦船
			// 沈没の小水紋
			if (
				!(
					_game.Units[m].Kind >= UnitKind.AirBase
					&& _game.Units[m].Kind <= UnitKind.Fortress
				)
			)
			{
				for (var i = 0; i <= 7; i++)
				{
					var f = _game.FindFreeEffect();
					ref var effect = ref _game.Effects[f];
					effect.Layer = EffectLayer.Lower;
					effect.TimeLeft = 200 + _game.Random(20);
					effect.Animation = 4;
					effect.Position = new WorldPosition(
						_game.Units[m].Position.X + _game.Random(100) - 50,
						_game.Units[m].Position.Y + _game.Random(100) - 50
					);
					effect.SpriteNumber = 8; // ソースファイル上の番号
				}
			}
			// 沈没の煙
			for (var i = 0; i <= 2; i++)
			{
				var f = _game.FindFreeEffect();
				ref var effect = ref _game.Effects[f];
				effect.Layer = EffectLayer.Upper;
				effect.TimeLeft = 150 + _game.Random(20);
				effect.Animation = 4;
				effect.Position = new WorldPosition(
					_game.Units[m].Position.X + _game.Random(30) - 15,
					_game.Units[m].Position.Y + _game.Random(30) - 15
				);
				effect.SpriteNumber = 5 + _game.Random(2); // ソースファイル上の番号
			}
			// 沈没の爆炎
			for (var i = 0; i <= 4; i++)
			{
				var f = _game.FindFreeEffect();
				ref var effect = ref _game.Effects[f];
				effect.Layer = EffectLayer.Upper;
				effect.TimeLeft = 30 + _game.Random(20);
				effect.Animation = 4;
				effect.Position = new WorldPosition(
					_game.Units[m].Position.X + _game.Random(40) - 20,
					_game.Units[m].Position.Y + _game.Random(40) - 20
				);
				effect.SpriteNumber = 9; // ソースファイル上の番号
			}
			// 沈没の小爆炎
			for (var i = 0; i <= 4; i++)
			{
				var f = _game.FindFreeEffect();
				ref var effect = ref _game.Effects[f];
				effect.Layer = EffectLayer.Upper;
				effect.TimeLeft = 40 + _game.Random(20);
				effect.Animation = 4;
				effect.Position = new WorldPosition(
					_game.Units[m].Position.X + _game.Random(60) - 30,
					_game.Units[m].Position.Y + _game.Random(60) - 30
				);
				effect.SpriteNumber = 10; // ソースファイル上の番号
			}
		}
		else
		{
			// 中小型艦船
			// 沈没の小水紋
			if (
				!(
					_game.Units[m].Kind >= UnitKind.AirBase
					&& _game.Units[m].Kind <= UnitKind.Fortress
				)
			)
			{
				for (var i = 0; i <= 3; i++)
				{
					var f = _game.FindFreeEffect();
					ref var effect = ref _game.Effects[f];
					effect.Layer = EffectLayer.Lower;
					effect.TimeLeft = 200 + _game.Random(20);
					effect.Animation = 4;
					effect.Position = new WorldPosition(
						_game.Units[m].Position.X + _game.Random(100) - 50,
						_game.Units[m].Position.Y + _game.Random(100) - 50
					);
					effect.SpriteNumber = 8; // ソースファイル上の番号
				}
			}
			// 沈没の煙
			for (var i = 0; i <= 1; i++)
			{
				var f = _game.FindFreeEffect();
				ref var effect = ref _game.Effects[f];
				effect.Layer = EffectLayer.Upper;
				effect.TimeLeft = 150 + _game.Random(20);
				effect.Animation = 4;
				effect.Position = new WorldPosition(
					_game.Units[m].Position.X + _game.Random(30) - 15,
					_game.Units[m].Position.Y + _game.Random(30) - 15
				);
				effect.SpriteNumber = 5 + _game.Random(2); // ソースファイル上の番号
			}
			// 沈没の爆炎
			for (var i = 0; i <= 0; i++)
			{
				var f = _game.FindFreeEffect();
				ref var effect = ref _game.Effects[f];
				effect.Layer = EffectLayer.Upper;
				effect.TimeLeft = 30 + _game.Random(20);
				effect.Animation = 4;
				effect.Position = new WorldPosition(
					_game.Units[m].Position.X + _game.Random(40) - 20,
					_game.Units[m].Position.Y + _game.Random(40) - 20
				);
				effect.SpriteNumber = 9; // ソースファイル上の番号
			}
			// 沈没の小爆炎
			for (var i = 0; i <= 1; i++)
			{
				var f = _game.FindFreeEffect();
				ref var effect = ref _game.Effects[f];
				effect.Layer = EffectLayer.Upper;
				effect.TimeLeft = 20 + _game.Random(20);
				effect.Animation = 4;
				effect.Position = new WorldPosition(
					_game.Units[m].Position.X + _game.Random(20) - 10,
					_game.Units[m].Position.Y + _game.Random(20) - 10
				);
				effect.SpriteNumber = 10; // ソースファイル上の番号
			}
		}
	}

	private void DrawMapTiles(
		int firstRow,
		int firstColumn,
		int editedRow,
		int editedColumn,
		ref Array256<byte> keys,
		ref RECT source,
		ref RECT field
	)
	{
		bool isSeen;
		RECT destination;
		for (var m = 0; m <= 10; m++)
		{
			for (var n = 0; n <= 10; n++)
			{
				if (
					!_game.IsEditingMap
					&& _game.Result == GameResult.None
					&& _game.MapTiles[firstRow + n][firstColumn + m] == 0
				)
				{
					isSeen = false;
					for (var f = 1; f <= _game.MaxUnitId; f++)
					{
						if (
							_game.Units[f].IsUsed
							&& _game.Units[f].Side == _game.LocalSide
							&& _game.Units[f].PlaneState != UnitState.Parked
						)
						{
							// 現地点からユニット地点への距離
							var tile = new WorldPosition(
								((firstColumn + m) * Sprites[SpriteId.MapTiles].Size.Width)
									- World.Right,
								World.Top
									- ((firstRow + n) * Sprites[SpriteId.MapTiles].Size.Height)
							);

							var eye = _game.EyeOf(ref _game.Units[f]); // 戦闘機場合、視点を

							//((cm_scrn_x+m)*sprt[MAP_TIP_NRML].wd)-World.Right

							var distance = (eye - tile).Length;
							if (distance <= (double)_game.GetDetectionSize(f, 0))
							{
								isSeen = true;
								break;
							}
						}
					}
				}
				else
				{
					isSeen = true;
				}

				Sprites[SpriteId.MapTiles].Position = new PixelPoint(
					m * Sprites[SpriteId.MapTiles].Size.Width
						- (int)(_game.CameraPosition.X - World.Left)
							% Sprites[SpriteId.MapTiles].Size.Width,
					n * Sprites[SpriteId.MapTiles].Size.Height
						- (int)(World.Top - _game.CameraPosition.Y)
							% Sprites[SpriteId.MapTiles].Size.Height
				);

				if (_game.MapTiles[firstRow + n][firstColumn + m] == 0)
				{
					if (isSeen)
					{
						// 見える範囲内の海
						Sprites[SpriteId.MapTiles].Frame = (FrameCount / 30) % 2; // ただの海
					}
					else
					{
						//見えない範囲内の海
						Sprites[SpriteId.MapTiles].Frame = 3; // ただの海
						continue;
					}
				}

				if (_game.MapTiles[firstRow + n][firstColumn + m] >= 1)
				{
					Sprites[SpriteId.MapTiles].Frame =
						6 + (_game.MapTiles[firstRow + n][firstColumn + m] - 1); // 陸地
				}

				// マップエディット時のプログ
				if (
					_game.IsEditingMap
					&& (firstRow + n) == (editedRow)
					&& (firstColumn + m) == (editedColumn)
				)
				{
					if ((FrameCount % 2) != 0)
						Sprites[SpriteId.MapTiles].Frame = 3;
					_game.GetKeyboardState(keys);

					if ((keys[VK_NUMPAD1] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 1;
					}
					if ((keys[VK_NUMPAD2] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 2;
					}
					if ((keys[VK_NUMPAD3] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 3;
					}
					if ((keys[VK_NUMPAD4] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 4;
					}
					if ((keys[VK_NUMPAD5] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 5;
					}
					if ((keys[VK_NUMPAD6] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 6;
					}
					if ((keys[VK_NUMPAD7] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 7;
					}
					if ((keys[VK_NUMPAD8] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 8;
					}
					if ((keys[VK_NUMPAD9] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 9;
					}
					if ((keys[VK_NUMPAD0] & 0x80) != 0)
					{
						_game.MapTiles[firstRow + n][firstColumn + m] = 0;
					}
				}

				// src_rect は ソースサーフェスのレクタングルです。
				source.left =
					Sprites[SpriteId.MapTiles].SheetPosition.X
					+ (
						Sprites[SpriteId.MapTiles].Size.Width
						* (
							Sprites[SpriteId.MapTiles].Frame
							% Sprites[SpriteId.MapTiles].FramesPerRow
						)
					);
				source.top =
					Sprites[SpriteId.MapTiles].SheetPosition.Y
					+ (
						Sprites[SpriteId.MapTiles].Size.Height
						* (
							Sprites[SpriteId.MapTiles].Frame
							/ Sprites[SpriteId.MapTiles].FramesPerRow
						)
					);
				source.right = (source.left + Sprites[SpriteId.MapTiles].Size.Width);
				source.bottom = (source.top + Sprites[SpriteId.MapTiles].Size.Height);

				// dstn_rect は ディスティネーションレクタングルです。
				destination.left =
					Sprites[SpriteId.MapTiles].Position.X - Sprites[SpriteId.MapTiles].Center.X;
				destination.top =
					Sprites[SpriteId.MapTiles].Position.Y - Sprites[SpriteId.MapTiles].Center.Y;
				destination.right = destination.left + Sprites[SpriteId.MapTiles].Size.Width;
				destination.bottom = destination.top + Sprites[SpriteId.MapTiles].Size.Height;

				if (_game.ClipRects(ref destination, ref source, ref field))
				{
					if (
						DDERR_SURFACELOST
						== IDirectDrawSurface_BltFast(
							lpDDSBack,
							destination.left,
							destination.top,
							OffscreenSurface,
							(RECT*)Unsafe.AsPointer(ref source),
							0
						)
					)
					{
						RestoreSurfaces();
					}
				}
			}
		}
	}

	private void DrawSelectedUnit(ref RECT destination, int right, int bottom)
	{
		var n = 10;
		DrawLine4(
			destination.left + n,
			destination.top + n,
			destination.right - n,
			destination.top + n,
			right,
			bottom
		);
		DrawLine4(
			destination.right - n,
			destination.top + n,
			destination.right - n,
			destination.bottom - n,
			right,
			bottom
		);
		DrawLine4(
			destination.right - n,
			destination.bottom - n,
			destination.left + n,
			destination.bottom - n,
			right,
			bottom
		);
		DrawLine4(
			destination.left + n,
			destination.bottom - n,
			destination.left + n,
			destination.top + n,
			right,
			bottom
		);
	}

	private void DrawLandingPoint(SpriteId sprite)
	{
		RECT mark;
		var n = 10 + (FrameCount % 8) * 2;

		mark.left = (int)(_game.Units[_game.SelectedUnit].LandingX - _game.CameraPosition.X - 40);
		mark.top = (int)(_game.CameraPosition.Y - _game.Units[_game.SelectedUnit].LandingY - 40);
		mark.right = mark.left + Sprites[sprite].Size.Width - 2;
		mark.bottom = mark.top + Sprites[sprite].Size.Height - 2;

		DrawLine4(
			mark.left + n,
			mark.top + n,
			mark.right - n,
			mark.top + n,
			CMBT_WIDTH - 1,
			CMBT_HEIGHT - 1
		);
		DrawLine4(
			mark.right - n,
			mark.top + n,
			mark.right - n,
			mark.bottom - n,
			CMBT_WIDTH - 1,
			CMBT_HEIGHT - 1
		);
		DrawLine4(
			mark.right - n,
			mark.bottom - n,
			mark.left + n,
			mark.bottom - n,
			CMBT_WIDTH - 1,
			CMBT_HEIGHT - 1
		);
		DrawLine4(
			mark.left + n,
			mark.bottom - n,
			mark.left + n,
			mark.top + n,
			CMBT_WIDTH - 1,
			CMBT_HEIGHT - 1
		);
	}

	private void DrawTargetLine(ref Unit unit)
	{
		WorldPosition target;
		if (
			0 != 0
			&& _game.Units[unit.Target].Kind == UnitKind.Submarine
			&& _game.Units[unit.Target].IsSubmerged
		)
		{ // 対潜水艦
			target = _game.Units[unit.Target].ContactPosition;
		}
		else
		{ // 対潜航潜水艦以外
			if (unit.Kind != UnitKind.Transport)
			{
				target = _game.Units[unit.Target].Position;
			}
			else
			{
				// 揚陸方向
				target = unit.LandingPoint;
			}
		}

		if (
			(
				_game.Units[unit.Target].IsFound
				|| (unit.Target == _game.MaxUnitId + 1 && unit.Kind == UnitKind.Transport)
			)
			&& !(
				_game.Units[unit.Target].Kind == UnitKind.Submarine
				&& _game.Units[unit.Target].IsSubmerged
			)
		)
		{ // 視認
			var bearing = (target - unit.Position).NonZero().Direction;

			var tip = unit.Position + WorldVector.FromDirection(bearing) * 40;

			var barb = bearing;
			barb -= 10;
			barb = barb.WrapNegative();
			DrawLine4(
				_game.WorldToScreen(tip),
				_game.WorldToScreen(unit.Position + WorldVector.FromDirection(barb) * 20),
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);

			barb = bearing;
			barb -= 350;
			barb = barb.WrapNegative();
			DrawLine4(
				_game.WorldToScreen(tip),
				_game.WorldToScreen(unit.Position + WorldVector.FromDirection(barb) * 20),
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
		}
		else
		{ // 視認不可
			var from = new WorldPosition(unit.Position.X + 10 + 20, unit.Position.Y + 40);
			var to = new WorldPosition(unit.Position.X - 10 + 20, unit.Position.Y + 20);
			DrawLine4(
				_game.WorldToScreen(from),
				_game.WorldToScreen(to),
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
			from = new WorldPosition(unit.Position.X - 10 + 20, unit.Position.Y + 40);
			to = new WorldPosition(unit.Position.X + 10 + 20, unit.Position.Y + 20);
			DrawLine4(
				_game.WorldToScreen(from),
				_game.WorldToScreen(to),
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
		}
	}

	// Whether a parked plane is shown, on the flight deck or in the hangar of the carrier or the air base whose information
	// the sidebar shows, as the view is switched (UnitInfoPanel[1]).
	internal bool IsOnShownDeck(ref Unit unit)
	{
		return unit.Carrier == _game.UnitInfoPanel[3]
			&& (
				(UnitKind)_game.UnitInfoPanel[0] == UnitKind.Carrier
				|| (UnitKind)_game.UnitInfoPanel[0] == UnitKind.LightCarrier
				|| (UnitKind)_game.UnitInfoPanel[0] == UnitKind.AirBase
			)
			&& (
				(
					(unit.Mode <= UnitMode.Slow)
						&& (_game.UnitInfoPanel[1] == 1 && unit.DeckPhase <= 2)
					|| (_game.UnitInfoPanel[1] == 0 && unit.DeckPhase >= 3)
				) || (unit.Mode == UnitMode.Return && (_game.UnitInfoPanel[1] == 0))
			);
	}

	private bool DrawUnitMarks(
		bool isPositioned,
		ref Unit unit,
		SpriteId sprite,
		ref int leftButton,
		int m,
		ref int rightButton
	)
	{
		RECT destination;
		RECT clickArea;
		int right;
		int bottom;
		if (!isPositioned)
		{
			//				sprite=UNIT_JPN;		//Off Screen Number		日本海軍の表示
			//				sprite=UNIT_USA;		//Off Screen Number		合衆国海軍の表示

			//The Slct された機体への移動予定の線引き、
			if (unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
			{
				if (IsOnShownDeck(ref unit))
				{
					// 空母で飛行甲板か格納庫かで航空機を表示するかしない。
					Sprites[sprite].Position = unit.Position.ToPixelPoint();
				}
				else
					return false;
			}
			else
			{
				if (
					unit.Side != _game.LocalSide
					&& unit.Kind == UnitKind.Submarine
					&& unit.IsSubmerged
				)
				{ // およその敵潜航潜水艦
					Sprites[sprite].Position = _game.WorldToScreen(unit.ContactPosition);
				}
				else
				{ // マップ上のユニット
					Sprites[sprite].Position = _game.WorldToScreen(unit.Position);
				}
			}
		}

		// dca1:
		destination.left = Sprites[sprite].Position.X - Sprites[sprite].Center.X;
		destination.top = Sprites[sprite].Position.Y - Sprites[sprite].Center.Y;
		destination.right = destination.left + Sprites[sprite].Size.Width - 2;
		destination.bottom = destination.top + Sprites[sprite].Size.Height - 2;

		clickArea.left = destination.left + 20;
		clickArea.top = destination.top + 20;
		clickArea.right = destination.right - 20;
		clickArea.bottom = destination.bottom - 20;

		// クリック選択・非選択
		if (
			_game.PointInRect(ref clickArea, _game.CursorPosition.x, _game.CursorPosition.y)
			&& _game.CanClickUnit(ref unit)
			&& !(unit.PlaneState != UnitState.Parked && _game.CursorPosition.x >= CMBT_WIDTH - 1)
		)
		{
			_game.HandleUnitClick(ref unit, ref destination, ref leftButton, m, sprite);
		}

		if (m == 0)
			return false;

		// 選択されてればマークの絵というか枠
		if (unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
		{
			right = SCRN_WIDTH - 1;
			bottom = SCRN_HEIGHT - 1;
		}
		else
		{
			right = CMBT_WIDTH - 1;
			bottom = CMBT_HEIGHT - 1;
		}
		if (_game.SelectedUnit == m && m != 0)
		{
			DrawSelectedUnit(ref destination, right, bottom);
		}
		else
		{
			if (_game.Selections[1][m] != 0)
			{
				var n = 20;
				DrawLine4(
					destination.left + n,
					destination.top + n,
					destination.right - n,
					destination.top + n,
					right,
					bottom
				);
				DrawLine4(
					destination.right - n,
					destination.top + n,
					destination.right - n,
					destination.bottom - n,
					right,
					bottom
				);
				DrawLine4(
					destination.right - n,
					destination.bottom - n,
					destination.left + n,
					destination.bottom - n,
					right,
					bottom
				);
				DrawLine4(
					destination.left + n,
					destination.bottom - n,
					destination.left + n,
					destination.top + n,
					right,
					bottom
				);
			}
		}

		if (
			_game.SelectedUnit != 0
			&& _game.Units[_game.SelectedUnit].Kind == UnitKind.Transport
			&& _game.Units[_game.SelectedUnit].Target == _game.MaxUnitId + 1
			&& m == _game.SelectedUnit
		)
		{
			// 輸送船の揚陸先のマーク
			DrawLandingPoint(sprite);
		}
		else if (_game.SelectedUnit != 0 && _game.Units[_game.SelectedUnit].Target == m)
		{
			// 攻撃先 としてのマーク
			var n = 10 + (FrameCount % 8) * 2;
			DrawLine4(
				destination.left + n,
				destination.top + n,
				destination.right - n,
				destination.top + n,
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
			DrawLine4(
				destination.right - n,
				destination.top + n,
				destination.right - n,
				destination.bottom - n,
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
			DrawLine4(
				destination.right - n,
				destination.bottom - n,
				destination.left + n,
				destination.bottom - n,
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
			DrawLine4(
				destination.left + n,
				destination.bottom - n,
				destination.left + n,
				destination.top + n,
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
		}
		else if (
			_game.SelectedUnit != 0
			&& _game.Units[_game.SelectedUnit].Carrier == m
			&& _game.Units[_game.SelectedUnit].Category == UnitCategory.Plane
			&& _game.Units[_game.SelectedUnit].PlaneState == UnitState.Flying
		)
		{
			// 攻撃先 としてのマーク
			var n = 15;
			DrawLine4(
				destination.right - n - 4,
				destination.bottom - n,
				destination.left + n + 4,
				destination.bottom - n,
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
			DrawLine4(
				destination.right - n - 4,
				destination.bottom - n + 2,
				destination.left + n + 4,
				destination.bottom - n + 2,
				CMBT_WIDTH - 1,
				CMBT_HEIGHT - 1
			);
		}

		// 攻撃先 着艦先 の方向
		if (unit.Target != 0 && unit.Side == _game.LocalSide)
		{
			DrawTargetLine(ref unit);
		}

		// 決定された進路線ひき
		if (_game.SelectedUnit == m && !_game.IsEditingMap)
		{
			// 緊急移動先までの線
			_game.DrawPathAndSetDestination(ref unit, sprite, leftButton, ref rightButton, m);
		}
		return true;
	}

	private void DrawUnit(
		ref Unit unit,
		SpriteId sprite,
		ref RECT source,
		ref RECT infoArea,
		ref RECT field,
		ref int playsPlaneSound
	)
	{
		RECT destination;
		unit.SpriteColumn = SpriteColumnOf(unit.Direction);

		if (unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
		{
			if (
				unit.Carrier == _game.UnitInfoPanel[3]
				&& (
					(UnitKind)_game.UnitInfoPanel[0] == UnitKind.Carrier
					|| (UnitKind)_game.UnitInfoPanel[0] == UnitKind.LightCarrier
					|| (UnitKind)_game.UnitInfoPanel[0] == UnitKind.AirBase
				)
			)
			{
				if (
					(
						(unit.Mode <= UnitMode.Slow)
							&& (_game.UnitInfoPanel[1] == 1 && unit.DeckPhase <= 2)
						|| (_game.UnitInfoPanel[1] == 0 && unit.DeckPhase >= 3)
					) || (unit.Mode == UnitMode.Return && (_game.UnitInfoPanel[1] == 0))
				)
				{
					// 駐機中のの飛行機
					Sprites[sprite].Frame = (unit.SpriteRow * 8) + unit.SpriteColumn;

					Sprites[sprite].Position = unit.Position.ToPixelPoint();

					// src_rect は ソースサーフェスのレクタングルです。
					source.left =
						Sprites[sprite].SheetPosition.X
						+ (
							Sprites[sprite].Size.Width
							* (Sprites[sprite].Frame % Sprites[sprite].FramesPerRow)
						)
						+ 1;
					source.top =
						Sprites[sprite].SheetPosition.Y
						+ (
							Sprites[sprite].Size.Height
							* (Sprites[sprite].Frame / Sprites[sprite].FramesPerRow)
						)
						+ 1;
					source.right = (source.left + Sprites[sprite].Size.Width) - 2;
					source.bottom = (source.top + Sprites[sprite].Size.Height) - 2;

					// dstn_rect は ディスティネーションレクタングルです。

					destination.left = Sprites[sprite].Position.X - Sprites[sprite].Center.X;
					destination.top = Sprites[sprite].Position.Y - Sprites[sprite].Center.Y;
					destination.right = destination.left + Sprites[sprite].Size.Width - 2;
					destination.bottom = destination.top + Sprites[sprite].Size.Height - 2;

					if (_game.ClipRects(ref destination, ref source, ref infoArea))
					{
						if (
							DDERR_SURFACELOST
							== IDirectDrawSurface_BltFast(
								lpDDSBack,
								destination.left,
								destination.top,
								OffscreenSurface,
								(RECT*)Unsafe.AsPointer(ref source),
								DDBLTFAST_SRCCOLORKEY
							)
						)
						{
							RestoreSurfaces();
						}
					}
				}
			}
		}
		else
		{
			Sprites[sprite].Position = _game.WorldToScreen(unit.Position);

			if (
				unit.Kind == UnitKind.Submarine
				&& unit.IsSubmerged
				&& _game.Result == GameResult.None
			)
			{ // 潜航潜水艦
				Sprites[sprite].Frame = ((unit.SpriteRow + 1) * 8) + unit.SpriteColumn;
				if (unit.Side != _game.LocalSide)
					Sprites[sprite].Position = new PixelPoint(-999, Sprites[sprite].Position.Y); // それが敵潜水艦なら表示を外す
			}
			else
			{
				Sprites[sprite].Frame = (unit.SpriteRow * 8) + unit.SpriteColumn;
			}

			// src_rect は ソースサーフェスのレクタングルです。
			source.left =
				Sprites[sprite].SheetPosition.X
				+ (
					Sprites[sprite].Size.Width
					* (Sprites[sprite].Frame % Sprites[sprite].FramesPerRow)
				)
				+ 1;
			source.top =
				Sprites[sprite].SheetPosition.Y
				+ (
					Sprites[sprite].Size.Height
					* (Sprites[sprite].Frame / Sprites[sprite].FramesPerRow)
				)
				+ 1;
			source.right = (source.left + Sprites[sprite].Size.Width) - 2;
			source.bottom = (source.top + Sprites[sprite].Size.Height) - 2;

			// dstn_rect は ディスティネーションレクタングルです。
			destination.left = Sprites[sprite].Position.X - Sprites[sprite].Center.X;
			destination.top = Sprites[sprite].Position.Y - Sprites[sprite].Center.Y;
			destination.right = destination.left + Sprites[sprite].Size.Width - 2;
			destination.bottom = destination.top + Sprites[sprite].Size.Height - 2;
			if (_game.ClipRects(ref destination, ref source, ref field))
			{
				if (
					DDERR_SURFACELOST
					== IDirectDrawSurface_BltFast(
						lpDDSBack,
						destination.left,
						destination.top,
						OffscreenSurface,
						(RECT*)Unsafe.AsPointer(ref source),
						DDBLTFAST_SRCCOLORKEY
					)
				)
				{
					RestoreSurfaces();
				}

				if (
					!_game.IsEditingMap
					&& playsPlaneSound == 0
					&& _game.Result == GameResult.None
					&& unit.Category == UnitCategory.Plane
					&& unit.PlaneState == UnitState.Flying
					&& (FrameCount % 10) == 0
				)
				{
					_game.Sound.PlaySoundEffect(SoundId.PlaneFlying, unit.Position);
					playsPlaneSound = 1;
				}
			}
		}
	}

	// The sheet a unit is drawn from: its side's. 1.22 also draws the Americans' fighters of type 1 in scenario 303, the
	// ones taken from Japan's air bases, as Japan's (0x40E070).
	public SpriteId GetUnitSprites(ref Unit unit)
	{
		if (
			unit.Side == Side.Japan
			|| (
				_game.Rules.DrawsCapturedFightersAsJapans
				&& _game.ScenarioNumber == 303
				&& unit.Side == Side.UnitedStates
				&& unit.Kind == UnitKind.Fighter
				&& unit.Variant == 1
			)
		)
			return SpriteId.JapanUnits; //Off Screen Number		日本海軍の表示
		return SpriteId.UnitedStatesUnits; //Off Screen Number		合衆国海軍の表示
	}

	//============================================================================
	//コンバットエリア描画 同時にユーザー(通信対戦時はホスト)入力を受け付けます
	//----------------------------------------------------------------------------
	[Original("draw_cmbt_area")]
	public void DrawBattleArea()
	{
		RECT source = default,
			field,
			infoArea;

		Array256<byte> keys = default;

		var playsPlaneSound = 0;

		// field_rect は 戦域画面のレクタングルです。
		field.left = 0;
		field.top = 0;
		field.right = CMBT_WIDTH;
		field.bottom = CMBT_HEIGHT;

		// info_rect は インフォのレクタングルです

		infoArea.left = Sprites[SpriteId.JapanUnitInfo].Position.X;
		infoArea.top = Sprites[SpriteId.JapanUnitInfo].Position.Y;
		infoArea.right = infoArea.left + Sprites[SpriteId.JapanUnitInfo].Size.Width;
		infoArea.bottom = infoArea.top + Sprites[SpriteId.JapanUnitInfo].Size.Height;

		// カーソルの示す、マップチップの場所
		var editedColumn = (int)(
			((_game.CursorPosition.x + 40 + (int)_game.CameraPosition.X) - World.Left)
			/ Sprites[SpriteId.MapTiles].Size.Width
		);
		var editedRow = (int)(
			(World.Top - ((int)_game.CameraPosition.Y - _game.CursorPosition.y - 40))
			/ Sprites[SpriteId.MapTiles].Size.Height
		);

		// 標準キャラよう背景の表示
		var firstColumn = (int)(
			(_game.CameraPosition.X - World.Left) / Sprites[SpriteId.MapTiles].Size.Width
		);
		var firstRow = (int)(
			(World.Top - _game.CameraPosition.Y) / Sprites[SpriteId.MapTiles].Size.Height
		);

		DrawMapTiles(
			firstRow,
			firstColumn,
			editedRow,
			editedColumn,
			ref keys,
			ref source,
			ref field
		);

		// ロウエフェクト
		UpdateLowerEffects(&field, &infoArea);

		// 自サイドユニットからの距離により、可視不可視

		_game.MoveOrders[0].Unit = 0;
		_game.SelectOrders[0].IsSet = false;

		_game.MoveOrders[1].Unit = 0;
		_game.SelectOrders[1].IsSet = false;

		// 標準キャラの表示
		int rightButton = _game.RightButton;
		int leftButton = _game.LeftButton;

		if (_game.HasOrdered && _game.Result == GameResult.None)
		{
			rightButton = 0;
			leftButton = 0;
		}

		if (rightButton == 1 || leftButton == 1)
			_game.Sound.PlayInterfaceSound(SoundId.Click1);

		// The host's UI's commands for clicks on the battlefield (UICommands.cs): taken here, as the clicks are, and dropped
		// as they are while an order waits for its turn; the units brushed are clicked, one a frame.
		_game.RunUICommands(
			UICommandSite.Battlefield,
			!(_game.HasOrdered && _game.Result == GameResult.None)
		);
		_game.RunBrushedUnits(!(_game.HasOrdered && _game.Result == GameResult.None));

		for (var m = 0; m <= _game.MaxUnitId; m++)
		{
			ref var unit = ref _game.Units[m];
			if (m == _game.JPN_PLANE_START)
			{
				// 雲を描画します。
				DrawClouds(&field);
			}

			var sprite = GetUnitSprites(ref unit);

			// ユニットを描画します
			if (
				unit.IsUsed
				&& (unit.Side == _game.LocalSide || unit.IsFound)
				&& (
					(
						(
							_game.CameraPosition.X - CMBT_REST <= unit.Position.X
							&& _game.CameraPosition.X + CMBT_WIDTH + CMBT_REST >= unit.Position.X
						)
						&& (
							_game.CameraPosition.Y + CMBT_REST >= unit.Position.Y
							&& _game.CameraPosition.Y - CMBT_HEIGHT - CMBT_REST <= unit.Position.Y
						)
					)
					|| (unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
				)
			)
			{
				DrawUnit(
					ref unit,
					sprite,
					ref source,
					ref infoArea,
					ref field,
					ref playsPlaneSound
				);
			}

			var isPositioned = false;
			if (
				m == 0
				&& _game.SelectedUnit != 0
				&& _game.Units[_game.SelectedUnit].Kind == UnitKind.Transport
				&& m != _game.SelectedUnit
			)
			{
				// カーソルのある場所が
				if (_game.AimLanding(sprite, _game.CursorPosition.x, _game.CursorPosition.y))
				{
					isPositioned = true;
				}
			}

			if (isPositioned || unit.IsUsed && (unit.Side == _game.LocalSide || unit.IsFound))
			{
				if (
					!DrawUnitMarks(
						isPositioned,
						ref unit,
						sprite,
						ref leftButton,
						m,
						ref rightButton
					)
				)
					continue;
			}
		}

		if (rightButton == 1)
		{
			_game.RestoreSelection();
		}

		// アッパーエフェクト
		UpdateUpperEffects(&field, &infoArea);
	}
}
