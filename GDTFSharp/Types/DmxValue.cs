using System;
using System.Globalization;

namespace GDTFSharp.Types
{
    /// <summary>
    /// GDTF <c>DMXValue</c> value type, written as <c>Uint/n</c> (byte mirroring) or <c>Uint/ns</c> (byte shifting),
    /// where n is the byte count the value is expressed in. The byte count is independent of the resolution
    /// of the DMX channel; use <see cref="ToResolution"/> to convert.
    /// </summary>
    public readonly struct DmxValue : IEquatable<DmxValue>
    {
        /// <summary>Maximum supported byte count. GDTF channels are at most 4 bytes; 8 leaves headroom for intermediate math.</summary>
        public const int MaxByteCount = 8;

        /// <summary>The value, in <see cref="ByteCount"/> bytes.</summary>
        public ulong Value { get; }

        /// <summary>Number of bytes <see cref="Value"/> is expressed in (1 to <see cref="MaxByteCount"/>).</summary>
        public int ByteCount { get; }

        /// <summary>True for the <c>s</c> suffix: convert by shifting instead of mirroring.</summary>
        public bool ByteShifting { get; }

        public DmxValue(ulong value, int byteCount, bool byteShifting = false)
        {
            if (byteCount < 1 || byteCount > MaxByteCount)
                throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, $"Byte count must be between 1 and {MaxByteCount}.");
            if (byteCount < 8 && value > MaxValueFor(byteCount))
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Value does not fit into {byteCount} byte(s).");
            Value = value;
            ByteCount = byteCount;
            ByteShifting = byteShifting;
        }

        public static ulong MaxValueFor(int byteCount) => byteCount >= 8 ? ulong.MaxValue : (1UL << (8 * byteCount)) - 1;

        /// <summary>
        /// Converts the value to a channel resolution of <paramref name="byteCount"/> bytes.
        /// Going up, byte mirroring repeats the value's bytes into the new low bytes (<c>255/1</c> becomes 65535 in 2 bytes),
        /// byte shifting fills them with zeros (<c>255/1s</c> becomes 65280). Going down, the low bytes are dropped.
        /// </summary>
        public ulong ToResolution(int byteCount)
        {
            if (byteCount < 1 || byteCount > MaxByteCount)
                throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, $"Byte count must be between 1 and {MaxByteCount}.");

            if (byteCount == ByteCount) return Value;
            if (byteCount < ByteCount) return Value >> (8 * (ByteCount - byteCount));

            var extraBytes = byteCount - ByteCount;
            var result = Value << (8 * extraBytes);
            if (ByteShifting) return result;

            // Mirroring: fill each new low byte with the value's bytes again, most significant first.
            for (var i = 0; i < extraBytes; i++)
            {
                var sourceByte = (Value >> (8 * (ByteCount - 1 - i % ByteCount))) & 0xFF;
                result |= sourceByte << (8 * (extraBytes - 1 - i));
            }

            return result;
        }

        public DmxValue WithResolution(int byteCount) => new DmxValue(ToResolution(byteCount), byteCount, ByteShifting);

        public static DmxValue Parse(string value)
        {
            if (!TryParse(value, out var result))
                throw new FormatException($"'{value}' is not a valid GDTF DMXValue.");
            return result;
        }

        public static bool TryParse(string? value, out DmxValue result)
        {
            result = default;
            if (value == null) return false;
            var text = value.Trim();
            var slash = text.IndexOf('/');
            if (slash <= 0) return false;

            var countText = text.Substring(slash + 1);
            var shifting = countText.EndsWith("s", StringComparison.Ordinal);
            if (shifting) countText = countText.Substring(0, countText.Length - 1);

            if (!ulong.TryParse(text.Substring(0, slash), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
                !int.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, out var count) ||
                count < 1 || count > MaxByteCount ||
                (count < 8 && number > MaxValueFor(count)))
                return false;

            result = new DmxValue(number, count, shifting);
            return true;
        }

        public bool Equals(DmxValue other) => Value == other.Value && ByteCount == other.ByteCount && ByteShifting == other.ByteShifting;
        public override bool Equals(object? obj) => obj is DmxValue other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, ByteCount, ByteShifting);
        public static bool operator ==(DmxValue left, DmxValue right) => left.Equals(right);
        public static bool operator !=(DmxValue left, DmxValue right) => !left.Equals(right);

        public override string ToString() =>
            $"{Value.ToString(CultureInfo.InvariantCulture)}/{ByteCount.ToString(CultureInfo.InvariantCulture)}{(ByteShifting ? "s" : "")}";
    }
}
