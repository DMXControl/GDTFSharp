using GDTFSharp.Models;
using GDTFSharp.Serialization;
using GDTFSharp.Types;
using GDTFSharp.Validation;

namespace GDTFSharp.Tests.Validation;

public class GdtfValidatorTests
{
    /// <summary>A small but complete fixture: a body with a dimmer and two referenced RGB pixels.</summary>
    private sealed class Fixture
    {
        public readonly FixtureType Type = new(new Name("Valid"), Guid.Parse("308EA87D-7164-42DE-8106-A6D273F57A51"));
        public readonly Feature Feature = new(new Name("Dimmer"));
        public readonly GdtfAttribute Dimmer;
        public readonly GdtfAttribute Red;
        public readonly GdtfAttribute Gobo;
        public readonly Wheel Wheel = new(new Name("Gobo"));
        public readonly Emitter Emitter = new(new Name("Red")) { Color = new ColorCIE(0.7f, 0.3f, 100f) };
        public readonly Geometry Body = new(new Name("Body"));
        public readonly Geometry Pixel = new(new Name("Pixel"));
        public readonly GeometryReference Pixel1 = new(new Name("Pixel1"));
        public readonly GeometryReference Pixel2 = new(new Name("Pixel2"));
        public readonly DmxMode Mode = new(new Name("Standard"));
        public readonly DmxChannel DimmerChannel;
        public readonly ChannelFunction DimmerFunction;
        public readonly DmxChannel GoboChannel;
        public readonly ChannelFunction GoboFunction;
        public readonly DmxChannel RedChannel;
        public readonly ChannelFunction RedFunction;

        public Fixture()
        {
            var group = new FeatureGroup(new Name("Dimmer"));
            group.Features.Add(Feature);
            Type.AttributeDefinitions.FeatureGroups.Add(group);
            Dimmer = new GdtfAttribute(new Name("Dimmer"), Feature);
            Red = new GdtfAttribute(new Name("ColorAdd_R"), Feature);
            Gobo = new GdtfAttribute(new Name("Gobo1"), Feature);
            Type.AttributeDefinitions.Attributes.Add(Dimmer);
            Type.AttributeDefinitions.Attributes.Add(Red);
            Type.AttributeDefinitions.Attributes.Add(Gobo);

            Wheel.Slots.Add(new WheelSlot(new Name("Open")));
            Wheel.Slots.Add(new WheelSlot(new Name("Dots")));
            Type.Wheels.Add(Wheel);
            Type.PhysicalDescriptions.Emitters.Add(Emitter);

            Type.Geometries.Add(Body);
            Type.Geometries.Add(Pixel);
            Pixel1.ReferencedGeometry = Pixel;
            Pixel1.Breaks.Add(new GeometryBreak { DmxOffset = new DmxAddress(3) });
            Pixel2.ReferencedGeometry = Pixel;
            Pixel2.Breaks.Add(new GeometryBreak { DmxOffset = new DmxAddress(4) });
            Body.Geometries.Add(Pixel1);
            Body.Geometries.Add(Pixel2);

            Mode.Geometry = Body;
            (DimmerChannel, DimmerFunction) = AddChannel(Body, Dimmer, 1);
            DimmerFunction.ChannelSets.Add(new ChannelSet(new Name("Closed")));
            DimmerFunction.ChannelSets.Add(new ChannelSet(new Name("Open")) { DmxFrom = DmxValue.Parse("255/1") });
            (GoboChannel, GoboFunction) = AddChannel(Body, Gobo, 2);
            GoboFunction.Wheel = Wheel;
            GoboFunction.ChannelSets.Add(new ChannelSet(new Name("Open")) { WheelSlotIndex = 1 });
            GoboFunction.ChannelSets.Add(new ChannelSet(new Name("Dots")) { DmxFrom = DmxValue.Parse("128/1"), WheelSlotIndex = 2 });
            (RedChannel, RedFunction) = AddChannel(Pixel, Red, 1);
            RedChannel.DmxBreak = DmxBreak.Overwrite;
            RedFunction.Emitter = Emitter;
            Mode.Relations.Add(new Relation(new Name("Virtual"), RelationType.Multiply) { Master = DimmerChannel, Follower = RedFunction });
            Type.DmxModes.Add(Mode);
        }

        public (DmxChannel, ChannelFunction) AddChannel(Geometry geometry, GdtfAttribute attribute, params int[] offsets)
        {
            var channel = new DmxChannel(geometry);
            foreach (var offset in offsets) channel.Offsets.Add(offset);
            var logical = new LogicalChannel(attribute);
            var function = new ChannelFunction(attribute);
            logical.ChannelFunctions.Add(function);
            channel.LogicalChannels.Add(logical);
            Mode.Channels.Add(channel);
            return (channel, function);
        }

        public GdtfValidationResult Validate() => GdtfValidator.Validate(Type);
    }

    private static void AssertError(GdtfValidationResult result, string message, string? location = null)
    {
        Assert.False(result.IsValid, "Expected an error containing: " + message);
        Assert.True(result.Errors.Any(e => e.Message.Contains(message) && (location == null || e.Location == location)),
            $"No error containing '{message}'{(location != null ? $" at {location}" : "")}. Got:\n{result}");
    }

    private static void AssertWarning(GdtfValidationResult result, string message)
    {
        Assert.True(result.Warnings.Any(w => w.Message.Contains(message)), $"No warning containing '{message}'. Got:\n{result}");
    }

    [Fact]
    public void ValidFixtureHasNoDiagnostics()
    {
        var fixture = new Fixture();
        var result = fixture.Validate();
        Assert.True(result.IsValid, result.ToString());
        Assert.Empty(result.Diagnostics);

        // The footprint the checks are based on: dimmer 1, gobo 2, the pixels' red at 3 and 4.
        Assert.Equal(4, fixture.Mode.GetFootprint()[1]);
    }

    [Fact]
    public void ValidFixtureIsValidAfterRoundTrip()
    {
        var document = new GdtfDocument(new Fixture().Type);
        var reread = GdtfReader.Read(GdtfWriter.WriteToString(document));
        Assert.Empty(reread.Diagnostics);
        Assert.Empty(GdtfValidator.Validate(reread.Document).Diagnostics);
    }

    // ---- Header and names ---------------------------------------------------------------------------------------

    [Fact]
    public void Header()
    {
        var fixture = new Fixture();
        fixture.Type.FixtureTypeId = Guid.Empty;
        fixture.Type.Name = default;
        var result = fixture.Validate();
        AssertError(result, "FixtureTypeID must not be empty");
        AssertError(result, "Name is required", "FixtureType");
    }

    [Fact]
    public void InvalidNameCharactersAreWarnings()
    {
        var fixture = new Fixture();
        fixture.Wheel.Name = Name.Unchecked("Gobo.Wheel");
        var result = fixture.Validate();
        Assert.True(result.IsValid);
        AssertWarning(result, "'Gobo.Wheel' contains characters that are not allowed");
    }

    [Fact]
    public void DuplicateNames()
    {
        var fixture = new Fixture();
        fixture.Type.AttributeDefinitions.Attributes.Add(new GdtfAttribute(new Name("Dimmer"), fixture.Feature));
        fixture.Wheel.Slots.Add(new WheelSlot(new Name("Open")));
        fixture.Type.Models.Add(new Model(new Name("M")));
        fixture.Type.Models.Add(new Model(new Name("M")));
        fixture.Pixel.Geometries.Add(new Geometry(new Name("Body")));
        fixture.Type.PhysicalDescriptions.ColorSpace = new ColorSpace { Name = new Name("CS") };
        fixture.Type.PhysicalDescriptions.AdditionalColorSpaces.Add(new ColorSpace { Name = new Name("CS") });
        var result = fixture.Validate();
        AssertError(result, "Attribute name 'Dimmer' is used 2 times");
        AssertError(result, "Slot name 'Open' is used 2 times", "FixtureType/Wheels/Wheel[Gobo]");
        AssertError(result, "Model name 'M' is used 2 times");
        AssertError(result, "Geometry name 'Body' is used 2 times");
        AssertError(result, "ColorSpace name 'CS' is used 2 times");
    }

    // ---- References -----------------------------------------------------------------------------------------------

    [Fact]
    public void MissingAndForeignReferences()
    {
        var fixture = new Fixture();
        var other = new Fixture();
        fixture.Dimmer.Feature = null;
        fixture.Red.MainAttribute = other.Red;
        fixture.Wheel.Slots[1].Filter = new Filter(new Name("Loose"));
        fixture.GoboFunction.Wheel = other.Wheel;
        var result = fixture.Validate();
        AssertError(result, "'Feature' is required", "FixtureType/AttributeDefinitions/Attributes/Attribute[Dimmer]");
        AssertError(result, "'MainAttribute' links to 'ColorAdd_R', which is not part of this fixture type");
        AssertError(result, "'Filter' links to 'Loose'");
        AssertError(result, "'Wheel' links to 'Gobo', which is not part of this fixture type");
    }

    [Fact]
    public void MainAttributeCycle()
    {
        var fixture = new Fixture();
        fixture.Dimmer.MainAttribute = fixture.Red;
        fixture.Red.MainAttribute = fixture.Dimmer;
        AssertError(fixture.Validate(), "MainAttribute forms a cycle");
    }

    // ---- Physical descriptions ------------------------------------------------------------------------------------

    [Fact]
    public void PhysicalDescriptionRules()
    {
        var fixture = new Fixture();
        var pd = fixture.Type.PhysicalDescriptions;
        fixture.Emitter.Color = null;
        var filter = new Filter(new Name("Cyan"));
        filter.Measurements.Add(new Measurement { Physical = 50, Transmission = 0.5f });
        filter.Measurements.Add(new Measurement { Physical = 20 });
        pd.Filters.Add(filter);
        var cri = new CriGroup();
        cri.Cris.Add(new Cri { ColorSample = 100 });
        cri.Cris.Add(new Cri { ColorSample = 5 });
        cri.Cris.Add(new Cri { ColorSample = 5 });
        pd.Cris.Add(cri);
        pd.Properties.OperatingTemperature = new OperatingTemperature { Low = 50, High = 40 };
        pd.Properties.PowerConsumptions.Add(new PowerConsumption());
        pd.AdditionalColorSpaces.Add(new ColorSpace { Name = new Name("Wide"), Mode = ColorSpaceMode.Custom });

        var result = fixture.Validate();
        AssertError(result, "needs a Color or a DominantWaveLength");
        AssertError(result, "A filter measurement needs a Transmission");
        AssertWarning(result, "ascending order of Physical");
        AssertError(result, "Color sample 100 is outside CES01 to CES99");
        AssertError(result, "CES05 appears 2 times");
        AssertError(result, "Low is higher than High");
        AssertError(result, "'Connector' is required");
        AssertWarning(result, "A Custom color space should define");
    }

    [Fact]
    public void EmitterMeasurementRange()
    {
        var fixture = new Fixture();
        fixture.Emitter.Measurements.Add(new Measurement { Physical = 0, LuminousIntensity = 10 });
        AssertError(fixture.Validate(), "greater than 0 and at most 100");
    }

    [Fact]
    public void WheelSlotRules()
    {
        var fixture = new Fixture();
        var slot = fixture.Wheel.Slots[1];
        slot.Facets.Add(new PrismFacet(RotationMatrix.Identity));
        slot.AnimationSystem = new AnimationSystem();
        slot.MediaFileName = "Vendor/dots";
        var filter = new Filter(new Name("F"));
        fixture.Type.PhysicalDescriptions.Filters.Add(filter);
        fixture.Wheel.Slots[0].Filter = filter;
        fixture.Wheel.Slots[0].Color = ColorCIE.White;
        var result = fixture.Validate();
        AssertError(result, "prism facets or an animation system, not both");
        AssertWarning(result, "contains a folder");
        AssertWarning(result, "should not define a Color");
    }

    // ---- Geometries -----------------------------------------------------------------------------------------------

    [Fact]
    public void GeometryReferenceRules()
    {
        var fixture = new Fixture();
        var inner = new Geometry(new Name("Inner"));
        fixture.Pixel.Geometries.Add(inner);
        fixture.Pixel1.ReferencedGeometry = inner;
        fixture.Pixel2.Breaks.Add(new GeometryBreak { DmxOffset = new DmxAddress(9) });
        var selfReference = new GeometryReference(new Name("Loop")) { ReferencedGeometry = fixture.Body };
        fixture.Body.Geometries.Add(selfReference);
        fixture.Body.Geometries.Add(new GeometryReference(new Name("Nothing")));

        var result = fixture.Validate();
        AssertError(result, "'Inner' is not a top-level geometry");
        AssertError(result, "DMX break 1 has 2 Break entries");
        AssertError(result, "inside 'Body', the geometry it references");
        AssertError(result, "'Geometry' is required", "FixtureType/Geometries/Geometry[Body]/GeometryReference[Nothing]");
    }

    [Fact]
    public void ReferenceNeedsABreakForEveryBreakItsChannelsUse()
    {
        var fixture = new Fixture();
        var (_, _) = fixture.AddChannel(fixture.Pixel, fixture.Dimmer, 1);
        fixture.Mode.Channels.Last().DmxBreak = new DmxBreak(2);
        AssertError(fixture.Validate(), "No Break for DMX break 2");

        var empty = new Fixture();
        empty.Pixel1.Breaks.Clear();
        AssertError(empty.Validate(), "No Break for the Overwrite channels");
    }

    [Fact]
    public void WiringAndLaserRules()
    {
        var fixture = new Fixture();
        var wire = new WiringObject(new Name("Wire"));
        wire.PinPatches.Add(new PinPatch());
        wire.PinPatches.Add(new PinPatch { ToWiringObject = wire });
        fixture.Body.Geometries.Add(wire);
        fixture.Body.Geometries.Add(new Laser(new Name("Laser")) { ColorType = LaserColorType.SingleWaveLength });
        var result = fixture.Validate();
        AssertError(result, "'ToWiringObject' is required");
        AssertWarning(result, "connects the wiring object to itself");
        AssertError(result, "SingleWaveLength laser needs a Color");
    }

    // ---- DMX modes ------------------------------------------------------------------------------------------------

    [Fact]
    public void ModeGeometryMustBeTopLevel()
    {
        var fixture = new Fixture();
        fixture.Mode.Geometry = fixture.Pixel1;
        AssertError(fixture.Validate(), "'Pixel1' is not a top-level geometry");

        var missing = new Fixture();
        missing.Mode.Geometry = null;
        AssertError(missing.Validate(), "'Geometry' is required", "FixtureType/DMXModes/DMXMode[Standard]");
    }

    [Fact]
    public void ChannelGeometryMustBeReachableFromTheMode()
    {
        var fixture = new Fixture();
        var loose = new Geometry(new Name("Loose"));
        fixture.Type.Geometries.Add(loose);
        fixture.AddChannel(loose, fixture.Gobo, 5);
        AssertError(fixture.Validate(), "'Loose' is neither in the mode's geometry tree nor in a geometry it references");
    }

    [Fact]
    public void OverlappingAddresses()
    {
        var fixture = new Fixture();
        fixture.GoboChannel.Offsets[0] = 3;
        AssertError(fixture.Validate(), "Address 3 of break 1 is used by 'Body_Gobo1' and 'Pixel_ColorAdd_R' of 'Pixel1'");
    }

    [Fact]
    public void AddressAbove512()
    {
        var fixture = new Fixture();
        fixture.Pixel2.Breaks[0].DmxOffset = new DmxAddress(512);
        fixture.RedChannel.Offsets.Add(2);
        AssertError(fixture.Validate(), "uses an address above 512");
    }

    [Fact]
    public void ChannelRules()
    {
        var fixture = new Fixture();
        fixture.DimmerChannel.Offsets.Add(1);
        fixture.GoboChannel.InitialFunction = fixture.DimmerFunction;
        fixture.DimmerChannel.Highlight = DmxValue.Parse("300/2");
        var empty = new DmxChannel(fixture.Body);
        empty.Offsets.Add(9);
        fixture.Mode.Channels.Add(empty);
        var result = fixture.Validate();
        AssertError(result, "Offsets must be different addresses");
        AssertError(result, "InitialFunction 'Dimmer 1' is not a channel function of this DMX channel");
        AssertError(result, "needs at least one logical channel");
    }

    [Fact]
    public void AttributeAndGeometryMustBeUniquePerMode()
    {
        var fixture = new Fixture();
        fixture.AddChannel(fixture.Body, fixture.Dimmer, 7);
        var result = fixture.Validate();
        AssertError(result, "Attribute 'Dimmer' is used for geometry 'Body' by 2 logical channels");
        AssertError(result, "DMXChannel name 'Body_Dimmer' is used 2 times");
    }

    [Fact]
    public void ChannelFunctionRanges()
    {
        var fixture = new Fixture();
        fixture.DimmerFunction.Default = DmxValue.Parse("10/1");
        var second = new ChannelFunction(fixture.Dimmer) { DmxFrom = DmxValue.Parse("5/1") };
        fixture.DimmerChannel.LogicalChannels[0].ChannelFunctions.Add(second);
        fixture.GoboFunction.ChannelSets.Add(new ChannelSet(new Name("Back")) { DmxFrom = DmxValue.Parse("100/1") });
        fixture.GoboFunction.ChannelSets[1].WheelSlotIndex = 7;
        var result = fixture.Validate();
        // The dimmer now ends at 4, so its default 10 and its "Open" set at 255 are outside.
        AssertWarning(result, "Default 10/1 is outside the function's DMX range 0..4");
        AssertError(result, "DMXFrom 255/1 is outside the channel function's DMX range 0..4");
        AssertError(result, "DMXFrom 100/1 does not come after the previous channel set");
        AssertWarning(result, "WheelSlotIndex 7 is not a slot of wheel 'Gobo' (1 to 2)");
        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("5..255"));
    }

    [Fact]
    public void DuplicateChannelFunctionNames()
    {
        var fixture = new Fixture();
        fixture.DimmerFunction.Name = new Name("Same");
        fixture.DimmerChannel.LogicalChannels[0].ChannelFunctions.Add(new ChannelFunction(fixture.Dimmer) { Name = new Name("Same"), DmxFrom = DmxValue.Parse("5/1") });
        AssertError(fixture.Validate(), "ChannelFunction name 'Same' is used 2 times");
    }

    [Fact]
    public void WheelSlotIndexZeroMeansNoSlot()
    {
        var fixture = new Fixture();
        fixture.GoboFunction.ChannelSets[0].WheelSlotIndex = 0;
        Assert.Empty(fixture.Validate().Diagnostics);
    }

    [Fact]
    public void ModeMasterRules()
    {
        var fixture = new Fixture();
        var other = new Fixture();
        fixture.GoboFunction.ModeMaster = fixture.GoboChannel;
        fixture.RedFunction.ModeMaster = other.DimmerChannel;
        fixture.DimmerFunction.ModeMaster = fixture.GoboFunction;
        fixture.DimmerFunction.ModeFrom = DmxValue.Parse("200/1");
        fixture.DimmerFunction.ModeTo = DmxValue.Parse("100/1");
        var result = fixture.Validate();
        AssertError(result, "ModeMaster links to the function's own DMX channel");
        AssertError(result, "ModeMaster 'Body_Dimmer' is not part of this DMX mode");
        AssertError(result, "ModeFrom 200/1 is above ModeTo 100/1");
    }

    [Fact]
    public void RelationsAndMacrosStayInTheirMode()
    {
        var fixture = new Fixture();
        var other = new Fixture();
        fixture.Mode.Relations.Add(new Relation(new Name("Virtual"), RelationType.Override) { Master = other.DimmerChannel });
        var macro = new FTMacro(new Name("M")) { ChannelFunction = other.DimmerFunction, DmxSteps = new List<MacroDmxStep>() };
        var step = new MacroDmxStep { Duration = -1 };
        step.Values.Add(new MacroDmxValue(DmxValue.Parse("1/1")));
        macro.DmxSteps.Add(step);
        fixture.Mode.Macros.Add(macro);
        var result = fixture.Validate();
        AssertError(result, "Relation name 'Virtual' is used 2 times");
        AssertError(result, "'Master' links to 'Body_Dimmer', which is not part of this DMX mode");
        AssertError(result, "'Follower' is required");
        AssertError(result, "'ChannelFunction' links to 'Dimmer 1', which is not part of this DMX mode");
        AssertError(result, "Duration must not be negative");
        AssertError(result, "'DMXChannel' is required");
    }

    [Fact]
    public void RdmRules()
    {
        var fixture = new Fixture();
        var rdm = new RdmProtocol { ManufacturerId = 0x12345 };
        var version = new RdmSoftwareVersion();
        version.DmxPersonalities.Add(new DmxPersonality { Value = 300 });
        rdm.SoftwareVersions.Add(version);
        fixture.Type.Protocols.Rdm = rdm;
        var result = fixture.Validate();
        AssertError(result, "ManufacturerID is a 16 bit ESTA ID");
        AssertError(result, "DMXPersonality is an 8 bit RDM value");
        AssertError(result, "'DMXMode' is required");
    }

    // ---- Archive ------------------------------------------------------------------------------------------------

    [Fact]
    public void ArchiveChecksResources()
    {
        var fixture = new Fixture();
        fixture.Type.Thumbnail = "thumb";
        fixture.Wheel.Slots[1].MediaFileName = "dots";
        fixture.Type.Models.Add(new Model(new Name("Body")) { File = "body" });
        var archive = new GdtfArchive(new GdtfDocument(fixture.Type));

        var result = archive.Validate();
        Assert.True(result.IsValid);
        AssertWarning(result, "Thumbnail 'thumb'");
        AssertWarning(result, "MediaFileName 'dots'");
        AssertWarning(result, "File 'body'");

        archive.Resources["thumb.svg"] = new byte[] { 1 };
        archive.Resources["wheels/dots.png"] = new byte[] { 1 };
        archive.Resources["models/gltf/body.glb"] = new byte[] { 1 };
        Assert.Empty(archive.Validate().Diagnostics);
    }

    [Fact]
    public void WheelMediaWithFolderIsFoundByFileName()
    {
        var archive = new GdtfArchive(new GdtfDocument(new FixtureType(new Name("X"), Guid.NewGuid())));
        archive.Resources["wheels/00591401.png"] = new byte[] { 7 };
        Assert.Equal(new byte[] { 7 }, archive.GetWheelMedia("Clay Paky/00591401"));
    }

    // ---- Real-world files ---------------------------------------------------------------------------------------

    /// <summary>The errors the validator finds in the sample files; all are genuine defects of those files.</summary>
    private static readonly Dictionary<string, string[]> KnownErrors = new()
    {
        // Dimmer channel sets "Closed" 0/2, "" 0/2: two sets start at the same value.
        ["allWorking.gdtf"] = new[] { "does not come after the previous channel set" },
        ["Robe lighting s.r.o.@BMFL Spot.gdtf"] = new[]
        {
            "ChannelFunction name 'NoFeature' is used 2 times",
            // Frost1 starts at 1/1 but its "Open" set at 0/1; Tilt sets 32896/2 then 32769/2.
            "is outside the channel function's DMX range",
            "does not come after the previous channel set"
        },
        ["virtual_channel.gdtf"] = Array.Empty<string>()
    };

    [Theory]
    [MemberData(nameof(TestData.ExternalFixtureNames), MemberType = typeof(TestData))]
    public void RealFixturesHaveOnlyKnownErrors(string fileName)
    {
        var archive = GdtfArchive.Load(TestData.RequireExternal(fileName)).Archive;
        var result = archive.Validate();
        var expected = KnownErrors.TryGetValue(fileName, out var known) ? known : Array.Empty<string>();
        foreach (var error in result.Errors)
            Assert.True(expected.Any(e => error.Message.Contains(e)), $"Unexpected error in {fileName}: {error}");
        foreach (var message in expected)
            Assert.True(result.Errors.Any(e => e.Message.Contains(message)), $"Expected error not reported for {fileName}: {message}");
    }
}
