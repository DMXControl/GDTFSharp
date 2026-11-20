using System;

namespace GDTFSharp.Types
{
    /// <summary>GDTF <c>ColorCIE</c> value type: a CIE 1931 xyY color, written as <c>x,y,Y</c>.</summary>
    public readonly struct ColorCIE : IEquatable<ColorCIE>
    {
        /// <summary>CIE D65 white point with full luminance; the default color of wheel slots.</summary>
        public static readonly ColorCIE White = new ColorCIE(0.3127f, 0.3290f, 100f);

        /// <summary>Chromaticity x.</summary>
        public float X { get; }

        /// <summary>Chromaticity y.</summary>
        public float Y { get; }

        /// <summary>Luminance (capital Y).</summary>
        public float Luminance { get; }

        public ColorCIE(float x, float y, float luminance)
        {
            X = x;
            Y = y;
            Luminance = luminance;
        }

        public static ColorCIE Parse(string value)
        {
            if (!TryParse(value, out var color))
                throw new FormatException($"'{value}' is not a valid GDTF ColorCIE.");
            return color;
        }

        public static bool TryParse(string? value, out ColorCIE color)
        {
            color = default;
            if (!GdtfFormat.TryParseFloatList(value, 3, out var parts)) return false;
            color = new ColorCIE(parts[0], parts[1], parts[2]);
            return true;
        }

        public bool Equals(ColorCIE other) => X.Equals(other.X) && Y.Equals(other.Y) && Luminance.Equals(other.Luminance);
        public override bool Equals(object? obj) => obj is ColorCIE other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Luminance);
        public static bool operator ==(ColorCIE left, ColorCIE right) => left.Equals(right);
        public static bool operator !=(ColorCIE left, ColorCIE right) => !left.Equals(right);

        public override string ToString() =>
            $"{GdtfFormat.FormatFloat(X)},{GdtfFormat.FormatFloat(Y)},{GdtfFormat.FormatFloat(Luminance)}";
    }
}
