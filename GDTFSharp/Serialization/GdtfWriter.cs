using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    /// <summary>Writes a <see cref="GdtfDocument"/> as a GDTF <c>description.xml</c>, always in <see cref="DataVersion.Current"/> format.</summary>
    public static partial class GdtfWriter
    {
        public static XDocument ToXDocument(GdtfDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var root = new ElementWriter("GDTF").String("DataVersion", DataVersion.Current.ToString());
            root.Child(WriteFixtureType(document.FixtureType));
            return new XDocument(new XDeclaration("1.0", "UTF-8", null), root.Element);
        }

        public static void Write(GdtfDocument document, Stream stream, bool indent = true)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = indent,
                IndentChars = "  ",
                CloseOutput = false
            };
            using var writer = XmlWriter.Create(stream, settings);
            ToXDocument(document).Save(writer);
        }

        public static string WriteToString(GdtfDocument document, bool indent = true)
        {
            using var stream = new MemoryStream();
            Write(document, stream, indent);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static XElement WriteFixtureType(FixtureType fixtureType)
        {
            var element = new ElementWriter("FixtureType")
                .Name(fixtureType.Name)
                .String("ShortName", fixtureType.ShortName)
                .String("LongName", fixtureType.LongName)
                .String("Manufacturer", fixtureType.Manufacturer)
                .String("Description", fixtureType.Description)
                .Guid("FixtureTypeID", fixtureType.FixtureTypeId)
                .String("Thumbnail", fixtureType.Thumbnail)
                .Int("ThumbnailOffsetX", fixtureType.ThumbnailOffsetX)
                .Int("ThumbnailOffsetY", fixtureType.ThumbnailOffsetY)
                .Guid("RefFT", fixtureType.RefFT)
                .YesNo("CanHaveChildren", fixtureType.CanHaveChildren);

            var sections = new List<XElement>();
            sections.Add(WriteAttributeDefinitions(fixtureType.AttributeDefinitions));
            sections.Add(WriteWheels(fixtureType));
            sections.Add(WritePhysicalDescriptions(fixtureType.PhysicalDescriptions));
            sections.Add(WriteModels(fixtureType));
            sections.Add(WriteRevisions(fixtureType));
            sections.Add(WriteFTPresets(fixtureType));
            sections.Add(WriteProtocols(fixtureType.Protocols));
            foreach (var unparsed in fixtureType.UnparsedSections)
            {
                if (sections.Any(s => s.Name == unparsed.Name))
                    throw new InvalidOperationException(
                        $"UnparsedSections contains '{unparsed.Name.LocalName}', which is modelled; use the FixtureType property instead.");
                sections.Add(new XElement(unparsed));
            }

            foreach (var mandatory in FixtureType.MandatorySections)
            {
                if (sections.All(s => s.Name.LocalName != mandatory)) sections.Add(new XElement(mandatory));
            }

            element.Children(sections.OrderBy(s => SectionRank(s.Name.LocalName)));
            return element;
        }

        private static int SectionRank(string name)
        {
            for (var i = 0; i < FixtureType.SectionOrder.Count; i++)
                if (FixtureType.SectionOrder[i] == name) return i;
            return int.MaxValue;
        }
    }
}
