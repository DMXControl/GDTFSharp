using System.Linq;
using System.Xml.Linq;
using GDTFSharp.Models;

namespace GDTFSharp.Serialization
{
    public static partial class GdtfWriter
    {
        private static XElement WriteAttributeDefinitions(AttributeDefinitions definitions) =>
            new ElementWriter("AttributeDefinitions")
                .Child(new ElementWriter("ActivationGroups").Children(
                    definitions.ActivationGroups.Select(g => (XElement)new ElementWriter("ActivationGroup").Name(g.Name))))
                .Child(new ElementWriter("FeatureGroups").Children(definitions.FeatureGroups.Select(WriteFeatureGroup)))
                .Child(new ElementWriter("Attributes").Children(definitions.Attributes.Select(WriteAttribute)));

        private static XElement WriteFeatureGroup(FeatureGroup group) =>
            new ElementWriter("FeatureGroup")
                .Name(group.Name)
                .String("Pretty", group.Pretty)
                .Children(group.Features.Select(f => (XElement)new ElementWriter("Feature").Name(f.Name)));

        // A missing Feature (required by the spec) is not written; validation reports it.
        private static XElement WriteAttribute(GdtfAttribute attribute) =>
            new ElementWriter("Attribute")
                .Name(attribute.Name)
                .String("Pretty", attribute.Pretty)
                .String("ActivationGroup", attribute.ActivationGroup?.Name.Value)
                .Node("Feature", attribute.Feature?.Path)
                .String("MainAttribute", attribute.MainAttribute?.Name.Value)
                .Enum("PhysicalUnit", attribute.PhysicalUnit)
                .Color("Color", attribute.Color)
                .Children(attribute.SubPhysicalUnits.Select(unit => (XElement)new ElementWriter("SubPhysicalUnit")
                    .Enum("Type", unit.Type)
                    .Enum("PhysicalUnit", unit.PhysicalUnit)
                    .Float("PhysicalFrom", unit.PhysicalFrom)
                    .Float("PhysicalTo", unit.PhysicalTo)));
    }
}
