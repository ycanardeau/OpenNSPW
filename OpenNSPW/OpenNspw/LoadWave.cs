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

// Port of LoadWave.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

// グローバル変数
public WAVEFORMATEX* m_pwfx;
public HMMIO m_hmmioIn;
public MMCKINFO m_ckIn;
public MMCKINFO m_ckInRiff;

/* WAVE 閉じる */
public int YWaveClose()
{
	mmioClose(m_hmmioIn,0);
	return 1;
}

/* WAVE 読み込み */
public int YWaveRead(uint size,byte* data,uint* read,WAVEFORMATEX* wf)
{
	// The globals are fields, whose addresses C# only gives with fixed.
	fixed (MMCKINFO* pckIn = &m_ckIn)
	if (FAILED(YWaveReadFile(m_hmmioIn,size,(byte*)data,pckIn,read)))
	{
		return 0;
	}
	/* フォーマットのコピー */
	if (wf!=null)
	{
		memcpy(wf,m_pwfx,(nuint)(sizeof(WAVEFORMATEX)));
	}
	free(m_pwfx);
	return 1;
}

/* WAVE 開く */
public int YWaveOpen(string name)
{
	m_pwfx = null;
	// The globals are fields, whose addresses C# only gives with fixed.
	fixed (HMMIO* phmmioIn = &m_hmmioIn)
	fixed (WAVEFORMATEX** ppwfx = &m_pwfx)
	fixed (MMCKINFO* pckIn = &m_ckIn, pckInRiff = &m_ckInRiff)
	{
	if (FAILED(YWaveOpenFile(name,phmmioIn,ppwfx,pckInRiff)))
	{
		return 0;
	}
	if (FAILED(YWaveStartDataRead(phmmioIn,pckIn,pckInRiff)))
	{
		return 0;
	}
	}
	return 1;
}

/* WAVE ファイル読み込み */
public int YWaveReadFile(HMMIO hmmioIn,uint cbRead,byte* pbDest,MMCKINFO* pckIn,uint* cbActualRead)
{
	MMIOINFO mmioinfoIn;
	*cbActualRead = 0;
	if (mmioGetInfo(hmmioIn,&mmioinfoIn,0)!=0)
	{
		return E_FAIL;
	}
	uint cbDataIn;
	cbDataIn = cbRead;
	if (cbDataIn > pckIn->cksize)
	{
		cbDataIn = pckIn->cksize;
	}
	pckIn->cksize -= cbDataIn;
	for (uint cT = 0;cT < cbDataIn;cT++)
	{
		if (mmioinfoIn.pchNext == mmioinfoIn.pchEndRead)
		{
			if (mmioAdvance(hmmioIn,&mmioinfoIn,MMIO_READ)!=0)
			{
				return E_FAIL;
			}
			if (mmioinfoIn.pchNext == mmioinfoIn.pchEndRead)
			{
				return E_FAIL;
			}
		}
		*((byte*)pbDest + cT) = *((byte*)mmioinfoIn.pchNext);
		mmioinfoIn.pchNext++;
	}
	if (mmioSetInfo(hmmioIn,&mmioinfoIn,0)!=0)
	{
		return E_FAIL;
	}
	*cbActualRead = cbDataIn;
	return S_OK;
}

/* WAVE データ読み込み */
public int YWaveStartDataRead(HMMIO* phmmioIn,MMCKINFO* pckIn,MMCKINFO* pckInRIFF)
{
	if (-1 == mmioSeek(*phmmioIn,(int)(pckInRIFF->dwDataOffset + sizeof(uint)),SEEK_SET))
	{
		return E_FAIL;
	}
	pckIn->ckid = mmioFOURCC('d','a','t','a');
	if (mmioDescend(*phmmioIn,pckIn,pckInRIFF,MMIO_FINDCHUNK)!=0)
	{
		return E_FAIL;
	}
	return S_OK;
}

/* Wave を開く */
public int YWaveOpenFile(string strFileName,HMMIO* phmmioIn,WAVEFORMATEX** ppwfxInfo,MMCKINFO* pckInRIFF)
{
	int hr;
	HMMIO hmmioIn;
	hmmioIn = default;
	if (!(hmmioIn = mmioOpen(strFileName,null,MMIO_ALLOCBUF | MMIO_READ)))
	{
		return E_FAIL;
	}
	if (FAILED(hr = YReadMMIO(hmmioIn,pckInRIFF,ppwfxInfo)))
	{
		mmioClose(hmmioIn,0);
		return E_FAIL;
	}
	*phmmioIn = hmmioIn;
	return S_OK;
}

/* MMIO 読み込み */
public int YReadMMIO(HMMIO hmmioIn,MMCKINFO* pckInRIFF,WAVEFORMATEX** ppwfxInfo)
{
	MMCKINFO ckIn;
	PCMWAVEFORMAT pcmWaveFormat;
	*ppwfxInfo = null;
	if (mmioDescend(hmmioIn,pckInRIFF,null,0)!=0)
	{
		return E_FAIL;
	}
	if ((pckInRIFF->ckid != FOURCC_RIFF) || (pckInRIFF->fccType != mmioFOURCC('W','A','V','E')))
	{
		return E_FAIL;
	}
	ckIn.ckid = mmioFOURCC('f','m','t',' ');
	if (mmioDescend(hmmioIn,&ckIn,pckInRIFF,MMIO_FINDCHUNK)!=0)
	{
		return E_FAIL;
	}
	if (ckIn.cksize < (int)sizeof(PCMWAVEFORMAT))
	{
		return E_FAIL;
	}
	if (mmioRead(hmmioIn,(byte*)&pcmWaveFormat,sizeof(PCMWAVEFORMAT)) != sizeof(PCMWAVEFORMAT))
	{
		return E_FAIL;
	}
	if (pcmWaveFormat.wf.wFormatTag == WAVE_FORMAT_PCM)
	{
		if ((*ppwfxInfo = (WAVEFORMATEX *)malloc((nuint)(sizeof(WAVEFORMATEX))))==null)
		{
			return E_FAIL;
		}
		memcpy(*ppwfxInfo,&pcmWaveFormat,(nuint)(sizeof(PCMWAVEFORMAT)));
		(*ppwfxInfo)->cbSize = 0;
	}
	else
	{
		ushort cbExtraBytes;
		cbExtraBytes = 0;
		if (mmioRead(hmmioIn,(byte*)&cbExtraBytes,sizeof(ushort)) != sizeof(ushort))
		{
			return E_FAIL;
		}
		*ppwfxInfo = (WAVEFORMATEX *)malloc((nuint)(sizeof(WAVEFORMATEX) + cbExtraBytes));		// new CHAR[sizeof(WAVEFORMATEX) + cbExtraBytes]
		if ((*ppwfxInfo)==null)
		{
			return E_FAIL;
		}
		memcpy(*ppwfxInfo,&pcmWaveFormat,(nuint)(sizeof(PCMWAVEFORMAT)));
		(*ppwfxInfo)->cbSize = cbExtraBytes;
		if (mmioRead(hmmioIn,(byte*)(((byte*) & ((*ppwfxInfo)->cbSize)) + sizeof(ushort)),cbExtraBytes) != cbExtraBytes)
		{
			free(*ppwfxInfo);
			*ppwfxInfo = null;
			return E_FAIL;
		}
	}
	if (mmioAscend(hmmioIn,&ckIn,0)!=0)
	{
		free(*ppwfxInfo);
		*ppwfxInfo = null;
		return E_FAIL;
	}
	return S_OK;
}

// WAVE ファイルの読み込み
public IDirectSoundBuffer? LoadWave(string name)
{
	byte* buf;
	uint readsize;
	WAVEFORMATEX wf;
	IDirectSoundBuffer? lpDSB = null;
	DSBUFFERDESC desc;
	void* pMem1,pMem2;
	uint size1,size2;
	// WAVE ファイルを開く
	if (YWaveOpen(name)==0)
	{
		return null;
	}
	// メモリ領域の確保
	buf = (byte*)malloc(m_ckIn.cksize);
	// 読み込み
	ZeroMemory(&wf,(nuint)(sizeof(WAVEFORMATEX)));
	if (YWaveRead(m_ckIn.cksize,buf,&readsize,&wf)==0)
	{
		free(buf);
		return null;
	}

		ZeroMemory(&desc,(nuint)(sizeof(DSBUFFERDESC)));
		desc.dwSize = (uint)(sizeof(DSBUFFERDESC));
		desc.dwFlags = /*DSBCAPS_LOCSOFTWARE |*/ DSBCAPS_CTRLVOLUME;	// 駄目ならソフトウェアバッファで試す。
		desc.dwBufferBytes = readsize;
		desc.lpwfxFormat = &wf;

		if (FAILED(lpDS.CreateSoundBuffer(&desc,out lpDSB,null)))
		{
			free(buf);
			return null;
		}

	// 領域をロック
	if (FAILED(lpDSB.Lock(0,readsize,&pMem1,&size1,&pMem2,&size2,0)))
	{
		free(buf);
		return null;
	}
	// 書き込み
	memcpy(pMem1,buf,size1);
	if (size2!=0)
	{
		memcpy(pMem2,buf + size1,size2);
	}
	// ロック解除
	lpDSB.Unlock(pMem1,size1,pMem2,size2);
	free(buf);
	// 閉じる
	YWaveClose();
	return lpDSB;
}
}
