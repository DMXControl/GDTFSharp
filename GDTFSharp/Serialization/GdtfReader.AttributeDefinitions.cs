using GDTFSharp.Models;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    public static partial class GdtfReader
    {
        private static void ReadAttributeDefinitions(ElementReader reader, AttributeDefinitions definitions)
        {
            var context = reader.Context;

            var activationGroups = reader.Child("ActivationGroups");
            if (activationGroups != null)
            {
                var index = 0;
                foreach (var groupReader in activationGroups.Children("ActivationGroup"))
                {
                    var group = new ActivationGroup(ReadName(groupReader, "ActivationGroup", ++index));
                    groupReader.ReportUnknown();
                    context.Register(GdtfScopes.ActivationGroups, new NodePath(group.Name), group, groupReader.Element);
                    definitions.ActivationGroups.Add(group);
                }

                activationGroups.ReportUnknown();
            }

            var featureGroups = reader.Child("FeatureGroups");
            if (featureGroups == null) context.Warning("Mandatory child 'FeatureGroups' is missing.", reader.Element);
            else
            {
                var groupIndex = 0;
                foreach (var groupReader in featureGroups.Children("FeatureGroup"))
                {
                    var group = new FeatureGroup(ReadName(groupReader, "FeatureGroup", ++groupIndex), groupReader.RequiredString("Pretty"));
                    var featureIndex = 0;
                    foreach (var featureReader in groupReader.Children("Feature"))
                    {
                        var feature = new Feature(ReadName(featureReader, "Feature", ++featureIndex));
                        featureReader.ReportUnknown();
                        group.Features.Add(feature);
                        context.Register(GdtfScopes.Features, feature.Path, feature, featureReader.Element);
                    }

                    groupReader.ReportUnknown();
                    definitions.FeatureGroups.Add(group);
                }

                featureGroups.ReportUnknown();
            }

            var attributes = reader.Child("Attributes");
            if (attributes == null) context.Warning("Mandatory child 'Attributes' is missing.", reader.Element);
            else
            {
                var index = 0;
                foreach (var attributeReader in attributes.Children("Attribute"))
                {
                    var attribute = ReadAttribute(attributeReader, ++index);
                    context.Register(GdtfScopes.Attributes, new NodePath(attribute.Name), attribute, attributeReader.Element);
                    definitions.Attributes.Add(attribute);
                }

                attributes.ReportUnknown();
            }

            reader.ReportUnknown();
        }

        private static GdtfAttribute ReadAttribute(ElementReader reader, int index)
        {
            var attribute = new GdtfAttribute(ReadName(reader, "Attribute", index), pretty: reader.RequiredString("Pretty"))
            {
                PhysicalUnit = reader.Enum("PhysicalUnit", PhysicalUnit.None),
                Color = reader.OptionalColor("Color")
            };
            reader.Reference("ActivationGroup", GdtfScopes.ActivationGroups, g => attribute.ActivationGroup = g);
            reader.Reference("Feature", GdtfScopes.Features, f => attribute.Feature = f, required: true);
            reader.Reference("MainAttribute", GdtfScopes.Attributes, a => attribute.MainAttribute = a);

            foreach (var unitReader in reader.Children("SubPhysicalUnit"))
            {
                if (!unitReader.Has("Type")) reader.Context.Warning("SubPhysicalUnit has no Type.", unitReader.Element);
                attribute.SubPhysicalUnits.Add(new SubPhysicalUnit(
                    unitReader.Enum("Type", SubPhysicalUnitType.PlacementOffset),
                    unitReader.Enum("PhysicalUnit", PhysicalUnit.None))
                {
                    PhysicalFrom = unitReader.Float("PhysicalFrom", 0f),
                    PhysicalTo = unitReader.Float("PhysicalTo", 1f)
                });
                unitReader.ReportUnknown();
            }

            reader.ReportUnknown();
            return attribute;
        }

        /// <summary>
        /// Reads the required <c>Name</c>. When it is missing, the spec default applies: the object type with its
        /// 1-based index in the parent, e.g. <c>Feature 2</c>.
        /// </summary>
        private static Name ReadName(ElementReader reader, string objectType, int index) =>
            reader.Name("Name", Name.Unchecked($"{objectType} {index}"), required: true);
    }
}
