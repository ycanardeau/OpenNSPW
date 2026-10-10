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


//#include "all_head.h"
//#include "all_extern.h"
//#include	"all_forward.h"

// Port of Audio.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{






/*--------------------------------------------
	ダイレクトミュージックの初期化とロードの指示
--------------------------------------------*/
public int	InitDMusic()
	{
//	int		i;



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

//	char dir[MAX_PATH];
//	WCHAR tmp[MAX_PATH];


#if false
	for(i=0; i<2; i++)
		{
		// カレントディレクトリを取得
		GetCurrentDirectory(MAX_PATH,dir);
		// UNICODE へ変換
		MultiByteToWideChar(CP_ACP,0,dir,-1,tmp,MAX_PATH);
		// 検索ディレクトリを設定
		lpDML->SetSearchDirectory(GUID_DirectMusicAllTypes,tmp,FALSE);

		// 読み込む MIDI のファイル名 (UNICODEに変換)
		switch( i )
			{
			case 0:
				MultiByteToWideChar( CP_ACP, 0, "ttl.mid", -1, tmp, MAX_PATH );
				break;

			case 1:
				MultiByteToWideChar( CP_ACP, 0, "crs1.mid", -1, tmp, MAX_PATH);
				break;
			}


		// MIDI の読み込み
		if (FAILED(lpDML->LoadObjectFromFile(CLSID_DirectMusicSegment,IID_IDirectMusicSegment8,tmp,(void **)&lpSeg[i])))
			{
			// 失敗
			RELEASE(lpDMP);
			RELEASE(lpDML);
			printf("MIDI の読み込みに失敗しました。\n");
			return FALSE;
			}

		// パフォーマンスにダウンロード
		lpSeg[i]->Download(lpDMP);

		// ボリューム設定
		long	nVolume=-700;
		lpDMP->SetGlobalParam( GUID_PerfMasterVolume, (void *)&nVolume,sizeof(long));

		// 再生区間設定
		lpSeg[i]->SetLoopPoints( 0,0);
		lpSeg[i]->SetRepeats( DMUS_SEG_REPEAT_INFINITE );
		}
#endif

	return TRUE;
	}




/*--------------------------------------------
	ダイレクトサウンドの初期化とロードの指示
--------------------------------------------*/
public int	InitDSound()
	{
	int		i;



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

	//
	lpDSB_[AA_BLT1][0]= LoadWave("WAV\\AA_BLT1.wav");
	if (lpDSB_[AA_BLT1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[AA_BLT1][0],out lpDSB_[AA_BLT1][i]);


	lpDSB_[AA_BLT2][0]= LoadWave("WAV\\AA_BLT2.wav");
	if (lpDSB_[AA_BLT2][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[AA_BLT2][0],out lpDSB_[AA_BLT2][i]);


	lpDSB_[AA_BLT3][0]= LoadWave("WAV\\AA_BLT3.wav");
	if (lpDSB_[AA_BLT3][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[AA_BLT3][0],out lpDSB_[AA_BLT3][i]);


	lpDSB_[AA_BLT4][0]= LoadWave("WAV\\AA_BLT4.wav");
	if (lpDSB_[AA_BLT4][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[AA_BLT4][0],out lpDSB_[AA_BLT4][i]);


	//
	lpDSB_[AA_SHL1][0]= LoadWave("WAV\\AA_SHL1.wav");
	if (lpDSB_[AA_SHL1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[AA_SHL1][0],out lpDSB_[AA_SHL1][i]);

	lpDSB_[AA_SHL2][0]= LoadWave("WAV\\AA_SHL2.wav");
	if (lpDSB_[AA_SHL2][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[AA_SHL2][0],out lpDSB_[AA_SHL2][i]);

	lpDSB_[AA_SHL3][0]= LoadWave("WAV\\AA_SHL3.wav");
	if (lpDSB_[AA_SHL3][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[AA_SHL3][0],out lpDSB_[AA_SHL3][i]);

	lpDSB_[AA_SHL4][0]= LoadWave("WAV\\AA_SHL4.wav");
	if (lpDSB_[AA_SHL4][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[AA_SHL4][0],out lpDSB_[AA_SHL4][i]);

	lpDSB_[AA_SHL5][0]= LoadWave("WAV\\AA_SHL5.wav");
	if (lpDSB_[AA_SHL5][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[AA_SHL5][0],out lpDSB_[AA_SHL5][i]);

	//
	lpDSB_[TPD_HIT1][0]= LoadWave("WAV\\TPD_HIT1.wav");
	if (lpDSB_[TPD_HIT1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[TPD_HIT1][0],out lpDSB_[TPD_HIT1][i]);

	lpDSB_[TPD_HIT2][0]= LoadWave("WAV\\TPD_HIT2.wav");
	if (lpDSB_[TPD_HIT2][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[TPD_HIT2][0],out lpDSB_[TPD_HIT2][i]);

	//
	lpDSB_[BOM_HIT1][0]= LoadWave("WAV\\BOM_HIT1.wav");
	if (lpDSB_[BOM_HIT1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[BOM_HIT1][0],out lpDSB_[BOM_HIT1][i]);

	lpDSB_[BOM_HIT2][0]= LoadWave("WAV\\BOM_HIT2.wav");
	if (lpDSB_[BOM_HIT2][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[BOM_HIT2][0],out lpDSB_[BOM_HIT2][i]);

	//
	lpDSB_[SHIP_SINK1][0]= LoadWave("WAV\\SHIP_SINK1.wav");
	if (lpDSB_[SHIP_SINK1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[SHIP_SINK1][0],out lpDSB_[SHIP_SINK1][i]);

	lpDSB_[SHIP_SINK2][0]= LoadWave("WAV\\SHIP_SINK2.wav");
	if (lpDSB_[SHIP_SINK2][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[SHIP_SINK2][0],out lpDSB_[SHIP_SINK2][i]);

	//
	lpDSB_[SPL1][0]= LoadWave("WAV\\SPL1.wav");
	if (lpDSB_[SPL1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[SPL1][0],out lpDSB_[SPL1][i]);

	//
	lpDSB_[SEA1][0]= LoadWave("WAV\\SEA1.wav");
	if (lpDSB_[SEA1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[SEA1][0],out lpDSB_[SEA1][i]);

	//
	lpDSB_[GUN1][0]= LoadWave("WAV\\GUN1.wav");
	if (lpDSB_[GUN1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[GUN1][0],out lpDSB_[GUN1][i]);

	lpDSB_[GUN2][0]= LoadWave("WAV\\GUN2.wav");
	if (lpDSB_[GUN2][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[GUN2][0],out lpDSB_[GUN2][i]);

	lpDSB_[GUN3][0]= LoadWave("WAV\\GUN3.wav");
	if (lpDSB_[GUN3][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[GUN3][0],out lpDSB_[GUN3][i]);

	//
	lpDSB_[FALL1][0]= LoadWave("WAV\\FALL1.wav");
	if (lpDSB_[FALL1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[FALL1][0],out lpDSB_[FALL1][i]);

	lpDSB_[BOMB_OFF][0]= LoadWave("WAV\\BOMB_OFF.wav");
	if (lpDSB_[BOMB_OFF][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[BOMB_OFF][0],out lpDSB_[BOMB_OFF][i]);

	lpDSB_[BB_BOMB][0]= LoadWave("WAV\\BB_BOMB.wav");
	if (lpDSB_[BB_BOMB][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[BB_BOMB][0],out lpDSB_[BB_BOMB][i]);

	lpDSB_[TPD_LOS][0]= LoadWave("WAV\\TPD_LOS.wav");
	if (lpDSB_[TPD_LOS][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[TPD_LOS][0],out lpDSB_[TPD_LOS][i]);

	lpDSB_[PLANE_FLYING][0]= LoadWave("WAV\\PLANE_FLYING.wav");
	if (lpDSB_[PLANE_FLYING][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[PLANE_FLYING][0],out lpDSB_[PLANE_FLYING][i]);

	//
	lpDSB_[PLANE1][0]= LoadWave("WAV\\PLANE1.wav");
	if (lpDSB_[PLANE1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[PLANE1][0],out lpDSB_[PLANE1][i]);

	lpDSB_[PLANE2][0]= LoadWave("WAV\\PLANE2.wav");
	if (lpDSB_[PLANE2][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[PLANE2][0],out lpDSB_[PLANE2][i]);
	
	//
	lpDSB_[TAKE_OFF][0]= LoadWave("WAV\\TAKE_OFF.wav");
	if (lpDSB_[TAKE_OFF][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[TAKE_OFF][0],out lpDSB_[TAKE_OFF][i]);
	
	lpDSB_[SNR][0]= LoadWave("WAV\\SNR.wav");
	if (lpDSB_[SNR][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[SNR][0],out lpDSB_[SNR][i]);

	//
	lpDSB_[CLICK1][0]= LoadWave("WAV\\CLICK1.wav");
	if (lpDSB_[CLICK1][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[CLICK1][0],out lpDSB_[CLICK1][i]);

	lpDSB_[CLICK2][0]= LoadWave("WAV\\CLICK2.wav");
	if (lpDSB_[CLICK2][0]==null)
		{
		MessageBox(null,"WAV 読み込みに失敗しました。",CAPTION,MB_OK | MB_ICONSTOP);
		SendMessage(hwndApp,WM_CLOSE,0,0);
		return FALSE;
		}
	for(i=1;i<SND_DUP;i++)
		lpDS.DuplicateSoundBuffer(lpDSB_[CLICK2][0],out lpDSB_[CLICK2][i]);






	return TRUE;
	}



/*-------------------------------------------
	主にゲーム中の効果音を鳴らす
---------------------------------------------*/
public void	play_snd(IDirectSoundBuffer? the_lpdsb,short* the_snd,int f)
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
public void SoundPlayEffect(int dwFlags,int no,double x,double y)
	{
//	HRESULT     dsrval;
//	IDirectSoundBuffer *pdsb = lpDSBuffer[no];
	RECT	field_rect;
	double	wrk_x,wrk_y,drctn,dstc;
	int		n,size=default /* C4701 */,flg;


#if !SND_SW

return;

#endif



	if( x!=(double)(MAP_RIGHT+1) )
		{
		flg=0;
		for(n=1;n<=max_unit && flg==0 ;n++)
			{
			if( unit[n].used!=0 && unit[n].used==your_side && unit[n].info[0]!=PARKING )
				{
				// マイユニットからこのエフェクトが見えるか
				// 現地点からユニット地点への距離

				wrk_x=unit[n].x;
				wrk_y=unit[n].y;
				if( unit[n].kind==UnitKind.Fighter )
					{	// 航空機の場合はちょっと前へ
					wrk_x+=cos(unit[n].drctn*a_PI)*FT_EYE;
					wrk_y+=sin(unit[n].drctn*a_PI)*FT_EYE;
					}

				wrk_x=wrk_x-x;
				wrk_y=wrk_y-y;


				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);

				dstc=((wrk_x)/(cos(drctn*a_PI)));

				switch( unit[n].kind )
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
			
		x=(int)(x-cmbt_x);
		y=(int)(cmbt_y-y);
		if( pt_in_rect3(ref field_rect, (int)x,(int)y )==0 )
			return;
		}

	if(1!=0/*lpDSBuffer[no]*/)
		{
//		IDirectSoundBuffer_SetCurrentPosition(pdsb, 0);
//		IDirectSoundBuffer_Play(pdsb, 0, 0, dwFlags);
//	play_snd( lpDSB_AA_BLT[3][snd_AA_BLT[3]], &snd_AA_BLT[3], 0 );

//		play_snd( lpDSB_[no][snd_[no]], &snd_[no], 0 );


		lpDSB_[no][snd_[no]].Stop();		// 
		lpDSB_[no][snd_[no]].SetCurrentPosition(0);	// 巻き戻し

//		if( f<-9600 )
//			f=-9600;

		lpDSB_[no][snd_[no]].SetVolume( 0/*f*/ );
		lpDSB_[no][snd_[no]].Play(0,0,0);		// 

		snd_[no]++;
		snd_[no]=(short)(snd_[no]%SND_DUP);


		}




	}
}
