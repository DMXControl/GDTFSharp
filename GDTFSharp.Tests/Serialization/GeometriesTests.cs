using System.Numerics;
using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Tests.Serialization;

public class GeometriesTests
{
    private const string Before =
        "<GDTF DataVersion=\"1.2\"><FixtureType Name=\"X\" FixtureTypeID=\"308EA87D-7164-42DE-8106-A6D273F57A51\">" +
        "<AttributeDefinitions><FeatureGroups /><Attributes /></AttributeDefinitions>" +
        "<PhysicalDescriptions><Emitters><Emitter Name=\"White\" Color=\"0.3127,0.329,100\" /><Emitter Name=\"Green\" Color=\"0.17,0.7,100\" /></Emitters></PhysicalDescriptions>" +
        "<Models><Model Name=\"Base\" /><Model Name=\"Head\" /><Model Name=\"Pixel\" /></Models>";

    private const string After = "<DMXModes /></FixtureType></GDTF>";

    private static GdtfReadResult Read(string geometries) => GdtfReader.Read(Before + "<Geometries>" + geometries + "</Geometries>" + After);

    private const string Full = """
        <Geometry Name="Base" Model="Base" Position="{1,0,0,0}{0,1,0,0}{0,0,1,0.5}{0,0,0,1}">
          <Axis Name="Yoke" Position="{1,0,0,0}{0,1,0,0}{0,0,1,-0.2}{0,0,0,1}">
            <Axis Name="Head" Model="Head" Position="{1,0,0,0.1}{0,1,0,0}{0,0,1,-0.3}{0,0,0,1}">
              <Beam Name="Beam" LampType="LED" PowerConsumption="300" LuminousFlux="12000" ColorTemperature="7000" BeamAngle="10" FieldAngle="14" ThrowRatio="1.5" RectangleRatio="1.333" BeamRadius="0.08" BeamType="Spot" ColorRenderingIndex="92" EmitterSpectrum="White" />
              <FilterBeam Name="Iris" />
              <FilterColor Name="FilterColor" />
              <FilterGobo Name="FilterGobo" />
              <FilterShaper Name="Shaper" />
            </Axis>
          </Axis>
          <MediaServerLayer Name="Layer" />
          <MediaServerCamera Name="Camera" />
          <MediaServerMaster Name="Master" />
          <Display Name="Screen" Texture="screen_tex" />
          <Laser Name="Laser" ColorType="SingleWaveLength" Color="532" OutputStrength="2" Emitter="Green" BeamDiameter="0.004" BeamDivergenceMin="0.5" BeamDivergenceMax="1.5" ScanAnglePan="60" ScanAngleTilt="40" ScanSpeed="30">
            <Protocol Name="ILDA" />
            <Protocol Name="FB4" />
          </Laser>
          <WiringObject Name="PowerIn" ConnectorType="PowerCon" ComponentType="Input" SignalType="Power" PinCount="3" SignalLayer="1" Orientation="Left" WireGroup="Mains">
            <PinPatch ToWiringObject="Fuse" FromPin="1" ToPin="1" />
            <PinPatch ToWiringObject="Psu" FromPin="2" ToPin="1" />
          </WiringObject>
          <WiringObject Name="Fuse" ComponentType="Fuse" FuseCurrent="10" FuseRating="B" />
          <WiringObject Name="Psu" ComponentType="Consumer" ElectricalPayLoad="300" VoltageRangeMin="100" VoltageRangeMax="240" FrequencyRangeMin="50" FrequencyRangeMax="60" CosPhi="0.95" />
          <WiringObject Name="Out" ComponentType="PowerSource" MaxPayLoad="3600" Voltage="230" />
          <Inventory Name="RainCover" Count="2" />
          <Structure Name="Truss" LinkedGeometry="Base" StructureType="CenterLineBased" CrossSectionType="Tube" CrossSectionHeight="0.05" CrossSectionWallThickness="0.002" TrussCrossSection="F34" />
          <Support Name="Hoist" SupportType="Rope" RopeCrossSection="Steel 8mm" RopeOffset="0,0,1.5" CapacityX="1" CapacityY="2" CapacityZ="3" CapacityXX="4" CapacityYY="5" CapacityZZ="6" ResistanceX="7" ResistanceY="8" ResistanceZ="9" ResistanceXX="10" ResistanceYY="11" ResistanceZZ="12" />
          <Magnet Name="Magnet" />
          <GeometryReference Name="Pixel 1" Geometry="Pixel" Model="Pixel" Position="{1,0,0,-0.1}{0,1,0,0}{0,0,1,0}{0,0,0,1}">
            <Break DMXOffset="1" />
            <Break DMXBreak="2" DMXOffset="4" />
          </GeometryReference>
          <GeometryReference Name="Pixel 2" Geometry="Pixel" />
        </Geometry>
        <Geometry Name="Pixel" Model="Pixel">
          <Beam Name="PixelBeam" />
        </Geometry>
        """;

    [Fact]
    public void ReadsTheTree()
    {
        var result = Read(Full);
        Assert.Empty(result.Diagnostics);
        var fixture = result.Document.FixtureType;

        Assert.Equal(new[] { "Base", "Pixel" }, fixture.Geometries.Select(g => g.Name.Value));
        var baseGeometry = fixture.Geometries[0];
        Assert.True(baseGeometry.IsTopLevel);
        Assert.Same(fixture, baseGeometry.Parent);
        Assert.Same(fixture.FindModel(new Name("Base")), baseGeometry.Model);

        var head = (Axis)fixture.FindGeometry(new Name("Head"))!;
        Assert.False(head.IsTopLevel);
        Assert.Equal("Yoke", head.ParentGeometry!.Name.Value);
        Assert.Same(baseGeometry, head.Root);
        Assert.Null(fixture.FindGeometry(new Name("Yoke"))!.Model);

        // Children keep document order across different geometry types.
        Assert.Equal(
            new[] { "Yoke", "Layer", "Camera", "Master", "Screen", "Laser", "PowerIn", "Fuse", "Psu", "Out", "RainCover", "Truss", "Hoist", "Magnet", "Pixel 1", "Pixel 2" },
            baseGeometry.Geometries.Select(g => g.Name.Value));
        Assert.Equal(
            new[] { typeof(Beam), typeof(FilterBeam), typeof(FilterColor), typeof(FilterGobo), typeof(FilterShaper) },
            head.Geometries.Select(g => g.GetType()));
        Assert.Equal(25, fixture.AllGeometries().Count());
    }

    [Fact]
    public void ReadsTypeSpecificAttributes()
    {
        var fixture = Read(Full).Document.FixtureType;
        var emitters = fixture.PhysicalDescriptions;

        var beam = (Beam)fixture.FindGeometry(new Name("Beam"))!;
        Assert.Equal(LampType.Led, beam.LampType);
        Assert.Equal(300f, beam.PowerConsumption);
        Assert.Equal(12000f, beam.LuminousFlux);
        Assert.Equal(7000f, beam.ColorTemperature);
        Assert.Equal(10f, beam.BeamAngle);
        Assert.Equal(14f, beam.FieldAngle);
        Assert.Equal(1.5f, beam.ThrowRatio);
        Assert.Equal(1.333f, beam.RectangleRatio);
        Assert.Equal(0.08f, beam.BeamRadius);
        Assert.Equal(BeamType.Spot, beam.BeamType);
        Assert.Equal(92, beam.ColorRenderingIndex);
        Assert.Same(emitters.FindEmitter(new Name("White")), beam.EmitterSpectrum);

        var pixelBeam = (Beam)fixture.FindGeometry(new Name("PixelBeam"))!;
        Assert.Equal(LampType.Discharge, pixelBeam.LampType);
        Assert.Equal(1000f, pixelBeam.PowerConsumption);
        Assert.Equal(1.7777f, pixelBeam.RectangleRatio);
        Assert.Equal(BeamType.Wash, pixelBeam.BeamType);
        Assert.Null(pixelBeam.EmitterSpectrum);

        Assert.Equal("screen_tex", ((Display)fixture.FindGeometry(new Name("Screen"))!).Texture);

        var laser = (Laser)fixture.FindGeometry(new Name("Laser"))!;
        Assert.Equal(LaserColorType.SingleWaveLength, laser.ColorType);
        Assert.Equal(532f, laser.Color);
        Assert.Same(emitters.FindEmitter(new Name("Green")), laser.Emitter);
        Assert.Equal(30f, laser.ScanSpeed);
        Assert.Equal(new[] { "ILDA", "FB4" }, laser.Protocols);

        var powerIn = (WiringObject)fixture.FindGeometry(new Name("PowerIn"))!;
        Assert.Equal("PowerCon", powerIn.ConnectorType);
        Assert.Equal(WiringComponentType.Input, powerIn.ComponentType);
        Assert.Equal(3, powerIn.PinCount);
        Assert.Equal(PinOrientation.Left, powerIn.Orientation);
        Assert.Equal(2, powerIn.PinPatches.Count);
        Assert.Same(fixture.FindGeometry(new Name("Fuse")), powerIn.PinPatches[0].ToWiringObject);
        Assert.Same(fixture.FindGeometry(new Name("Psu")), powerIn.PinPatches[1].ToWiringObject);
        Assert.Equal(2, powerIn.PinPatches[1].FromPin);
        var fuse = (WiringObject)fixture.FindGeometry(new Name("Fuse"))!;
        Assert.Equal(FuseRating.B, fuse.FuseRating);
        Assert.Null(fuse.Orientation);
        Assert.Equal(0.95f, ((WiringObject)fixture.FindGeometry(new Name("Psu"))!).CosPhi);

        Assert.Equal(2, ((Inventory)fixture.FindGeometry(new Name("RainCover"))!).Count);

        var truss = (Structure)fixture.FindGeometry(new Name("Truss"))!;
        Assert.Same(fixture.Geometries[0], truss.LinkedGeometry);
        Assert.Equal(StructureType.CenterLineBased, truss.StructureType);
        Assert.Equal(CrossSectionType.Tube, truss.CrossSectionType);
        Assert.Equal("F34", truss.TrussCrossSection);

        var hoist = (Support)fixture.FindGeometry(new Name("Hoist"))!;
        Assert.Equal(SupportType.Rope, hoist.SupportType);
        Assert.Equal(new Vector3(0, 0, 1.5f), hoist.RopeOffset);
        Assert.Equal(6f, hoist.CapacityZZ);
        Assert.Equal(12f, hoist.ResistanceZZ);

        var pixel1 = (GeometryReference)fixture.FindGeometry(new Name("Pixel 1"))!;
        Assert.Same(fixture.Geometries[1], pixel1.ReferencedGeometry);
        Assert.Same(fixture.FindModel(new Name("Pixel")), pixel1.Model);
        Assert.Equal(2, pixel1.Breaks.Count);
        Assert.Equal(1, pixel1.Breaks[0].DmxBreak);
        Assert.Equal(1, pixel1.Breaks[0].DmxOffset.Absolute);
        Assert.Equal(2, pixel1.Breaks[1].DmxBreak);
        Assert.Equal(4, pixel1.Breaks[1].DmxOffset.Absolute);
        Assert.Empty(((GeometryReference)fixture.FindGeometry(new Name("Pixel 2"))!).Breaks);
    }

    [Fact]
    public void RoundTripIsEquivalent()
    {
        var document = Read(Full).Document;
        var written = GdtfWriter.ToXDocument(document).Root!.Element("FixtureType")!.Element("Geometries")!;
        XmlEquivalence.AssertEquivalent(XElement.Parse("<Geometries>" + Full + "</Geometries>"), written);

        var reread = GdtfReader.Read(GdtfWriter.WriteToString(document));
        Assert.Empty(reread.Diagnostics);
        Assert.Equal(GdtfWriter.WriteToString(document), GdtfWriter.WriteToString(reread.Document));
    }

    [Fact]
    public void AbsolutePositionComposesParents()
    {
        var head = Read(Full).Document.FixtureType.FindGeometry(new Name("Head"))!;
        var origin = Vector3.Transform(Vector3.Zero, head.AbsolutePosition);
        Assert.Equal(0.1f, origin.X, 5);
        Assert.Equal(0f, origin.Y, 5);
        Assert.Equal(0.5f - 0.2f - 0.3f, origin.Z, 5);
    }

    [Fact]
    public void AbsolutePositionAppliesParentRotation()
    {
        // Parent rotated 90° around Z, child translated 1 m along its own X: the child ends up on the parent's +Y.
        var parent = new Geometry(new Name("P")) { Position = Matrix4x4.CreateRotationZ(MathF.PI / 2) };
        var child = new Geometry(new Name("C")) { Position = Matrix4x4.CreateTranslation(1, 0, 0) };
        parent.Geometries.Add(child);
        var origin = Vector3.Transform(Vector3.Zero, child.AbsolutePosition);
        Assert.Equal(0f, origin.X, 5);
        Assert.Equal(1f, origin.Y, 5);
    }

    [Fact]
    public void ProblemsAreReported()
    {
        var result = Read("""
            <Geometry Name="Base" Model="NoSuchModel">
              <Geometry Name="Inner" />
              <GeometryReference Name="Ref1" Geometry="Inner" />
              <GeometryReference Name="Ref2" />
              <WiringObject Name="Wire"><PinPatch ToWiringObject="Inner" FromPin="1" ToPin="1" /></WiringObject>
              <Laser Name="L" ColorType="SingleWaveLength" />
              <Beam Name="B" BeamType="Laser" />
              <Hologram Name="H" />
              <Geometry Name="Inner" />
            </Geometry>
            """);
        var messages = result.Diagnostics.Select(d => $"{d.Severity}: {d.Message}").ToList();
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'NoSuchModel'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'Inner' could not be resolved in top-level Geometries"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("Required attribute 'Geometry'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'Inner' is a Geometry, not a WiringObject"));
        Assert.Contains(messages, m => m.StartsWith("Warning") && m.Contains("SingleWaveLength laser"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("'Laser' is not a valid value for 'BeamType'"));
        Assert.Contains(messages, m => m.StartsWith("Info") && m.Contains("'Hologram'"));
        Assert.Contains(messages, m => m.StartsWith("Error") && m.Contains("Duplicate name 'Inner' in Geometries"));
    }

    [Fact]
    public void GeometryCanOnlyHaveOneParent()
    {
        var fixture = new FixtureType(new Name("F"), Guid.NewGuid());
        var geometry = new Geometry(new Name("G"));
        fixture.Geometries.Add(geometry);
        Assert.True(geometry.IsTopLevel);
        Assert.Throws<InvalidOperationException>(() => new Geometry(new Name("Other")).Geometries.Add(geometry));
        fixture.Geometries.Remove(geometry);
        Assert.Null(geometry.Parent);
        Assert.False(geometry.IsTopLevel);
    }

    [Fact]
    public void FixtureBuiltInCodeValidates()
    {
        if (!File.Exists(TestData.Xsd)) Assert.Skip("gdtf.xsd missing; run scripts/fetch-testdata.sh.");
        var fixture = new FixtureType(new Name("Built"), Guid.NewGuid());
        var model = new Model(new Name("Body")) { Length = 0.3f, Width = 0.3f, Height = 0.4f, PrimitiveType = PrimitiveType.Cube };
        fixture.Models.Add(model);
        var emitter = new Emitter(new Name("White")) { Color = ColorCIE.White };
        fixture.PhysicalDescriptions.Emitters.Add(emitter);

        var body = new Geometry(new Name("Body")) { Model = model };
        var yoke = new Axis(new Name("Yoke")) { Position = Matrix4x4.CreateTranslation(0, 0, -0.1f) };
        var head = new Axis(new Name("Head"));
        head.Geometries.Add(new Beam(new Name("Beam")) { LampType = LampType.Led, BeamType = BeamType.Spot, EmitterSpectrum = emitter });
        head.Geometries.Add(new FilterGobo(new Name("Gobo")));
        yoke.Geometries.Add(head);
        body.Geometries.Add(yoke);
        body.Geometries.Add(new Display(new Name("Screen")) { Texture = "tex" });
        var laser = new Laser(new Name("Laser")) { ColorType = LaserColorType.SingleWaveLength, Color = 532, Emitter = emitter };
        laser.Protocols.Add("ILDA");
        body.Geometries.Add(laser);
        var fuse = new WiringObject(new Name("Fuse")) { ComponentType = WiringComponentType.Fuse, FuseRating = FuseRating.C, FuseCurrent = 10 };
        var input = new WiringObject(new Name("PowerIn")) { ComponentType = WiringComponentType.Input, ConnectorType = "PowerCon", PinCount = 3 };
        input.PinPatches.Add(new PinPatch { ToWiringObject = fuse, FromPin = 1, ToPin = 1 });
        body.Geometries.Add(input);
        body.Geometries.Add(fuse);
        body.Geometries.Add(new Magnet(new Name("Magnet")));
        fixture.Geometries.Add(body);

        var pixel = new Geometry(new Name("Pixel"));
        pixel.Geometries.Add(new Beam(new Name("PixelBeam")));
        fixture.Geometries.Add(pixel);
        var reference = new GeometryReference(new Name("Pixel 1")) { ReferencedGeometry = pixel };
        reference.Breaks.Add(new GeometryBreak { DmxOffset = new DmxAddress(5) });
        body.Geometries.Add(reference);

        var document = new GdtfDocument(fixture);
        var xml = GdtfWriter.ToXDocument(document);
        XsdValidation.AssertValid(xml);
        Assert.Equal("{1,0,0,0}{0,1,0,0}{0,0,1,-0.1}{0,0,0,1}",
            xml.Descendants("Axis").First().Attribute("Position")!.Value);

        var reread = GdtfReader.Read(GdtfWriter.WriteToString(document));
        Assert.Empty(reread.Diagnostics);
        Assert.Equal(GdtfWriter.WriteToString(document), GdtfWriter.WriteToString(reread.Document));
    }
}
