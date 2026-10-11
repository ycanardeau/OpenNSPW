using Xunit;

namespace OpenNspw.Tests;

// The port's structs and globals against their layout in the reference (Layout.json), so that state can be compared
// byte for byte.
public class LayoutTests
{
	private static TheoryData<string> ToTheoryData(IEnumerable<string> values)
	{
		var data = new TheoryData<string>();
		foreach (var value in values)
		{
			data.Add(value);
		}

		return data;
	}

	public static TheoryData<string> Structs => ToTheoryData(ReferenceLayout.Instance.Structs.Keys.Order());

	[Theory]
	[MemberData(nameof(Structs))]
	public void Struct_has_the_reference_layout(string name)
	{
		var reference = ReferenceLayout.Instance.Structs[name];
		var type = OriginalNames.FindStruct(name);

		Assert.Equal(reference.Size, TypeLayout.SizeOf(type));
		Assert.Equal(
			reference.Fields.OrderBy(f => f.Value.Offset).Select(f => $"{f.Key}: {f.Value.Size} bytes at {f.Value.Offset}"),
			TypeLayout.FieldsOf(type).SelectMany(f => OriginalNames.Slices(f.Field, f.Offset)).Select(s => $"{s.Name}: {s.Size} bytes at {s.Offset}"));
	}

	public static TheoryData<string> Globals => ToTheoryData(ReferenceLayout.Instance.Globals.Select(g => g.Name));

	[Theory]
	[MemberData(nameof(Globals))]
	public void Global_has_the_reference_size(string name)
	{
		Assert.Equal(Global.All[Global.Indexes[name]].Size, OriginalNames.FindGlobal(name).Size);
	}

	[Fact]
	public void Initial_state_is_the_reference_initial_state()
	{
		Assert.Empty(GameState.Initial.Differences(new Nspw()));
	}
}
