using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Tests.Serialization;

public class DmxModesTests
{
    private const string Attributes = """
        <AttributeDefinitions>
          <FeatureGroups>
            <FeatureGroup Name="Dimmer" Pretty="Dimmer"><Feature Name="Dimmer" /></FeatureGroup>
            <FeatureGroup Name="Color" Pretty="Color"><Feature Name="Color" /><Feature Name="RGB" /></FeatureGroup>
            <FeatureGroup Name="Beam" Pretty="Beam"><Feature Name="Beam" /></FeatureGroup>
            <FeatureGroup Name="Control" Pretty="Control"><Feature Name="Control" /></FeatureGroup>
          </FeatureGroups>
          <Attributes>
            <Attribute Name="Dimmer" Pretty="Dim" Feature="Dimmer.Dimmer" PhysicalUnit="LuminousIntensity" />
            <Attribute Name="Color1" Pretty="C1" Feature="Color.Color" />
            <Attribute Name="ColorAdd_R" Pretty="R" Feature="Color.RGB" />
            <Attribute Name="ColorAdd_G" Pretty="G" Feature="Color.RGB" />
            <Attribute Name="ColorAdd_B" Pretty="B" Feature="Color.RGB" />
            <Attribute Name="Shutter1" Pretty="Sh1" Feature="Beam.Beam" />
            <Attribute Name="Shutter1Strobe" Pretty="Strobe" Feature="Beam.Beam" PhysicalUnit="Frequency">
              <SubPhysicalUnit Type="DutyCycle" PhysicalUnit="Percent" />
              <SubPhysicalUnit Type="Duration" PhysicalUnit="Time" />
            </Attribute>
            <Attribute Name="Control1" Pretty="Ctrl" Feature="Control.Control" />
          </Attributes>
        </AttributeDefinitions>
        """;

    private static GdtfReadResult Read(string afterAttributes, string version = "1.2") =>
        GdtfReader.Read($"<GDTF DataVersion=\"{version}\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\">" +
                        Attributes + afterAttributes + "</FixtureType></GDTF>");

    // ---- Spec example "Multiple Breaks in Geometrical References" ------------------------------------------------

    private const string BreaksExample = """
        <Wheels><Wheel Name="ColorScroller">
          <Slot Name="Gel1" /><Slot Name="Gel2" /><Slot Name="Gel3" /><Slot Name="Gel4" /><Slot Name="Gel5" />
        </Wheel></Wheels>
        <Geometries>
          <Geometry Name="Head">
            <Beam Name="Beam" Position="{1,0,0,0}{0,1,0,0}{0,0,1,-0.2}{0,0,0,1}" LampType="Halogen" />
          </Geometry>
          <Geometry Name="Body">
            <GeometryReference Name="Head1" Geometry="Head"><Break DMXOffset="1" /><Break DMXBreak="2" DMXOffset="1" /></GeometryReference>
            <GeometryReference Name="Head2" Geometry="Head"><Break DMXOffset="2" /><Break DMXBreak="2" DMXOffset="2" /></GeometryReference>
            <GeometryReference Name="Head3" Geometry="Head"><Break DMXOffset="3" /><Break DMXBreak="2" DMXOffset="3" /></GeometryReference>
            <GeometryReference Name="Head4" Geometry="Head"><Break DMXOffset="4" /><Break DMXBreak="2" DMXOffset="4" /></GeometryReference>
          </Geometry>
        </Geometries>
        <DMXModes>
          <DMXMode Name="Mode 1" Geometry="Body">
            <DMXChannels>
              <DMXChannel Offset="1" Highlight="255/1" Geometry="Head">
                <LogicalChannel Attribute="Dimmer">
                  <ChannelFunction Attribute="Dimmer" DMXFrom="0/1" Default="0/1" PhysicalFrom="0" PhysicalTo="1" RealFade="0" />
                </LogicalChannel>
              </DMXChannel>
              <DMXChannel DMXBreak="2" Offset="1" Highlight="0/1" Geometry="Head">
                <LogicalChannel Attribute="Color1">
                  <ChannelFunction Attribute="Color1" DMXFrom="0/1" Default="0/1" PhysicalFrom="-0.5" PhysicalTo="0.5" Wheel="ColorScroller">
                    <ChannelSet Name="Gel1" DMXFrom="0/1" PhysicalFrom="0" PhysicalTo="0.5" WheelSlotIndex="1" />
                    <ChannelSet Name="Gel2" DMXFrom="51/1" PhysicalFrom="-0.5" PhysicalTo="0.5" WheelSlotIndex="2" />
                    <ChannelSet Name="Gel3" DMXFrom="102/1" WheelSlotIndex="3" />
                    <ChannelSet Name="Gel4" DMXFrom="153/1" PhysicalFrom="-0.5" PhysicalTo="0.5" WheelSlotIndex="4" />
                    <ChannelSet Name="Gel5" DMXFrom="204/1" PhysicalFrom="-0.5" PhysicalTo="0" WheelSlotIndex="5" />
                  </ChannelFunction>
                </LogicalChannel>
              </DMXChannel>
            </DMXChannels>
            <Relations />
          </DMXMode>
        </DMXModes>
        """;

    [Fact]
    public void ReadsTheBreaksExample()
    {
        var result = Read(BreaksExample);
        Assert.Empty(result.Diagnostics);
        var fixture = result.Document.FixtureType;
        var mode = fixture.FindDmxMode(new Name("Mode 1"))!;
        Assert.Same(fixture.FindGeometry(new Name("Body")), mode.Geometry);

        Assert.Equal(new[] { "Head_Dimmer", "Head_Color1" }, mode.Channels.Select(c => c.Name));
        var dimmer = mode.FindChannel("Head_Dimmer")!;
        Assert.Same(fixture.FindGeometry(new Name("Head")), dimmer.Geometry);
        Assert.Equal(1, dimmer.DmxBreak.Number);
        Assert.Equal(new[] { 1 }, dimmer.Offsets);
        Assert.Equal(DmxValue.Parse("255/1"), dimmer.Highlight);
        Assert.Same(fixture.AttributeDefinitions.FindAttribute(new Name("Dimmer")), dimmer.LogicalChannels[0].Attribute);

        var color = mode.FindChannel("Head_Color1")!;
        Assert.Equal(2, color.DmxBreak.Number);
        var function = color.ChannelFunctions.Single();
        Assert.Same(fixture.FindWheel(new Name("ColorScroller")), function.Wheel);
        Assert.Equal("Color1 1", function.EffectiveName);
        Assert.Null(function.Name);
        Assert.Equal("Head_Color1.Color1.Color1 1", function.Path.ToString());

        var gels = function.ChannelSets;
        Assert.Equal(new ulong[] { 50, 101, 152, 203, 255 }, gels.Select(s => s.DmxTo));
        Assert.Equal("Gel3", gels[2].WheelSlot!.Name.Value);
        Assert.Equal(-0.5f, gels[2].EffectivePhysicalFrom);
        Assert.Equal(0.5f, gels[2].EffectivePhysicalTo);
        Assert.Null(gels[2].PhysicalFrom);
    }

    [Fact]
    public void ExpandsChannelsAcrossGeometryReferences()
    {
        var mode = Read(BreaksExample).Document.FixtureType.DmxModes[0];
        var instances = mode.GetChannelInstances();

        // Four heads, each with a dimmer on break 1 and a scroller on break 2, at the reference's offset.
        Assert.Equal(8, instances.Count);
        Assert.All(instances, i => Assert.NotNull(i.Reference));
        Assert.Equal(new[] { 1, 2, 3, 4 }, instances.Where(i => i.DmxBreak == 1).Select(i => i.Addresses.Single()));
        Assert.Equal(new[] { 1, 2, 3, 4 }, instances.Where(i => i.DmxBreak == 2).Select(i => i.Addresses.Single()));
        Assert.Equal(new[] { "Head1", "Head2", "Head3", "Head4" },
            instances.Where(i => i.Channel.Name == "Head_Dimmer").Select(i => i.Reference!.Name.Value));

        var footprint = mode.GetFootprint();
        Assert.Equal(4, footprint[1]);
        Assert.Equal(4, footprint[2]);
    }

    // ---- Spec Listing 1: Overwrite breaks and relations --------------------------------------------------------

    private const string OverwriteExample = """
        <Geometries>
          <Geometry Name="Pixel"><Beam Name="PixelBeam" /></Geometry>
          <Geometry Name="Body">
            <Beam Name="Main" />
            <GeometryReference Name="Pixel1" Geometry="Pixel"><Break DMXOffset="1" /><Break DMXBreak="1" DMXOffset="2" /></GeometryReference>
            <GeometryReference Name="Pixel2" Geometry="Pixel"><Break DMXBreak="1" DMXOffset="5" /></GeometryReference>
          </Geometry>
        </Geometries>
        <DMXModes>
          <DMXMode Name="Default" Geometry="Body" Description="Dimmer and three pixels">
            <DMXChannels>
              <DMXChannel Offset="1" Highlight="255/1" Geometry="Main">
                <LogicalChannel Attribute="Dimmer" Master="Grand" Snap="Off" MibFade="0.5" DMXChangeTimeLimit="0.1">
                  <ChannelFunction Attribute="Dimmer" DMXFrom="0/1" Default="0/1">
                    <ChannelSet Name="closed" DMXFrom="0/1" PhysicalTo="0" />
                    <ChannelSet DMXFrom="1/1" />
                    <ChannelSet Name="open" DMXFrom="255/1" PhysicalFrom="1" />
                  </ChannelFunction>
                </LogicalChannel>
              </DMXChannel>
              <DMXChannel DMXBreak="Overwrite" Offset="1" Geometry="PixelBeam">
                <LogicalChannel Attribute="ColorAdd_R"><ChannelFunction Attribute="ColorAdd_R" Default="255/1" /></LogicalChannel>
              </DMXChannel>
              <DMXChannel DMXBreak="Overwrite" Offset="2" Geometry="PixelBeam">
                <LogicalChannel Attribute="ColorAdd_G"><ChannelFunction Attribute="ColorAdd_G" Default="255/1" /></LogicalChannel>
              </DMXChannel>
              <DMXChannel DMXBreak="Overwrite" Offset="3" Geometry="PixelBeam">
                <LogicalChannel Attribute="ColorAdd_B"><ChannelFunction Attribute="ColorAdd_B" Default="255/1" /></LogicalChannel>
              </DMXChannel>
            </DMXChannels>
            <Relations>
              <Relation Name="VirtualDimmer" Master="Main_Dimmer" Follower="PixelBeam_ColorAdd_R.ColorAdd_R.ColorAdd_R 1" Type="Multiply" />
              <Relation Name="VirtualDimmerG" Master="Main_Dimmer" Follower="PixelBeam_ColorAdd_G.ColorAdd_G.ColorAdd_G 1" Type="Override" />
            </Relations>
            <FTMacros>
              <FTMacro Name="Reset" ChannelFunction="Main_Dimmer.Dimmer.Dimmer 1">
                <MacroDMX>
                  <MacroDMXStep Duration="2">
                    <MacroDMXValue Value="255/1" DMXChannel="Main_Dimmer" />
                    <MacroDMXValue Value="0/1" DMXChannel="PixelBeam_ColorAdd_R" />
                  </MacroDMXStep>
                  <MacroDMXStep />
                </MacroDMX>
              </FTMacro>
              <FTMacro Name="Empty" />
            </FTMacros>
          </DMXMode>
        </DMXModes>
        """;

    [Fact]
    public void ReadsRelationsMacrosAndLogicalChannels()
    {
        var result = Read(OverwriteExample);
        Assert.Empty(result.Diagnostics);
        var mode = result.Document.FixtureType.DmxModes[0];
        Assert.Equal("Dimmer and three pixels", mode.Description);

        var logical = mode.FindChannel("Main_Dimmer")!.LogicalChannels[0];
        Assert.Equal(LogicalChannelMaster.Grand, logical.Master);
        Assert.Equal(Snap.Off, logical.Snap);
        Assert.Equal(0.5f, logical.MibFade);
        Assert.Equal(0.1f, logical.DmxChangeTimeLimit);

        var sets = logical.ChannelFunctions[0].ChannelSets;
        Assert.Equal("", sets[1].Name.Value);
        Assert.Equal(new ulong[] { 0, 254, 255 }, sets.Select(s => s.DmxTo));

        var red = mode.FindChannel("PixelBeam_ColorAdd_R")!;
        Assert.True(red.DmxBreak.IsOverwrite);
        Assert.Equal(DmxValue.Parse("0/1"), red.ChannelFunctions.Single().DmxFrom);
        Assert.Equal(DmxValue.Parse("255/1"), red.ChannelFunctions.Single().Default);
        Assert.Null(red.Highlight);

        Assert.Equal(2, mode.Relations.Count);
        Assert.Same(mode.FindChannel("Main_Dimmer"), mode.Relations[0].Master);
        Assert.Same(red.ChannelFunctions.Single(), mode.Relations[0].Follower);
        Assert.Equal(RelationType.Override, mode.Relations[1].Type);

        var reset = mode.Macros[0];
        Assert.Same(logical.ChannelFunctions[0], reset.ChannelFunction);
        Assert.Equal(2, reset.DmxSteps!.Count);
        Assert.Equal(2f, reset.DmxSteps[0].Duration);
        Assert.Equal(1f, reset.DmxSteps[1].Duration);
        Assert.Same(red, reset.DmxSteps[0].Values[1].DmxChannel);
        Assert.Null(mode.Macros[1].DmxSteps);
    }

    [Fact]
    public void OverwriteChannelsUseTheLastBreakOfEachReference()
    {
        var instances = Read(OverwriteExample).Document.FixtureType.DmxModes[0].GetChannelInstances();
        Assert.Equal(7, instances.Count);
        Assert.Null(instances[0].Reference);
        Assert.Equal(new[] { 1 }, instances[0].Addresses);

        var pixel1 = instances.Where(i => i.Reference?.Name.Value == "Pixel1").ToList();
        Assert.Equal(new[] { 2, 3, 4 }, pixel1.Select(i => i.Addresses.Single()));
        var pixel2 = instances.Where(i => i.Reference?.Name.Value == "Pixel2").ToList();
        Assert.Equal(new[] { 5, 6, 7 }, pixel2.Select(i => i.Addresses.Single()));
        Assert.Equal(7, Read(OverwriteExample).Document.FixtureType.DmxModes[0].GetFootprint()[1]);
    }

    [Fact]
    public void ExamplesRoundTrip()
    {
        foreach (var example in new[] { BreaksExample, OverwriteExample })
        {
            var document = Read(example).Document;
            var written = GdtfWriter.ToXDocument(document).Root!.Element("FixtureType")!.Element("DMXModes")!;
            var original = XElement.Parse("<Root>" + example + "</Root>").Element("DMXModes")!;
            XmlEquivalence.AssertEquivalent(original, written);

            var reread = GdtfReader.Read(GdtfWriter.WriteToString(document));
            Assert.Empty(reread.Diagnostics);
            Assert.Equal(GdtfWriter.WriteToString(document), GdtfWriter.WriteToString(reread.Document));
        }
    }

    // ---- Channel functions --------------------------------------------------------------------------------------

    private static string Mode(string channels, string extra = "") => $"""
        <Wheels><Wheel Name="Gobo"><Slot Name="Open" /></Wheel></Wheels>
        <PhysicalDescriptions>
          <Emitters><Emitter Name="Red" Color="0.7,0.3,100" /></Emitters>
          <Filters><Filter Name="Cyan" /></Filters>
          <ColorSpace Mode="sRGB" />
          <Gamuts><Gamut Name="Native" Points="{"{"}0.7,0.29,100{"}"}" /></Gamuts>
          <DMXProfiles><DMXProfile Name="Curve" /></DMXProfiles>
        </PhysicalDescriptions>
        <Geometries><Geometry Name="Body" /></Geometries>
        <DMXModes><DMXMode Name="M" Geometry="Body"><DMXChannels>{channels}</DMXChannels>{extra}</DMXMode></DMXModes>
        """;

    [Fact]
    public void ReadsAllChannelFunctionAttributes()
    {
        var result = Read(Mode("""
            <DMXChannel Offset="1,2" Geometry="Body" InitialFunction="Body_Shutter1Strobe.Shutter1Strobe.Strobe">
              <LogicalChannel Attribute="Shutter1Strobe">
                <ChannelFunction Name="Closed" Attribute="Shutter1" OriginalAttribute="Shutter" DMXFrom="0/1" Default="0/1" PhysicalFrom="0" PhysicalTo="0" />
                <ChannelFunction Name="Strobe" Attribute="Shutter1Strobe" DMXFrom="32/1" Default="64/1" PhysicalFrom="1" PhysicalTo="25"
                  RealFade="1.5" RealAcceleration="0.2" Wheel="Gobo" Emitter="Red" Filter="Cyan" ColorSpace="Default" Gamut="Native"
                  ModeMaster="Body_Shutter1Strobe" ModeFrom="0/1" ModeTo="10/1" DMXProfile="Curve" Min="2" Max="20" CustomName="Strobe/Rate">
                  <SubChannelSet Name="Short" PhysicalFrom="0" PhysicalTo="50" SubPhysicalUnit="DutyCycle" DMXProfile="Curve" />
                  <SubChannelSet Name="Long" PhysicalFrom="0.1" PhysicalTo="2" SubPhysicalUnit="Shutter1Strobe.Duration" />
                </ChannelFunction>
                <ChannelFunction Attribute="NoFeature" DMXFrom="200/1" Default="200/1" ModeMaster="Body_Shutter1Strobe.Shutter1Strobe.Closed" />
              </LogicalChannel>
            </DMXChannel>
            """));
        Assert.Empty(result.Diagnostics);
        var fixture = result.Document.FixtureType;
        var pd = fixture.PhysicalDescriptions;
        var channel = fixture.DmxModes[0].Channels.Single();
        var functions = channel.ChannelFunctions.ToList();

        Assert.Equal(2, channel.ByteCount);
        Assert.Equal(65535UL, channel.MaxDmxValue);
        Assert.Same(functions[1], channel.InitialFunction);

        var closed = functions[0];
        Assert.Equal("Closed", closed.EffectiveName);
        Assert.Same(fixture.AttributeDefinitions.FindAttribute(new Name("Shutter1")), closed.Attribute);
        Assert.Equal("Shutter", closed.OriginalAttribute);
        // 32/1 in a 16 bit channel is 0x2020 (byte mirroring), so "Closed" ends at 0x201F.
        Assert.Equal(0x201FUL, closed.DmxTo);

        var strobe = functions[1];
        Assert.Equal(DmxValue.Parse("64/1"), strobe.Default);
        Assert.Equal(1.5f, strobe.RealFade);
        Assert.Equal(0.2f, strobe.RealAcceleration);
        Assert.Same(fixture.Wheels[0], strobe.Wheel);
        Assert.Same(pd.Emitters[0], strobe.Emitter);
        Assert.Same(pd.Filters[0], strobe.Filter);
        Assert.Same(pd.ColorSpace, strobe.ColorSpace);
        Assert.Same(pd.Gamuts[0], strobe.Gamut);
        Assert.Same(channel, strobe.ModeMaster);
        Assert.Equal(DmxValue.Parse("10/1"), strobe.ModeTo);
        Assert.Same(pd.DmxProfiles[0], strobe.DmxProfile);
        Assert.Equal(2f, strobe.EffectiveMin);
        Assert.Equal(20f, strobe.EffectiveMax);
        Assert.Equal("Strobe/Rate", strobe.CustomName);
        // The next function starts at 200/1 = 0xC8C8 in 16 bit.
        Assert.Equal(0xC8C7UL, strobe.DmxTo);

        var strobeAttribute = fixture.AttributeDefinitions.FindAttribute(new Name("Shutter1Strobe"))!;
        Assert.Same(strobeAttribute.SubPhysicalUnits[0], strobe.SubChannelSets[0].SubPhysicalUnit);
        Assert.Same(strobeAttribute.SubPhysicalUnits[1], strobe.SubChannelSets[1].SubPhysicalUnit);
        Assert.Same(pd.DmxProfiles[0], strobe.SubChannelSets[0].DmxProfile);
        Assert.Equal(50f, strobe.SubChannelSets[0].PhysicalTo);

        var noFeature = functions[2];
        Assert.Null(noFeature.Attribute);
        Assert.Equal("NoFeature 3", noFeature.EffectiveName);
        Assert.Same(closed, noFeature.ModeMaster);
        Assert.Equal(65535UL, noFeature.DmxTo);
        Assert.Equal(1f, noFeature.EffectiveMax);
    }

    [Fact]
    public void FunctionWithoutDefaultStartsAtDmxFrom()
    {
        var result = Read(Mode("""
            <DMXChannel Offset="1" Geometry="Body"><LogicalChannel Attribute="Dimmer"><ChannelFunction Attribute="Dimmer" DMXFrom="10/1" /></LogicalChannel></DMXChannel>
            """));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(DmxValue.Parse("10/1"), result.Document.FixtureType.DmxModes[0].Channels[0].ChannelFunctions.Single().Default);
    }

    [Fact]
    public void VirtualChannelUsesTheResolutionOfItsValues()
    {
        var channel = Read(Mode("""
            <DMXChannel Geometry="Body"><LogicalChannel Attribute="Dimmer"><ChannelFunction Attribute="Dimmer" DMXFrom="0/2" /></LogicalChannel></DMXChannel>
            """)).Document.FixtureType.DmxModes[0].Channels[0];
        Assert.True(channel.IsVirtual);
        Assert.Equal(2, channel.ByteCount);
        Assert.Empty(channel.Offsets);
        Assert.Equal("None", GdtfWriter.ToXDocument(new GdtfDocument(channel.Mode!.Geometry!.Parent as FixtureType ?? throw new Exception())).Descendants("DMXChannel").Single().Attribute("Offset")!.Value);
    }

    [Fact]
    public void LegacyChannelDefaultIsMigratedToTheMatchingFunction()
    {
        const string channel = """
            <DMXChannel Offset="1" Default="128/1" Geometry="Body">
              <LogicalChannel Attribute="Shutter1">
                <ChannelFunction Attribute="Shutter1" DMXFrom="0/1" />
                <ChannelFunction Attribute="Shutter1Strobe" DMXFrom="100/1" />
                <ChannelFunction Attribute="NoFeature" DMXFrom="200/1" />
              </LogicalChannel>
            </DMXChannel>
            """;
        var legacy = Read(Mode(channel), "1.0");
        Assert.Empty(legacy.Diagnostics);
        var migrated = legacy.Document.FixtureType.DmxModes[0].Channels[0];
        var functions = migrated.ChannelFunctions.ToList();
        Assert.Equal(DmxValue.Parse("0/1"), functions[0].Default);
        Assert.Equal(DmxValue.Parse("128/1"), functions[1].Default);
        Assert.Equal(DmxValue.Parse("200/1"), functions[2].Default);
        Assert.Same(functions[1], migrated.InitialFunction);

        var written = GdtfWriter.ToXDocument(legacy.Document).Descendants("DMXChannel").Single();
        Assert.Null(written.Attribute("Default"));
        Assert.Equal("Body_Shutter1.Shutter1.Shutter1Strobe 2", written.Attribute("InitialFunction")!.Value);

        // The same in a 1.1+ file is reported, then migrated the same way.
        var modern = Read(Mode(channel));
        Assert.Contains(modern.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Warning && d.Message.Contains("GDTF 1.0"));
    }

    [Fact]
    public void LegacyDefaultInTheFirstFunctionNeedsNoInitialFunction()
    {
        var migrated = Read(Mode("""
            <DMXChannel Offset="1" Default="20/1" Geometry="Body"><LogicalChannel Attribute="Dimmer">
              <ChannelFunction Attribute="Dimmer" DMXFrom="0/1" /><ChannelFunction Attribute="NoFeature" DMXFrom="200/1" />
            </LogicalChannel></DMXChannel>
            """), "1.0").Document.FixtureType.DmxModes[0].Channels[0];
        Assert.Null(migrated.InitialFunction);
        Assert.Equal(DmxValue.Parse("20/1"), migrated.ChannelFunctions.First().Default);
        Assert.Same(migrated.ChannelFunctions.First(), migrated.EffectiveInitialFunction);
    }

    [Fact]
    public void LegacyFunctionNamesAreNotCheckedInOldFiles()
    {
        const string channel = """
            <DMXChannel Offset="1" Geometry="Body"><LogicalChannel Attribute="Dimmer"><ChannelFunction Name="Speed, slow" Attribute="Dimmer" /></LogicalChannel></DMXChannel>
            """;
        Assert.Empty(Read(Mode(channel), "1.0").Diagnostics);
        Assert.Contains(Read(Mode(channel)).Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Warning);
    }

    [Fact]
    public void FunctionsChainAcrossLogicalChannels()
    {
        var channel = Read(Mode("""
            <DMXChannel Offset="1" Geometry="Body">
              <LogicalChannel Attribute="Dimmer"><ChannelFunction Attribute="Dimmer" DMXFrom="0/1" /></LogicalChannel>
              <LogicalChannel Attribute="Control1">
                <ChannelFunction Attribute="Control1" DMXFrom="128/1" />
                <ChannelFunction Name="Same" Attribute="Control1" DMXFrom="128/1" />
              </LogicalChannel>
            </DMXChannel>
            """)).Document.FixtureType.DmxModes[0].Channels[0];
        var functions = channel.ChannelFunctions.ToList();
        Assert.Equal(127UL, functions[0].DmxTo);
        // The next function does not start later, so this one runs to the channel maximum.
        Assert.Equal(255UL, functions[1].DmxTo);
        Assert.Equal(255UL, functions[2].DmxTo);
    }

    [Fact]
    public void GammutSpellingOfTheXsdIsAccepted()
    {
        var result = Read(Mode("""
            <DMXChannel Offset="1" Geometry="Body"><LogicalChannel Attribute="Dimmer"><ChannelFunction Attribute="Dimmer" Gammut="Native" /></LogicalChannel></DMXChannel>
            """));
        Assert.Empty(result.Diagnostics);
        var function = result.Document.FixtureType.DmxModes[0].Channels[0].ChannelFunctions.Single();
        Assert.NotNull(function.Gamut);
        Assert.Equal("Native", GdtfWriter.ToXDocument(result.Document).Descendants("ChannelFunction").Single().Attribute("Gamut")!.Value);
    }

    [Fact]
    public void NoFeatureResolvesWhenDefined()
    {
        var xml = Mode("""
            <DMXChannel Offset="1" Geometry="Body"><LogicalChannel Attribute="Dimmer"><ChannelFunction Attribute="NoFeature" /></LogicalChannel></DMXChannel>
            """);
        var defined = GdtfReader.Read("<GDTF DataVersion=\"1.2\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\">" +
                                      Attributes.Replace("</Attributes>", "<Attribute Name=\"NoFeature\" Pretty=\"\" Feature=\"Control.Control\" /></Attributes>") +
                                      xml + "</FixtureType></GDTF>");
        Assert.Empty(defined.Diagnostics);
        Assert.Equal("NoFeature", defined.Document.FixtureType.DmxModes[0].Channels[0].ChannelFunctions.Single().Attribute!.Name.Value);
    }

    [Fact]
    public void ProblemsAreReported()
    {
        var result = Read(Mode("""
            <DMXChannel Offset="1" Geometry="Nowhere" InitialFunction="Body_Dimmer.Dimmer">
              <LogicalChannel Attribute="Dimmer">
                <ChannelFunction Attribute="Dimmer" ModeMaster="Body_Dimmer.Dimmer" />
                <ChannelFunction Attribute="Unknown" />
              </LogicalChannel>
            </DMXChannel>
            <DMXChannel Offset="2" Geometry="Body"><LogicalChannel Attribute="Dimmer">
              <ChannelFunction Attribute="Shutter1Strobe"><SubChannelSet SubPhysicalUnit="Amplitude" /></ChannelFunction>
            </LogicalChannel></DMXChannel>
            <DMXChannel Offset="3" Geometry="Body"><LogicalChannel Attribute="Dimmer"><ChannelFunction /></LogicalChannel></DMXChannel>
            """, "<Relations><Relation Name=\"R\" Master=\"Body_Dimmer.Dimmer\" Follower=\"Ghost\" Type=\"Multiply\" /></Relations>"));
        var messages = result.Diagnostics.Select(d => $"{d.Severity}: {d.Message}").ToList();
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'Nowhere'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'Body_Dimmer.Dimmer' is not a channel function"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("ModeMaster must link to a DMX channel or a channel function"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'Unknown'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("Sub physical unit 'Amplitude' not found"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("Duplicate name 'Body_Dimmer'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'Body_Dimmer.Dimmer' is not a DMX channel"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'Ghost'"));
    }

    // ---- Built in code ------------------------------------------------------------------------------------------

    private static FixtureType BuildFixture()
    {
        var fixture = new FixtureType(new Name("Built"), Guid.NewGuid());
        var group = new FeatureGroup(new Name("Dimmer"));
        var feature = new Feature(new Name("Dimmer"));
        group.Features.Add(feature);
        fixture.AttributeDefinitions.FeatureGroups.Add(group);
        var dimmerAttribute = new GdtfAttribute(new Name("Dimmer"), feature);
        var redAttribute = new GdtfAttribute(new Name("ColorAdd_R"), feature);
        fixture.AttributeDefinitions.Attributes.Add(dimmerAttribute);
        fixture.AttributeDefinitions.Attributes.Add(redAttribute);

        var body = new Geometry(new Name("Body"));
        var pixel = new Geometry(new Name("Pixel"));
        fixture.Geometries.Add(body);
        fixture.Geometries.Add(pixel);
        var reference = new GeometryReference(new Name("Pixel1")) { ReferencedGeometry = pixel };
        reference.Breaks.Add(new GeometryBreak { DmxOffset = new DmxAddress(2) });
        body.Geometries.Add(reference);

        var mode = new DmxMode(new Name("Standard")) { Geometry = body };
        var dimmer = new DmxChannel(body) { Highlight = DmxValue.Parse("255/1") };
        dimmer.Offsets.Add(1);
        var dimmerLogical = new LogicalChannel(dimmerAttribute) { Master = LogicalChannelMaster.Grand };
        var dimmerFunction = new ChannelFunction(dimmerAttribute) { PhysicalTo = 1 };
        dimmerFunction.ChannelSets.Add(new ChannelSet(new Name("Closed")));
        dimmerFunction.ChannelSets.Add(new ChannelSet(new Name("Open")) { DmxFrom = DmxValue.Parse("255/1") });
        dimmerLogical.ChannelFunctions.Add(dimmerFunction);
        dimmer.LogicalChannels.Add(dimmerLogical);
        mode.Channels.Add(dimmer);

        var red = new DmxChannel(pixel) { DmxBreak = DmxBreak.Overwrite };
        red.Offsets.Add(1);
        var redLogical = new LogicalChannel(redAttribute);
        var redFunction = new ChannelFunction(redAttribute) { Default = DmxValue.Parse("255/1"), ModeMaster = dimmer };
        redLogical.ChannelFunctions.Add(redFunction);
        red.LogicalChannels.Add(redLogical);
        mode.Channels.Add(red);

        mode.Relations.Add(new Relation(new Name("Virtual"), RelationType.Multiply) { Master = dimmer, Follower = redFunction });
        var macro = new FTMacro(new Name("Full")) { ChannelFunction = dimmerFunction, DmxSteps = new List<MacroDmxStep>() };
        var step = new MacroDmxStep { Duration = 0.5f };
        step.Values.Add(new MacroDmxValue(DmxValue.Parse("255/1"), dimmer));
        macro.DmxSteps.Add(step);
        mode.Macros.Add(macro);
        fixture.DmxModes.Add(mode);
        return fixture;
    }

    [Fact]
    public void ModelBuiltInCodeWritesPathsAndRoundTrips()
    {
        var document = new GdtfDocument(BuildFixture());
        var xml = GdtfWriter.ToXDocument(document);
        var relation = xml.Descendants("Relation").Single();
        Assert.Equal("Body_Dimmer", relation.Attribute("Master")!.Value);
        Assert.Equal("Pixel_ColorAdd_R.ColorAdd_R.ColorAdd_R 1", relation.Attribute("Follower")!.Value);
        Assert.Equal("Body_Dimmer", xml.Descendants("ChannelFunction").Last().Attribute("ModeMaster")!.Value);
        Assert.Equal("Overwrite", xml.Descendants("DMXChannel").Last().Attribute("DMXBreak")!.Value);
        Assert.Equal("Body_Dimmer.Dimmer.Dimmer 1", xml.Descendants("FTMacro").Single().Attribute("ChannelFunction")!.Value);

        var reread = GdtfReader.Read(GdtfWriter.WriteToString(document));
        Assert.Empty(reread.Diagnostics);
        Assert.Equal(GdtfWriter.WriteToString(document), GdtfWriter.WriteToString(reread.Document));
        var instances = reread.Document.FixtureType.DmxModes[0].GetChannelInstances();
        Assert.Equal(new[] { 1, 2 }, instances.Select(i => i.Addresses.Single()));
    }

    [Fact]
    public void ModelBuiltInCodeValidates()
    {
        if (!File.Exists(TestData.Xsd)) Assert.Skip("gdtf.xsd missing; run scripts/fetch-testdata.sh.");
        XsdValidation.AssertValid(GdtfWriter.ToXDocument(new GdtfDocument(BuildFixture())));
    }

    [Fact]
    public void RdmPersonalityReferencesTheMode()
    {
        var result = Read(BreaksExample + "<Protocols><FTRDM ManufacturerID=\"0x1\" DeviceModelID=\"0x2\"><SoftwareVersionID Value=\"0x1\">" +
                          "<DMXPersonality Value=\"0x1\" DMXMode=\"Mode 1\" /><DMXPersonality Value=\"0x2\" DMXMode=\"Mode 9\" />" +
                          "</SoftwareVersionID></FTRDM></Protocols>");
        var personalities = result.Document.FixtureType.Protocols.Rdm!.SoftwareVersions[0].DmxPersonalities;
        Assert.Same(result.Document.FixtureType.DmxModes[0], personalities[0].DmxMode);
        Assert.Null(personalities[1].DmxMode);
        Assert.Single(result.Diagnostics, d => d.Message.Contains("'Mode 9'"));
    }
}
