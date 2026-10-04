/*
** DirectX 8.0 アクションゲームプログラミング
** 
** LoadWave - WAVE ファイルを読み込むためのプログラム
** 
** Copyright (c) 1997-2001 山羊さん
** All Rights Reserved.
** 
** mailto:yagi@and.or.jp
** http://www.and.or.jp/~yagi/
** 
** このソースコードは WAVE ファイルを簡単に読み込むために提供されています。
** 開発者は、アプリケーションのプロジェクトにLoadWave.cppを追加し、LoadWave.hをインクルードすることにより機能を使用することができます。
** このソースコードを使用・引用・改変した結果如何なる損害が発生しても、著者および出版社は責任を負いません。
*/

// グローバル変数
extern WAVEFORMATEX* m_pwfx;
extern HMMIO m_hmmioIn;
extern MMCKINFO m_ckIn;
extern MMCKINFO m_ckInRiff;

// 関数プロトタイプ
int YWaveClose();
int YWaveRead(UINT size,char *data,UINT *read,WAVEFORMATEX *wf);
int YWaveOpen(char *name);
HRESULT YWaveReadFile(HMMIO hmmioIn,UINT cbRead,BYTE* pbDest,MMCKINFO* pckIn, UINT* cbActualRead);
HRESULT YWaveStartDataRead(HMMIO* phmmioIn,MMCKINFO* pckIn,MMCKINFO* pckInRIFF);
HRESULT YWaveOpenFile(CHAR* strFileName,HMMIO* phmmioIn,WAVEFORMATEX** ppwfxInfo,MMCKINFO* pckInRIFF);
HRESULT YReadMMIO(HMMIO hmmioIn,MMCKINFO* pckInRIFF,WAVEFORMATEX **ppwfxInfo);

