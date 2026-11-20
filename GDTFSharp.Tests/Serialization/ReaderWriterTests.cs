using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Tests.Serialization;

public class ReaderWriterTests
{
    private static GdtfReadResult ReadMinimal()
    {
        using var stream = File.OpenRead(TestData.MinimalDescription);
        return GdtfReader.Read(stream);
    }

    [Fact]
    public void ReadsHeader()
    {
        var result = ReadMinimal();
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity != GdtfDiagnosticSeverity.Info);

        var document = result.Document;
        Assert.Equal(DataVersion.V1_2, document.DataVersion);
        var fixture = document.FixtureType;
        Assert.Equal("Minimal LED", fixture.Name.Value);
        Assert.Equal("MinLED", fixture.ShortName);
        Assert.Equal("Minimal LED & Friends \"Test\"", fixture.LongName);
        Assert.Equal("GDTFSharp", fixture.Manufacturer);
        Assert.Equal("Hand-written test fixture <1>", fixture.Description);
        Assert.Equal(Guid.Parse("308EA87D-7164-42DE-8106-A6D273F57A51"), fixture.FixtureTypeId);
        Assert.Equal("thumbnail", fixture.Thumbnail);
        Assert.Equal(10, fixture.ThumbnailOffsetX);
        Assert.Equal(-5, fixture.ThumbnailOffsetY);
        Assert.Null(fixture.RefFT);
        Assert.False(fixture.CanHaveChildren);
    }

    [Fact]
    public void AppliesSpecDefaults()
    {
        var result = GdtfReader.Read(
            "<GDTF DataVersion=\"1.2\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\" Thumbnail=\"\" RefFT=\"\">" +
            "<AttributeDefinitions><FeatureGroups /><Attributes /></AttributeDefinitions><Geometries /><DMXModes /></FixtureType></GDTF>");
        Assert.Empty(result.Diagnostics);
        var fixture = result.Document.FixtureType;
        Assert.True(fixture.CanHaveChildren);
        Assert.Equal(0, fixture.ThumbnailOffsetX);
        Assert.Null(fixture.Thumbnail);
        Assert.Null(fixture.RefFT);
        Assert.Equal(string.Empty, fixture.ShortName);
    }

    [Fact]
    public void RoundTripPreservesHeaderAndUnparsedSections()
    {
        var original = ReadMinimal().Document;
        var xml = GdtfWriter.WriteToString(original);
        var reread = GdtfReader.Read(xml).Document;

        var a = original.FixtureType;
        var b = reread.FixtureType;
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.LongName, b.LongName);
        Assert.Equal(a.Description, b.Description);
        Assert.Equal(a.FixtureTypeId, b.FixtureTypeId);
        Assert.Equal(a.ThumbnailOffsetY, b.ThumbnailOffsetY);
        Assert.Equal(a.CanHaveChildren, b.CanHaveChildren);
        Assert.Equal(a.UnparsedSections.Count, b.UnparsedSections.Count);
        for (var i = 0; i < a.UnparsedSections.Count; i++)
            Assert.True(XNode.DeepEquals(a.UnparsedSections[i], b.UnparsedSections[i]), a.UnparsedSections[i].Name.LocalName);
    }

    [Fact]
    public void WritesSpecRootAndEscapesValues()
    {
        var fixture = new FixtureType(new Name("Test"), Guid.Parse("308EA87D-7164-42DE-8106-A6D273F57A51"))
        {
            Description = "a \"quoted\" <tag> & more"
        };
        var xml = GdtfWriter.WriteToString(new GdtfDocument(fixture));

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml);
        var root = XDocument.Parse(xml).Root!;
        Assert.Equal("GDTF", root.Name.LocalName);
        Assert.Equal("1.2", root.Attribute("DataVersion")!.Value);
        var fixtureType = root.Element("FixtureType")!;
        Assert.Equal("a \"quoted\" <tag> & more", fixtureType.Attribute("Description")!.Value);
        Assert.Equal("308EA87D-7164-42DE-8106-A6D273F57A51", fixtureType.Attribute("FixtureTypeID")!.Value);
        Assert.Equal("Yes", fixtureType.Attribute("CanHaveChildren")!.Value);
        Assert.Null(fixtureType.Attribute("RefFT"));
        Assert.Null(fixtureType.Attribute("Thumbnail"));
    }

    [Fact]
    public void WritesMandatorySectionsInSpecOrder()
    {
        var fixture = new FixtureType(new Name("Test"), Guid.NewGuid());
        fixture.UnparsedSections.Add(new XElement("Revisions"));
        fixture.UnparsedSections.Add(new XElement("Wheels"));
        var root = GdtfWriter.ToXDocument(new GdtfDocument(fixture)).Root!;
        var sections = root.Element("FixtureType")!.Elements().Select(e => e.Name.LocalName);
        Assert.Equal(new[] { "AttributeDefinitions", "Wheels", "Geometries", "DMXModes", "Revisions" }, sections);
    }

    [Fact]
    public void ReportsOutOfOrderAndUnknownSections()
    {
        var result = GdtfReader.Read(
            "<GDTF DataVersion=\"1.2\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\">" +
            "<Geometries /><AttributeDefinitions /><DMXModes /><Vendor /></FixtureType></GDTF>");
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("'AttributeDefinitions' is out of the order"));
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("Unknown FixtureType section 'Vendor'"));
    }

    [Fact]
    public void ReportsMissingMandatorySections()
    {
        var result = GdtfReader.Read(
            "<GDTF DataVersion=\"1.2\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\" /></GDTF>");
        Assert.Equal(3, result.Diagnostics.Count(d => d.Message.StartsWith("Mandatory section")));
    }

    [Fact]
    public void KeepsDataVersionOfOlderFiles()
    {
        var result = GdtfReader.Read(
            "<GDTF DataVersion=\"1.0\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\">" +
            "<AttributeDefinitions /><Geometries /><DMXModes /></FixtureType></GDTF>");
        Assert.Equal(DataVersion.V1_0, result.Document.DataVersion);
        Assert.Contains("DataVersion=\"1.2\"", GdtfWriter.WriteToString(result.Document));
    }

    [Theory]
    [InlineData("<NotGDTF />")]
    [InlineData("<GDTF DataVersion=\"1.2\" />")]
    [InlineData("<GDTF DataVersion=\"1.2\"><FixtureType")]
    public void UnreadableFilesThrow(string xml) => Assert.Throws<GdtfReadException>(() => GdtfReader.Read(xml));

    [Fact]
    public void StrictModeThrowsOnErrors()
    {
        const string xml = "<GDTF DataVersion=\"1.2\"><FixtureType Name=\"X\" FixtureTypeID=\"not-a-guid\">" +
                           "<AttributeDefinitions /><Geometries /><DMXModes /></FixtureType></GDTF>";
        Assert.True(GdtfReader.Read(xml).HasErrors);
        Assert.Throws<GdtfReadException>(() => GdtfReader.Read(xml, new GdtfReadOptions { Strict = true }));
    }

    [Fact]
    public void WrittenMinimalFixtureValidatesAgainstXsd()
    {
        if (!File.Exists(TestData.Xsd)) Assert.Skip("gdtf.xsd missing; run scripts/fetch-testdata.sh.");
        XsdValidation.AssertValid(XDocument.Load(TestData.MinimalDescription));
        var document = ReadMinimal().Document;
        document.FixtureType.RefFT = Guid.NewGuid();
        XsdValidation.AssertValid(GdtfWriter.ToXDocument(document));
    }

    [Fact]
    public void XsdValidationDetectsErrors()
    {
        if (!File.Exists(TestData.Xsd)) Assert.Skip("gdtf.xsd missing; run scripts/fetch-testdata.sh.");
        var xml = GdtfWriter.ToXDocument(ReadMinimal().Document);
        xml.Root!.Element("FixtureType")!.SetAttributeValue("CanHaveChildren", "Maybe");
        Assert.NotEmpty(XsdValidation.Validate(xml));
    }
}
