namespace OpenNspw;

// Whether the player can request the reinforcement chosen in the supply panel (RequestSupply), or why not.
public enum SupplyRequestState
{
	// 要求する: it can be requested.
	Available,

	// 割当点数不足: not enough supply points.
	ShortOfPoints,

	// ユニットリミット: the side has as many units of its category as it can.
	UnitLimit,

	// 要求制限: the scenario allows no reinforcement of this kind (ArrivalControl).
	Restricted,
}
