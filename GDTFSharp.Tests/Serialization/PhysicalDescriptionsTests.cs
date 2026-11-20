using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Tests.Serialization;

public class PhysicalDescriptionsTests
{
    private const string Header =
        "<GDTF DataVersion=\"1.2\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\">" +
        "<AttributeDefinitions><FeatureGroups /><Attributes /></AttributeDefinitions>";

    private const string Footer = "<Geometries /><DMXModes /></FixtureType></GDTF>";

    private static GdtfReadResult Read(string physicalDescriptions, string wheels = "") =>
        GdtfReader.Read(Header + wheels + "<PhysicalDescriptions>" + physicalDescriptions + "</PhysicalDescriptions>" + Footer);

    private const string Full = """
        <Emitters>
          <Emitter Name="R" Color="0.6951,0.3044,100" DiodePart="LED-R1">
            <Measurement Physical="100" LuminousIntensity="534" InterpolationTo="Step">
              <MeasurementPoint WaveLength="634" Energy="0.0482" />
              <MeasurementPoint WaveLength="637" Energy="0.069" />
            </Measurement>
            <Measurement Physical="50" LuminousIntensity="250" />
          </Emitter>
          <Emitter Name="UV" DominantWaveLength="395" />
        </Emitters>
        <Filters>
          <Filter Name="Cyan" Color="0.2,0.3,60">
            <Measurement Physical="0" Transmission="1" />
            <Measurement Physical="100" Transmission="0.4" InterpolationTo="Log" />
          </Filter>
        </Filters>
        <ColorSpace Mode="sRGB" />
        <AdditionalColorSpaces>
          <ColorSpace Name="Wide" Mode="Custom" Red="0.7,0.29,20" Green="0.17,0.8,70" Blue="0.13,0.04,10" WhitePoint="0.3127,0.329,100" />
          <ColorSpace Name="Film" Mode="ProPhoto" />
          <ColorSpace Name="Theatre" Mode="ANSI" />
        </AdditionalColorSpaces>
        <Gamuts>
          <Gamut Name="Native" Points="{0.7,0.29,100}{0.17,0.8,100}{0.13,0.04,100}" />
        </Gamuts>
        <DMXProfiles>
          <DMXProfile Name="Square">
            <Point DMXPercentage="0" CFC2="0.01" />
            <Point DMXPercentage="75" CFC0="56.25" CFC1="2" />
          </DMXProfile>
        </DMXProfiles>
        <CRIs>
          <CRIGroup ColorTemperature="3200">
            <CRI CES="CES01" ColorRenderingIndex="92" />
            <CRI CES="CES99" ColorRenderingIndex="88" />
          </CRIGroup>
          <CRIGroup>
            <CRI />
          </CRIGroup>
        </CRIs>
        <Connectors>
          <Connector Name="Power In" Type="PowerCon" Gender="-1" Length="1.5" />
          <Connector Name="DMX In" Type="XLR5" DMXBreak="1" />
        </Connectors>
        <Properties>
          <OperatingTemperature Low="-10" High="45" />
          <Weight Value="23.5" />
          <PowerConsumption Value="800" PowerFactor="0.95" Connector="Power In" VoltageLow="100" VoltageHigh="240" FrequencyLow="50" FrequencyHigh="60" />
          <LegHeight Value="0.05" />
        </Properties>
        """;

    [Fact]
    public void ReadsEverything()
    {
        var result = Read(Full);
        Assert.Empty(result.Diagnostics);
        var pd = result.Document.FixtureType.PhysicalDescriptions;

        var red = pd.FindEmitter(new Name("R"))!;
        Assert.Equal(new ColorCIE(0.6951f, 0.3044f, 100f), red.Color);
        Assert.Equal("LED-R1", red.DiodePart);
        Assert.Null(red.DominantWaveLength);
        Assert.Equal(2, red.Measurements.Count);
        Assert.Equal(534f, red.Measurements[0].LuminousIntensity);
        Assert.Null(red.Measurements[0].Transmission);
        Assert.Equal(InterpolationTo.Step, red.Measurements[0].InterpolationTo);
        Assert.Equal(InterpolationTo.Linear, red.Measurements[1].InterpolationTo);
        Assert.Equal(637f, red.Measurements[0].Points[1].WaveLength);
        Assert.Equal(0.069f, red.Measurements[0].Points[1].Energy);

        var uv = pd.FindEmitter(new Name("UV"))!;
        Assert.Null(uv.Color);
        Assert.Equal(395f, uv.DominantWaveLength);

        var cyan = pd.FindFilter(new Name("Cyan"))!;
        Assert.Equal(0.4f, cyan.Measurements[1].Transmission);
        Assert.Equal(InterpolationTo.Log, cyan.Measurements[1].InterpolationTo);

        Assert.Equal("Default", pd.ColorSpace!.Name.Value);
        Assert.Equal(ColorSpaceMode.SRgb, pd.ColorSpace.Mode);
        var wide = pd.FindColorSpace(new Name("Wide"))!;
        Assert.Equal(ColorSpaceMode.Custom, wide.Mode);
        Assert.Equal(new ColorCIE(0.17f, 0.8f, 70f), wide.Green);
        Assert.Same(pd.ColorSpace, pd.FindColorSpace(new Name("Default")));
        Assert.Equal(ColorSpaceMode.ProPhoto, pd.AdditionalColorSpaces[1].Mode);
        Assert.Equal(ColorSpaceMode.Ansi, pd.AdditionalColorSpaces[2].Mode);

        var gamut = pd.FindGamut(new Name("Native"))!;
        Assert.Equal(3, gamut.Points.Count);
        Assert.Equal(new ColorCIE(0.13f, 0.04f, 100f), gamut.Points[2]);

        var profile = pd.FindDmxProfile(new Name("Square"))!;
        Assert.Equal(2, profile.Points.Count);
        Assert.Equal(56.25f, profile.Points[1].Cfc0);

        Assert.Equal(3200f, pd.Cris[0].ColorTemperature);
        Assert.Equal(99, pd.Cris[0].Cris[1].ColorSample);
        Assert.Equal(88, pd.Cris[0].Cris[1].ColorRenderingIndex);
        Assert.Equal(6000f, pd.Cris[1].ColorTemperature);
        Assert.Equal(1, pd.Cris[1].Cris[0].ColorSample);
        Assert.Equal(100, pd.Cris[1].Cris[0].ColorRenderingIndex);

        var power = pd.FindConnector(new Name("Power In"))!;
        Assert.Equal("PowerCon", power.Type.Value);
        Assert.Equal(-1, power.Gender);
        Assert.Equal(1.5f, power.Length);
        Assert.Null(power.DmxBreak);
        Assert.Equal(1u, pd.FindConnector(new Name("DMX In"))!.DmxBreak);

        var properties = pd.Properties;
        Assert.Equal(-10f, properties.OperatingTemperature!.Low);
        Assert.Equal(45f, properties.OperatingTemperature.High);
        Assert.Equal(23.5f, properties.Weight);
        Assert.Equal(0.05f, properties.LegHeight);
        var consumption = Assert.Single(properties.PowerConsumptions);
        Assert.Same(power, consumption.Connector);
        Assert.Equal(800f, consumption.Value);
        Assert.Equal(0.95f, consumption.PowerFactor);
        Assert.Equal(60f, consumption.FrequencyHigh);
    }

    [Fact]
    public void RoundTripIsEquivalent()
    {
        var written = GdtfWriter.ToXDocument(Read(Full).Document).Root!.Element("FixtureType")!.Element("PhysicalDescriptions")!;
        XmlEquivalence.AssertEquivalent(XElement.Parse("<PhysicalDescriptions>" + Full + "</PhysicalDescriptions>"), written);
        Assert.Equal("ANSI", written.Element("AdditionalColorSpaces")!.Elements().Last().Attribute("Mode")!.Value);
        Assert.Equal("CES99", written.Descendants("CRI").ElementAt(1).Attribute("CES")!.Value);
    }

    [Fact]
    public void EmptySectionAppliesDefaults()
    {
        var result = Read("<Properties><OperatingTemperature /></Properties>");
        Assert.Empty(result.Diagnostics);
        var pd = result.Document.FixtureType.PhysicalDescriptions;
        Assert.Null(pd.ColorSpace);
        Assert.Equal(0f, pd.Properties.OperatingTemperature!.Low);
        Assert.Equal(40f, pd.Properties.OperatingTemperature.High);
        Assert.Null(pd.Properties.Weight);
        Assert.Null(pd.Properties.LegHeight);
    }

    [Fact]
    public void WheelSlotFilterIsResolved()
    {
        var result = Read("<Filters><Filter Name=\"Red\" /></Filters>",
            "<Wheels><Wheel Name=\"Color\"><Slot Name=\"Red\" Filter=\"Red\" /><Slot Name=\"Blue\" Filter=\"Blue\" /></Wheel></Wheels>");
        var slots = result.Document.FixtureType.Wheels[0].Slots;
        Assert.Same(result.Document.FixtureType.PhysicalDescriptions.Filters[0], slots[0].Filter);
        Assert.Null(slots[1].Filter);
        Assert.Single(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error && d.Message.Contains("'Blue'"));

        var writtenSlot = GdtfWriter.ToXDocument(result.Document).Descendants("Slot").First();
        Assert.Equal("Red", writtenSlot.Attribute("Filter")!.Value);
    }

    [Fact]
    public void ProblemsAreReported()
    {
        var result = Read("""
            <Emitters><Emitter Name="Bare" /></Emitters>
            <ColorSpace Name="Main" />
            <AdditionalColorSpaces><ColorSpace Name="Main" Mode="Custom" /></AdditionalColorSpaces>
            <CRIs><CRIGroup><CRI CES="CES100" ColorRenderingIndex="300" /></CRIGroup></CRIs>
            <Gamuts><Gamut Name="G" Points="0.1,0.2,0.3" /></Gamuts>
            <Properties><Weight Value="1" /><Weight Value="2" /><PowerConsumption Value="5" Connector="Missing" /><PowerConsumption /></Properties>
            """);
        var messages = result.Diagnostics.Select(d => $"{d.Severity}: {d.Message}").ToList();
        Assert.Contains(messages, m => m.StartsWith("Warning") && m.Contains("DominantWaveLength"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("Duplicate name 'Main'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'CES100'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'300'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'0.1,0.2,0.3'"));
        Assert.Contains(messages, m => m.StartsWith("Warning") && m.Contains("'Weight' appears 2 times"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'Missing'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("Required attribute 'Connector'"));
        Assert.Equal(1f, result.Document.FixtureType.PhysicalDescriptions.Properties.Weight);
    }

    [Fact]
    public void EmptyConnectorsAreNotWritten()
    {
        var xml = GdtfWriter.ToXDocument(Read("<Connectors />").Document);
        Assert.Null(xml.Descendants("Connectors").FirstOrDefault());
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(50f, 25f)]
    [InlineData(75f, 56.25f)]
    [InlineData(100f, 106.25f)]
    public void DmxProfileEvaluatesPiecewiseCubic(float x, float expected)
    {
        // f(x) = 0.01·x² up to 75%, then g(x) = 56.25 + 2·(x - 75).
        var profile = new DmxProfile(new Name("P"));
        profile.Points.Add(new DmxProfilePoint { DmxPercentage = 75, Cfc0 = 56.25f, Cfc1 = 2 });
        profile.Points.Add(new DmxProfilePoint { DmxPercentage = 0, Cfc2 = 0.01f });
        Assert.Equal(expected, profile.Evaluate(x), 3);
    }

    [Fact]
    public void ColorArrayFormat()
    {
        Assert.True(GdtfFormat.TryParseColorArray("{0.1,0.2,3}{0.4,0.5,6}", out var colors));
        Assert.Equal(new[] { new ColorCIE(0.1f, 0.2f, 3f), new ColorCIE(0.4f, 0.5f, 6f) }, colors);
        Assert.Equal("{0.1,0.2,3}{0.4,0.5,6}", GdtfFormat.FormatColorArray(colors));
        Assert.True(GdtfFormat.TryParseColorArray("", out var none));
        Assert.Empty(none);
        Assert.False(GdtfFormat.TryParseColorArray("{0.1,0.2}", out _));
    }

    [Fact]
    public void FixtureBuiltInCodeValidates()
    {
        if (!File.Exists(TestData.Xsd)) Assert.Skip("gdtf.xsd missing; run scripts/fetch-testdata.sh.");
        var fixture = new FixtureType(new Name("Built"), Guid.NewGuid());
        var pd = fixture.PhysicalDescriptions;

        var red = new Emitter(new Name("Red")) { Color = new ColorCIE(0.69f, 0.3f, 100f), DiodePart = "R1" };
        var measurement = new Measurement { Physical = 100, LuminousIntensity = 500 };
        measurement.Points.Add(new MeasurementPoint(630, 0.05f));
        red.Measurements.Add(measurement);
        pd.Emitters.Add(red);
        var filter = new Filter(new Name("Cyan")) { Color = new ColorCIE(0.2f, 0.3f, 60f) };
        filter.Measurements.Add(new Measurement { Physical = 100, Transmission = 0.4f });
        pd.Filters.Add(filter);
        pd.ColorSpace = new ColorSpace();
        pd.AdditionalColorSpaces.Add(new ColorSpace { Name = new Name("Wide"), Mode = ColorSpaceMode.Custom, Red = new ColorCIE(0.7f, 0.29f, 20f) });
        var gamut = new Gamut(new Name("Native"));
        gamut.Points.Add(new ColorCIE(0.7f, 0.29f, 100f));
        pd.Gamuts.Add(gamut);
        var profile = new DmxProfile(new Name("Linear"));
        profile.Points.Add(new DmxProfilePoint { Cfc1 = 1 });
        pd.DmxProfiles.Add(profile);
        var cri = new CriGroup { ColorTemperature = 3200 };
        cri.Cris.Add(new Cri { ColorSample = 7, ColorRenderingIndex = 95 });
        pd.Cris.Add(cri);
        var connector = new Connector(new Name("Power"), new Name("PowerCon"));
        pd.Connectors.Add(connector);
        pd.Properties.OperatingTemperature = new OperatingTemperature { Low = -5 };
        pd.Properties.Weight = 12;
        pd.Properties.LegHeight = 0.1f;
        pd.Properties.PowerConsumptions.Add(new PowerConsumption { Connector = connector, Value = 300 });

        var wheel = new Wheel(new Name("Color"));
        wheel.Slots.Add(new WheelSlot(new Name("Cyan")) { Filter = filter });
        fixture.Wheels.Add(wheel);

        var document = new GdtfDocument(fixture);
        XsdValidation.AssertValid(GdtfWriter.ToXDocument(document));

        var reread = GdtfReader.Read(GdtfWriter.WriteToString(document));
        Assert.Empty(reread.Diagnostics);
        Assert.Equal(GdtfWriter.WriteToString(document), GdtfWriter.WriteToString(reread.Document));
    }
}
