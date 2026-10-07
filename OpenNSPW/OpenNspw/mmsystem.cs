using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the multimedia functions that the game uses (mmsystem.h): timeGetTime, and the buffered file I/O of
// mmio, which LoadWave.cpp uses to read RIFF WAVE files. An mmio file is read into native memory whole, so that
// MMIOINFO's pointers point into it.

// A handle to an open mmio file. Zero is NULL.
[StructLayout(LayoutKind.Sequential)]
public readonly struct HMMIO(int value)
{
	public int Value { get; } = value;

	public static bool operator !(HMMIO h) => h.Value == 0;
	public static bool operator true(HMMIO h) => h.Value != 0;
	public static bool operator false(HMMIO h) => h.Value == 0;
}

[StructLayout(LayoutKind.Sequential)]
public struct MMCKINFO
{
	public uint ckid;
	public uint cksize;
	public uint fccType;
	public uint dwDataOffset;
	public uint dwFlags;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct MMIOINFO
{
	public uint dwFlags;
	public uint fccIOProc;
	public nint pIOProc;
	public uint wErrorRet;
	public nint htask;
	public int cchBuffer;
	public byte* pchBuffer;
	public byte* pchNext;
	public byte* pchEndRead;
	public byte* pchEndWrite;
	public int lBufOffset;
	public int lDiskOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct PCMWAVEFORMAT
{
	public WAVEFORMAT wf;
	public ushort wBitsPerSample;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct WAVEFORMAT
{
	public ushort wFormatTag;
	public ushort nChannels;
	public uint nSamplesPerSec;
	public uint nAvgBytesPerSec;
	public ushort nBlockAlign;
}

public static class mmsystem
{
	public const uint MMIO_READ = 0x00000000;
	public const uint MMIO_ALLOCBUF = 0x00010000;
	public const uint MMIO_FINDCHUNK = 0x0010;
	public const uint MMIO_FINDRIFF = 0x0020;
	public const uint MMIO_FINDLIST = 0x0040;

	public const int SEEK_SET = 0;
	public const int SEEK_CUR = 1;
	public const int SEEK_END = 2;

	public const uint MMIOERR_CHUNKNOTFOUND = 265;
	public const uint MMIOERR_CANNOTREAD = 260;

	public static uint mmioFOURCC(char ch0, char ch1, char ch2, char ch3)
	{
		return (byte)ch0 | ((uint)(byte)ch1 << 8) | ((uint)(byte)ch2 << 16) | ((uint)(byte)ch3 << 24);
	}

	public static readonly uint FOURCC_RIFF = mmioFOURCC('R', 'I', 'F', 'F');
	public static readonly uint FOURCC_LIST = mmioFOURCC('L', 'I', 'S', 'T');
}

// An open mmio file: its whole contents, and the current position.
internal sealed unsafe class MmioFile(byte* data, int size)
{
	public byte* Data { get; } = data;

	public int Size { get; } = size;

	public int Position { get; set; }
}

public unsafe partial class Nspw
{
	private readonly Dictionary<int, MmioFile> _mmioFiles = [];
	private int _nextMmio = 1;

	public uint timeGetTime()
	{
		return _platform.timeGetTime();
	}

	public HMMIO mmioOpen(string pszFileName, MMIOINFO* pmmioinfo, uint fdwOpen)
	{
		using var stream = _platform.OpenFile(pszFileName.Replace('\\', '/'), FileMode.Open, FileAccess.Read, FileShare.Read);
		if (stream is null)
		{
			return default;
		}

		using var memory = new MemoryStream();
		stream.CopyTo(memory);
		var bytes = memory.ToArray();
		var data = (byte*)NativeMemory.Alloc((nuint)Math.Max(bytes.Length, 1));
		bytes.CopyTo(new Span<byte>(data, bytes.Length));
		var handle = _nextMmio++;
		_mmioFiles[handle] = new MmioFile(data, bytes.Length);
		return new HMMIO(handle);
	}

	public uint mmioClose(HMMIO hmmio, uint fuClose)
	{
		if (_mmioFiles.Remove(hmmio.Value, out var file))
		{
			NativeMemory.Free(file.Data);
		}

		return 0;
	}

	public int mmioRead(HMMIO hmmio, byte* pch, int cch)
	{
		var file = _mmioFiles[hmmio.Value];
		var count = Math.Max(0, Math.Min(cch, file.Size - file.Position));
		Buffer.MemoryCopy(file.Data + file.Position, pch, count, count);
		file.Position += count;
		return count;
	}

	public int mmioSeek(HMMIO hmmio, int lOffset, int iOrigin)
	{
		var file = _mmioFiles[hmmio.Value];
		var position = iOrigin switch
		{
			mmsystem.SEEK_CUR => file.Position + lOffset,
			mmsystem.SEEK_END => file.Size + lOffset,
			_ => lOffset,
		};
		if (position < 0 || position > file.Size)
		{
			return -1;
		}

		file.Position = position;
		return position;
	}

	private static uint ReadUInt(MmioFile file, int offset)
	{
		return offset + 4 <= file.Size ? *(uint*)(file.Data + offset) : 0;
	}

	// Enters a chunk: reads its header at the current position, or with MMIO_FINDCHUNK (FINDRIFF, FINDLIST) looks for
	// one with pmmcki's ckid (and fccType) up to the end of the parent. The position is then at the chunk's data, after
	// the form type of a RIFF or LIST chunk.
	public uint mmioDescend(HMMIO hmmio, MMCKINFO* pmmcki, MMCKINFO* pmmckiParent, uint fuDescend)
	{
		var file = _mmioFiles[hmmio.Value];
		var end = pmmckiParent is null ? file.Size : (int)(pmmckiParent->dwDataOffset + pmmckiParent->cksize);
		var wantedId = (fuDescend & mmsystem.MMIO_FINDRIFF) != 0 ? mmsystem.FOURCC_RIFF
			: (fuDescend & mmsystem.MMIO_FINDLIST) != 0 ? mmsystem.FOURCC_LIST
			: pmmcki->ckid;
		var find = (fuDescend & (mmsystem.MMIO_FINDCHUNK | mmsystem.MMIO_FINDRIFF | mmsystem.MMIO_FINDLIST)) != 0;
		while (file.Position + 8 <= end)
		{
			var ckid = ReadUInt(file, file.Position);
			var cksize = ReadUInt(file, file.Position + 4);
			var dataOffset = file.Position + 8;
			var hasType = ckid == mmsystem.FOURCC_RIFF || ckid == mmsystem.FOURCC_LIST;
			var fccType = hasType ? ReadUInt(file, dataOffset) : 0;
			if (!find || (ckid == wantedId && ((fuDescend & (mmsystem.MMIO_FINDRIFF | mmsystem.MMIO_FINDLIST)) == 0 || fccType == pmmcki->fccType)))
			{
				pmmcki->ckid = ckid;
				pmmcki->cksize = cksize;
				pmmcki->fccType = fccType;
				pmmcki->dwDataOffset = (uint)dataOffset;
				pmmcki->dwFlags = 0;
				file.Position = dataOffset + (hasType ? 4 : 0);
				return 0;
			}

			file.Position = dataOffset + (int)((cksize + 1) & ~1u);
		}

		return mmsystem.MMIOERR_CHUNKNOTFOUND;
	}

	// Leaves a chunk: moves to the end of its data, padded to an even size.
	public uint mmioAscend(HMMIO hmmio, MMCKINFO* pmmcki, uint fuAscend)
	{
		var file = _mmioFiles[hmmio.Value];
		file.Position = Math.Min(file.Size, (int)(pmmcki->dwDataOffset + ((pmmcki->cksize + 1) & ~1u)));
		return 0;
	}

	// The I/O buffer is the whole file, so pchNext is the current position and pchEndRead the end of the file.
	public uint mmioGetInfo(HMMIO hmmio, MMIOINFO* pmmioi, uint fuInfo)
	{
		var file = _mmioFiles[hmmio.Value];
		*pmmioi = default;
		pmmioi->cchBuffer = file.Size;
		pmmioi->pchBuffer = file.Data;
		pmmioi->pchNext = file.Data + file.Position;
		pmmioi->pchEndRead = file.Data + file.Size;
		pmmioi->pchEndWrite = file.Data + file.Size;
		return 0;
	}

	public uint mmioSetInfo(HMMIO hmmio, MMIOINFO* pmmioi, uint fuInfo)
	{
		var file = _mmioFiles[hmmio.Value];
		file.Position = (int)(pmmioi->pchNext - file.Data);
		return 0;
	}

	// There is nothing more to read after the end of the buffer, so pchNext stays at pchEndRead.
	public uint mmioAdvance(HMMIO hmmio, MMIOINFO* pmmioi, uint fuAdvance)
	{
		return 0;
	}
}
