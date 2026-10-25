namespace OpenNspw;

// An angle in degrees, counterclockwise from east, as the original keeps headings and bearings in doubles (drctn). One
// double, like the fields it replaces (UNIT.drctn, FIRE.drctn, UNIT.to_ldr_drctn), so that the layout of the structs
// that hold it is unchanged. Every operation computes what the original's expression computes, in the same order: an
// angle is not brought into 0 to 360 unless the code says so, and in the order it says.
public readonly struct Angle(double degrees) : IEquatable<Angle>
{
	public readonly double Degrees = degrees;

	public static implicit operator Angle(double degrees)
	{
		return new Angle(degrees);
	}

	// The whole degrees, truncated as `(int)drctn` does.
	public static explicit operator int(Angle angle)
	{
		return (int)angle.Degrees;
	}

	public static Angle operator +(Angle left, Angle right)
	{
		return new Angle(left.Degrees + right.Degrees);
	}

	public static Angle operator -(Angle left, Angle right)
	{
		return new Angle(left.Degrees - right.Degrees);
	}

	public static bool operator ==(Angle left, Angle right)
	{
		return left.Degrees == right.Degrees;
	}

	public static bool operator !=(Angle left, Angle right)
	{
		return left.Degrees != right.Degrees;
	}

	public static bool operator <(Angle left, Angle right)
	{
		return left.Degrees < right.Degrees;
	}

	public static bool operator >(Angle left, Angle right)
	{
		return left.Degrees > right.Degrees;
	}

	public static bool operator <=(Angle left, Angle right)
	{
		return left.Degrees <= right.Degrees;
	}

	public static bool operator >=(Angle left, Angle right)
	{
		return left.Degrees >= right.Degrees;
	}

	public bool Equals(Angle other)
	{
		return Degrees.Equals(other.Degrees);
	}

	public override bool Equals(object? obj)
	{
		return obj is Angle other && Equals(other);
	}

	public override int GetHashCode()
	{
		return Degrees.GetHashCode();
	}

	// sin(drctn*a_PI) and cos(drctn*a_PI), as the original computes them (nspw_math.SinDegrees and CosDegrees).
	public double Sin()
	{
		return nspw_math.SinDegrees(Degrees);
	}

	public double Cos()
	{
		return nspw_math.CosDegrees(Degrees);
	}

	// The angle plus a full turn if it is below 0: `if(drctn<0) drctn=360+drctn;`.
	public Angle WrapNegative()
	{
		return Degrees < 0 ? new Angle(360 + Degrees) : this;
	}

	// The angle less a full turn if it is a full turn or more: `if(drctn>=360) drctn=drctn-360;`.
	public Angle WrapFullTurn()
	{
		return Degrees >= 360 ? new Angle(Degrees - 360) : this;
	}

	// WrapFullTurn, then WrapNegative, the order in which the original brings most angles into 0 to 360. Where it tests
	// below 0 first, the code says WrapNegative().WrapFullTurn(): 360 plus an angle just below 0 can round to 360.
	public Angle Wrapped()
	{
		return WrapFullTurn().WrapNegative();
	}

	// The whole degrees from -359 to 359: `(int)drctn%360`.
	public Angle WholeDegrees()
	{
		return (int)Degrees % 360;
	}

	// The heading of the eight, 45° apart, that the original's switch on `(int)(drctn/22.5)` gives: 0 for 0° to 22.5° and
	// for 337.5° to 360°, 45 from 22.5°, and so on (and 0 from -22.5° to 0°, as the cast truncates toward 0). An angle
	// that is no case of the switch, 360 or more or -22.5 or less, gives null.
	public Angle? SnappedToEightDirections()
	{
		return (int)(Degrees / 22.5) switch
		{
			0 or 15 => 0.0,
			1 or 2 => 45.0,
			3 or 4 => 90.0,
			5 or 6 => 90.0 + 45.0,
			7 or 8 => 180.0,
			9 or 10 => 180.0 + 45.0,
			11 or 12 => 270.0,
			13 or 14 => 270.0 + 45.0,
			_ => null,
		};
	}

	// The same, or `otherwise`, the value the switch leaves alone, for an angle that is no case.
	public Angle SnappedToEightDirections(Angle otherwise)
	{
		return SnappedToEightDirections() ?? otherwise;
	}

	public override string ToString()
	{
		return Degrees.ToString();
	}
}
