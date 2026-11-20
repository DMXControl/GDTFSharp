using System.Collections.Generic;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>
    /// A Fixture Type Attribute (XML node <c>Attribute</c>), e.g. Dimmer, Pan or ColorAdd_R.
    /// Named <c>GdtfAttribute</c> to avoid clashing with <see cref="System.Attribute"/>.
    /// </summary>
    public sealed class GdtfAttribute
    {
        public Name Name { get; set; }

        /// <summary>Display name of the attribute.</summary>
        public string Pretty { get; set; }

        /// <summary>Optional activation group the attribute is activated together with.</summary>
        public ActivationGroup? ActivationGroup { get; set; }

        /// <summary>The feature the attribute belongs to. Required by the spec.</summary>
        public Feature? Feature { get; set; }

        /// <summary>
        /// Optional main attribute, for attributes that are a variation of another one
        /// (e.g. <c>Gobo1WheelSpin</c> of <c>Gobo1</c>).
        /// </summary>
        public GdtfAttribute? MainAttribute { get; set; }

        /// <summary>Physical unit of the attribute's values. Default: <see cref="Types.PhysicalUnit.None"/>.</summary>
        public PhysicalUnit PhysicalUnit { get; set; }

        /// <summary>Optional color of the attribute, e.g. for color mixing attributes.</summary>
        public ColorCIE? Color { get; set; }

        /// <summary>Additional physical properties of the attribute, e.g. DutyCycle for pulse attributes (Annex F).</summary>
        public IList<SubPhysicalUnit> SubPhysicalUnits { get; } = new List<SubPhysicalUnit>();

        public GdtfAttribute(Name name, Feature? feature = null, string? pretty = null)
        {
            Name = name;
            Feature = feature;
            Pretty = pretty ?? name.Value;
        }

        public override string ToString() => Name.Value;
    }

    /// <summary>An additional physical property of a <see cref="GdtfAttribute"/> (XML node <c>SubPhysicalUnit</c>).</summary>
    public sealed class SubPhysicalUnit
    {
        public SubPhysicalUnitType Type { get; set; }

        public PhysicalUnit PhysicalUnit { get; set; }

        /// <summary>Default physical from, in <see cref="PhysicalUnit"/>. Default: 0.</summary>
        public float PhysicalFrom { get; set; }

        /// <summary>Default physical to, in <see cref="PhysicalUnit"/>. Default: 1.</summary>
        public float PhysicalTo { get; set; } = 1f;

        public SubPhysicalUnit(SubPhysicalUnitType type, PhysicalUnit physicalUnit = PhysicalUnit.None)
        {
            Type = type;
            PhysicalUnit = physicalUnit;
        }

        public override string ToString() => $"{Type} {PhysicalFrom}..{PhysicalTo} {PhysicalUnit}";
    }
}
