using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenNspw.Tests;

internal sealed record FieldLayout(FieldInfo Field, string Name, Type Type, int Offset, int Size);

// A number inside a value of some type: its path (".pp_x[3]"), and where its bytes start.
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

	// The offset of a field in its struct, as `&value.field - &value`. Nothing is written, because writing a misaligned
	// field of a packed struct through reflection crashes on arm64.
	private static int OffsetOf(FieldInfo field)
	{
		var method = new DynamicMethod($"offset_{field.Name}", typeof(int), Type.EmptyTypes, typeof(TypeLayout).Module, skipVisibility: true);
		var il = method.GetILGenerator();
		il.DeclareLocal(field.DeclaringType!);
		il.Emit(OpCodes.Ldloca_S, (byte)0);
		il.Emit(OpCodes.Ldflda, field);
		il.Emit(OpCodes.Ldloca_S, (byte)0);
		il.Emit(OpCodes.Sub);
		il.Emit(OpCodes.Conv_I4);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Func<int>>()();
	}

	private static IReadOnlyList<FieldLayout> FindFields(Type type)
	{
		var fields = new List<FieldLayout>();
		foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
		{
			fields.Add(new FieldLayout(field, field.Name, field.FieldType, OffsetOf(field), SizeOf(field.FieldType)));
		}

		return [.. fields.OrderBy(f => f.Offset)];
	}

	private static readonly ConcurrentDictionary<Type, IReadOnlyList<FieldLayout>> FieldsCache = new();

	// The fields of a struct, ordered by offset.
	public static IReadOnlyList<FieldLayout> FieldsOf(Type type)
	{
		return FieldsCache.GetOrAdd(type, FindFields);
	}

	// Whether a value of the type is one number, which an enum is too.
	public static bool IsScalar(Type type)
	{
		return type.IsPrimitive || type.IsEnum;
	}

	private static bool IsInlineArray(Type type)
	{
		return type.IsDefined(typeof(InlineArrayAttribute));
	}

	// The number that contains the byte at `offset` in a value of `type`.
	public static Location Locate(Type type, int offset)
	{
		if (IsScalar(type))
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

	// The value of a number, read from its bytes. An enum is formatted as its underlying type.
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
