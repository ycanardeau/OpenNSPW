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

	for(var sound=0;sound<SoundFileNames.Length;sound++)
		{
		SoundBuffers[sound][0]= LoadWave($"WAV\\{SoundFileNames[sound]}.wav");
		if (SoundBuffers[sound][0]==null)
			{
			MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
			SendMessage(hwndApp,WM_CLOSE,0,0);
			return FALSE;
			}
		for(var i=1;i<SND_DUP;i++)
			lpDS.DuplicateSoundBuffer(SoundBuffers[sound][0],out SoundBuffers[sound][i]);
		}

	return TRUE;
	}

/*-------------------------------------------
	主にゲーム中の効果音を鳴らす
---------------------------------------------*/
[Original("play_snd")]
public void	PlaySound(IDirectSoundBuffer? buffer,short* nextBuffer,int volume)
	{

	buffer.Stop();		//
	buffer.SetCurrentPosition(0);	// 巻き戻し

	if( volume<-9600 )
		volume=-9600;

	buffer.SetVolume( volume );
	buffer.Play(0,0,0);		//

	(*nextBuffer)++;
	*nextBuffer=(short)((*nextBuffer)%SND_DUP);

	}

/*-------------------------------------------
	主にゲーム中の効果音を鳴らす
---------------------------------------------*/
[Original("SoundPlayEffect")]
public void PlaySoundEffect(int flags,SoundId sound,double x,double y)
	{
	RECT	hearingArea;
	int size=default /* C4701 */;

#if !SND_SW

return;

#endif

	if( x!=(double)(MAP_RIGHT+1) )
		{
		var isHeard=0;
		for(var n=1;n<=MaxUnitId && isHeard==0 ;n++)
			{
			if( Units[n].IsUsed && Units[n].Side==LocalSide && Units[n].PlaneState!=UnitState.Parked )
				{
				// マイユニットからこのエフェクトが見えるか
				// 現地点からユニット地点への距離

				var eye=EyeOf(ref Units[n]);	// 航空機の場合はちょっと前へ

				var distance=(eye-new WorldPosition(x, y)).Length;

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

				if( distance<=size )
					{
					isHeard=1;
					}
				}
			}

		if(isHeard==0)
			{
			// マイユニットが聞けない位置
			return;
			}

		// ptin dbg
		hearingArea.left=-500;
		hearingArea.top=CMBT_HEIGHT+500;
		hearingArea.right=CMBT_WIDTH+500;
		hearingArea.bottom=-500;

		x=(int)(x-CameraPosition.X);
		y=(int)(CameraPosition.Y-y);
		if( PointInRect3(ref hearingArea, (int)x,(int)y )==0 )
			return;
		}

	if(1!=0)
		{

		SoundBuffers[(int)sound][NextSoundBuffers[(int)sound]].Stop();		//
		SoundBuffers[(int)sound][NextSoundBuffers[(int)sound]].SetCurrentPosition(0);	// 巻き戻し

		SoundBuffers[(int)sound][NextSoundBuffers[(int)sound]].SetVolume( 0 );
		SoundBuffers[(int)sound][NextSoundBuffers[(int)sound]].Play(0,0,0);		//

		NextSoundBuffers[(int)sound]++;
		NextSoundBuffers[(int)sound]=(short)(NextSoundBuffers[(int)sound]%SND_DUP);

		}

	}
}
