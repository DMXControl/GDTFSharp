using System;
using System.Text.RegularExpressions;

namespace GDTFSharp.Types
{
    /// <summary>
    /// GDTF <c>Name</c> value type: a restricted literal used for unique object names.
    /// Allowed characters are listed in DIN SPEC 15800 Annex C: within the first 128 code points only space,
    /// <c>" # % ' ( ) * + - /</c>, digits, <c>: ; &lt; = &gt; @</c>, letters, <c>_</c> and <c>`</c> are allowed;
    /// every code point above 127 is allowed. The dot is excluded because it separates the segments of a <see cref="NodePath"/>.
    /// </summary>
    public readonly struct Name : IEquatable<Name>
    {
        private static readonly Regex InvalidCharacters =
            new Regex(@"[\u0000-\u001F!$&,.?\[\\\]^{|}~\u007F]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly string? _value;

        /// <summary>The name; empty for a default <see cref="Name"/>.</summary>
        public string Value => _value ?? string.Empty;

        /// <summary>True for an empty name.</summary>
        public bool IsEmpty => string.IsNullOrEmpty(_value);

        /// <exception cref="ArgumentException">The value contains characters not allowed by Annex C.</exception>
        public Name(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!IsValid(value))
                throw new ArgumentException($"'{value}' contains characters that are not allowed in a GDTF Name.", nameof(value));
            _value = value;
        }

        private Name(string value, bool _)
        {
            _value = value;
        }

        public static bool IsValid(string? value) => value != null && !InvalidCharacters.IsMatch(value);

        public static bool TryCreate(string? value, out Name name)
        {
            if (IsValid(value))
            {
                name = new Name(value!, false);
                return true;
            }

            name = default;
            return false;
        }

        /// <summary>Creates a name without validation. Used by the reader to keep data from non-conforming files.</summary>
        internal static Name Unchecked(string value) => new Name(value, false);

        public bool Equals(Name other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is Name other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public static bool operator ==(Name left, Name right) => left.Equals(right);
        public static bool operator !=(Name left, Name right) => !left.Equals(right);

        public static implicit operator string(Name name) => name.Value;
        public static explicit operator Name(string value) => new Name(value);

        public override string ToString() => Value;
    }
}
