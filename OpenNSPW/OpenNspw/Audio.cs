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

// Port of Audio.cpp.

using System.Collections.Immutable;

namespace OpenNspw;

public unsafe partial class Nspw
{

/*--------------------------------------------
	ダイレクトミュージックの初期化とロードの指示
--------------------------------------------*/
[Original("InitDMusic")]
public int	InitializeDirectMusic()
	{

	// DirectMusicLoader8 オブジェクトと DirectMusicPerformance8 オブジェクトの作成
	if (FAILED(CoCreateInstance(CLSID_DirectMusicLoader,null,CLSCTX_INPROC,IID_IDirectMusicLoader8,out lpDML)))
		{
		// 失敗
		printf("DirectMusicLoader8 オブジェクトの作成に失敗しました。\n");
		return FALSE;
		}
	if (FAILED(CoCreateInstance(CLSID_DirectMusicPerformance,null,CLSCTX_INPROC,IID_IDirectMusicPerformance8,out lpDMP)))
		{
		// 失敗
		RELEASE(ref lpDML);
		printf("DirectMusicPerformance8 オブジェクトの作成に失敗しました。\n");
		return FALSE;
		}
	// パフォーマンスを初期化
	if (FAILED(lpDMP.InitAudio(null,null,null,DMUS_APATH_SHARED_STEREOPLUSREVERB,64,DMUS_AUDIOF_ALL,null)))
		{
		// 失敗
		RELEASE(ref lpDMP);
		RELEASE(ref lpDML);
		printf("パフォーマンスの初期化に失敗しました。\n");
		return FALSE;
		}

	return TRUE;
	}

// The names of the sounds' files, WAV\<name>.wav, by SoundId.
private static readonly ImmutableArray<string> SoundFileNames = ["AA_BLT1", "AA_BLT2", "AA_BLT3", "AA_BLT4", "AA_SHL1", "AA_SHL2", "AA_SHL3", "AA_SHL4", "AA_SHL5", "TPD_HIT1", "TPD_HIT2", "BOM_HIT1", "BOM_HIT2", "SHIP_SINK1", "SHIP_SINK2", "SPL1", "SEA1", "GUN1", "GUN2", "GUN3", "FALL1", "BOMB_OFF", "BB_BOMB", "TPD_LOS", "PLANE_FLYING", "PLANE1", "PLANE2", "TAKE_OFF", "SNR", "CLICK1", "CLICK2"];

/*--------------------------------------------
	ダイレクトサウンドの初期化とロードの指示
--------------------------------------------*/
[Original("InitDSound")]
public int	InitializeDirectSound()
	{
	int		i;
	int		sound;

	// DirectSound8 の作成
	if (FAILED(DirectSoundCreate8(null,out lpDS,null)))
		{
		MessageBox(null,"DirectSound オブジェクトの作成に失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		return FALSE;
		}

	// DirectSound の協調レベルを設定
	if (FAILED(lpDS.SetCooperativeLevel(hwndApp,DSSCL_PRIORITY)))
		{
		MessageBox(null,"DirectSound の協調レベルの設定に失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		// 閉じる
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}

	// プライマリ サウンドバッファ作成
	DSBUFFERDESC desc;
	ZeroMemory(&desc,(nuint)(sizeof(DSBUFFERDESC)));
	desc.dwSize = (uint)(sizeof(DSBUFFERDESC));
	desc.dwFlags = /*DSBCAPS_CTRLVOLUME |*/ DSBCAPS_PRIMARYBUFFER;
	desc.dwBufferBytes=0;
	desc.lpwfxFormat=null;
	if (FAILED(lpDS.CreateSoundBuffer(&desc,out lpDSP,null)))
		{
		MessageBox(null,"プライマリサウンドバッファの作成に失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		// 閉じる
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}

// サウンドカードのバッファ性能を調べます。

	for(sound=0;sound<SoundFileNames.Length;sound++)
		{
		lpDSB_[sound][0]= LoadWave($"WAV\\{SoundFileNames[sound]}.wav");
		if (lpDSB_[sound][0]==null)
			{
			MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
			SendMessage(hwndApp,WM_CLOSE,0,0);
			return FALSE;
			}
		for(i=1;i<SND_DUP;i++)
			lpDS.DuplicateSoundBuffer(lpDSB_[sound][0],out lpDSB_[sound][i]);
		}

	return TRUE;
	}

/*-------------------------------------------
	主にゲーム中の効果音を鳴らす
---------------------------------------------*/
[Original("play_snd")]
public void	PlaySound(IDirectSoundBuffer? the_lpdsb,short* the_snd,int f)
	{

	the_lpdsb.Stop();		//
	the_lpdsb.SetCurrentPosition(0);	// 巻き戻し

	if( f<-9600 )
		f=-9600;

	the_lpdsb.SetVolume( f );
	the_lpdsb.Play(0,0,0);		//

	(*the_snd)++;
	*the_snd=(short)((*the_snd)%SND_DUP);

	}

/*-------------------------------------------
	主にゲーム中の効果音を鳴らす
---------------------------------------------*/
[Original("SoundPlayEffect")]
public void PlaySoundEffect(int dwFlags,SoundId no,double x,double y)
	{
	RECT	field_rect;
	double	wrk_x,wrk_y,drctn,dstc;
	int		n,size=default /* C4701 */,flg;

#if !SND_SW

return;

#endif

	if( x!=(double)(MAP_RIGHT+1) )
		{
		flg=0;
		for(n=1;n<=MaxUnitId && flg==0 ;n++)
			{
			if( Units[n].IsUsed && Units[n].Side==LocalSide && Units[n].PlaneState!=UnitState.Parked )
				{
				// マイユニットからこのエフェクトが見えるか
				// 現地点からユニット地点への距離

				wrk_x=Units[n].Position.X;
				wrk_y=Units[n].Position.Y;
				if( Units[n].Kind==UnitKind.Fighter )
					{	// 航空機の場合はちょっと前へ
					wrk_x+=CosDegrees(Units[n].Direction)*FT_EYE;
					wrk_y+=SinDegrees(Units[n].Direction)*FT_EYE;
					}

				wrk_x=wrk_x-x;
				wrk_y=wrk_y-y;

				dstc=Distance(wrk_x, wrk_y);

				switch( Units[n].Kind )
					{
					case UnitKind.Battleship:		size=BB1_SIGHT;		break;
					case UnitKind.Cruiser:		size=CA1_SIGHT;		break;
					case UnitKind.Destroyer:		size=DD1_SIGHT;		break;
					case UnitKind.Submarine:		size=SS1_SIGHT;		break;
					case UnitKind.Carrier:		size=CV1_SIGHT;		break;
					case UnitKind.LightCarrier:		size=CVL1_SIGHT;	break;
					case UnitKind.Transport:		size=TR1_SIGHT;		break;
					case UnitKind.Fighter:		size=FT1_SIGHT;		break;
					case UnitKind.Attacker:		size=AT1_SIGHT;		break;
					case UnitKind.Bomber:		size=BM1_SIGHT;		break;
					case UnitKind.AirBase: case UnitKind.NavalBase:		size=AP_SIGHT;		break;
					case UnitKind.City:		size=CT1_SIGHT;		break;
					case UnitKind.Mine:		size=MN1_SIGHT;		break;
					case UnitKind.InfantryBase:		size=GF1_SIGHT;		break;
					case UnitKind.Pillboxes:		size=GF2_SIGHT;		break;
					case UnitKind.Fortress:		size=GF3_SIGHT;		break;
					}

				if( dstc<=size )
					{
					flg=1;
					}
				}
			}

		if(flg==0)
			{
			// マイユニットが聞けない位置
			return;
			}

		// ptin dbg
		field_rect.left=-500;
		field_rect.top=CMBT_HEIGHT+500;
		field_rect.right=CMBT_WIDTH+500;
		field_rect.bottom=-500;

		x=(int)(x-CameraPosition.X);
		y=(int)(CameraPosition.Y-y);
		if( PointInRect3(ref field_rect, (int)x,(int)y )==0 )
			return;
		}

	if(1!=0)
		{

		lpDSB_[(int)no][NextSoundBuffers[(int)no]].Stop();		//
		lpDSB_[(int)no][NextSoundBuffers[(int)no]].SetCurrentPosition(0);	// 巻き戻し

		lpDSB_[(int)no][NextSoundBuffers[(int)no]].SetVolume( 0 );
		lpDSB_[(int)no][NextSoundBuffers[(int)no]].Play(0,0,0);		//

		NextSoundBuffers[(int)no]++;
		NextSoundBuffers[(int)no]=(short)(NextSoundBuffers[(int)no]%SND_DUP);

		}

	}
}
