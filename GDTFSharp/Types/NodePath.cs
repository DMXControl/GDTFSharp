using System;
using System.Collections.Generic;
using System.Linq;

namespace GDTFSharp.Types
{
    /// <summary>
    /// GDTF <c>Node</c> value type: a link to an element, written as <c>Name.Name.Name...</c>.
    /// The element the path starts from is defined separately by every XML attribute that uses it.
    /// </summary>
    public readonly struct NodePath : IEquatable<NodePath>
    {
        private readonly Name[]? _segments;

        /// <summary>The names of the path, from the starting point down.</summary>
        public IReadOnlyList<Name> Segments => _segments ?? Array.Empty<Name>();

        public bool IsEmpty => _segments == null || _segments.Length == 0;

        public NodePath(params Name[] segments)
        {
            _segments = segments?.ToArray() ?? throw new ArgumentNullException(nameof(segments));
        }

        public NodePath(IEnumerable<Name> segments) : this(segments.ToArray())
        {
        }

        /// <summary>Parses a dot-separated path. An empty string gives an empty path.</summary>
        /// <exception cref="FormatException">A segment is not a valid <see cref="Name"/>.</exception>
        public static NodePath Parse(string value)
        {
            if (!TryParse(value, out var path))
                throw new FormatException($"'{value}' is not a valid GDTF node path.");
            return path;
        }

        public static bool TryParse(string? value, out NodePath path)
        {
            path = default;
            if (value == null) return false;
            if (value.Length == 0) return true;

            var parts = value.Split('.');
            var segments = new Name[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0 || !Name.TryCreate(parts[i], out segments[i])) return false;
            }

            path = new NodePath(segments);
            return true;
        }

        /// <summary>Splits on dots without validating the segments. Used by the reader for non-conforming files.</summary>
        internal static NodePath ParseUnchecked(string value) =>
            value.Length == 0 ? default : new NodePath(value.Split('.').Select(Name.Unchecked).ToArray());

        public NodePath Append(Name segment) => new NodePath(Segments.Append(segment));

        public bool Equals(NodePath other) => Segments.SequenceEqual(other.Segments);
        public override bool Equals(object? obj) => obj is NodePath other && Equals(other);

        public override int GetHashCode()
        {
            var hash = 17;
            foreach (var segment in Segments) hash = hash * 31 + segment.GetHashCode();
            return hash;
        }

        public static bool operator ==(NodePath left, NodePath right) => left.Equals(right);
        public static bool operator !=(NodePath left, NodePath right) => !left.Equals(right);

        public override string ToString() => string.Join(".", Segments);
    }
}
