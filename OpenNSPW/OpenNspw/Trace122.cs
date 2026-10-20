using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenNspw;

// The points of a single-player game that a trace records, in the order 1.22 reaches them: game_init's start and its last
// srand, then, for each frame, its start, each tick (before chara_cont) and decision, the drawing, and the orders.
public enum TracePoint : byte
{
	End = 0,
	Start = 1,
	Seeded = 2,
	Frame = 3,
	Tick = 4,
	Decision = 5,
	Orders = 6,
	Drawing = 7,
}

// A trace of a single-player game (docs/Verification.md): a recording of 1.22 that the game follows and checks itself
// against, or one that records the game.
public interface ISingleplayerTrace
{
	// rand's state when the game starts, and the seed of game_init's last srand; null to start as 1.22 does.
	(uint Start, uint Battle)? Seeds { get; }

	// The ticks of the frame that has just started; null for those of the speed.
	int? TicksOfNextFrame();

	// Whether the game takes the player's input: a replay gives it none while it follows 1.22, whose input it does not
	// have but for the speed and the view.
	bool TakesInput { get; }

	// The game has reached a point.
	void Reach(Nspw game, TracePoint point);
}

// A point of a trace, with what it records: rand's state, and, for a frame, the speed, for a tick, cc_count, whether
// the tick is drawn, and the units and the fires, for the drawing, the view (cmbt_x, cmbt_y).
public sealed record Trace122Point(TracePoint Kind, uint Random, int Value, bool IsDrawn, byte[]? State, WorldPosition Camera = default);

// The file of a trace of 1.22, compressed with gzip: the magic, the scenario, then the points, each a byte (TracePoint)
// and what it records: rand's state, but for a frame, the speed; for a tick, cc_count, whether it is drawn, then rand's
// state and the units and the fires in 1.22's layout, XORed with the last tick's; for the drawing, rand's state, then the
// view, two doubles. An End point ends it.
public static class Trace122
{
	public const int UnitCount = 101;
	public const int UnitSize = 0x598;
	public const int FireCount = 256;
	public const int FireSize = 0x70;
	public const int StateSize = UnitCount * UnitSize + FireCount * FireSize;

	public static ReadOnlySpan<byte> Magic => "N122TRC2"u8;
}

public sealed class Trace122Writer : IDisposable
{
	private readonly BinaryWriter _writer;
	private readonly byte[] _lastState = new byte[Trace122.StateSize];
	private readonly byte[] _delta = new byte[Trace122.StateSize];

	public Trace122Writer(Stream stream, int scenario)
	{
		_writer = new BinaryWriter(new GZipStream(stream, CompressionLevel.Optimal), Encoding.ASCII);
		_writer.Write(Trace122.Magic);
		_writer.Write(scenario);
	}

	// Start, Seeded, Decision or Orders.
	public void WriteRandom(TracePoint point, uint random)
	{
		_writer.Write((byte)point);
		_writer.Write(random);
	}

	public void WriteFrame(int speed)
	{
		_writer.Write((byte)TracePoint.Frame);
		_writer.Write(speed);
	}

	public void WriteDrawing(uint random, WorldPosition camera)
	{
		_writer.Write((byte)TracePoint.Drawing);
		_writer.Write(random);
		_writer.Write(camera.X);
		_writer.Write(camera.Y);
	}

	public void WriteTick(int tick, bool isDrawn, uint random, ReadOnlySpan<byte> state)
	{
		_writer.Write((byte)TracePoint.Tick);
		_writer.Write(tick);
		_writer.Write(isDrawn);
		_writer.Write(random);
		for (var i = 0; i < Trace122.StateSize; i++)
			_delta[i] = (byte)(state[i] ^ _lastState[i]);
		_writer.Write(_delta);
		state[..Trace122.StateSize].CopyTo(_lastState);
	}

	public void Dispose()
	{
		_writer.Write((byte)TracePoint.End);
		_writer.Dispose();
	}
}

public sealed class Trace122Reader : IDisposable
{
	private readonly BinaryReader _reader;
	private readonly byte[] _state = new byte[Trace122.StateSize];

	public int Scenario { get; }

	public Trace122Reader(Stream stream)
	{
		_reader = new BinaryReader(new GZipStream(stream, CompressionMode.Decompress), Encoding.ASCII);
		if (!_reader.ReadBytes(Trace122.Magic.Length).AsSpan().SequenceEqual(Trace122.Magic))
			throw new InvalidDataException("Not a trace of 1.22, or one of an older format: record it again.");
		Scenario = _reader.ReadInt32();
	}

	// The next point; End at the end of the file, also when it was cut short, as when the recorder was closed.
	public Trace122Point Next()
	{
		try
		{
			var kind = (TracePoint)_reader.ReadByte();
			switch (kind)
			{
				case TracePoint.End:
					return new(kind, 0, 0, false, null);
				case TracePoint.Frame:
					return new(kind, 0, _reader.ReadInt32(), false, null);
				case TracePoint.Tick:
				{
					var tick = _reader.ReadInt32();
					var isDrawn = _reader.ReadBoolean();
					var random = _reader.ReadUInt32();
					var delta = _reader.ReadBytes(Trace122.StateSize);
					if (delta.Length < Trace122.StateSize)
						throw new EndOfStreamException();
					for (var i = 0; i < Trace122.StateSize; i++)
						_state[i] ^= delta[i];
					return new(kind, random, tick, isDrawn, (byte[])_state.Clone());
				}
				case TracePoint.Drawing:
				{
					var random = _reader.ReadUInt32();
					var x = _reader.ReadDouble();
					var y = _reader.ReadDouble();
					return new(kind, random, 0, false, null, new WorldPosition(x, y));
				}
				default:
					return new(kind, _reader.ReadUInt32(), 0, false, null);
			}
		}
		catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
		{
			return new(TracePoint.End, 0, 0, false, null);
		}
	}

	public void Dispose()
	{
		_reader.Dispose();
	}
}

// The fields of the state that a trace records, by their offset in it, to name a difference: a unit's or a fire's, in
// 1.22's layout.
public static class Trace122Layout
{
	public sealed record Field(int Offset, int Size, string Name, Type Type);

	// 1.22's UNIT where it differs from Unit (docs/Singleplayer.md): gas, as int, found and tech, then the random numbers.
	private const int UnitGasOffset = 0x538;
	private const int UnitFoundOffset = 0x558;
	private const int UnitRandomOffset = 0x560;

	private static int SizeOf(Type type)
	{
		return (int)typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!.MakeGenericMethod(type).Invoke(null, null)!;
	}

	private static FieldInfo[] FieldsOf(Type type)
	{
		return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(field => field.MetadataToken).ToArray();
	}

	private static int AlignmentOf(Type type)
	{
		if (type.IsPrimitive || type.IsEnum)
			return SizeOf(type);
		return FieldsOf(type).Select(field => AlignmentOf(field.FieldType)).DefaultIfEmpty(1).Max();
	}

	// The fields of a type that are numbers, at their offsets, with their names: Unit's Position.X, PathX[3].
	private static IEnumerable<Field> Leaves(Type type, int offset, string name)
	{
		if (type.IsPrimitive || type.IsEnum)
		{
			yield return new(offset, SizeOf(type), name, type);
			yield break;
		}

		if (type.GetCustomAttribute<InlineArrayAttribute>() is { } array)
		{
			var element = FieldsOf(type)[0].FieldType;
			for (var i = 0; i < array.Length; i++)
				foreach (var leaf in Leaves(element, offset + i * SizeOf(element), $"{name}[{i}]"))
					yield return leaf;
			yield break;
		}

		var fieldOffset = 0;
		foreach (var field in FieldsOf(type))
		{
			var alignment = AlignmentOf(field.FieldType);
			fieldOffset = (fieldOffset + alignment - 1) / alignment * alignment;
			var fieldName = field.Name.TrimStart('_');
			foreach (var leaf in Leaves(field.FieldType, offset + fieldOffset, name.Length == 0 ? fieldName : $"{name}.{fieldName}"))
				yield return leaf;
			fieldOffset += SizeOf(field.FieldType);
		}
	}

	private static Field[] MakeUnitFields()
	{
		var leaves = Leaves(typeof(Unit), 0, "").ToArray();
		int OffsetOfField(string name) => leaves.First(leaf => leaf.Name == name || leaf.Name.StartsWith(name + "[", StringComparison.Ordinal) || leaf.Name.StartsWith(name + ".", StringComparison.Ordinal)).Offset;
		var subWeapons = OffsetOfField("SubWeapons");
		var found = OffsetOfField("IsFound");
		var random = OffsetOfField("Random250");
		var fields = leaves.Where(leaf => leaf.Offset < subWeapons).ToList();
		for (var i = 0; i < 8; i++)
			fields.Add(new(UnitGasOffset + 4 * i, 4, $"gas[{i}]", typeof(int)));
		fields.AddRange(leaves.Where(leaf => leaf.Offset >= found && leaf.Offset < found + 8).Select(leaf => leaf with { Offset = leaf.Offset - found + UnitFoundOffset }));
		fields.AddRange(leaves.Where(leaf => leaf.Offset >= random).Select(leaf => leaf with { Offset = leaf.Offset - random + UnitRandomOffset }));
		return fields.Where(field => field.Offset + field.Size <= Trace122.UnitSize).ToArray();
	}

	public static readonly Field[] Unit = MakeUnitFields();
	public static readonly Field[] Fire = Leaves(typeof(Fire), 0, "").ToArray();
}

// Follows a trace of 1.22: gives the game its seeds, the speed and the ticks of each frame, and the view it draws, which
// moves by the player's input; and checks it at every point, rand's state, and, before every tick, its cc_count and its
// units and fires, whose doubles may differ by a millionth. The first difference is kept (Divergence), after which
// nothing more is checked.
public sealed class Trace122Replay : ISingleplayerTrace, IDisposable
{
	private const double Tolerance = 1e-6;

	private readonly Trace122Reader _reader;
	private readonly Queue<Trace122Point> _frame = new();
	private readonly byte[] _state = new byte[Trace122.StateSize];
	private Trace122Point _next;

	public int Scenario => _reader.Scenario;
	public (uint Start, uint Battle)? Seeds { get; }

	// The first difference, or null.
	public string? Divergence { get; private set; }

	// The frames compared so far, and whether the trace has ended.
	public int Frames { get; private set; }
	public bool HasEnded { get; private set; }

	public bool TakesInput => HasEnded || Divergence is not null;

	public Trace122Replay(Stream stream)
	{
		_reader = new Trace122Reader(stream);
		var start = _reader.Next();
		var seeded = _reader.Next();
		if (start.Kind != TracePoint.Start || seeded.Kind != TracePoint.Seeded)
			throw new InvalidDataException("The trace does not start with game_init.");
		Seeds = (start.Random, seeded.Random);
		_next = _reader.Next();
	}

	private static string DescribeValue(ReadOnlySpan<byte> bytes, Type type)
	{
		if (type == typeof(double))
			return BitConverter.ToDouble(bytes).ToString("R");
		return bytes.Length switch
		{
			1 => bytes[0].ToString(),
			2 => BitConverter.ToInt16(bytes).ToString(),
			4 => BitConverter.ToInt32(bytes).ToString(),
			_ => Convert.ToHexString(bytes),
		};
	}

	private static string? CompareRecords(string what, ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, Trace122Layout.Field[] fields)
	{
		var differences = new List<string>();
		foreach (var field in fields)
		{
			var e = expected.Slice(field.Offset, field.Size);
			var a = actual.Slice(field.Offset, field.Size);
			if (e.SequenceEqual(a))
				continue;
			if (field.Type == typeof(double))
			{
				var x = BitConverter.ToDouble(e);
				var y = BitConverter.ToDouble(a);
				if (Math.Abs(x - y) <= Tolerance * Math.Max(1, Math.Abs(x)))
					continue;
			}
			differences.Add($"{field.Name}: 1.22 {DescribeValue(e, field.Type)}, port {DescribeValue(a, field.Type)}");
		}
		return differences.Count == 0 ? null : $"{what}: {string.Join("; ", differences.Take(8))}";
	}

	private string? CompareState(byte[] expected)
	{
		var differences = new List<string>();
		for (var m = 0; m < Trace122.UnitCount; m++)
		{
			var offset = m * Trace122.UnitSize;
			if (CompareRecords($"unit {m}", expected.AsSpan(offset, Trace122.UnitSize), _state.AsSpan(offset, Trace122.UnitSize), Trace122Layout.Unit) is { } difference)
				differences.Add(difference);
		}
		for (var f = 0; f < Trace122.FireCount; f++)
		{
			var offset = Trace122.UnitCount * Trace122.UnitSize + f * Trace122.FireSize;
			if (CompareRecords($"fire {f}", expected.AsSpan(offset, Trace122.FireSize), _state.AsSpan(offset, Trace122.FireSize), Trace122Layout.Fire) is { } difference)
				differences.Add(difference);
		}
		return differences.Count == 0 ? null : string.Join("\n", differences.Take(10));
	}

	private void Diverge(Nspw game, TracePoint point, string what)
	{
		Divergence = $"Frame {Frames}, {point} (cc_count {game.Tick}): {what}";
	}

	private void StartFrame(Nspw game)
	{
		if (_next.Kind == TracePoint.End)
		{
			HasEnded = true;
			return;
		}
		if (_next.Kind != TracePoint.Frame)
		{
			Diverge(game, TracePoint.Frame, $"1.22 reached {_next.Kind} before its next frame.");
			return;
		}
		Frames++;
		game.GameSpeed = (short)_next.Value;
		_frame.Clear();
		for (_next = _reader.Next(); _next.Kind is not (TracePoint.Frame or TracePoint.End); _next = _reader.Next())
			_frame.Enqueue(_next);
	}

	public int? TicksOfNextFrame()
	{
		return HasEnded || Divergence is not null ? null : _frame.Count(point => point.Kind == TracePoint.Tick);
	}

	public void Reach(Nspw game, TracePoint point)
	{
		if (Divergence is not null || HasEnded)
			return;

		switch (point)
		{
			case TracePoint.Start:
			case TracePoint.Seeded:
				return;
			case TracePoint.Frame:
				StartFrame(game);
				return;
		}

		if (!_frame.TryDequeue(out var expected))
		{
			Diverge(game, point, "1.22 reached nothing more in this frame.");
			return;
		}
		if (expected.Kind != point)
		{
			Diverge(game, point, $"1.22 reached {expected.Kind}.");
			return;
		}
		if (expected.Random != game.RandomState)
		{
			Diverge(game, point, $"rand's state: 1.22 {expected.Random:X8}, port {game.RandomState:X8}.");
			return;
		}
		if (point == TracePoint.Drawing)
		{
			game.CameraPosition = expected.Camera;
			return;
		}
		if (point != TracePoint.Tick)
			return;
		if (expected.Value != game.Tick || expected.IsDrawn != game.IsDrawnTick)
		{
			Diverge(game, point, $"cc_count and the drawn tick: 1.22 {expected.Value}, {expected.IsDrawn}; port {game.Tick}, {game.IsDrawnTick}.");
			return;
		}
		game.WriteStateAs122(_state);
		if (CompareState(expected.State!) is { } difference)
			Diverge(game, point, "\n" + difference);
	}

	public void Dispose()
	{
		_reader.Dispose();
	}
}

// Records a single-player game of the port as a trace of 1.22, to replay it (tests).
public sealed class Trace122Recorder(Stream stream) : ISingleplayerTrace, IDisposable
{
	private readonly Stream _stream = stream;
	private readonly byte[] _state = new byte[Trace122.StateSize];
	private Trace122Writer? _writer;

	public (uint Start, uint Battle)? Seeds => null;

	public bool TakesInput => true;

	public int? TicksOfNextFrame()
	{
		return null;
	}

	public void Reach(Nspw game, TracePoint point)
	{
		if (point == TracePoint.Start)
			_writer = new Trace122Writer(_stream, game.ScenarioNumber);
		if (_writer is null)
			return;

		switch (point)
		{
			case TracePoint.Frame:
				_writer.WriteFrame(game.GameSpeed);
				break;
			case TracePoint.Tick:
				game.WriteStateAs122(_state);
				_writer.WriteTick(game.Tick, game.IsDrawnTick, game.RandomState, _state);
				break;
			case TracePoint.Drawing:
				_writer.WriteDrawing(game.RandomState, game.CameraPosition);
				break;
			default:
				_writer.WriteRandom(point, game.RandomState);
				break;
		}
	}

	public void Dispose()
	{
		_writer?.Dispose();
	}
}
