using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    /// <summary>The result of reading a description.xml: the document and every problem found on the way.</summary>
    public sealed class GdtfReadResult
    {
        public GdtfDocument Document { get; }
        public IReadOnlyList<GdtfDiagnostic> Diagnostics { get; }

        public bool HasErrors => Diagnostics.Any(d => d.Severity == GdtfDiagnosticSeverity.Error);

        public GdtfReadResult(GdtfDocument document, IReadOnlyList<GdtfDiagnostic> diagnostics)
        {
            Document = document;
            Diagnostics = diagnostics;
        }
    }

    /// <summary>Reads a GDTF <c>description.xml</c> into a <see cref="GdtfDocument"/>.</summary>
    public static partial class GdtfReader
    {
        public static GdtfReadResult Read(Stream xml, GdtfReadOptions? options = null)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(xml, LoadOptions.SetLineInfo);
            }
            catch (XmlException e)
            {
                throw new GdtfReadException($"description.xml is not well-formed XML: {e.Message}", e);
            }

            return Read(document, options);
        }

        public static GdtfReadResult Read(string xml, GdtfReadOptions? options = null)
        {
            XDocument document;
            try
            {
                document = XDocument.Parse(xml, LoadOptions.SetLineInfo);
            }
            catch (XmlException e)
            {
                throw new GdtfReadException($"description.xml is not well-formed XML: {e.Message}", e);
            }

            return Read(document, options);
        }

        public static GdtfReadResult Read(XDocument xml, GdtfReadOptions? options = null)
        {
            var context = new GdtfReadContext(options);
            var root = xml.Root;
            if (root == null || root.Name.LocalName != "GDTF")
                throw new GdtfReadException($"Expected root element 'GDTF' but found '{root?.Name.LocalName}'.");

            var rootReader = new ElementReader(root, context);
            context.DataVersion = rootReader.Value<DataVersion>("DataVersion", DataVersion.TryParse, DataVersion.Current, required: true);
            rootReader.ReportUnknownAttributes();
            if (context.DataVersion > DataVersion.Current)
                context.Warning($"DataVersion {context.DataVersion} is newer than {DataVersion.Current}; unknown content is ignored.", root);

            var fixtureTypeElement = root.Element("FixtureType");
            if (fixtureTypeElement == null) throw new GdtfReadException("The GDTF node has no FixtureType child.");
            foreach (var other in root.Elements().Where(e => e != fixtureTypeElement))
                context.Info($"Unknown element '{other.Name.LocalName}' was ignored.", other);

            var fixtureType = ReadFixtureType(fixtureTypeElement, context);
            context.ResolveReferences();

            return new GdtfReadResult(new GdtfDocument(fixtureType) { DataVersion = context.DataVersion }, context.Diagnostics);
        }

        private static FixtureType ReadFixtureType(XElement element, GdtfReadContext context)
        {
            var reader = new ElementReader(element, context);
            var fixtureType = new FixtureType(reader.Name(required: true), reader.Guid("FixtureTypeID", required: true) ?? Guid.Empty)
            {
                ShortName = reader.String("ShortName", string.Empty),
                LongName = reader.String("LongName", string.Empty),
                Manufacturer = reader.String("Manufacturer", string.Empty),
                Description = reader.String("Description", string.Empty),
                ThumbnailOffsetX = reader.Int("ThumbnailOffsetX"),
                ThumbnailOffsetY = reader.Int("ThumbnailOffsetY"),
                RefFT = reader.Guid("RefFT"),
                CanHaveChildren = reader.YesNo("CanHaveChildren", true)
            };
            var thumbnail = reader.String("Thumbnail");
            fixtureType.Thumbnail = string.IsNullOrEmpty(thumbnail) ? null : thumbnail;
            reader.ReportUnknownAttributes();

            var lastIndex = -1;
            var readSections = new HashSet<string>(StringComparer.Ordinal);
            foreach (var section in element.Elements())
            {
                var name = section.Name.LocalName;
                var index = IndexOf(FixtureType.SectionOrder, name);
                if (index < 0)
                    context.Warning($"Unknown FixtureType section '{name}' is kept as is.", section);
                else if (index < lastIndex)
                    context.Warning($"Section '{name}' is out of the order the spec requires; it is written in order.", section);
                lastIndex = Math.Max(lastIndex, index);

                switch (name)
                {
                    case "AttributeDefinitions" when !readSections.Contains(name):
                        ReadAttributeDefinitions(new ElementReader(section, context), fixtureType.AttributeDefinitions);
                        break;
                    case "Wheels" when !readSections.Contains(name):
                        ReadWheels(new ElementReader(section, context), fixtureType);
                        break;
                    case "PhysicalDescriptions" when !readSections.Contains(name):
                        ReadPhysicalDescriptions(new ElementReader(section, context), fixtureType.PhysicalDescriptions);
                        break;
                    case "Geometries" when !readSections.Contains(name):
                        ReadGeometries(new ElementReader(section, context), fixtureType);
                        break;
                    case "Models" when !readSections.Contains(name):
                        ReadModels(new ElementReader(section, context), fixtureType);
                        break;
                    case "Revisions" when !readSections.Contains(name):
                        ReadRevisions(new ElementReader(section, context), fixtureType);
                        break;
                    case "FTPresets" when !readSections.Contains(name):
                        ReadFTPresets(new ElementReader(section, context), fixtureType);
                        break;
                    case "Protocols" when !readSections.Contains(name):
                        ReadProtocols(new ElementReader(section, context), fixtureType.Protocols);
                        break;
                    default:
                        if (index >= 0 && readSections.Contains(name))
                        {
                            context.Warning($"Section '{name}' appears more than once; only the first one is read.", section);
                            continue;
                        }

                        // Sections not modelled yet are kept verbatim.
                        fixtureType.UnparsedSections.Add(new XElement(section));
                        break;
                }

                readSections.Add(name);
            }

            foreach (var mandatory in FixtureType.MandatorySections)
            {
                if (element.Element(mandatory) == null) context.Warning($"Mandatory section '{mandatory}' is missing.", element);
            }

            return fixtureType;
        }

        private static int IndexOf(IReadOnlyList<string> list, string value)
        {
            for (var i = 0; i < list.Count; i++)
                if (list[i] == value) return i;
            return -1;
        }
    }
}
