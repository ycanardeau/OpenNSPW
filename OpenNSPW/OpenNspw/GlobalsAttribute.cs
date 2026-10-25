namespace OpenNspw;

// A class that holds some of the original's globals, as a part of Nspw (docs/Refactoring.md, Splitting the partial
// class). The tests and the tools find a global in Nspw's own fields, and in the fields of the objects of these classes
// that Nspw's fields hold.
[AttributeUsage(AttributeTargets.Class)]
public sealed class GlobalsAttribute : Attribute;
