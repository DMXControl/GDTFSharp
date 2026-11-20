using System;
using System.Globalization;

namespace GDTFSharp.Types
{
    /// <summary>
    /// GDTF <c>DMXAddress</c> value type: an absolute DMX address, alternatively written as <c>Universe.Address</c>
    /// (universe starting at 1, address 1 to 512). Absolute address = (Universe - 1) * 512 + Address.
    /// </summary>
    public readonly struct DmxAddress : IEquatable<DmxAddress>, IComparable<DmxAddress>
    {
        public const int AddressesPerUniverse = 512;

        public int Absolute { get; }

        /// <summary>1-based universe of a positive absolute address.</summary>
        public int Universe => (Absolute - 1) / AddressesPerUniverse + 1;

        /// <summary>Address within <see cref="Universe"/> (1 to 512) of a positive absolute address.</summary>
        public int Address => (Absolute - 1) % AddressesPerUniverse + 1;

        public DmxAddress(int absolute)
        {
            Absolute = absolute;
        }

        public static DmxAddress FromUniverse(int universe, int address)
        {
            if (universe < 1) throw new ArgumentOutOfRangeException(nameof(universe), universe, "Universes start at 1.");
            if (address < 1 || address > AddressesPerUniverse)
                throw new ArgumentOutOfRangeException(nameof(address), address, "Address must be between 1 and 512.");
            return new DmxAddress((universe - 1) * AddressesPerUniverse + address);
        }

        public static DmxAddress Parse(string value)
        {
            if (!TryParse(value, out var result))
                throw new FormatException($"'{value}' is not a valid GDTF DMXAddress.");
            return result;
        }

        public static bool TryParse(string? value, out DmxAddress result)
        {
            result = default;
            if (value == null) return false;
            var text = value.Trim();
            var dot = text.IndexOf('.');
            if (dot < 0)
            {
                if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var absolute)) return false;
                result = new DmxAddress(absolute);
                return true;
            }

            if (!int.TryParse(text.Substring(0, dot), NumberStyles.None, CultureInfo.InvariantCulture, out var universe) ||
                !int.TryParse(text.Substring(dot + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var address) ||
                universe < 1 || address < 1 || address > AddressesPerUniverse)
                return false;

            result = FromUniverse(universe, address);
            return true;
        }

        public int CompareTo(DmxAddress other) => Absolute.CompareTo(other.Absolute);
        public bool Equals(DmxAddress other) => Absolute == other.Absolute;
        public override bool Equals(object? obj) => obj is DmxAddress other && Equals(other);
        public override int GetHashCode() => Absolute;
        public static bool operator ==(DmxAddress left, DmxAddress right) => left.Equals(right);
        public static bool operator !=(DmxAddress left, DmxAddress right) => !left.Equals(right);

        /// <summary>Writes the absolute form, which is what the XSD accepts.</summary>
        public override string ToString() => Absolute.ToString(CultureInfo.InvariantCulture);
    }
}
