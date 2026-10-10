namespace OpenNspw;

// A BYTE that is used as a boolean, like Bool32 for a BOOL: tested and assigned as a bool, but it keeps the byte and
// its value, so that the layout, the files and the messages that hold it are unchanged.
public readonly struct Bool8(byte value)
{
	public readonly byte Value = value;

	// Whether the byte is not 0, as `if( x!=0 )` tests it.
	public static implicit operator bool(Bool8 value)
	{
		return value.Value != 0;
	}

	// 1 for true and 0 for false, as the original assigns them.
	public static implicit operator Bool8(bool value)
	{
		return new Bool8(value ? (byte)1 : (byte)0);
	}
}
