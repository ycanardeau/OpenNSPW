namespace OpenNspw;

// Stand-ins for the MSVC C runtime functions that the game calls, with the same results.

public static class crt
{
	// Unlike Math.Abs, abs(int.MinValue) is int.MinValue, as in C.
	public static int abs(int x)
	{
		return x < 0 ? -x : x;
	}
}

// rand() and srand() keep their state per game, like the per-thread state of the MSVC CRT, so that two games can run
// in one process.
public partial class Nspw
{
	private uint _holdrand = 1;

	public void srand(uint seed)
	{
		_holdrand = seed;
	}

	public int rand()
	{
		_holdrand = _holdrand * 214013 + 2531011;
		return (int)((_holdrand >> 16) & 0x7FFF);
	}
}
