using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    /// <summary>
    /// Typed access to the XML attributes of one element. Missing attributes give the spec default; unparsable values
    /// give the default and report an error. Every attribute read is remembered so the rest can be reported with
    /// <see cref="ReportUnknownAttributes"/>.
    /// </summary>
    public sealed class ElementReader
    {
        private readonly HashSet<string> _consumed = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _consumedChildren = new HashSet<string>(StringComparer.Ordinal);

        public delegate bool TryParser<T>(string? value, out T result);

        public XElement Element { get; }
        public GdtfReadContext Context { get; }

        public ElementReader(XElement element, GdtfReadContext context)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private XAttribute? Take(string name)
        {
            _consumed.Add(name);
            return Element.Attribute(name);
        }

        public bool Has(string name) => Element.Attribute(name) != null;

        /// <summary>Raw string value, or <c>null</c> if the attribute is missing.</summary>
        public string? String(string name) => Take(name)?.Value;

        public string String(string name, string defaultValue) => Take(name)?.Value ?? defaultValue;

        /// <summary>Like <see cref="String(string)"/>, but reports an error when the attribute is missing.</summary>
        public string RequiredString(string name)
        {
            var attribute = Take(name);
            if (attribute != null) return attribute.Value;
            Context.Error($"Required attribute '{name}' is missing.", Element);
            return string.Empty;
        }

        /// <summary>Reads an attribute with a custom parser. <paramref name="required"/> reports an error when it is missing.</summary>
        public T Value<T>(string name, TryParser<T> parser, T defaultValue, bool required = false)
        {
            var attribute = Take(name);
            if (attribute == null)
            {
                if (required) Context.Error($"Required attribute '{name}' is missing.", Element);
                return defaultValue;
            }

            if (parser(attribute.Value, out var result)) return result;
            Context.Error($"'{attribute.Value}' is not a valid value for '{name}'.", attribute);
            return defaultValue;
        }

        /// <summary>Reads an optional attribute; missing or empty values give <c>null</c>.</summary>
        public T? Optional<T>(string name, TryParser<T> parser) where T : struct
        {
            var attribute = Take(name);
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value)) return null;
            if (parser(attribute.Value, out var result)) return result;
            Context.Error($"'{attribute.Value}' is not a valid value for '{name}'.", attribute);
            return null;
        }

        /// <summary>
        /// Reads a <see cref="Types.Name"/>. Names with characters outside Annex C are kept as they are and reported as a warning.
        /// </summary>
        public Name Name(string name = "Name", Name defaultValue = default, bool required = false)
        {
            var attribute = Take(name);
            if (attribute == null)
            {
                if (required) Context.Error($"Required attribute '{name}' is missing.", Element);
                return defaultValue;
            }

            if (Types.Name.TryCreate(attribute.Value, out var result)) return result;
            Context.Warning($"'{attribute.Value}' contains characters that are not allowed in a GDTF Name.", attribute);
            return Types.Name.Unchecked(attribute.Value);
        }

        /// <summary>Reads a Node path. Invalid segments are kept and reported as a warning. Missing or empty gives an empty path.</summary>
        public NodePath Node(string name)
        {
            var attribute = Take(name);
            if (attribute == null || attribute.Value.Length == 0) return default;
            if (NodePath.TryParse(attribute.Value, out var path)) return path;
            Context.Warning($"'{attribute.Value}' contains characters that are not allowed in a GDTF Node path.", attribute);
            return NodePath.ParseUnchecked(attribute.Value);
        }

        /// <summary>
        /// Reads a Node path and queues it for resolution in <paramref name="scope"/>.
        /// <paramref name="prefix"/> is prepended when the attribute's starting point is below the scope root.
        /// </summary>
        public void Reference<T>(string name, ReferenceScope<T> scope, Action<T> assign, bool required = false, NodePath prefix = default)
            where T : class
        {
            var path = Node(name);
            if (path.IsEmpty)
            {
                if (required) Context.Error($"Required attribute '{name}' is missing.", Element);
                return;
            }

            var fullPath = prefix.IsEmpty ? path : new NodePath(prefix.Segments.Concat(path.Segments));
            Context.Resolve(scope, fullPath, assign, Element.Attribute(name));
        }

        public float Float(string name, float defaultValue = 0f, bool required = false) =>
            Value<float>(name, GdtfFormat.TryParseFloat, defaultValue, required);

        public int Int(string name, int defaultValue = 0, bool required = false) =>
            Value<int>(name, GdtfFormat.TryParseInt, defaultValue, required);

        public uint UInt(string name, uint defaultValue = 0, bool required = false) =>
            Value<uint>(name, GdtfFormat.TryParseUInt, defaultValue, required);

        public ulong Hex(string name, ulong defaultValue = 0, bool required = false) =>
            Value<ulong>(name, GdtfFormat.TryParseHex, defaultValue, required);

        public bool YesNo(string name, bool defaultValue) => Value<bool>(name, GdtfFormat.TryParseYesNo, defaultValue);

        public DateTime? Date(string name) => Optional<DateTime>(name, GdtfFormat.TryParseDate);

        public Guid? Guid(string name, bool required = false)
        {
            if (required && string.IsNullOrWhiteSpace(Element.Attribute(name)?.Value ?? "x"))
                Context.Error($"Required attribute '{name}' is empty.", Element);
            return Value<Guid?>(name, GdtfFormat.TryParseGuid, null, required);
        }

        public Vector3 Vector3(string name, Vector3 defaultValue = default) =>
            Value<Vector3>(name, GdtfFormat.TryParseVector3, defaultValue);

        public Matrix4x4 Matrix(string name) => Value<Matrix4x4>(name, GdtfFormat.TryParseMatrix, Matrix4x4.Identity);

        public RotationMatrix Rotation(string name) =>
            Value<RotationMatrix>(name, RotationMatrix.TryParse, RotationMatrix.Identity);

        public ColorCIE Color(string name, ColorCIE defaultValue) => Value<ColorCIE>(name, ColorCIE.TryParse, defaultValue);

        public ColorCIE? OptionalColor(string name) => Optional<ColorCIE>(name, ColorCIE.TryParse);

        public DmxValue DmxValue(string name, DmxValue defaultValue, bool required = false) =>
            Value<DmxValue>(name, Types.DmxValue.TryParse, defaultValue, required);

        /// <summary>Reads an optional DMXValue; <c>None</c> gives <c>null</c>.</summary>
        public DmxValue? OptionalDmxValue(string name)
        {
            var attribute = Element.Attribute(name);
            if (attribute != null && attribute.Value.Trim() == "None")
            {
                _consumed.Add(name);
                return null;
            }

            return Optional<DmxValue>(name, Types.DmxValue.TryParse);
        }

        public DmxAddress DmxAddress(string name, DmxAddress defaultValue) =>
            Value<DmxAddress>(name, Types.DmxAddress.TryParse, defaultValue);

        public DmxBreak DmxBreak(string name, DmxBreak defaultValue) => Value<DmxBreak>(name, Types.DmxBreak.TryParse, defaultValue);

        public int[] IntList(string name) => Value<int[]>(name, GdtfFormat.TryParseIntList, Array.Empty<int>());

        public T Enum<T>(string name, T defaultValue, bool required = false) where T : struct, System.Enum =>
            Value<T>(name, GdtfFormat.TryParseEnum, defaultValue, required);

        /// <summary>The child elements named <paramref name="name"/>, each wrapped in its own reader.</summary>
        public IEnumerable<ElementReader> Children(string name)
        {
            _consumedChildren.Add(name);
            return Element.Elements(name).Select(e => new ElementReader(e, Context)).ToList();
        }

        /// <summary>
        /// The single child element named <paramref name="name"/>, or <c>null</c>. Reports an error when it is
        /// <paramref name="required"/> and missing, and a warning when there is more than one (the first one is used).
        /// </summary>
        public ElementReader? Child(string name, bool required = false)
        {
            _consumedChildren.Add(name);
            var elements = Element.Elements(name).ToList();
            if (elements.Count == 0)
            {
                if (required) Context.Error($"Required child '{name}' is missing.", Element);
                return null;
            }

            if (elements.Count > 1) Context.Warning($"'{name}' appears {elements.Count} times; only the first one is read.", elements[1]);
            return new ElementReader(elements[0], Context);
        }

        /// <summary>Reports every child element that was not read as an <see cref="GdtfDiagnosticSeverity.Info"/> diagnostic.</summary>
        public void ReportUnknownChildren()
        {
            foreach (var child in Element.Elements())
            {
                if (_consumedChildren.Contains(child.Name.LocalName)) continue;
                Context.Info($"Unknown element '{child.Name.LocalName}' was ignored.", child);
            }
        }

        /// <summary>Reports every unread attribute and child element. Call once the element is fully read.</summary>
        public void ReportUnknown()
        {
            ReportUnknownAttributes();
            ReportUnknownChildren();
        }

        /// <summary>Reports every attribute that was not read as an <see cref="GdtfDiagnosticSeverity.Info"/> diagnostic.</summary>
        public void ReportUnknownAttributes()
        {
            foreach (var attribute in Element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || _consumed.Contains(attribute.Name.LocalName)) continue;
                Context.Info($"Unknown attribute '{attribute.Name.LocalName}' was ignored.", attribute);
            }
        }
    }
}
