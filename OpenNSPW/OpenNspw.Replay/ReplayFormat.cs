using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Aigamo.MatchGenerator;

namespace OpenNspw.Replay;

// Replays (docs/Replays.md): a battle recorded from one player's seat, with only what the game takes from outside, to
// play it again as it was played.

// The kind of battle a replay records.
public enum ReplayMode : byte
{
	Singleplayer = 0,

	// A network battle, from the host's seat or the guest's.
	NetworkHost = 1,
	NetworkGuest = 2,
}

// A file of the game that a battle started from, by its path in the game's directory as the game names it: the map
// editor's scenario, or the battle saved to resume it.
public sealed record StartFile(string Path, byte[] Bytes);

// The start of a recorded battle: what the game needs to start it again as it started. Scenario: ScenarioNumber, below 0
// for a battle started from a saved one. Version: the app's that recorded it. RandomState: rand's state as
// InitializeGame starts, which a single-player scenario's random setup draws from. BattleSeed: the seed of
// InitializeGame's last srand, in a single-player battle. HashInterval: the frames between two hashes of the battle
// (ReplayRecorder.HashInterval). Network: a network battle's settings. Options: a single-player battle's options
// (BattleOptions), which it plays again with, whatever the player's are now; none in a network battle, and in a replay of
// version 2. Files: the files it started from.
public sealed record ReplayStart(
	ReplayMode Mode,
	short Scenario,
	DateTimeOffset Date,
	string Version,
	uint RandomState,
	uint BattleSeed,
	int HashInterval,
	NetworkBattleSettings? Network,
	BattleOptions Options,
	IReadOnlyList<StartFile> Files
);

// What a replay records after its start, each a byte and what it has: a frame, with the milliseconds since the last one,
// its ticks, the game's speed shown as it starts and whether it holds the battle (Nspw.IsBattleHeld); then, within the frame, the view as it is drawn when it has changed, what the player did, the hash of
// the battle as the frame ends, a note, the result as the battle has one; then what the game took after the frame: a
// message of the rival, the rival leaving. End ends the file.
public enum ReplayRecord : byte
{
	End = 0,
	Frame = 1,
	Camera = 2,
	Hash = 3,
	Note = 4,
	Result = 5,
	Action = 6,
	Received = 7,
	RivalLeft = 8,
}

// The cases of the player's actions (PlayerAction), as a replay writes them: a byte, then what the case has.
[GenerateMatch]
public enum ReplayActionCase : byte
{
	ClickUnit = 0,
	SetDestination = 1,
	CancelSelection = 2,
	RestoreSelection = 3,
	ChooseCombatMenuItem = 4,
	ToggleDeckView = 5,
	SelectSupplyTarget = 6,
	RequestSupply = 7,
	ConfirmSystemMenu = 8,
	SwapLocalSide = 9,
	SinkSelectedUnit = 10,
	GiveOrders = 11,
}

// A record of a replay, with what it has: Value, a frame's ticks or the result; Time, a frame's milliseconds since the
// last; Camera; Hash; Text, a note's; Action; Message, a message received; Speed, the game's speed shown as a frame
// starts; IsHeld, whether a frame holds the battle.
public readonly record struct ReplayItem(
	ReplayRecord Kind,
	int Value = 0,
	int Time = 0,
	WorldPosition Camera = default,
	ulong Hash = 0,
	string? Text = null,
	PlayerAction? Action = null,
	byte[]? Message = null,
	short Speed = 0,
	bool IsHeld = false
);

// The file of a replay, compressed with gzip: the magic, the start (ReplayStart), then the records (ReplayRecord).
public static class ReplayFormat
{
	public const string Extension = ".onspwr";

	// The magic of this version of the format, and what every version's starts with: an older replay is told apart, to be
	// refused (ReplayReader). 2 writes each action by its case (ReplayActionCase); 3 writes a frame's speed, which a frame
	// of 2 tells by its ticks only, as the frames of a speed may run other ticks (docs/SteadySpeed.md), and whether it
	// holds the battle, which none of 2 does, and the battle's options (docs/Options.md), which 2 has none of. 3 reads 2.
	public static ReadOnlySpan<byte> Magic => "ONSPWRP3"u8;

	public static ReadOnlySpan<byte> MagicOfVersion2 => "ONSPWRP2"u8;

	public static ReadOnlySpan<byte> MagicOfAnyVersion => "ONSPWRP"u8;

	// The app's version, which a replay records.
	public static string Version { get; } =
		typeof(ReplayFormat)
			.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			?.InformationalVersion
		?? "";

	// Whether a file starts as a replay does.
	public static bool IsReplay(Stream stream)
	{
		var position = stream.Position;
		try
		{
			using var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
			Span<byte> magic = stackalloc byte[MagicOfAnyVersion.Length];
			gzip.ReadExactly(magic);
			return magic.SequenceEqual(MagicOfAnyVersion);
		}
		catch (Exception exception) when (exception is InvalidDataException or EndOfStreamException)
		{
			return false;
		}
		finally
		{
			stream.Position = position;
		}
	}

	// The hash of the battle that a replay records: the units and the fires, rand's state and the tick. Not the units'
	// sprite's column (SpriteColumn), which the drawing sets for the units it draws: it follows the camera and the
	// cursor, not the battle, which never reads it.
	public static ulong HashBattle(Nspw game)
	{
		var hash = new Hash64();
		foreach (var drawn in (ReadOnlySpan<Unit>)game.Units)
		{
			var unit = drawn;
			unit.SpriteColumn = 0;
			hash.Add(MemoryMarshal.AsBytes(new ReadOnlySpan<Unit>(in unit)));
		}
		hash.Add(MemoryMarshal.AsBytes((ReadOnlySpan<Fire>)game.Fires));
		hash.Add(game.RandomState);
		hash.Add((ulong)game.Tick);
		return hash.Value;
	}
}

public sealed class ReplayWriter : IDisposable
{
	private readonly BinaryWriter _writer;

	// Whether there is a network battle's settings, then the settings.
	private void WriteNetwork(NetworkBattleSettings? network)
	{
		_writer.Write(network is not null);
		if (network is null)
			return;

		_writer.Write(network.IsHost);
		_writer.Write(network.Scenario);
		_writer.Write(network.SharedRandomSeed);
		_writer.Write(network.HostSide);
		_writer.Write(network.HostSupplyRate);
		_writer.Write(network.GuestSupplyRate);
		_writer.Write(network.IsDecisionEnabled);
		_writer.Write(network.HostInitialSupplyPoints);
		_writer.Write(network.GuestInitialSupplyPoints);
		_writer.Write(network.ArrivalControl);
		_writer.Write(network.SwapTime);
		_writer.Write(network.SwapRule);
		_writer.Write(network.UserScenarioFileName);
		_writer.Write(network.LocalPlayerName);
		_writer.Write(network.RivalPlayerName);
		_writer.Write(network.RivalMode);
		_writer.Write(network.CanAdvance1);
		_writer.Write(network.CanAdvance2);
		_writer.Write(network.RivalTickChecksum);
		_writer.Write(network.RivalUnitChecksum);
		_writer.Write(network.RivalRandomChecksum);
		_writer.Write(network.RivalTickWait);
	}

	// Each option, in the order of BattleOptions.
	private void WriteOptions(BattleOptions options)
	{
		_writer.Write(options.SharedEffectRandom);
		_writer.Write(options.SteadySpeed);
	}

	public ReplayWriter(Stream stream, ReplayStart start)
	{
		_writer = new BinaryWriter(new GZipStream(stream, CompressionLevel.Optimal), Encoding.UTF8);
		_writer.Write(ReplayFormat.Magic);
		_writer.Write((byte)start.Mode);
		_writer.Write(start.Scenario);
		_writer.Write(start.Date.ToUnixTimeMilliseconds());
		_writer.Write(start.Version);
		_writer.Write(start.RandomState);
		_writer.Write(start.BattleSeed);
		_writer.Write7BitEncodedInt(start.HashInterval);
		WriteNetwork(start.Network);
		WriteOptions(start.Options);
		_writer.Write7BitEncodedInt(start.Files.Count);
		foreach (var file in start.Files)
		{
			_writer.Write(file.Path);
			_writer.Write7BitEncodedInt(file.Bytes.Length);
			_writer.Write(file.Bytes);
		}
	}

	public void WriteFrame(int time, int ticks, short speed, bool isHeld)
	{
		_writer.Write((byte)ReplayRecord.Frame);
		_writer.Write7BitEncodedInt(time);
		_writer.Write7BitEncodedInt(ticks);
		_writer.Write7BitEncodedInt(speed);
		_writer.Write(isHeld);
	}

	public void WriteCamera(WorldPosition camera)
	{
		_writer.Write((byte)ReplayRecord.Camera);
		_writer.Write(camera.X);
		_writer.Write(camera.Y);
	}

	public void WriteHash(ulong hash)
	{
		_writer.Write((byte)ReplayRecord.Hash);
		_writer.Write(hash);
	}

	public void WriteNote(string text)
	{
		_writer.Write((byte)ReplayRecord.Note);
		_writer.Write(text);
	}

	public void WriteResult(GameResult result)
	{
		_writer.Write((byte)ReplayRecord.Result);
		_writer.Write((byte)result);
	}

	private void WritePosition(WorldPosition position)
	{
		_writer.Write(position.X);
		_writer.Write(position.Y);
	}

	// A frame's orders, as a trace of 1.22 writes them.
	private void WriteOrders(PlayerOrders orders)
	{
		_writer.Write(orders.Move.Unit);
		WritePosition(orders.Move.Destination);
		_writer.Write(orders.Move.ClearsPath);
		_writer.Write(orders.Select.IsSet.Value);
		_writer.Write(orders.Select.SelectedUnit);
		_writer.Write(orders.Select.Unit);
		WritePosition(orders.Select.GroundPosition);
		_writer.Write((short)orders.Menu.Menu);
		_writer.Write(orders.Menu.SelectedUnit);
		_writer.Write(orders.SelectedUnit);
		_writer.Write(orders.SelectionCount);
		foreach (var selection in orders.Selections)
			_writer.Write(selection);
	}

	// An action: its case (ReplayActionCase), then what that case has.
	public void WriteAction(PlayerAction action)
	{
		_writer.Write((byte)ReplayRecord.Action);
		action.Match<Action>(
			ClickUnit: click =>
				() =>
				{
					_writer.Write((byte)ReplayActionCase.ClickUnit);
					_writer.Write7BitEncodedInt(click.Unit);
					WritePosition(click.Ground);
				},
			SetDestination: destination =>
				() =>
				{
					_writer.Write((byte)ReplayActionCase.SetDestination);
					WritePosition(destination.Destination);
				},
			CancelSelection: cancel =>
				() =>
				{
					_writer.Write((byte)ReplayActionCase.CancelSelection);
					_writer.Write7BitEncodedInt(cancel.Unit);
				},
			RestoreSelection: _ => () => _writer.Write((byte)ReplayActionCase.RestoreSelection),
			ChooseCombatMenuItem: choose =>
				() =>
				{
					_writer.Write((byte)ReplayActionCase.ChooseCombatMenuItem);
					_writer.Write7BitEncodedInt((int)choose.Item);
				},
			ToggleDeckView: _ => () => _writer.Write((byte)ReplayActionCase.ToggleDeckView),
			SelectSupplyTarget: select =>
				() =>
				{
					_writer.Write((byte)ReplayActionCase.SelectSupplyTarget);
					_writer.Write(select.Next);
				},
			RequestSupply: _ => () => _writer.Write((byte)ReplayActionCase.RequestSupply),
			ConfirmSystemMenu: confirm =>
				() =>
				{
					_writer.Write((byte)ReplayActionCase.ConfirmSystemMenu);
					_writer.Write7BitEncodedInt((int)confirm.Answer);
				},
			SwapLocalSide: _ => () => _writer.Write((byte)ReplayActionCase.SwapLocalSide),
			SinkSelectedUnit: _ => () => _writer.Write((byte)ReplayActionCase.SinkSelectedUnit),
			GiveOrders: give =>
				() =>
				{
					_writer.Write((byte)ReplayActionCase.GiveOrders);
					WriteOrders(give.Orders);
				}
		)();
	}

	public void WriteReceived(ReadOnlySpan<byte> message)
	{
		_writer.Write((byte)ReplayRecord.Received);
		_writer.Write7BitEncodedInt(message.Length);
		_writer.Write(message);
	}

	public void WriteRivalLeft()
	{
		_writer.Write((byte)ReplayRecord.RivalLeft);
	}

	// Writes what is buffered, so that a game that stops without ending the file leaves it whole up to here.
	public void Flush()
	{
		_writer.Flush();
	}

	public void Dispose()
	{
		_writer.Write((byte)ReplayRecord.End);
		_writer.Dispose();
	}
}

public sealed class ReplayReader : IDisposable
{
	private readonly BinaryReader _reader;

	// Whether the replay is of version 2, whose frames have no speed (SpeedOfVersion2).
	private readonly bool _isVersion2;

	// The speed of the last frame of version 2 that was not run with Space held.
	private short _speedOfVersion2 = 1;

	public ReplayStart Start { get; }

	// Bytes of a count that the file says, or EndOfStreamException if it is cut before they end.
	private byte[] ReadBytes(int count)
	{
		var bytes = _reader.ReadBytes(count);
		return bytes.Length == count ? bytes : throw new EndOfStreamException();
	}

	private NetworkBattleSettings? ReadNetwork()
	{
		if (!_reader.ReadBoolean())
			return null;

		return new NetworkBattleSettings(
			_reader.ReadBoolean(),
			_reader.ReadInt16(),
			_reader.ReadInt16(),
			_reader.ReadInt32(),
			_reader.ReadInt16(),
			_reader.ReadInt16(),
			_reader.ReadByte(),
			_reader.ReadInt16(),
			_reader.ReadInt16(),
			_reader.ReadByte(),
			_reader.ReadInt16(),
			_reader.ReadInt16(),
			_reader.ReadString(),
			_reader.ReadString(),
			_reader.ReadString(),
			_reader.ReadInt16(),
			_reader.ReadBoolean(),
			_reader.ReadBoolean(),
			_reader.ReadByte(),
			_reader.ReadByte(),
			_reader.ReadByte(),
			_reader.ReadByte()
		);
	}

	// The options; none in a replay of version 2.
	private BattleOptions ReadOptions()
	{
		if (_isVersion2)
			return BattleOptions.None;

		return new BattleOptions(
			SharedEffectRandom: _reader.ReadBoolean(),
			SteadySpeed: _reader.ReadBoolean()
		);
	}

	private StartFile[] ReadFiles()
	{
		var files = new StartFile[_reader.Read7BitEncodedInt()];
		for (var i = 0; i < files.Length; i++)
		{
			var path = _reader.ReadString();
			files[i] = new StartFile(path, ReadBytes(_reader.Read7BitEncodedInt()));
		}

		return files;
	}

	public ReplayReader(Stream stream)
	{
		_reader = new BinaryReader(
			new GZipStream(stream, CompressionMode.Decompress),
			Encoding.UTF8
		);
		try
		{
			var magic = _reader.ReadBytes(ReplayFormat.Magic.Length).AsSpan();
			if (!magic.StartsWith(ReplayFormat.MagicOfAnyVersion))
				throw new InvalidDataException("Not a replay of OpenNSPW.");
			_isVersion2 = magic.SequenceEqual(ReplayFormat.MagicOfVersion2);
			if (!_isVersion2 && !magic.SequenceEqual(ReplayFormat.Magic))
				throw new InvalidDataException(
					"A replay of another version of OpenNSPW, which this one does not play."
				);
			Start = new ReplayStart(
				(ReplayMode)_reader.ReadByte(),
				_reader.ReadInt16(),
				DateTimeOffset.FromUnixTimeMilliseconds(_reader.ReadInt64()),
				_reader.ReadString(),
				_reader.ReadUInt32(),
				_reader.ReadUInt32(),
				_reader.Read7BitEncodedInt(),
				ReadNetwork(),
				ReadOptions(),
				ReadFiles()
			);
		}
		catch (EndOfStreamException)
		{
			throw new InvalidDataException("The replay ends before its start.");
		}
	}

	// The game's speed shown in a frame of version 2, as its ticks tell it: 1.22 runs speed + 1 ticks a frame, a network
	// game speed; both run 20 while Space is held, which leaves the speed as it was. A speed changed while Space is held
	// shows once it is let go.
	private short SpeedOfVersion2(int ticks)
	{
		if (ticks != 20)
			_speedOfVersion2 = (short)(
				Start.Mode == ReplayMode.Singleplayer ? Math.Max(ticks - 1, 0) : ticks
			);
		return _speedOfVersion2;
	}

	private ReplayItem ReadFrame()
	{
		var time = _reader.Read7BitEncodedInt();
		var ticks = _reader.Read7BitEncodedInt();
		var speed = _isVersion2 ? SpeedOfVersion2(ticks) : (short)_reader.Read7BitEncodedInt();
		var isHeld = !_isVersion2 && _reader.ReadBoolean();
		return new(ReplayRecord.Frame, Time: time, Value: ticks, Speed: speed, IsHeld: isHeld);
	}

	private WorldPosition ReadPosition() => new(_reader.ReadDouble(), _reader.ReadDouble());

	private PlayerOrders ReadOrders()
	{
		var move = new MoveOrder
		{
			Unit = _reader.ReadInt16(),
			Destination = ReadPosition(),
			ClearsPath = _reader.ReadInt32(),
		};
		var select = new SelectOrder
		{
			IsSet = new Bool32(_reader.ReadInt32()),
			SelectedUnit = _reader.ReadInt16(),
			Unit = _reader.ReadInt16(),
			GroundPosition = ReadPosition(),
		};
		var menu = new MenuOrder
		{
			Menu = (CombatMenuItem)_reader.ReadInt16(),
			SelectedUnit = _reader.ReadInt16(),
		};
		var selectedUnit = _reader.ReadInt16();
		var selectionCount = _reader.ReadInt16();
		var selections = new short[PlayerOrders.SelectionLength];
		for (var s = 0; s < selections.Length; s++)
			selections[s] = _reader.ReadInt16();
		return new PlayerOrders(move, select, menu, selectedUnit, selectionCount, selections);
	}

	// An action, by its case; a byte that is no case is not a replay's.
	private PlayerAction ReadAction()
	{
		var actionCase = (ReplayActionCase)_reader.ReadByte();
		if (!Enum.IsDefined(actionCase))
			throw new InvalidDataException($"Not an action of a replay: {actionCase}.");

		return actionCase.Match<PlayerAction>(
			ClickUnit: () =>
				new PlayerAction.ClickUnit(_reader.Read7BitEncodedInt(), ReadPosition()),
			SetDestination: () => new PlayerAction.SetDestination(ReadPosition()),
			CancelSelection: () => new PlayerAction.CancelSelection(_reader.Read7BitEncodedInt()),
			RestoreSelection: static () => new PlayerAction.RestoreSelection(),
			ChooseCombatMenuItem: () =>
				new PlayerAction.ChooseCombatMenuItem((CombatMenuItem)_reader.Read7BitEncodedInt()),
			ToggleDeckView: static () => new PlayerAction.ToggleDeckView(),
			SelectSupplyTarget: () => new PlayerAction.SelectSupplyTarget(_reader.ReadBoolean()),
			RequestSupply: static () => new PlayerAction.RequestSupply(),
			ConfirmSystemMenu: () =>
				new PlayerAction.ConfirmSystemMenu((MessageType)_reader.Read7BitEncodedInt()),
			SwapLocalSide: static () => new PlayerAction.SwapLocalSide(),
			SinkSelectedUnit: static () => new PlayerAction.SinkSelectedUnit(),
			GiveOrders: () => new PlayerAction.GiveOrders(ReadOrders())
		);
	}

	// The next record; End at the end of the file, also when it was cut short, as when the game stopped.
	public ReplayItem Next()
	{
		try
		{
			var kind = (ReplayRecord)_reader.ReadByte();
			return kind switch
			{
				ReplayRecord.Frame => ReadFrame(),
				ReplayRecord.Camera => new(
					kind,
					Camera: new WorldPosition(_reader.ReadDouble(), _reader.ReadDouble())
				),
				ReplayRecord.Hash => new(kind, Hash: _reader.ReadUInt64()),
				ReplayRecord.Note => new(kind, Text: _reader.ReadString()),
				ReplayRecord.Result => new(kind, Value: _reader.ReadByte()),
				ReplayRecord.Action => new(kind, Action: ReadAction()),
				ReplayRecord.Received => new(
					kind,
					Message: ReadBytes(_reader.Read7BitEncodedInt())
				),
				ReplayRecord.RivalLeft => new(kind),
				_ => new(ReplayRecord.End),
			};
		}
		catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
		{
			return new(ReplayRecord.End);
		}
	}

	public void Dispose()
	{
		_reader.Dispose();
	}
}

// What a list of replays shows of one: its start, its frames, how long it was played, in milliseconds, and the battle's
// last result.
public sealed record ReplaySummary(
	ReplayStart Start,
	int Frames,
	long Milliseconds,
	GameResult Result
)
{
	// Reads a replay through, to its end or to where it was cut.
	public static ReplaySummary Read(Stream stream)
	{
		using var reader = new ReplayReader(stream);
		var (frames, milliseconds, result) = (0, 0L, GameResult.None);
		for (var item = reader.Next(); item.Kind != ReplayRecord.End; item = reader.Next())
		{
			if (item.Kind == ReplayRecord.Frame)
			{
				frames++;
				milliseconds += item.Time;
			}
			else if (item.Kind == ReplayRecord.Result)
			{
				result = (GameResult)item.Value;
			}
		}

		return new ReplaySummary(reader.Start, frames, milliseconds, result);
	}
}

// The files a replay's battle started from (ReplayStart.Files), as a layer of the game's files that is only read
// (GameFileLayers), before the others, so that the battle starts from them and not from the files the host has.
public sealed class StartFileSource(IReadOnlyList<StartFile> files) : IGameFileSource
{
	private readonly IReadOnlyList<StartFile> _files = files;

	private static bool IsAt(StartFile file, string path) =>
		string.Equals(file.Path.Replace('\\', '/'), path, StringComparison.OrdinalIgnoreCase);

	public Stream? Open(string path, FileShare share)
	{
		return _files.FirstOrDefault(file => IsAt(file, path)) is { } found
			? new MemoryStream(found.Bytes, writable: false)
			: null;
	}

	public IReadOnlyList<string> Find(string directory, string pattern) => [];
}
