using System;
using System.Globalization;

namespace GDTFSharp.Types
{
    /// <summary>The GDTF <c>DataVersion</c> (<c>Major.Minor</c>, each one byte).</summary>
    public readonly struct DataVersion : IEquatable<DataVersion>, IComparable<DataVersion>
    {
        /// <summary>GDTF 1.0.</summary>
        public static readonly DataVersion V1_0 = new DataVersion(1, 0);
        /// <summary>GDTF 1.1 (DIN SPEC 15800:2020).</summary>
        public static readonly DataVersion V1_1 = new DataVersion(1, 1);
        /// <summary>GDTF 1.2 (DIN SPEC 15800:2022).</summary>
        public static readonly DataVersion V1_2 = new DataVersion(1, 2);

        /// <summary>The version this library writes by default.</summary>
        public static readonly DataVersion Current = V1_2;

        public byte Major { get; }
        public byte Minor { get; }

        public DataVersion(byte major, byte minor)
        {
            Major = major;
            Minor = minor;
        }

        public static DataVersion Parse(string value)
        {
            if (!TryParse(value, out var version))
                throw new FormatException($"'{value}' is not a valid GDTF DataVersion.");
            return version;
        }

        public static bool TryParse(string? value, out DataVersion version)
        {
            version = default;
            if (value == null) return false;
            var parts = value.Trim().Split('.');
            if (parts.Length != 2) return false;
            if (!byte.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var major) ||
                !byte.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
                return false;
            version = new DataVersion(major, minor);
            return true;
        }

        public int CompareTo(DataVersion other) =>
            Major != other.Major ? Major.CompareTo(other.Major) : Minor.CompareTo(other.Minor);

        public bool Equals(DataVersion other) => Major == other.Major && Minor == other.Minor;
        public override bool Equals(object? obj) => obj is DataVersion other && Equals(other);
        public override int GetHashCode() => (Major << 8) | Minor;
        public static bool operator ==(DataVersion left, DataVersion right) => left.Equals(right);
        public static bool operator !=(DataVersion left, DataVersion right) => !left.Equals(right);
        public static bool operator <(DataVersion left, DataVersion right) => left.CompareTo(right) < 0;
        public static bool operator >(DataVersion left, DataVersion right) => left.CompareTo(right) > 0;
        public static bool operator <=(DataVersion left, DataVersion right) => left.CompareTo(right) <= 0;
        public static bool operator >=(DataVersion left, DataVersion right) => left.CompareTo(right) >= 0;

        public override string ToString() => $"{Major}.{Minor}";
    }
}
