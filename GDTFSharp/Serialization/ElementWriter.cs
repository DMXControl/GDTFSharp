using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    /// <summary>Fluent helper that writes typed values as XML attributes in GDTF format. <c>null</c> values are omitted.</summary>
    public sealed class ElementWriter
    {
        public XElement Element { get; }

        public ElementWriter(string elementName) : this(new XElement(elementName))
        {
        }

        public ElementWriter(XElement element)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
        }

        public ElementWriter String(string name, string? value)
        {
            if (value != null) Element.SetAttributeValue(name, value);
            return this;
        }

        public ElementWriter Name(string name, Name value) => String(name, value.Value);
        public ElementWriter Name(Name value) => Name("Name", value);
        public ElementWriter Name(string name, Name? value) => value == null ? this : Name(name, value.Value);

        public ElementWriter Node(string name, NodePath? value) => String(name, value?.ToString());

        public ElementWriter Float(string name, float value) => String(name, GdtfFormat.FormatFloat(value));
        public ElementWriter Float(string name, float? value) => value == null ? this : Float(name, value.Value);
        public ElementWriter Int(string name, long value) => String(name, GdtfFormat.FormatInt(value));
        public ElementWriter Int(string name, long? value) => value == null ? this : Int(name, value.Value);
        public ElementWriter Hex(string name, ulong value) => String(name, GdtfFormat.FormatHex(value));
        public ElementWriter YesNo(string name, bool value) => String(name, GdtfFormat.FormatYesNo(value));
        public ElementWriter Date(string name, DateTime? value) => value == null ? this : String(name, GdtfFormat.FormatDate(value.Value));
        public ElementWriter Guid(string name, Guid? value) => value == null ? this : String(name, GdtfFormat.FormatGuid(value.Value));
        public ElementWriter Vector2(string name, Vector2 value) => String(name, GdtfFormat.FormatVector2(value));
        public ElementWriter Vector3(string name, Vector3 value) => String(name, GdtfFormat.FormatVector3(value));
        public ElementWriter Vector3(string name, Vector3? value) => value == null ? this : Vector3(name, value.Value);
        public ElementWriter Matrix(string name, Matrix4x4 value) => String(name, GdtfFormat.FormatMatrix(value));
        public ElementWriter Rotation(string name, RotationMatrix value) => String(name, value.ToString());
        public ElementWriter Color(string name, ColorCIE? value) => String(name, value?.ToString());
        public ElementWriter DmxValue(string name, DmxValue? value) => String(name, value?.ToString());
        public ElementWriter DmxAddress(string name, DmxAddress value) => String(name, value.ToString());
        public ElementWriter DmxBreak(string name, DmxBreak value) => String(name, value.ToString());
        public ElementWriter IntList(string name, IReadOnlyCollection<int> value) => String(name, GdtfFormat.FormatIntList(value));
        public ElementWriter Enum<T>(string name, T value) where T : struct, System.Enum => String(name, GdtfFormat.FormatEnum(value));
        public ElementWriter Enum<T>(string name, T? value) where T : struct, System.Enum => value == null ? this : Enum(name, value.Value);

        public ElementWriter Child(XElement? child)
        {
            if (child != null) Element.Add(child);
            return this;
        }

        public ElementWriter Children(IEnumerable<XElement> children)
        {
            foreach (var child in children) Element.Add(child);
            return this;
        }

        public static implicit operator XElement(ElementWriter writer) => writer.Element;
    }
}
