using System;
using System.Globalization;

namespace GDTFSharp.Types
{
    /// <summary>
    /// DMX break of a DMX channel or a GeometryReference <c>Break</c>: either a break number or <c>Overwrite</c>,
    /// which means the break is taken from the GeometryReference that references the geometry.
    /// </summary>
    public readonly struct DmxBreak : IEquatable<DmxBreak>
    {
        public static readonly DmxBreak Overwrite = new DmxBreak(0, true);
        public static readonly DmxBreak Default = new DmxBreak(1);

        private readonly int _number;

        public bool IsOverwrite { get; }

        /// <exception cref="InvalidOperationException">The break is <see cref="Overwrite"/>.</exception>
        public int Number => IsOverwrite ? throw new InvalidOperationException("An Overwrite break has no number.") : _number;

        public DmxBreak(int number) : this(number, false)
        {
        }

        private DmxBreak(int number, bool overwrite)
        {
            _number = number;
            IsOverwrite = overwrite;
        }

        public static DmxBreak Parse(string value)
        {
            if (!TryParse(value, out var result))
                throw new FormatException($"'{value}' is not a valid GDTF DMXBreak.");
            return result;
        }

        public static bool TryParse(string? value, out DmxBreak result)
        {
            result = default;
            if (value == null) return false;
            var text = value.Trim();
            if (text == "Overwrite")
            {
                result = Overwrite;
                return true;
            }

            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)) return false;
            result = new DmxBreak(number);
            return true;
        }

        public bool Equals(DmxBreak other) => IsOverwrite == other.IsOverwrite && _number == other._number;
        public override bool Equals(object? obj) => obj is DmxBreak other && Equals(other);
        public override int GetHashCode() => IsOverwrite ? -1 : _number;
        public static bool operator ==(DmxBreak left, DmxBreak right) => left.Equals(right);
        public static bool operator !=(DmxBreak left, DmxBreak right) => !left.Equals(right);

        public override string ToString() => IsOverwrite ? "Overwrite" : _number.ToString(CultureInfo.InvariantCulture);
    }
}
