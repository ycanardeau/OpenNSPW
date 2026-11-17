namespace Aigamo.Otsuki.Actors;

/// <summary>
/// Marks a delegate parameter that is called on another thread than the caller's, such as a transport's handler of
/// what it receives: an actor that gives one must touch none of its own state in it but by posting to itself
/// (OTSUKI0007).
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class CalledOnAnotherThreadAttribute : Attribute;
