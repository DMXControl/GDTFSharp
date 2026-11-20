using System;

namespace GDTFSharp.Types
{
    /// <summary>
    /// GDTF <c>Rotation</c> value type: a 3x3 rotation matrix written row by row as
    /// <c>{m11,m12,m13}{m21,m22,m23}{m31,m32,m33}</c>. Mij is row i, column j.
    /// </summary>
    public readonly struct RotationMatrix : IEquatable<RotationMatrix>
    {
        public static readonly RotationMatrix Identity = new RotationMatrix(1, 0, 0, 0, 1, 0, 0, 0, 1);

        public float M11 { get; }
        public float M12 { get; }
        public float M13 { get; }
        public float M21 { get; }
        public float M22 { get; }
        public float M23 { get; }
        public float M31 { get; }
        public float M32 { get; }
        public float M33 { get; }

        public RotationMatrix(float m11, float m12, float m13, float m21, float m22, float m23, float m31, float m32, float m33)
        {
            M11 = m11; M12 = m12; M13 = m13;
            M21 = m21; M22 = m22; M23 = m23;
            M31 = m31; M32 = m32; M33 = m33;
        }

        public float this[int row, int column]
        {
            get
            {
                switch (row * 3 + column)
                {
                    case 0: return M11;
                    case 1: return M12;
                    case 2: return M13;
                    case 3: return M21;
                    case 4: return M22;
                    case 5: return M23;
                    case 6: return M31;
                    case 7: return M32;
                    case 8: return M33;
                    default: throw new ArgumentOutOfRangeException(nameof(row), "Row and column must be between 0 and 2.");
                }
            }
        }

        public static RotationMatrix Parse(string value)
        {
            if (!TryParse(value, out var result))
                throw new FormatException($"'{value}' is not a valid GDTF Rotation.");
            return result;
        }

        /// <summary>Parses the bracketed row format. <c>None</c> gives <see cref="Identity"/>.</summary>
        public static bool TryParse(string? value, out RotationMatrix result)
        {
            result = Identity;
            if (value != null && value.Trim() == "None") return true;
            if (!GdtfFormat.TryParseBracketedRows(value, 3, 3, out var m)) return false;
            result = new RotationMatrix(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8]);
            return true;
        }

        public bool Equals(RotationMatrix other) =>
            M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) &&
            M21.Equals(other.M21) && M22.Equals(other.M22) && M23.Equals(other.M23) &&
            M31.Equals(other.M31) && M32.Equals(other.M32) && M33.Equals(other.M33);

        public override bool Equals(object? obj) => obj is RotationMatrix other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(HashCode.Combine(M11, M12, M13, M21, M22, M23), M31, M32, M33);
        public static bool operator ==(RotationMatrix left, RotationMatrix right) => left.Equals(right);
        public static bool operator !=(RotationMatrix left, RotationMatrix right) => !left.Equals(right);

        public override string ToString() =>
            GdtfFormat.FormatBracketedRows(new[] { M11, M12, M13, M21, M22, M23, M31, M32, M33 }, 3);
    }
}
