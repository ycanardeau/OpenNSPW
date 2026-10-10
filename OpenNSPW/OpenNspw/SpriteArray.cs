using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace OpenNspw;

// The sprites (sprt), the regions of the offscreen surface that the game draws from: an Array25<SPRT> that can also
// be indexed by SpriteId, with the same layout.
[InlineArray(25)]
public struct SpriteArray
{
	private SPRT _element0;

	[UnscopedRef] public ref SPRT this[SpriteId id] => ref this[(int)id];
}
