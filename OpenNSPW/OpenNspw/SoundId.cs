namespace OpenNspw;

// A sound effect (the original's enum SND_NO), an index into the sound buffers (lpDSB_) and their counters
// (NextSoundBuffers). The sounds are loaded from WAV\<original name>.wav, in this order.
public enum SoundId
{
	// 対空機銃弾
	[Original("AA_BLT1")]
	AntiAircraftBullet1 = 0,

	[Original("AA_BLT2")]
	AntiAircraftBullet2 = 1,

	[Original("AA_BLT3")]
	AntiAircraftBullet3 = 2,

	[Original("AA_BLT4")]
	AntiAircraftBullet4 = 3,

	// 対空炸裂弾
	[Original("AA_SHL1")]
	AntiAircraftShell1 = 4,

	[Original("AA_SHL2")]
	AntiAircraftShell2 = 5,

	[Original("AA_SHL3")]
	AntiAircraftShell3 = 6,

	[Original("AA_SHL4")]
	AntiAircraftShell4 = 7,

	[Original("AA_SHL5")]
	AntiAircraftShell5 = 8,

	[Original("TPD_HIT1")]
	TorpedoHit1 = 9,

	[Original("TPD_HIT2")]
	TorpedoHit2 = 10,

	[Original("BOM_HIT1")]
	BombHit1 = 11,

	[Original("BOM_HIT2")]
	BombHit2 = 12,

	[Original("SHIP_SINK1")]
	ShipSinking1 = 13,

	[Original("SHIP_SINK2")]
	ShipSinking2 = 14,

	[Original("SPL1")]
	Splash = 15,

	// The sea, looped during a battle.
	[Original("SEA1")]
	Sea = 16,

	[Original("GUN1")]
	Gun1 = 17,

	[Original("GUN2")]
	Gun2 = 18,

	[Original("GUN3")]
	Gun3 = 19,

	// A bomb falling, 50 ticks after it is dropped.
	[Original("FALL1")]
	BombFalling = 20,

	[Original("BOMB_OFF")]
	BombRelease = 21,

	// A bomber dropping its bombs.
	[Original("BB_BOMB")]
	BomberBombRelease = 22,

	[Original("TPD_LOS")]
	TorpedoLaunch = 23,

	[Original("PLANE_FLYING")]
	PlaneFlying = 24,

	[Original("PLANE1")]
	Plane1 = 25,

	[Original("PLANE2")]
	Plane2 = 26,

	[Original("TAKE_OFF")]
	TakeOff = 27,

	[Original("SNR")]
	Sonar = 28,

	[Original("CLICK1")]
	Click1 = 29,

	[Original("CLICK2")]
	Click2 = 30,
}
