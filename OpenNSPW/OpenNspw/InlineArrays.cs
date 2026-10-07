using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenNspw;

// Fixed-size arrays (C++ `T a[N]`), stored inline like in C++, so that structs and globals keep their layout and a
// struct copy copies its arrays.

// Lets code that only has an object, such as the arguments of sprintf, read a char array's bytes.
public interface IInlineArray
{
	byte[] ToBytes();
}

[InlineArray(2)]
public struct Array2<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 2).ToArray();
	}
}

[InlineArray(3)]
public struct Array3<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 3).ToArray();
	}
}

[InlineArray(4)]
public struct Array4<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 4).ToArray();
	}
}

[InlineArray(6)]
public struct Array6<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 6).ToArray();
	}
}

[InlineArray(8)]
public struct Array8<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 8).ToArray();
	}
}

[InlineArray(9)]
public struct Array9<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 9).ToArray();
	}
}

[InlineArray(10)]
public struct Array10<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 10).ToArray();
	}
}

[InlineArray(14)]
public struct Array14<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 14).ToArray();
	}
}

[InlineArray(16)]
public struct Array16<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 16).ToArray();
	}
}

[InlineArray(25)]
public struct Array25<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 25).ToArray();
	}
}

[InlineArray(32)]
public struct Array32<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 32).ToArray();
	}
}

[InlineArray(35)]
public struct Array35<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 35).ToArray();
	}
}

[InlineArray(40)]
public struct Array40<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 40).ToArray();
	}
}

[InlineArray(50)]
public struct Array50<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 50).ToArray();
	}
}

[InlineArray(64)]
public struct Array64<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 64).ToArray();
	}
}

[InlineArray(90)]
public struct Array90<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 90).ToArray();
	}
}

[InlineArray(128)]
public struct Array128<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 128).ToArray();
	}
}

[InlineArray(256)]
public struct Array256<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 256).ToArray();
	}
}

[InlineArray(260)]
public struct Array260<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 260).ToArray();
	}
}

[InlineArray(512)]
public struct Array512<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 512).ToArray();
	}
}

[InlineArray(1024)]
public struct Array1024<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 1024).ToArray();
	}
}

[InlineArray(4096)]
public struct Array4096<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 4096).ToArray();
	}
}

[InlineArray(5)]
public struct Array5<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 5).ToArray();
	}
}

[InlineArray(7)]
public struct Array7<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 7).ToArray();
	}
}

[InlineArray(12)]
public struct Array12<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 12).ToArray();
	}
}

[InlineArray(20)]
public struct Array20<T> : IInlineArray
{
	private T _element0;

	public readonly byte[] ToBytes()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in this[0])), Unsafe.SizeOf<T>() * 20).ToArray();
	}
}
