using System.IO.Compression;
using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Tests.Serialization;

public class AttributeDefinitionsTests
{
    private const string Header =
        "<GDTF DataVersion=\"1.2\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\">";

    private const string Footer = "<Geometries /><DMXModes /></FixtureType></GDTF>";

    private static GdtfReadResult Read(string attributeDefinitions) =>
        GdtfReader.Read(Header + "<AttributeDefinitions>" + attributeDefinitions + "</AttributeDefinitions>" + Footer);

    private const string Full = """
        <ActivationGroups>
          <ActivationGroup Name="PanTilt" />
          <ActivationGroup Name="Gobo1" />
        </ActivationGroups>
        <FeatureGroups>
          <FeatureGroup Name="Position" Pretty="Position">
            <Feature Name="PanTilt" />
          </FeatureGroup>
          <FeatureGroup Name="Gobo" Pretty="Gobo">
            <Feature Name="Gobo" />
          </FeatureGroup>
        </FeatureGroups>
        <Attributes>
          <Attribute Name="Gobo1WheelSpin" Pretty="G1 Spin" MainAttribute="Gobo1" ActivationGroup="Gobo1" Feature="Gobo.Gobo" PhysicalUnit="AngularSpeed" />
          <Attribute Name="Gobo1" Pretty="G1" ActivationGroup="Gobo1" Feature="Gobo.Gobo" />
          <Attribute Name="Pan" Pretty="P" ActivationGroup="PanTilt" Feature="Position.PanTilt" PhysicalUnit="Angle" Color="0.3127,0.329,100" />
          <Attribute Name="PanAcc" Pretty="P Acc" Feature="Position.PanTilt" PhysicalUnit="AngularAccc">
            <SubPhysicalUnit Type="Duration" PhysicalUnit="Time" PhysicalFrom="0.5" PhysicalTo="2" />
            <SubPhysicalUnit Type="DutyCycle" />
          </Attribute>
        </Attributes>
        """;

    [Fact]
    public void ReadsAllNodesAndResolvesReferences()
    {
        var result = Read(Full);
        Assert.Empty(result.Diagnostics);
        var definitions = result.Document.FixtureType.AttributeDefinitions;

        Assert.Equal(new[] { "PanTilt", "Gobo1" }, definitions.ActivationGroups.Select(g => g.Name.Value));
        Assert.Equal(new[] { "Position", "Gobo" }, definitions.FeatureGroups.Select(g => g.Name.Value));

        var panTilt = definitions.FindFeature(NodePath.Parse("Position.PanTilt"));
        Assert.NotNull(panTilt);
        Assert.Same(definitions.FeatureGroups[0], panTilt.Group);

        var pan = definitions.FindAttribute(new Name("Pan"))!;
        Assert.Equal("P", pan.Pretty);
        Assert.Same(panTilt, pan.Feature);
        Assert.Same(definitions.FindActivationGroup(new Name("PanTilt")), pan.ActivationGroup);
        Assert.Equal(PhysicalUnit.Angle, pan.PhysicalUnit);
        Assert.Equal(new ColorCIE(0.3127f, 0.329f, 100f), pan.Color);
        Assert.Null(pan.MainAttribute);

        // Forward reference: Gobo1 is defined after Gobo1WheelSpin.
        var spin = definitions.FindAttribute(new Name("Gobo1WheelSpin"))!;
        Assert.Same(definitions.FindAttribute(new Name("Gobo1")), spin.MainAttribute);

        var gobo = definitions.FindAttribute(new Name("Gobo1"))!;
        Assert.Equal(PhysicalUnit.None, gobo.PhysicalUnit);
        Assert.Null(gobo.Color);

        var acc = definitions.FindAttribute(new Name("PanAcc"))!;
        Assert.Equal(PhysicalUnit.AngularAcceleration, acc.PhysicalUnit);
        Assert.Equal(2, acc.SubPhysicalUnits.Count);
        Assert.Equal(SubPhysicalUnitType.Duration, acc.SubPhysicalUnits[0].Type);
        Assert.Equal(PhysicalUnit.Time, acc.SubPhysicalUnits[0].PhysicalUnit);
        Assert.Equal(0.5f, acc.SubPhysicalUnits[0].PhysicalFrom);
        Assert.Equal(2f, acc.SubPhysicalUnits[0].PhysicalTo);

        // Spec defaults: PhysicalUnit None, PhysicalFrom 0, PhysicalTo 1.
        Assert.Equal(PhysicalUnit.None, acc.SubPhysicalUnits[1].PhysicalUnit);
        Assert.Equal(0f, acc.SubPhysicalUnits[1].PhysicalFrom);
        Assert.Equal(1f, acc.SubPhysicalUnits[1].PhysicalTo);
    }

    [Fact]
    public void WriteIsEquivalentToInput()
    {
        var input = XElement.Parse("<AttributeDefinitions>" + Full + "</AttributeDefinitions>");
        var written = GdtfWriter.ToXDocument(Read(Full).Document).Root!.Element("FixtureType")!.Element("AttributeDefinitions")!;
        XmlEquivalence.AssertEquivalent(input, written);
        Assert.Equal("AngularAccc", written.Element("Attributes")!.Elements().Last().Attribute("PhysicalUnit")!.Value);
    }

    [Fact]
    public void WritesModelBuiltInCode()
    {
        var fixture = new FixtureType(new Name("Built"), Guid.NewGuid());
        var definitions = fixture.AttributeDefinitions;
        var group = new FeatureGroup(new Name("Dimmer"));
        var feature = new Feature(new Name("Dimmer"));
        group.Features.Add(feature);
        definitions.FeatureGroups.Add(group);
        var activation = new ActivationGroup(new Name("Dim"));
        definitions.ActivationGroups.Add(activation);
        var dimmer = new GdtfAttribute(new Name("Dimmer"), feature) { ActivationGroup = activation, PhysicalUnit = PhysicalUnit.LuminousIntensity };
        definitions.Attributes.Add(dimmer);
        definitions.Attributes.Add(new GdtfAttribute(new Name("DimmerFine"), feature, "Dim fine") { MainAttribute = dimmer });

        var xml = GdtfWriter.ToXDocument(new GdtfDocument(fixture)).Root!.Element("FixtureType")!.Element("AttributeDefinitions")!;
        var attributes = xml.Element("Attributes")!.Elements("Attribute").ToList();
        Assert.Equal("Dimmer.Dimmer", attributes[0].Attribute("Feature")!.Value);
        Assert.Equal("Dimmer", attributes[0].Attribute("Pretty")!.Value);
        Assert.Equal("Dim", attributes[0].Attribute("ActivationGroup")!.Value);
        Assert.Equal("LuminousIntensity", attributes[0].Attribute("PhysicalUnit")!.Value);
        Assert.Null(attributes[0].Attribute("MainAttribute"));
        Assert.Null(attributes[0].Attribute("Color"));
        Assert.Equal("Dimmer", attributes[1].Attribute("MainAttribute")!.Value);
        Assert.Equal("Dim fine", attributes[1].Attribute("Pretty")!.Value);
        Assert.Equal("Dimmer", xml.Element("FeatureGroups")!.Element("FeatureGroup")!.Attribute("Pretty")!.Value);

        var reread = GdtfReader.Read(GdtfWriter.WriteToString(new GdtfDocument(fixture)));
        Assert.DoesNotContain(reread.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error);
        var rereadDefinitions = reread.Document.FixtureType.AttributeDefinitions;
        Assert.Same(rereadDefinitions.Attributes[0], rereadDefinitions.Attributes[1].MainAttribute);
    }

    [Fact]
    public void DanglingReferencesAreErrors()
    {
        var result = Read("""
            <FeatureGroups><FeatureGroup Name="Dimmer" Pretty="Dimmer"><Feature Name="Dimmer" /></FeatureGroup></FeatureGroups>
            <Attributes>
              <Attribute Name="A" Pretty="A" Feature="Dimmer.Missing" ActivationGroup="Nope" MainAttribute="Ghost" />
            </Attributes>
            """);
        var errors = result.Diagnostics.Where(d => d.Severity == GdtfDiagnosticSeverity.Error).ToList();
        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.Message.Contains("'Dimmer.Missing'") && e.Location == "FixtureType[X]/AttributeDefinitions/Attributes/Attribute[A]@Feature");
        Assert.Contains(errors, e => e.Message.Contains("'Nope'"));
        Assert.Contains(errors, e => e.Message.Contains("'Ghost'"));
        var attribute = result.Document.FixtureType.AttributeDefinitions.Attributes.Single();
        Assert.Null(attribute.Feature);
        Assert.Null(attribute.ActivationGroup);
        Assert.Null(attribute.MainAttribute);
    }

    [Fact]
    public void MissingFeatureIsAnError()
    {
        var result = Read("<FeatureGroups /><Attributes><Attribute Name=\"A\" Pretty=\"A\" /></Attributes>");
        Assert.Contains(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error && d.Message.Contains("'Feature'"));
    }

    [Fact]
    public void FeatureNamesAreScopedByGroup()
    {
        var result = Read("""
            <FeatureGroups>
              <FeatureGroup Name="Beam" Pretty="Beam"><Feature Name="Beam" /></FeatureGroup>
              <FeatureGroup Name="Color" Pretty="Color"><Feature Name="Beam" /></FeatureGroup>
            </FeatureGroups>
            <Attributes><Attribute Name="A" Pretty="A" Feature="Color.Beam" /></Attributes>
            """);
        Assert.Empty(result.Diagnostics);
        var definitions = result.Document.FixtureType.AttributeDefinitions;
        Assert.Same(definitions.FeatureGroups[1].Features[0], definitions.Attributes[0].Feature);
    }

    [Fact]
    public void DuplicateNamesAreErrors()
    {
        var result = Read("""
            <FeatureGroups><FeatureGroup Name="G" Pretty="G"><Feature Name="F" /><Feature Name="F" /></FeatureGroup></FeatureGroups>
            <Attributes>
              <Attribute Name="A" Pretty="first" Feature="G.F" />
              <Attribute Name="A" Pretty="second" Feature="G.F" MainAttribute="A" />
            </Attributes>
            """);
        Assert.Equal(2, result.Diagnostics.Count(d => d.Severity == GdtfDiagnosticSeverity.Error && d.Message.StartsWith("Duplicate name")));
        var attributes = result.Document.FixtureType.AttributeDefinitions.Attributes;
        Assert.Same(attributes[0], attributes[1].MainAttribute);
    }

    [Fact]
    public void MissingNameGetsSpecDefault()
    {
        var result = Read("<FeatureGroups><FeatureGroup Name=\"G\" Pretty=\"G\"><Feature Name=\"F\" /><Feature /></FeatureGroup></FeatureGroups><Attributes />");
        Assert.Contains(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error && d.Message.Contains("'Name'"));
        Assert.Equal("Feature 2", result.Document.FixtureType.AttributeDefinitions.FeatureGroups[0].Features[1].Name.Value);
    }

    [Fact]
    public void ReportsMissingMandatoryChildrenAndUnknownContent()
    {
        var missing = Read("");
        Assert.Contains(missing.Diagnostics, d => d.Message.Contains("'FeatureGroups'"));
        Assert.Contains(missing.Diagnostics, d => d.Message.Contains("'Attributes'"));

        var unknown = Read("<FeatureGroups /><Attributes><Attribute Name=\"A\" Pretty=\"A\" Vendor=\"x\"><Extra /></Attribute></Attributes>");
        Assert.Contains(unknown.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Info && d.Message.Contains("'Vendor'"));
        Assert.Contains(unknown.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Info && d.Message.Contains("'Extra'"));
    }

    [Fact]
    public void SecondAttributeDefinitionsSectionIsIgnored()
    {
        var result = GdtfReader.Read(Header +
                                     "<AttributeDefinitions><FeatureGroups /><Attributes /></AttributeDefinitions>" +
                                     "<AttributeDefinitions><FeatureGroups /><Attributes /></AttributeDefinitions>" + Footer);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("appears more than once"));
        Assert.DoesNotContain(result.Document.FixtureType.UnparsedSections, s => s.Name.LocalName == "AttributeDefinitions");
    }

    // ---- Real-world files ---------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(TestData.ExternalFixtureNames), MemberType = typeof(TestData))]
    public void RealFixturesAreReadCompletely(string fileName)
    {
        var path = TestData.RequireExternal(fileName);
        var result = GdtfArchive.Load(path);
        Assert.DoesNotContain(result.Diagnostics, d => d.Location?.Contains("/AttributeDefinitions") == true);

        var original = ReadOriginalAttributeDefinitions(path);
        var definitions = result.Archive.Document.FixtureType.AttributeDefinitions;
        Assert.Equal(original.Descendants("Attribute").Count(), definitions.Attributes.Count);
        Assert.All(definitions.Attributes, a => Assert.NotNull(a.Feature));

        var written = GdtfWriter.ToXDocument(result.Archive.Document).Root!.Element("FixtureType")!.Element("AttributeDefinitions")!;
        XmlEquivalence.AssertEquivalent(original, written);
    }

    private static XElement ReadOriginalAttributeDefinitions(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        using var stream = zip.GetEntry(GdtfArchive.DescriptionFileName)!.Open();
        return XDocument.Load(stream).Root!.Element("FixtureType")!.Element("AttributeDefinitions")!;
    }
}

public class ChildCollectionTests
{
    [Fact]
    public void MaintainsParent()
    {
        var group = new FeatureGroup(new Name("G"));
        var feature = new Feature(new Name("F"));
        Assert.Equal("F", feature.Path.ToString());

        group.Features.Add(feature);
        Assert.Same(group, feature.Group);
        Assert.Equal("G.F", feature.Path.ToString());

        group.Features.Remove(feature);
        Assert.Null(feature.Group);

        group.Features.Add(feature);
        group.Features[0] = new Feature(new Name("Other"));
        Assert.Null(feature.Group);

        group.Features.Clear();
        Assert.Empty(group.Features);
    }

    [Fact]
    public void RejectsSecondParent()
    {
        var feature = new Feature(new Name("F"));
        var first = new FeatureGroup(new Name("A"));
        first.Features.Add(feature);
        Assert.Throws<InvalidOperationException>(() => new FeatureGroup(new Name("B")).Features.Add(feature));
        Assert.Throws<InvalidOperationException>(() => first.Features.Add(feature));
        Assert.Throws<ArgumentNullException>(() => first.Features.Add(null!));
    }
}

public class XmlEquivalenceTests
{
    [Theory]
    [InlineData("<A X=\"1\" />", "<A />")]
    [InlineData("<A X=\"1\" />", "<A X=\"2\" />")]
    [InlineData("<A R=\"{1,0,0}{0,1,0}{0,0,1}\" />", "<A R=\"{1,0,0}{0,1,0}{0,0,2}\" />")]
    [InlineData("<A R=\"{1,0,0}{0,1,0}{0,0,1}\" />", "<A R=\"1,0,0,0,1,0,0,0,1\" />")]
    [InlineData("<A />", "<A Y=\"1\" />")]
    [InlineData("<A><B /></A>", "<A />")]
    [InlineData("<A><B N=\"1\" /><B N=\"2\" /></A>", "<A><B N=\"2\" /><B N=\"1\" /></A>")]
    [InlineData("<Attribute />", "<Attribute PhysicalUnit=\"Angle\" />")]
    public void DetectsDifferences(string original, string written) =>
        Assert.ThrowsAny<Exception>(() => XmlEquivalence.AssertEquivalent(XElement.Parse(original), XElement.Parse(written)));

    [Theory]
    [InlineData("<A C=\"0.225000,0.329000,78.700000\" />", "<A C=\"0.225,0.329,78.7\" />")]
    [InlineData("<Attribute />", "<Attribute PhysicalUnit=\"None\" />")]
    [InlineData("<A R=\"\" />", "<A />")]
    [InlineData("<A R=\"{1.000000,0.500000,0}{0,1,0}{0,0,1}\" />", "<A R=\"{1,0.5,0}{0,1,0}{0,0,1}\" />")]
    [InlineData("<A H=\"0xab\" />", "<A H=\"0xAB\" />")]
    [InlineData("<X />", "<X><ActivationGroups /></X>")]
    [InlineData("<A><B /><C X=\"1\" /></A>", "<A><C X=\"1\" /><B /></A>")]
    [InlineData("<A><Connectors /></A>", "<A />")]
    public void AcceptsEquivalentForms(string original, string written) =>
        XmlEquivalence.AssertEquivalent(XElement.Parse(original), XElement.Parse(written));
}

public class AttributeDefinitionsXsdTests
{
    [Fact]
    public void FixtureBuiltInCodeValidates()
    {
        if (!File.Exists(TestData.Xsd)) Assert.Skip("gdtf.xsd missing; run scripts/fetch-testdata.sh.");
        var fixture = new FixtureType(new Name("Built"), Guid.NewGuid());
        var group = new FeatureGroup(new Name("Position"));
        var panTilt = new Feature(new Name("PanTilt"));
        group.Features.Add(panTilt);
        fixture.AttributeDefinitions.FeatureGroups.Add(group);
        var activation = new ActivationGroup(new Name("PanTilt"));
        fixture.AttributeDefinitions.ActivationGroups.Add(activation);
        var pan = new GdtfAttribute(new Name("Pan"), panTilt, "P") { ActivationGroup = activation, PhysicalUnit = PhysicalUnit.Angle };
        pan.SubPhysicalUnits.Add(new SubPhysicalUnit(SubPhysicalUnitType.Amplitude, PhysicalUnit.Percent));
        fixture.AttributeDefinitions.Attributes.Add(pan);
        fixture.AttributeDefinitions.Attributes.Add(new GdtfAttribute(new Name("PanRotate"), panTilt) { MainAttribute = pan, Color = ColorCIE.White });

        XsdValidation.AssertValid(GdtfWriter.ToXDocument(new GdtfDocument(fixture)));
    }
}
