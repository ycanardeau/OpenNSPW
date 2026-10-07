using System.Runtime.CompilerServices;

namespace OpenNspw;

// Fixed-size arrays (C++ `T a[N]`), stored inline like in C++, so that structs and globals keep their layout and a
// struct copy copies its arrays.

[InlineArray(2)]
public struct Array2<T>
{
	private T _element0;
}

[InlineArray(3)]
public struct Array3<T>
{
	private T _element0;
}

[InlineArray(4)]
public struct Array4<T>
{
	private T _element0;
}

[InlineArray(6)]
public struct Array6<T>
{
	private T _element0;
}

[InlineArray(8)]
public struct Array8<T>
{
	private T _element0;
}

[InlineArray(9)]
public struct Array9<T>
{
	private T _element0;
}

[InlineArray(10)]
public struct Array10<T>
{
	private T _element0;
}

[InlineArray(16)]
public struct Array16<T>
{
	private T _element0;
}

[InlineArray(25)]
public struct Array25<T>
{
	private T _element0;
}

[InlineArray(32)]
public struct Array32<T>
{
	private T _element0;
}

[InlineArray(35)]
public struct Array35<T>
{
	private T _element0;
}

[InlineArray(40)]
public struct Array40<T>
{
	private T _element0;
}

[InlineArray(50)]
public struct Array50<T>
{
	private T _element0;
}

[InlineArray(64)]
public struct Array64<T>
{
	private T _element0;
}

[InlineArray(90)]
public struct Array90<T>
{
	private T _element0;
}

[InlineArray(128)]
public struct Array128<T>
{
	private T _element0;
}

[InlineArray(256)]
public struct Array256<T>
{
	private T _element0;
}

[InlineArray(260)]
public struct Array260<T>
{
	private T _element0;
}

[InlineArray(512)]
public struct Array512<T>
{
	private T _element0;
}

[InlineArray(1024)]
public struct Array1024<T>
{
	private T _element0;
}

[InlineArray(4096)]
public struct Array4096<T>
{
	private T _element0;
}
