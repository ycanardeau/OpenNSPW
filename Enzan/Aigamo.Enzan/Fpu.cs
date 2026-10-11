namespace Aigamo.Enzan;

public sealed class Fpu
{
	public FpuStack Stack { get; } = new();
	public FpuStatus Status { get; } = new();

	// The functions of fsin, fcos and fpatan: Math's by default, which may differ between platforms, or others that give
	// the same result on every platform.
	public Func<double, double> Sin { get; set; } = Math.Sin;
	public Func<double, double> Cos { get; set; } = Math.Cos;
	public Func<double, double, double> Atan2 { get; set; } = Math.Atan2;
}
