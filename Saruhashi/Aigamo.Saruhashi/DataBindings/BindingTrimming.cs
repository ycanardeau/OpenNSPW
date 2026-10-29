namespace Aigamo.Saruhashi;

// Why data binding cannot be trimmed: an app that trims, and binds, keeps the types it binds itself.
internal static class BindingTrimming
{
	public const string Message =
		"Data binding finds the properties and converters of the types it binds by reflection (TypeDescriptor), which trimming may remove.";
}
