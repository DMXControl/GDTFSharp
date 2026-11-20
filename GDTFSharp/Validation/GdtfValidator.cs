using System;
using System.Collections.Generic;
using System.Linq;
using GDTFSharp.Models;
using GDTFSharp.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Validation
{
    /// <summary>The result of validating a fixture type.</summary>
    public sealed class GdtfValidationResult
    {
        /// <summary>All errors and warnings.</summary>
        public IReadOnlyList<GdtfDiagnostic> Diagnostics { get; }

        /// <summary>Spec violations.</summary>
        public IEnumerable<GdtfDiagnostic> Errors => Diagnostics.Where(d => d.Severity == GdtfDiagnosticSeverity.Error);

        /// <summary>Discouraged or ambiguous constructs.</summary>
        public IEnumerable<GdtfDiagnostic> Warnings => Diagnostics.Where(d => d.Severity == GdtfDiagnosticSeverity.Warning);

        /// <summary>True when there are no errors (warnings are allowed).</summary>
        public bool IsValid => !Errors.Any();

        public GdtfValidationResult(IReadOnlyList<GdtfDiagnostic> diagnostics)
        {
            Diagnostics = diagnostics;
        }

        public override string ToString() => string.Join(Environment.NewLine, Diagnostics);
    }

    /// <summary>
    /// Checks a fixture type model against the rules of DIN SPEC 15800 (GDTF 1.2) that the model itself does not enforce:
    /// unique names, required and dangling references, references into other fixture types, DMX addressing and ranges.
    /// Errors are spec violations; warnings are discouraged or ambiguous constructs. Locations are model paths such as
    /// <c>FixtureType/DMXModes/DMXMode[Mode 1]/DMXChannel[Head_Dimmer]</c>.
    /// </summary>
    public static class GdtfValidator
    {
        public static GdtfValidationResult Validate(GdtfDocument document) => Validate(document.FixtureType);

        public static GdtfValidationResult Validate(FixtureType fixtureType)
        {
            var validator = new Run(fixtureType);
            validator.ValidateAll();
            return new GdtfValidationResult(validator.Diagnostics);
        }

        /// <summary>The highest DMX address of a break.</summary>
        public const int MaxDmxAddress = DmxAddress.AddressesPerUniverse;

        private sealed class Run
        {
            private readonly FixtureType _fixture;
            public readonly List<GdtfDiagnostic> Diagnostics = new List<GdtfDiagnostic>();

            // Everything that belongs to this fixture type, to detect references into other fixture types.
            private readonly HashSet<object> _owned = new HashSet<object>(ReferenceEqualityComparer.Instance);

            private const string Root = "FixtureType";

            public Run(FixtureType fixture)
            {
                _fixture = fixture;
                var definitions = fixture.AttributeDefinitions;
                var pd = fixture.PhysicalDescriptions;
                Own(definitions.ActivationGroups);
                Own(definitions.FeatureGroups.SelectMany(g => g.Features));
                Own(definitions.Attributes);
                Own(definitions.Attributes.SelectMany(a => a.SubPhysicalUnits));
                Own(fixture.Wheels);
                Own(pd.Emitters);
                Own(pd.Filters);
                if (pd.ColorSpace != null) _owned.Add(pd.ColorSpace);
                Own(pd.AdditionalColorSpaces);
                Own(pd.Gamuts);
                Own(pd.DmxProfiles);
                Own(pd.Connectors);
                Own(fixture.Models);
                Own(fixture.AllGeometries());
                Own(fixture.DmxModes);
            }

            private void Own(IEnumerable<object> items)
            {
                foreach (var item in items) _owned.Add(item);
            }

            private void Error(string location, string message) =>
                Diagnostics.Add(new GdtfDiagnostic(GdtfDiagnosticSeverity.Error, message, location));

            private void Warning(string location, string message) =>
                Diagnostics.Add(new GdtfDiagnostic(GdtfDiagnosticSeverity.Warning, message, location));

            private static string Segment(string element, string? name) => name == null ? element : $"{element}[{name}]";

            // ---- Shared checks ----------------------------------------------------------------------------------

            /// <summary>A required Name: not empty and only characters allowed by Annex C.</summary>
            private void CheckName(string location, Name name, bool required = true)
            {
                if (name.IsEmpty)
                {
                    if (required) Error(location, "Name is required.");
                    return;
                }

                if (!Name.IsValid(name.Value))
                    Warning(location, $"Name '{name.Value}' contains characters that are not allowed in a GDTF Name (Annex C).");
            }

            private void CheckUnique<T>(string location, IEnumerable<T> items, Func<T, string> key, string what)
            {
                foreach (var duplicate in items.GroupBy(key, StringComparer.Ordinal).Where(g => g.Count() > 1))
                    Error(location, $"{what} name '{duplicate.Key}' is used {duplicate.Count()} times; names must be unique.");
            }

            /// <summary>A reference: reports a missing required one and one to an object of another fixture type.</summary>
            private void CheckReference(string location, object? target, string attribute, bool required = false)
            {
                if (target == null)
                {
                    if (required) Error(location, $"'{attribute}' is required.");
                    return;
                }

                if (!_owned.Contains(target))
                    Error(location, $"'{attribute}' links to '{target}', which is not part of this fixture type.");
            }

            public void ValidateAll()
            {
                ValidateHeader();
                ValidateAttributeDefinitions();
                ValidateWheels();
                ValidatePhysicalDescriptions();
                ValidateModels();
                ValidateGeometries();
                ValidateDmxModes();
                ValidateProtocols();
            }

            // ---- Header -----------------------------------------------------------------------------------------

            private void ValidateHeader()
            {
                CheckName(Root, _fixture.Name);
                if (_fixture.FixtureTypeId == Guid.Empty) Error(Root, "FixtureTypeID must not be empty.");
                if (_fixture.RefFT == _fixture.FixtureTypeId) Warning(Root, "RefFT references the fixture type itself.");
                if (_fixture.UnparsedSections.Count > 0)
                    Warning(Root, $"Unknown sections are written unchanged: {string.Join(", ", _fixture.UnparsedSections.Select(s => s.Name.LocalName))}.");
            }

            // ---- Attribute definitions --------------------------------------------------------------------------

            private void ValidateAttributeDefinitions()
            {
                var definitions = _fixture.AttributeDefinitions;
                var location = Root + "/AttributeDefinitions";

                CheckUnique(location + "/ActivationGroups", definitions.ActivationGroups, g => g.Name.Value, "ActivationGroup");
                foreach (var group in definitions.ActivationGroups)
                    CheckName($"{location}/ActivationGroups/{Segment("ActivationGroup", group.Name.Value)}", group.Name);

                CheckUnique(location + "/FeatureGroups", definitions.FeatureGroups, g => g.Name.Value, "FeatureGroup");
                foreach (var group in definitions.FeatureGroups)
                {
                    var groupLocation = $"{location}/FeatureGroups/{Segment("FeatureGroup", group.Name.Value)}";
                    CheckName(groupLocation, group.Name);
                    CheckUnique(groupLocation, group.Features, f => f.Name.Value, "Feature");
                    foreach (var feature in group.Features) CheckName($"{groupLocation}/{Segment("Feature", feature.Name.Value)}", feature.Name);
                }

                CheckUnique(location + "/Attributes", definitions.Attributes, a => a.Name.Value, "Attribute");
                foreach (var attribute in definitions.Attributes)
                {
                    var attributeLocation = $"{location}/Attributes/{Segment("Attribute", attribute.Name.Value)}";
                    CheckName(attributeLocation, attribute.Name);
                    CheckReference(attributeLocation, attribute.Feature, "Feature", required: true);
                    CheckReference(attributeLocation, attribute.ActivationGroup, "ActivationGroup");
                    CheckReference(attributeLocation, attribute.MainAttribute, "MainAttribute");

                    var seen = new HashSet<GdtfAttribute>();
                    for (var main = attribute.MainAttribute; main != null; main = main.MainAttribute)
                    {
                        if (main == attribute || !seen.Add(main))
                        {
                            Error(attributeLocation, "MainAttribute forms a cycle.");
                            break;
                        }
                    }

                    foreach (var duplicate in attribute.SubPhysicalUnits.GroupBy(u => u.Type).Where(g => g.Count() > 1))
                        Warning(attributeLocation, $"SubPhysicalUnit type {duplicate.Key} appears {duplicate.Count()} times; references to it are ambiguous.");
                }
            }

            // ---- Wheels -----------------------------------------------------------------------------------------

            private void ValidateWheels()
            {
                var location = Root + "/Wheels";
                CheckUnique(location, _fixture.Wheels, w => w.Name.Value, "Wheel");
                foreach (var wheel in _fixture.Wheels)
                {
                    var wheelLocation = $"{location}/{Segment("Wheel", wheel.Name.Value)}";
                    CheckName(wheelLocation, wheel.Name);
                    CheckUnique(wheelLocation, wheel.Slots, s => s.Name.Value, "Slot");
                    foreach (var slot in wheel.Slots)
                    {
                        var slotLocation = $"{wheelLocation}/{Segment("Slot", slot.Name.Value)}";
                        CheckName(slotLocation, slot.Name);
                        CheckReference(slotLocation, slot.Filter, "Filter");
                        if (slot.Filter != null && slot.Color != null) Warning(slotLocation, "A slot with a Filter should not define a Color.");
                        if (slot.Facets.Count > 0 && slot.AnimationSystem != null)
                            Error(slotLocation, "A slot can have prism facets or an animation system, not both.");
                        if (slot.MediaFileName != null && (slot.MediaFileName.Contains('/') || slot.MediaFileName.Contains('\\')))
                            Warning(slotLocation, $"MediaFileName '{slot.MediaFileName}' contains a folder; it should be the file name only (the folder is always 'wheels').");
                        if (slot.AnimationSystem != null && slot.AnimationSystem.Radius < 0)
                            Error(slotLocation, "AnimationSystem Radius must not be negative.");
                    }
                }
            }

            // ---- Physical descriptions --------------------------------------------------------------------------

            private void ValidatePhysicalDescriptions()
            {
                var pd = _fixture.PhysicalDescriptions;
                var location = Root + "/PhysicalDescriptions";

                CheckUnique(location + "/Emitters", pd.Emitters, e => e.Name.Value, "Emitter");
                foreach (var emitter in pd.Emitters)
                {
                    var emitterLocation = $"{location}/Emitters/{Segment("Emitter", emitter.Name.Value)}";
                    CheckName(emitterLocation, emitter.Name);
                    if (emitter.Color == null && emitter.DominantWaveLength == null)
                        Error(emitterLocation, "An emitter needs a Color or a DominantWaveLength.");
                    ValidateMeasurements(emitterLocation, emitter.Measurements, additive: true);
                }

                CheckUnique(location + "/Filters", pd.Filters, f => f.Name.Value, "Filter");
                foreach (var filter in pd.Filters)
                {
                    var filterLocation = $"{location}/Filters/{Segment("Filter", filter.Name.Value)}";
                    CheckName(filterLocation, filter.Name);
                    ValidateMeasurements(filterLocation, filter.Measurements, additive: false);
                }

                var colorSpaces = (pd.ColorSpace == null ? Enumerable.Empty<ColorSpace>() : new[] { pd.ColorSpace }).Concat(pd.AdditionalColorSpaces).ToList();
                CheckUnique(location, colorSpaces, c => c.Name.Value, "ColorSpace");
                foreach (var colorSpace in colorSpaces)
                {
                    var colorSpaceLocation = $"{location}/{Segment("ColorSpace", colorSpace.Name.Value)}";
                    CheckName(colorSpaceLocation, colorSpace.Name);
                    if (colorSpace.Mode == ColorSpaceMode.Custom &&
                        (colorSpace.Red == null || colorSpace.Green == null || colorSpace.Blue == null || colorSpace.WhitePoint == null))
                        Warning(colorSpaceLocation, "A Custom color space should define Red, Green, Blue and WhitePoint.");
                }

                CheckUnique(location + "/Gamuts", pd.Gamuts, g => g.Name.Value, "Gamut");
                foreach (var gamut in pd.Gamuts)
                {
                    var gamutLocation = $"{location}/Gamuts/{Segment("Gamut", gamut.Name.Value)}";
                    CheckName(gamutLocation, gamut.Name);
                    if (gamut.Points.Count < 3) Warning(gamutLocation, "A gamut polygon needs at least 3 points.");
                }

                CheckUnique(location + "/DMXProfiles", pd.DmxProfiles, p => p.Name.Value, "DMXProfile");
                foreach (var profile in pd.DmxProfiles)
                    CheckName($"{location}/DMXProfiles/{Segment("DMXProfile", profile.Name.Value)}", profile.Name);

                for (var i = 0; i < pd.Cris.Count; i++)
                {
                    var groupLocation = $"{location}/CRIs/CRIGroup[{i + 1}]";
                    foreach (var cri in pd.Cris[i].Cris.Where(c => c.ColorSample < 1 || c.ColorSample > 99))
                        Error(groupLocation, $"Color sample {cri.ColorSample} is outside CES01 to CES99.");
                    foreach (var duplicate in pd.Cris[i].Cris.GroupBy(c => c.ColorSample).Where(g => g.Count() > 1))
                        Error(groupLocation, $"Color sample CES{duplicate.Key:00} appears {duplicate.Count()} times.");
                }

                CheckUnique(location + "/Connectors", pd.Connectors, c => c.Name.Value, "Connector");
                foreach (var connector in pd.Connectors)
                {
                    var connectorLocation = $"{location}/Connectors/{Segment("Connector", connector.Name.Value)}";
                    CheckName(connectorLocation, connector.Name);
                    CheckName(connectorLocation, connector.Type);
                }

                var properties = pd.Properties;
                if (properties.OperatingTemperature is { } temperature && temperature.Low > temperature.High)
                    Error(location + "/Properties/OperatingTemperature", "Low is higher than High.");
                if (properties.Weight < 0) Error(location + "/Properties/Weight", "Weight must not be negative.");
                if (properties.LegHeight < 0) Error(location + "/Properties/LegHeight", "LegHeight must not be negative.");
                foreach (var power in properties.PowerConsumptions)
                    CheckReference(location + "/Properties/PowerConsumption", power.Connector, "Connector", required: true);
            }

            private void ValidateMeasurements(string location, IList<Measurement> measurements, bool additive)
            {
                for (var i = 0; i < measurements.Count; i++)
                {
                    var measurement = measurements[i];
                    var measurementLocation = $"{location}/Measurement[{i + 1}]";
                    if (additive && (measurement.Physical <= 0 || measurement.Physical > 100))
                        Error(measurementLocation, "Physical of an emitter measurement must be greater than 0 and at most 100.");
                    if (!additive && (measurement.Physical < 0 || measurement.Physical > 100))
                        Error(measurementLocation, "Physical of a filter measurement must be between 0 and 100.");
                    if (additive && measurement.LuminousIntensity == null) Error(measurementLocation, "An emitter measurement needs a LuminousIntensity.");
                    if (!additive && measurement.Transmission == null) Error(measurementLocation, "A filter measurement needs a Transmission.");
                    if (i > 0 && measurement.Physical <= measurements[i - 1].Physical)
                        Warning(measurementLocation, "Measurements should be in ascending order of Physical.");
                    foreach (var duplicate in measurement.Points.GroupBy(p => p.WaveLength).Where(g => g.Count() > 1))
                        Error(measurementLocation, $"WaveLength {duplicate.Key} appears {duplicate.Count()} times.");
                }
            }

            // ---- Models -----------------------------------------------------------------------------------------

            private void ValidateModels()
            {
                var location = Root + "/Models";
                CheckUnique(location, _fixture.Models, m => m.Name.Value, "Model");
                foreach (var model in _fixture.Models)
                {
                    var modelLocation = $"{location}/{Segment("Model", model.Name.Value)}";
                    CheckName(modelLocation, model.Name);
                    if (model.Length < 0 || model.Width < 0 || model.Height < 0) Error(modelLocation, "Dimensions must not be negative.");
                }
            }

            // ---- Geometries -------------------------------------------------------------------------------------

            private static string GeometryLocation(Geometry geometry)
            {
                var segments = new List<string>();
                for (var current = geometry; current != null; current = current.ParentGeometry)
                    segments.Add(Segment(current.ElementName, current.Name.Value));
                segments.Reverse();
                return Root + "/Geometries/" + string.Join("/", segments);
            }

            private void ValidateGeometries()
            {
                var all = _fixture.AllGeometries().ToList();
                CheckUnique(Root + "/Geometries", all, g => g.Name.Value, "Geometry");

                foreach (var geometry in all)
                {
                    var location = GeometryLocation(geometry);
                    CheckName(location, geometry.Name);
                    CheckReference(location, geometry.Model, "Model");

                    switch (geometry)
                    {
                        case Beam beam:
                            CheckReference(location, beam.EmitterSpectrum, "EmitterSpectrum");
                            if (beam.BeamAngle < 0 || beam.FieldAngle < 0) Error(location, "BeamAngle and FieldAngle must not be negative.");
                            break;
                        case Laser laser:
                            CheckReference(location, laser.Emitter, "Emitter");
                            if (laser.ColorType == LaserColorType.SingleWaveLength && laser.Color == null)
                                Error(location, "A SingleWaveLength laser needs a Color (wavelength).");
                            break;
                        case WiringObject wiring:
                            for (var i = 0; i < wiring.PinPatches.Count; i++)
                            {
                                var patch = wiring.PinPatches[i];
                                CheckReference($"{location}/PinPatch[{i + 1}]", patch.ToWiringObject, "ToWiringObject", required: true);
                                if (patch.ToWiringObject == wiring) Warning($"{location}/PinPatch[{i + 1}]", "The pin patch connects the wiring object to itself.");
                            }

                            break;
                        case Structure structure:
                            CheckReference(location, structure.LinkedGeometry, "LinkedGeometry");
                            break;
                        case GeometryReference reference:
                            ValidateGeometryReference(location, reference);
                            break;
                    }
                }
            }

            private void ValidateGeometryReference(string location, GeometryReference reference)
            {
                var target = reference.ReferencedGeometry;
                CheckReference(location, target, "Geometry", required: true);
                if (target == null) return;
                if (!target.IsTopLevel) Error(location, $"'{target.Name}' is not a top-level geometry; only top-level geometries can be referenced.");
                if (target == reference.Root) Error(location, $"The reference is inside '{target.Name}', the geometry it references.");
                foreach (var duplicate in reference.Breaks.GroupBy(b => b.DmxBreak).Where(g => g.Count() > 1))
                    Error(location, $"DMX break {duplicate.Key} has {duplicate.Count()} Break entries.");
                foreach (var geometryBreak in reference.Breaks.Where(b => b.DmxOffset.Absolute < 1))
                    Error(location, $"DMXOffset {geometryBreak.DmxOffset.Absolute} of break {geometryBreak.DmxBreak} must be at least 1.");
            }

            // ---- DMX modes --------------------------------------------------------------------------------------

            private void ValidateDmxModes()
            {
                var location = Root + "/DMXModes";
                CheckUnique(location, _fixture.DmxModes, m => m.Name.Value, "DMXMode");
                foreach (var mode in _fixture.DmxModes) ValidateDmxMode($"{location}/{Segment("DMXMode", mode.Name.Value)}", mode);
            }

            private void ValidateDmxMode(string location, DmxMode mode)
            {
                CheckName(location, mode.Name);
                CheckReference(location, mode.Geometry, "Geometry", required: true);
                if (mode.Geometry != null && !mode.Geometry.IsTopLevel)
                    Error(location, $"'{mode.Geometry.Name}' is not a top-level geometry.");

                var modeChannels = new HashSet<DmxChannel>(mode.Channels);
                var modeFunctions = new HashSet<ChannelFunction>(mode.Channels.SelectMany(c => c.ChannelFunctions));

                // Geometries the channels of this mode may control: the mode's tree and the trees it references.
                var tree = mode.Geometry == null ? new List<Geometry>() : new[] { mode.Geometry }.Concat(mode.Geometry.Descendants()).ToList();
                var references = tree.OfType<GeometryReference>().Where(r => r.ReferencedGeometry != null).ToList();
                var reachableRoots = new HashSet<Geometry>(references.Select(r => r.ReferencedGeometry!));
                if (mode.Geometry != null) reachableRoots.Add(mode.Geometry);

                CheckUnique(location, mode.Channels, c => c.Name, "DMXChannel");
                foreach (var duplicate in mode.Channels
                             .SelectMany(c => c.LogicalChannels.Select(l => (Channel: c, Logical: l)))
                             .Where(x => x.Channel.Geometry != null && x.Logical.Attribute != null)
                             .GroupBy(x => (x.Channel.Geometry, x.Logical.Attribute))
                             .Where(g => g.Count() > 1))
                {
                    Error(location, $"Attribute '{duplicate.Key.Attribute!.Name}' is used for geometry '{duplicate.Key.Geometry!.Name}' by {duplicate.Count()} logical channels; the combination must be unique in a DMX mode.");
                }

                foreach (var channel in mode.Channels)
                    ValidateDmxChannel($"{location}/{Segment("DMXChannel", channel.Name)}", channel, modeChannels, modeFunctions, reachableRoots, tree);

                ValidateAddresses(location, mode);
                ValidateReferenceBreaks(location, mode, references);

                CheckUnique(location + "/Relations", mode.Relations, r => r.Name.Value, "Relation");
                foreach (var relation in mode.Relations)
                {
                    var relationLocation = $"{location}/Relations/{Segment("Relation", relation.Name.Value)}";
                    CheckName(relationLocation, relation.Name);
                    CheckInMode(relationLocation, relation.Master, "Master", modeChannels, required: true);
                    CheckInMode(relationLocation, relation.Follower, "Follower", modeFunctions, required: true);
                }

                CheckUnique(location + "/FTMacros", mode.Macros, m => m.Name.Value, "FTMacro");
                foreach (var macro in mode.Macros)
                {
                    var macroLocation = $"{location}/FTMacros/{Segment("FTMacro", macro.Name.Value)}";
                    CheckName(macroLocation, macro.Name);
                    CheckInMode(macroLocation, macro.ChannelFunction, "ChannelFunction", modeFunctions);
                    foreach (var step in macro.DmxSteps ?? new List<MacroDmxStep>())
                    {
                        if (step.Duration < 0) Error(macroLocation, "A MacroDMXStep Duration must not be negative.");
                        foreach (var value in step.Values) CheckInMode(macroLocation, value.DmxChannel, "DMXChannel", modeChannels, required: true);
                    }
                }
            }

            private void CheckInMode<T>(string location, T? target, string attribute, HashSet<T> inMode, bool required = false) where T : class
            {
                if (target == null)
                {
                    if (required) Error(location, $"'{attribute}' is required.");
                    return;
                }

                if (!inMode.Contains(target)) Error(location, $"'{attribute}' links to '{target}', which is not part of this DMX mode.");
            }

            private void ValidateDmxChannel(string location, DmxChannel channel, HashSet<DmxChannel> modeChannels,
                HashSet<ChannelFunction> modeFunctions, HashSet<Geometry> reachableRoots, List<Geometry> tree)
            {
                CheckReference(location, channel.Geometry, "Geometry", required: true);
                if (channel.Geometry != null && _owned.Contains(channel.Geometry) && !reachableRoots.Contains(channel.Geometry.Root))
                    Error(location, $"Geometry '{channel.Geometry.Name}' is neither in the mode's geometry tree nor in a geometry it references.");
                if (channel.DmxBreak.IsOverwrite && channel.Geometry != null && tree.Contains(channel.Geometry))
                    Warning(location, "DMXBreak Overwrite only has an effect for geometries that are referenced by a GeometryReference.");
                if (!channel.DmxBreak.IsOverwrite && channel.DmxBreak.Number < 1)
                    Error(location, $"DMXBreak {channel.DmxBreak.Number} must be at least 1.");
                if (channel.LogicalChannels.Count == 0) Error(location, "A DMX channel needs at least one logical channel.");
                if (channel.Offsets.Any(o => o < 1)) Error(location, "Offsets must be at least 1.");
                if (channel.Offsets.Distinct().Count() != channel.Offsets.Count) Error(location, "Offsets must be different addresses.");
                if (channel.Offsets.Count > 4) Warning(location, $"{channel.Offsets.Count} offsets: DMX channels have at most 4 bytes.");
                if (channel.InitialFunction != null && !channel.ChannelFunctions.Contains(channel.InitialFunction))
                    Error(location, $"InitialFunction '{channel.InitialFunction}' is not a channel function of this DMX channel.");
                if (channel.Highlight is { } highlight && highlight.ToResolution(channel.ByteCount) > channel.MaxDmxValue)
                    Error(location, "Highlight is above the channel's maximum DMX value.");

                foreach (var logical in channel.LogicalChannels)
                {
                    var logicalLocation = $"{location}/{Segment("LogicalChannel", logical.Name)}";
                    CheckReference(logicalLocation, logical.Attribute, "Attribute", required: true);
                    if (logical.MibFade < 0 || logical.DmxChangeTimeLimit < 0)
                        Error(logicalLocation, "MibFade and DMXChangeTimeLimit must not be negative.");
                    CheckUnique(logicalLocation, logical.ChannelFunctions, f => f.EffectiveName, "ChannelFunction");
                    foreach (var function in logical.ChannelFunctions)
                        ValidateChannelFunction($"{logicalLocation}/{Segment("ChannelFunction", function.EffectiveName)}", function, channel, modeChannels, modeFunctions);
                }
            }

            private void ValidateChannelFunction(string location, ChannelFunction function, DmxChannel channel,
                HashSet<DmxChannel> modeChannels, HashSet<ChannelFunction> modeFunctions)
            {
                if (function.Name is { } name) CheckName(location, name, required: false);
                CheckReference(location, function.Attribute, "Attribute");
                CheckReference(location, function.Wheel, "Wheel");
                CheckReference(location, function.Emitter, "Emitter");
                CheckReference(location, function.Filter, "Filter");
                CheckReference(location, function.ColorSpace, "ColorSpace");
                CheckReference(location, function.Gamut, "Gamut");
                CheckReference(location, function.DmxProfile, "DMXProfile");

                var bytes = channel.ByteCount;
                var from = function.DmxFrom.ToResolution(bytes);
                var to = function.DmxTo;
                if (from > channel.MaxDmxValue) Error(location, $"DMXFrom {function.DmxFrom} is above the channel's maximum DMX value.");
                var defaultValue = function.Default.ToResolution(bytes);
                if (defaultValue < from || defaultValue > to)
                    Warning(location, $"Default {function.Default} is outside the function's DMX range {from}..{to}.");

                if (function.ModeMaster != null)
                {
                    var inMode = function.ModeMaster switch
                    {
                        DmxChannel master => modeChannels.Contains(master),
                        ChannelFunction master => modeFunctions.Contains(master),
                        _ => false
                    };
                    if (!inMode) Error(location, $"ModeMaster '{function.ModeMaster}' is not part of this DMX mode.");
                    if (function.ModeMaster == channel || function.ModeMaster == function)
                        Error(location, "ModeMaster links to the function's own DMX channel.");
                    if (function.ModeFrom.ToResolution(4) > function.ModeTo.ToResolution(4))
                        Error(location, $"ModeFrom {function.ModeFrom} is above ModeTo {function.ModeTo}.");
                }

                ulong previous = 0;
                for (var i = 0; i < function.ChannelSets.Count; i++)
                {
                    var set = function.ChannelSets[i];
                    var setLocation = $"{location}/{Segment("ChannelSet", set.Name.IsEmpty ? (i + 1).ToString() : set.Name.Value)}";
                    if (!set.Name.IsEmpty) CheckName(setLocation, set.Name, required: false);
                    var setFrom = set.DmxFrom.ToResolution(bytes);
                    if (setFrom < from || setFrom > to)
                        Error(setLocation, $"DMXFrom {set.DmxFrom} is outside the channel function's DMX range {from}..{to}.");
                    else if (i > 0 && setFrom <= previous)
                        Error(setLocation, $"DMXFrom {set.DmxFrom} does not come after the previous channel set.");
                    previous = setFrom;

                    // 0 means "no slot" (libMVRgdtf's default); anything else has to be a slot of the wheel.
                    if (set.WheelSlotIndex is { } index && index != 0 && function.Wheel != null && (index < 1 || index > function.Wheel.Slots.Count))
                        Warning(setLocation, $"WheelSlotIndex {index} is not a slot of wheel '{function.Wheel.Name}' (1 to {function.Wheel.Slots.Count}).");
                }

                foreach (var sub in function.SubChannelSets)
                {
                    if (sub.SubPhysicalUnit != null && (function.Attribute == null || !function.Attribute.SubPhysicalUnits.Contains(sub.SubPhysicalUnit)))
                        Warning(location, $"SubChannelSet '{sub.Name}' links to a sub physical unit of another attribute.");
                    CheckReference(location, sub.DmxProfile, "DMXProfile");
                }
            }

            /// <summary>Patched addresses: no address used twice per break, all within 1..512.</summary>
            private void ValidateAddresses(string location, DmxMode mode)
            {
                var instances = mode.GetChannelInstances();
                foreach (var instance in instances.Where(i => i.Addresses.Any(a => a > MaxDmxAddress)))
                    Error(location, $"{Describe(instance)} uses an address above {MaxDmxAddress}.");

                var used = instances
                    .SelectMany(i => i.Addresses.Select(a => (Instance: i, i.DmxBreak, Address: a)))
                    .GroupBy(x => (x.DmxBreak, x.Address))
                    .Where(g => g.Count() > 1);
                foreach (var clash in used)
                    Error(location, $"Address {clash.Key.Address} of break {clash.Key.DmxBreak} is used by {string.Join(" and ", clash.Select(c => Describe(c.Instance)))}.");
            }

            private static string Describe(DmxChannelInstance instance) =>
                instance.Reference == null ? $"'{instance.Channel.Name}'" : $"'{instance.Channel.Name}' of '{instance.Reference.Name}'";

            /// <summary>Every GeometryReference needs a Break for each DMX break its geometry's channels use.</summary>
            private void ValidateReferenceBreaks(string location, DmxMode mode, List<GeometryReference> references)
            {
                foreach (var reference in references)
                {
                    var channels = mode.Channels.Where(c => c.Geometry != null && c.Geometry.Root == reference.ReferencedGeometry && !c.IsVirtual).ToList();
                    var needed = channels.Where(c => !c.DmxBreak.IsOverwrite).Select(c => c.DmxBreak.Number).Distinct();
                    foreach (var missing in needed.Where(b => reference.Breaks.All(rb => rb.DmxBreak != b)))
                        Error(GeometryLocation(reference), $"No Break for DMX break {missing}, which DMX mode '{mode.Name}' uses for this geometry.");
                    if (channels.Any(c => c.DmxBreak.IsOverwrite) && reference.Breaks.Count == 0)
                        Error(GeometryLocation(reference), $"No Break for the Overwrite channels of DMX mode '{mode.Name}'.");
                }
            }

            // ---- Protocols --------------------------------------------------------------------------------------

            private void ValidateProtocols()
            {
                var rdm = _fixture.Protocols.Rdm;
                if (rdm == null) return;
                var location = Root + "/Protocols/FTRDM";
                if (rdm.ManufacturerId > 0xFFFF) Error(location, "ManufacturerID is a 16 bit ESTA ID.");
                if (rdm.DeviceModelId > 0xFFFF) Error(location, "DeviceModelID is a 16 bit RDM value.");
                foreach (var version in rdm.SoftwareVersions)
                {
                    if (version.Value > 0xFFFFFFFF) Error(location, "SoftwareVersionID is a 32 bit RDM value.");
                    foreach (var personality in version.DmxPersonalities)
                    {
                        CheckReference(location, personality.DmxMode, "DMXMode", required: true);
                        if (personality.Value > 0xFF) Error(location, "DMXPersonality is an 8 bit RDM value.");
                    }
                }
            }
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
