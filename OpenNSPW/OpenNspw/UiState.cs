namespace OpenNspw;

// What the host's UI shows of the battle that the game writes as text, or works out, as it draws its sidebar
// (docs/Ui.md). Not part of the original. On the game's thread, as it shows a frame.
public unsafe partial class Nspw
{
	private static string Decode(ReadOnlySpan<byte> text, int length)
	{
		return ShiftJis.GetString(text[..Math.Clamp(length, 0, text.Length)]);
	}

	// The texts of the unit whose information the sidebar shows (WriteUnitDetails), if it shows one: its kind, its damage
	// or building time, its ammunition and its fuel.
	public string[] ReadUnitDetails()
	{
		if( UnitInfoPanel[0]==0 )
			return [];

		Array8<int> len = default;
		Array8<Array128<byte>> ach = default;
		WriteUnitDetails(ref len, ref ach);
		return [Decode(ach[0], len[0]), Decode(ach[1], len[1]), Decode(ach[2], len[2]), Decode(ach[3], len[3])];
	}

	// The name of the reinforcement chosen in the supply panel (WriteSupplyTargetName).
	public string ReadSupplyTargetName()
	{
		Array8<int> len = default;
		Array8<Array128<byte>> ach = default;
		WriteSupplyTargetName(ref len, ref ach);
		return Decode(ach[0], len[0]);
	}

	// Whether the chosen reinforcement can be requested, or why not (GetSupplyRequestState).
	public SupplyRequestState ReadSupplyRequestState()
	{
		return GetSupplyRequestState();
	}
}
