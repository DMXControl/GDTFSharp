using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace GDTFSharp.Types
{
    /// <summary>
    /// Parsing and formatting of the GDTF XML attribute value types (DIN SPEC 15800, Table 1).
    /// Everything is culture invariant: floats always use "." as decimal separator.
    /// </summary>
    public static class GdtfFormat
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly Regex BracketedRow = new Regex(@"\{([^{}]*)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public const string DateFormat = "yyyy-MM-ddTHH:mm:ss";

        // ---- Numbers ----------------------------------------------------------------------------------------------

        public static bool TryParseFloat(string? value, out float result) =>
            float.TryParse(value?.Trim(), NumberStyles.Float, Invariant, out result);

        /// <summary>Shortest round-trippable representation, never in exponent notation.</summary>
        public static string FormatFloat(float value)
        {
            var text = value.ToString("R", Invariant);
            if (text.IndexOf('E') < 0) return text;
            return Math.Abs(value) < 7.9e27f ? ((decimal)double.Parse(text, Invariant)).ToString(Invariant) : text;
        }

        public static bool TryParseInt(string? value, out int result) =>
            int.TryParse(value?.Trim(), NumberStyles.AllowLeadingSign, Invariant, out result);

        public static bool TryParseUInt(string? value, out uint result) =>
            uint.TryParse(value?.Trim(), NumberStyles.AllowLeadingSign, Invariant, out result);

        public static string FormatInt(long value) => value.ToString(Invariant);

        /// <summary>Parses <c>0x</c>-prefixed hexadecimal; the prefix is optional when reading.</summary>
        public static bool TryParseHex(string? value, out ulong result)
        {
            result = 0;
            if (value == null) return false;
            var text = value.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text.Substring(2);
            return text.Length > 0 && ulong.TryParse(text, NumberStyles.AllowHexSpecifier, Invariant, out result);
        }

        public static string FormatHex(ulong value) => "0x" + value.ToString("X", Invariant);

        // ---- YesNo, Date, GUID ------------------------------------------------------------------------------------

        /// <summary>Parses the <c>Yes</c>/<c>No</c> enum, ignoring case.</summary>
        public static bool TryParseYesNo(string? value, out bool result)
        {
            result = false;
            switch (value?.Trim().ToLowerInvariant())
            {
                case "yes":
                    result = true;
                    return true;
                case "no":
                    return true;
                default:
                    return false;
            }
        }

        public static string FormatYesNo(bool value) => value ? "Yes" : "No";

        /// <summary>Parses a UTC date. Accepts the spec format and other ISO 8601 variants found in the wild.</summary>
        public static bool TryParseDate(string? value, out DateTime result)
        {
            var text = value?.Trim();
            const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
            return DateTime.TryParseExact(text, DateFormat, Invariant, styles, out result) ||
                   DateTime.TryParse(text, Invariant, styles, out result);
        }

        public static string FormatDate(DateTime value) =>
            (value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value).ToString(DateFormat, Invariant);

        /// <summary>Parses a GUID. An empty or whitespace value is valid and gives <c>null</c> (the XSD allows it).</summary>
        public static bool TryParseGuid(string? value, out Guid? result)
        {
            result = null;
            if (value == null) return false;
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (!Guid.TryParseExact(value.Trim(), "D", out var guid)) return false;
            result = guid;
            return true;
        }

        public static string FormatGuid(Guid value) => value.ToString("D").ToUpperInvariant();

        // ---- Vectors and matrices ---------------------------------------------------------------------------------

        /// <summary>Parses exactly <paramref name="count"/> comma-separated floats, optionally in braces.</summary>
        public static bool TryParseFloatList(string? value, int count, out float[] result)
        {
            result = Array.Empty<float>();
            if (value == null) return false;
            var text = value.Trim();
            if (text.StartsWith("{", StringComparison.Ordinal) && text.EndsWith("}", StringComparison.Ordinal))
                text = text.Substring(1, text.Length - 2);

            var parts = text.Split(',');
            if (parts.Length != count) return false;
            var floats = new float[count];
            for (var i = 0; i < count; i++)
            {
                if (!TryParseFloat(parts[i], out floats[i])) return false;
            }

            result = floats;
            return true;
        }

        public static string FormatFloatList(IEnumerable<float> values) => string.Join(",", values.Select(FormatFloat));

        /// <summary>Parses two comma-separated floats, e.g. an AnimationSystem point.</summary>
        public static bool TryParseVector2(string? value, out Vector2 result)
        {
            result = Vector2.Zero;
            if (!TryParseFloatList(value, 2, out var v)) return false;
            result = new Vector2(v[0], v[1]);
            return true;
        }

        public static string FormatVector2(Vector2 value) => FormatFloatList(new[] { value.X, value.Y });

        public static bool TryParseVector3(string? value, out Vector3 result)
        {
            result = Vector3.Zero;
            if (value != null && value.Trim() == "None") return true;
            if (!TryParseFloatList(value, 3, out var v)) return false;
            result = new Vector3(v[0], v[1], v[2]);
            return true;
        }

        public static string FormatVector3(Vector3 value) => FormatFloatList(new[] { value.X, value.Y, value.Z });

        /// <summary>Parses <paramref name="rows"/> brace-enclosed rows of <paramref name="columns"/> floats, row-major.</summary>
        public static bool TryParseBracketedRows(string? value, int rows, int columns, out float[] result)
        {
            result = Array.Empty<float>();
            if (value == null) return false;
            var matches = BracketedRow.Matches(value);
            if (matches.Count != rows) return false;
            if (BracketedRow.Replace(value, "").Trim().Length != 0) return false;

            var values = new float[rows * columns];
            for (var row = 0; row < rows; row++)
            {
                if (!TryParseFloatList(matches[row].Groups[1].Value, columns, out var rowValues)) return false;
                Array.Copy(rowValues, 0, values, row * columns, columns);
            }

            result = values;
            return true;
        }

        public static string FormatBracketedRows(IReadOnlyList<float> values, int columns)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < values.Count; i += columns)
            {
                builder.Append('{').Append(FormatFloatList(values.Skip(i).Take(columns))).Append('}');
            }

            return builder.ToString();
        }

        /// <summary>Parses an array of ColorCIE written as <c>{x,y,Y}{x,y,Y}…</c>. Empty gives an empty list.</summary>
        public static bool TryParseColorArray(string? value, out ColorCIE[] result)
        {
            result = Array.Empty<ColorCIE>();
            if (value == null) return false;
            if (value.Trim().Length == 0) return true;
            var matches = BracketedRow.Matches(value);
            if (matches.Count == 0 || BracketedRow.Replace(value, "").Trim().Length != 0) return false;
            var colors = new ColorCIE[matches.Count];
            for (var i = 0; i < matches.Count; i++)
            {
                if (!ColorCIE.TryParse(matches[i].Groups[1].Value, out colors[i])) return false;
            }

            result = colors;
            return true;
        }

        public static string FormatColorArray(IEnumerable<ColorCIE> colors) => string.Concat(colors.Select(c => "{" + c + "}"));

        /// <summary>
        /// Parses a GDTF <c>Matrix</c>. Rows of the file map to rows of <see cref="Matrix4x4"/> (row i, column j to Mij),
        /// so the translation ends up in M14, M24, M34. <c>None</c> gives the identity.
        /// </summary>
        public static bool TryParseMatrix(string? value, out Matrix4x4 result)
        {
            result = Matrix4x4.Identity;
            if (value != null && value.Trim() == "None") return true;
            if (!TryParseBracketedRows(value, 4, 4, out var m)) return false;
            result = new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
            return true;
        }

        public static string FormatMatrix(Matrix4x4 m) => FormatBracketedRows(new[]
        {
            m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44
        }, 4);

        /// <summary>Parses a comma-separated integer list such as a DMXChannel <c>Offset</c>. <c>None</c> or empty gives an empty list.</summary>
        public static bool TryParseIntList(string? value, out int[] result)
        {
            result = Array.Empty<int>();
            if (value == null) return false;
            var text = value.Trim();
            if (text.Length == 0 || text == "None") return true;
            var parts = text.Split(',');
            var ints = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!TryParseInt(parts[i], out ints[i])) return false;
            }

            result = ints;
            return true;
        }

        public static string FormatIntList(IReadOnlyCollection<int> values) =>
            values.Count == 0 ? "None" : string.Join(",", values.Select(v => v.ToString(Invariant)));

        // ---- Enums ------------------------------------------------------------------------------------------------

        private static readonly ConcurrentDictionary<System.Type, EnumNames> EnumCache = new ConcurrentDictionary<System.Type, EnumNames>();

        private sealed class EnumNames
        {
            public readonly Dictionary<string, object> ByName = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<object, string> ByValue = new Dictionary<object, string>();
        }

        private static EnumNames GetEnumNames(System.Type type) => EnumCache.GetOrAdd(type, t =>
        {
            var names = new EnumNames();
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var name = field.GetCustomAttribute<XmlEnumAttribute>()?.Name ?? field.Name;
                var value = field.GetValue(null)!;
                names.ByName[field.Name] = value;
                names.ByName[name] = value;
                names.ByValue[value] = name;
            }

            return names;
        });

        /// <summary>
        /// Parses a GDTF enum literal, matching the XML name (<see cref="XmlEnumAttribute"/> or the member name)
        /// or the C# member name, ignoring case.
        /// </summary>
        public static bool TryParseEnum<T>(string? value, out T result) where T : struct, Enum
        {
            result = default;
            if (value == null) return false;
            var names = GetEnumNames(typeof(T));
            var text = value.Trim();
            if (names.ByName.TryGetValue(text, out var match))
            {
                result = (T)match;
                return true;
            }

            return false;
        }

        public static string FormatEnum<T>(T value) where T : struct, Enum =>
            GetEnumNames(typeof(T)).ByValue.TryGetValue(value, out var name) ? name : value.ToString();
    }
}
