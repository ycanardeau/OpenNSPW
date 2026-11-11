namespace System.Runtime.CompilerServices;

// What the compiler fills a parameter with the caller's argument through, which netstandard2.0 lacks.
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
	public string ParameterName { get; } = parameterName;
}
