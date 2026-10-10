using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace OpenNspw;

// The sprites (sprt), the regions of the offscreen surface that the game draws from: an Array25<Sprite> that can also
// be indexed by SpriteId, with the same layout.
[InlineArray(25)]
public struct SpriteArray
{
	private Sprite _element0;

	[UnscopedRef] public ref Sprite this[SpriteId id] => ref this[(int)id];
}
