namespace OpenNspw;

// A BOOL: an int that is used as a boolean (docs/Refactoring.md, Value types). It is tested and assigned as a bool,
// but it keeps the 4 bytes and the value of the int, so that the layout of the structs that hold it is unchanged, and
// the few places that compare it with 1 can still do so through Value.
public readonly struct Bool32(int value)
{
	public readonly int Value = value;

	// Whether the int is not 0, as `if( x!=0 )` tests it.
	public static implicit operator bool(Bool32 value)
	{
		return value.Value != 0;
	}

	// 1 for true and 0 for false, as the original assigns them.
	public static implicit operator Bool32(bool value)
	{
		return new Bool32(value ? 1 : 0);
	}
}
