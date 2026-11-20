using System.Numerics;
using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Tests.Serialization;

/// <summary>Wheels, Models, Revisions, FTPresets and Protocols.</summary>
public class CollectionSectionsTests
{
    private const string Header =
        "<GDTF DataVersion=\"1.2\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\">" +
        "<AttributeDefinitions><FeatureGroups /><Attributes /></AttributeDefinitions>";

    private const string Footer = "</FixtureType></GDTF>";

    private const string GeometriesAndModes =
        "<Geometries><Geometry Name=\"Body\" /></Geometries><DMXModes>" +
        "<DMXMode Name=\"Mode 1\" Geometry=\"Body\"><DMXChannels /></DMXMode>" +
        "<DMXMode Name=\"Mode 2\" Geometry=\"Body\"><DMXChannels /></DMXMode></DMXModes>";

    private static GdtfReadResult Read(string sections) =>
        GdtfReader.Read(Header + sections.Replace("<!--GEOMETRIES-->", GeometriesAndModes) + Footer);

    private static XElement Written(GdtfDocument document, string section) =>
        GdtfWriter.ToXDocument(document).Root!.Element("FixtureType")!.Element(section)!;

    // ---- Wheels -------------------------------------------------------------------------------------------------

    private const string WheelsXml = """
        <Wheels>
          <Wheel Name="Gobo1">
            <Slot Name="Open" />
            <Slot Name="Dots" MediaFileName="gobo_dots" Color="0.3,0.3,50" />
            <Slot Name="Red" Filter="Red Filter" />
          </Wheel>
          <Wheel Name="Prism">
            <Slot Name="3 Facet">
              <Facet Color="0.3127,0.329,100" Rotation="{1,0,0}{0,1,0}{0.5,0,1}" />
              <Facet Rotation="{-0.5,0.866,0}{-0.866,-0.5,0}{-0.25,0.433,1}" />
            </Slot>
          </Wheel>
          <Wheel Name="Animation">
            <Slot Name="Flames" MediaFileName="flames">
              <AnimationSystem P1="-0.5,0.25" P2="0,0.5" P3="0.5,0.25" Radius="0.3" />
            </Slot>
          </Wheel>
        </Wheels>
        """;

    private const string RedFilterXml =
        "<PhysicalDescriptions><Filters><Filter Name=\"Red Filter\" Color=\"0.64,0.33,20\" /></Filters></PhysicalDescriptions>";

    [Fact]
    public void ReadsWheels()
    {
        var result = Read(WheelsXml + RedFilterXml + "<!--GEOMETRIES-->");
        Assert.Empty(result.Diagnostics);
        var fixture = result.Document.FixtureType;
        Assert.Equal(new[] { "Gobo1", "Prism", "Animation" }, fixture.Wheels.Select(w => w.Name.Value));

        var gobo = fixture.FindWheel(new Name("Gobo1"))!;
        var open = gobo.Slots[0];
        Assert.Null(open.Color);
        Assert.Equal(ColorCIE.White, open.EffectiveColor);
        Assert.Null(open.MediaFileName);
        Assert.Null(open.Filter);
        Assert.Equal(1, open.Index);
        Assert.Same(gobo, open.Wheel);

        var dots = gobo.GetSlot(2)!;
        Assert.Equal("Dots", dots.Name.Value);
        Assert.Equal("gobo_dots", dots.MediaFileName);
        Assert.Equal(new ColorCIE(0.3f, 0.3f, 50f), dots.Color);
        Assert.Same(fixture.PhysicalDescriptions.FindFilter(new Name("Red Filter")), gobo.FindSlot(new Name("Red"))!.Filter);
        Assert.NotNull(gobo.FindSlot(new Name("Red"))!.Filter);
        Assert.Null(gobo.GetSlot(0));
        Assert.Null(gobo.GetSlot(4));

        var facets = fixture.FindWheel(new Name("Prism"))!.Slots[0].Facets;
        Assert.Equal(2, facets.Count);
        Assert.Equal(ColorCIE.White, facets[0].Color);
        Assert.Equal(0.5f, facets[0].Rotation.M31);
        Assert.Null(facets[1].Color);
        Assert.Equal(-0.866f, facets[1].Rotation.M21);

        var animation = fixture.FindWheel(new Name("Animation"))!.Slots[0].AnimationSystem!;
        Assert.Equal(new Vector2(-0.5f, 0.25f), animation.P1);
        Assert.Equal(new Vector2(0, 0.5f), animation.P2);
        Assert.Equal(new Vector2(0.5f, 0.25f), animation.P3);
        Assert.Equal(0.3f, animation.Radius);
    }

    [Fact]
    public void WheelsRoundTrip()
    {
        var document = Read(WheelsXml + RedFilterXml + "<!--GEOMETRIES-->").Document;
        XmlEquivalence.AssertEquivalent(XElement.Parse(WheelsXml), Written(document, "Wheels"));
    }

    [Fact]
    public void WheelProblemsAreReported()
    {
        var result = Read("""
            <Wheels>
              <Wheel Name="W">
                <Slot Name="Both" Color="0.3,0.3,100" Filter="F">
                  <Facet Rotation="{1,0,0}{0,1,0}{0,0,1}" />
                  <AnimationSystem P1="0,0" P2="0,0" P3="0,0" Radius="1" />
                </Slot>
                <Slot />
                <Slot Name="BadFacet"><Facet /></Slot>
              </Wheel>
            </Wheels>
            <!--GEOMETRIES-->
            """);
        Assert.Contains(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Warning && d.Message.Contains("should not define a Color"));
        Assert.Contains(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Warning && d.Message.Contains("not both"));
        Assert.Contains(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error && d.Message.Contains("'Name'"));
        Assert.Contains(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error && d.Message.Contains("'Rotation'"));
        Assert.Equal("Slot 2", result.Document.FixtureType.Wheels[0].Slots[1].Name.Value);
    }

    [Fact]
    public void SlotIndexFollowsOrder()
    {
        var wheel = new Wheel(new Name("W"));
        var a = new WheelSlot(new Name("A"));
        var b = new WheelSlot(new Name("B"));
        wheel.Slots.Add(a);
        wheel.Slots.Add(b);
        Assert.Equal(2, b.Index);
        wheel.Slots.Remove(a);
        Assert.Equal(1, b.Index);
        Assert.Equal(0, a.Index);
    }

    [Fact]
    public void WheelMediaIsFoundInArchive()
    {
        var document = Read(WheelsXml + RedFilterXml + "<!--GEOMETRIES-->").Document;
        var archive = new GdtfArchive(document);
        archive.Resources["wheels/gobo_dots.png"] = new byte[] { 42 };
        var slot = document.FixtureType.Wheels[0].Slots[1];
        Assert.Equal(new byte[] { 42 }, archive.GetWheelMedia(slot.MediaFileName!));
    }

    // ---- Models -------------------------------------------------------------------------------------------------

    private const string ModelsXml = """
        <Models>
          <Model Name="Base" Length="0.483" Width="0.339" Height="0.121" PrimitiveType="Base" File="base"
                 SVGOffsetX="1.5" SVGOffsetY="2" SVGSideOffsetX="3" SVGSideOffsetY="4" SVGFrontOffsetX="5" SVGFrontOffsetY="-6" />
          <Model Name="Body" PrimitiveType="Conventional1_1" />
          <Model Name="Plain" />
        </Models>
        """;

    [Fact]
    public void ReadsModels()
    {
        var result = Read(ModelsXml + "<!--GEOMETRIES-->");
        Assert.Empty(result.Diagnostics);
        var models = result.Document.FixtureType.Models;

        var baseModel = result.Document.FixtureType.FindModel(new Name("Base"))!;
        Assert.Equal(0.483f, baseModel.Length);
        Assert.Equal(0.339f, baseModel.Width);
        Assert.Equal(0.121f, baseModel.Height);
        Assert.Equal(PrimitiveType.Base, baseModel.PrimitiveType);
        Assert.Equal("base", baseModel.File);
        Assert.Equal(1.5f, baseModel.SvgOffsetX);
        Assert.Equal(-6f, baseModel.SvgFrontOffsetY);

        Assert.Equal(PrimitiveType.Conventional1_1, models[1].PrimitiveType);
        Assert.Equal(PrimitiveType.Undefined, models[2].PrimitiveType);
        Assert.Equal(0f, models[2].Length);
        Assert.Null(models[2].File);
    }

    [Fact]
    public void ModelsRoundTrip()
    {
        var document = Read(ModelsXml + "<!--GEOMETRIES-->").Document;
        XmlEquivalence.AssertEquivalent(XElement.Parse(ModelsXml), Written(document, "Models"));
    }

    [Fact]
    public void InvalidPrimitiveTypeIsAnError()
    {
        var result = Read("<Models><Model Name=\"M\" PrimitiveType=\"Blob\" /></Models><!--GEOMETRIES-->");
        Assert.Contains(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error && d.Message.Contains("'Blob'"));
        Assert.Equal(PrimitiveType.Undefined, result.Document.FixtureType.Models[0].PrimitiveType);
    }

    // ---- Revisions and FTPresets --------------------------------------------------------------------------------

    [Fact]
    public void ReadsRevisions()
    {
        var result = Read("""
            <!--GEOMETRIES-->
            <Revisions>
              <Revision Text="Created" Date="2017-08-03T12:07:57" UserID="42" ModifiedBy="GDTF Builder" />
              <Revision />
            </Revisions>
            """);
        Assert.Empty(result.Diagnostics);
        var revisions = result.Document.FixtureType.Revisions;
        Assert.Equal("Created", revisions[0].Text);
        Assert.Equal(new DateTime(2017, 8, 3, 12, 7, 57, DateTimeKind.Utc), revisions[0].Date);
        Assert.Equal(42u, revisions[0].UserId);
        Assert.Equal("GDTF Builder", revisions[0].ModifiedBy);
        Assert.Equal("", revisions[1].Text);
        Assert.Null(revisions[1].Date);
        Assert.Equal(0u, revisions[1].UserId);

        var written = Written(result.Document, "Revisions").Elements().First();
        Assert.Equal("2017-08-03T12:07:57", written.Attribute("Date")!.Value);
        Assert.Equal("42", written.Attribute("UserID")!.Value);
    }

    [Fact]
    public void FTPresetsAreKeptVerbatim()
    {
        var result = Read("<!--GEOMETRIES--><FTPresets><FTPreset Name=\"P\"><Vendor X=\"1\" /></FTPreset></FTPresets>");
        Assert.Empty(result.Diagnostics);
        var preset = Assert.Single(result.Document.FixtureType.FTPresets);
        Assert.Equal("P", preset.Attribute("Name")!.Value);
        Assert.True(XNode.DeepEquals(preset, Written(result.Document, "FTPresets").Elements().Single()));
    }

    // ---- Protocols ----------------------------------------------------------------------------------------------

    private const string ProtocolsXml = """
        <Protocols>
          <FTRDM ManufacturerID="0x5253" DeviceModelID="0x91">
            <SoftwareVersionID Value="0x1A">
              <DMXPersonality Value="0x1" DMXMode="Mode 1" />
              <DMXPersonality Value="0x2" DMXMode="Mode 2" />
            </SoftwareVersionID>
          </FTRDM>
          <Art-Net>
            <Map Key="3" Value="255" />
          </Art-Net>
          <sACN />
          <PosiStageNet />
          <CITP Vendor="x"><Something /></CITP>
        </Protocols>
        """;

    [Fact]
    public void ReadsProtocols()
    {
        var result = Read("<!--GEOMETRIES-->" + ProtocolsXml);
        Assert.Empty(result.Diagnostics);
        var protocols = result.Document.FixtureType.Protocols;

        var rdm = protocols.Rdm!;
        Assert.Equal(0x5253UL, rdm.ManufacturerId);
        Assert.Equal(0x91UL, rdm.DeviceModelId);
        var version = Assert.Single(rdm.SoftwareVersions);
        Assert.Equal(0x1AUL, version.Value);
        Assert.Equal(result.Document.FixtureType.DmxModes, version.DmxPersonalities.Select(p => p.DmxMode));
        Assert.Equal(2UL, version.DmxPersonalities[1].Value);

        var map = Assert.Single(protocols.ArtNet!.Maps);
        Assert.Equal(3u, map.Key);
        Assert.Equal(255u, map.Value);
        Assert.Empty(protocols.Sacn!.Maps);

        Assert.Equal(new[] { "PosiStageNet", "CITP" }, protocols.Other.Select(o => o.Name.LocalName));
    }

    [Fact]
    public void ProtocolsRoundTrip()
    {
        var document = Read("<!--GEOMETRIES-->" + ProtocolsXml).Document;
        var written = Written(document, "Protocols");
        XmlEquivalence.AssertEquivalent(XElement.Parse(ProtocolsXml), written);
        Assert.Equal("0x5253", written.Element("FTRDM")!.Attribute("ManufacturerID")!.Value);
    }

    [Fact]
    public void AbsentProtocolsStayAbsent()
    {
        var result = Read("<!--GEOMETRIES--><Protocols />");
        var protocols = result.Document.FixtureType.Protocols;
        Assert.Null(protocols.Rdm);
        Assert.Null(protocols.ArtNet);
        Assert.Null(protocols.Sacn);
        Assert.False(Written(result.Document, "Protocols").HasElements);
    }

    [Fact]
    public void UnknownProtocolIsKeptAndReported()
    {
        var result = Read("<!--GEOMETRIES--><Protocols><VendorNet Port=\"1\" /></Protocols>");
        Assert.Contains(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Info && d.Message.Contains("'VendorNet'"));
        Assert.Equal("VendorNet", Written(result.Document, "Protocols").Elements().Single().Name.LocalName);
    }

    // ---- Whole fixture built in code ----------------------------------------------------------------------------

    private static FixtureType BuildFixture()
    {
        var fixture = new FixtureType(new Name("Built"), Guid.NewGuid());
        var group = new FeatureGroup(new Name("Gobo"));
        var feature = new Feature(new Name("Gobo"));
        group.Features.Add(feature);
        fixture.AttributeDefinitions.FeatureGroups.Add(group);
        fixture.AttributeDefinitions.Attributes.Add(new GdtfAttribute(new Name("Gobo1"), feature));

        var wheel = new Wheel(new Name("Gobo1"));
        wheel.Slots.Add(new WheelSlot(new Name("Open")));
        wheel.Slots.Add(new WheelSlot(new Name("Dots")) { MediaFileName = "dots", Color = new ColorCIE(0.3f, 0.3f, 80f) });
        var prism = new WheelSlot(new Name("Prism"));
        prism.Facets.Add(new PrismFacet(RotationMatrix.Identity) { Color = ColorCIE.White });
        wheel.Slots.Add(prism);
        wheel.Slots.Add(new WheelSlot(new Name("Anim"))
        {
            AnimationSystem = new AnimationSystem { P1 = new Vector2(-0.5f, 0), P2 = Vector2.Zero, P3 = new Vector2(0.5f, 0), Radius = 0.25f }
        });
        fixture.Wheels.Add(wheel);

        fixture.Models.Add(new Model(new Name("Body")) { Length = 0.2f, Width = 0.2f, Height = 0.3f, PrimitiveType = PrimitiveType.Cube, File = "body" });
        fixture.Revisions.Add(new Revision { Text = "Created", Date = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc), ModifiedBy = "GDTFSharp" });

        var rdm = new RdmProtocol { ManufacturerId = 0x5253, DeviceModelId = 0x91 };
        var version = new RdmSoftwareVersion { Value = 1 };
        var body = new Geometry(new Name("Body"));
        fixture.Geometries.Add(body);
        var mode = new DmxMode(new Name("Standard")) { Geometry = body };
        fixture.DmxModes.Add(mode);
        version.DmxPersonalities.Add(new DmxPersonality { Value = 1, DmxMode = mode });
        rdm.SoftwareVersions.Add(version);
        fixture.Protocols.Rdm = rdm;
        var artNet = new ValueMappedProtocol();
        artNet.Maps.Add(new ValueMap(0, 1));
        fixture.Protocols.ArtNet = artNet;
        return fixture;
    }

    [Fact]
    public void FixtureBuiltInCodeRoundTrips()
    {
        var document = new GdtfDocument(BuildFixture());
        var first = GdtfWriter.WriteToString(document);
        var reread = GdtfReader.Read(first);
        Assert.Empty(reread.Diagnostics);
        Assert.Equal(first, GdtfWriter.WriteToString(reread.Document));
    }

    [Fact]
    public void FixtureBuiltInCodeValidates()
    {
        if (!File.Exists(TestData.Xsd)) Assert.Skip("gdtf.xsd missing; run scripts/fetch-testdata.sh.");
        XsdValidation.AssertValid(GdtfWriter.ToXDocument(new GdtfDocument(BuildFixture())));
    }
}
