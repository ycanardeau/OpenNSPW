using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenNspw.Tests;

internal sealed record FieldLayout(string Name, Type Type, int Offset, int Size);

// A primitive value inside a value of some type: its path (".pp_x[3]"), and where its bytes start.
internal sealed record Location(string Path, int Start, Type Type);

// The layout of the port's types, found at run time, and descriptions of their bytes.
internal static class TypeLayout
{
	private delegate Span<byte> BoxBytes(object box);

	private static Span<byte> Bytes<T>(object box) where T : struct
	{
		return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.Unbox<T>(box), 1));
	}

	private static readonly MethodInfo BytesMethod =
		typeof(TypeLayout).GetMethod(nameof(Bytes), BindingFlags.NonPublic | BindingFlags.Static)!;

	private static readonly ConcurrentDictionary<Type, BoxBytes> BoxBytesCache = new();

	// The bytes of a boxed value, in the box.
	public static Span<byte> BytesOf(object box)
	{
		var bytes = BoxBytesCache.GetOrAdd(box.GetType(), type => BytesMethod.MakeGenericMethod(type).CreateDelegate<BoxBytes>());
		return bytes(box);
	}

	public static int SizeOf(Type type)
	{
		return BytesOf(Activator.CreateInstance(type)!).Length;
	}

	// Finds the offset of each field by filling it with 0xFF in an otherwise zero value.
	private static IReadOnlyList<FieldLayout> FindFields(Type type)
	{
		var fields = new List<FieldLayout>();
		foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
		{
			var value = Activator.CreateInstance(field.FieldType)!;
			BytesOf(value).Fill(0xFF);
			var box = Activator.CreateInstance(type)!;
			field.SetValue(box, value);
			fields.Add(new FieldLayout(field.Name, field.FieldType, BytesOf(box).IndexOf((byte)0xFF), BytesOf(value).Length));
		}

		return [.. fields.OrderBy(f => f.Offset)];
	}

	private static readonly ConcurrentDictionary<Type, IReadOnlyList<FieldLayout>> FieldsCache = new();

	// The fields of a struct, ordered by offset.
	public static IReadOnlyList<FieldLayout> FieldsOf(Type type)
	{
		return FieldsCache.GetOrAdd(type, FindFields);
	}

	private static bool IsInlineArray(Type type)
	{
		return type.IsDefined(typeof(InlineArrayAttribute));
	}

	// The primitive value that contains the byte at `offset` in a value of `type`.
	public static Location Locate(Type type, int offset)
	{
		if (type.IsPrimitive)
		{
			return new Location("", 0, type);
		}

		if (IsInlineArray(type))
		{
			var element = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0].FieldType;
			var size = SizeOf(element);
			var index = offset / size;
			var inner = Locate(element, offset % size);
			return new Location($"[{index}]{inner.Path}", index * size + inner.Start, inner.Type);
		}

		foreach (var field in FieldsOf(type).Reverse())
		{
			if (field.Offset <= offset)
			{
				if (offset >= field.Offset + field.Size)
				{
					return new Location($" (padding after .{field.Name})", offset, typeof(byte));
				}

				var inner = Locate(field.Type, offset - field.Offset);
				return new Location($".{field.Name}{inner.Path}", field.Offset + inner.Start, inner.Type);
			}
		}

		return new Location(" (padding)", offset, typeof(byte));
	}

	private static string FormatDouble(double value)
	{
		return $"{value.ToString("R", CultureInfo.InvariantCulture)} (0x{BitConverter.DoubleToInt64Bits(value):X16})";
	}

	// The value of a primitive, read from its bytes.
	public static string Format(Type type, ReadOnlySpan<byte> bytes)
	{
		return Type.GetTypeCode(type) switch
		{
			TypeCode.Boolean => bytes[0].ToString(CultureInfo.InvariantCulture),
			TypeCode.Byte => bytes[0].ToString(CultureInfo.InvariantCulture),
			TypeCode.SByte => ((sbyte)bytes[0]).ToString(CultureInfo.InvariantCulture),
			TypeCode.Int16 => MemoryMarshal.Read<short>(bytes).ToString(CultureInfo.InvariantCulture),
			TypeCode.UInt16 => MemoryMarshal.Read<ushort>(bytes).ToString(CultureInfo.InvariantCulture),
			TypeCode.Int32 => MemoryMarshal.Read<int>(bytes).ToString(CultureInfo.InvariantCulture),
			TypeCode.UInt32 => MemoryMarshal.Read<uint>(bytes).ToString(CultureInfo.InvariantCulture),
			TypeCode.Int64 => MemoryMarshal.Read<long>(bytes).ToString(CultureInfo.InvariantCulture),
			TypeCode.UInt64 => MemoryMarshal.Read<ulong>(bytes).ToString(CultureInfo.InvariantCulture),
			TypeCode.Double => FormatDouble(MemoryMarshal.Read<double>(bytes)),
			_ => Convert.ToHexString(bytes),
		};
	}
}
