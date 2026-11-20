using System;
using System.Collections.Generic;
using GDTFSharp.Models;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    public static partial class GdtfReader
    {
        /// <summary>XML node names of all geometry types, mapped to a factory.</summary>
        private static readonly Dictionary<string, Func<Name, Geometry>> GeometryFactories =
            new Dictionary<string, Func<Name, Geometry>>(StringComparer.Ordinal)
            {
                ["Geometry"] = n => new Geometry(n),
                ["Axis"] = n => new Axis(n),
                ["FilterBeam"] = n => new FilterBeam(n),
                ["FilterColor"] = n => new FilterColor(n),
                ["FilterGobo"] = n => new FilterGobo(n),
                ["FilterShaper"] = n => new FilterShaper(n),
                ["Beam"] = n => new Beam(n),
                ["MediaServerLayer"] = n => new MediaServerLayer(n),
                ["MediaServerCamera"] = n => new MediaServerCamera(n),
                ["MediaServerMaster"] = n => new MediaServerMaster(n),
                ["Display"] = n => new Display(n),
                ["GeometryReference"] = n => new GeometryReference(n),
                ["Laser"] = n => new Laser(n),
                ["WiringObject"] = n => new WiringObject(n),
                ["Inventory"] = n => new Inventory(n),
                ["Structure"] = n => new Structure(n),
                ["Support"] = n => new Support(n),
                ["Magnet"] = n => new Magnet(n),
            };

        private static void ReadGeometries(ElementReader reader, FixtureType fixtureType)
        {
            var index = 0;
            foreach (var childReader in reader.ChildrenInOrder(GeometryFactories.Keys))
            {
                var geometry = ReadGeometry(childReader, ++index);
                reader.Context.Register(GdtfScopes.TopLevelGeometries, new NodePath(geometry.Name), geometry, childReader.Element);
                fixtureType.Geometries.Add(geometry);
            }

            reader.ReportUnknown();
        }

        private static Geometry ReadGeometry(ElementReader reader, int index)
        {
            var context = reader.Context;
            var elementName = reader.Element.Name.LocalName;
            var geometry = GeometryFactories[elementName](ReadName(reader, elementName, index));
            geometry.Position = reader.Matrix("Position");
            reader.Reference("Model", GdtfScopes.Models, m => geometry.Model = m);
            context.Register(GdtfScopes.Geometries, new NodePath(geometry.Name), geometry, reader.Element);

            switch (geometry)
            {
                case Beam beam:
                    beam.LampType = reader.Enum("LampType", LampType.Discharge);
                    beam.PowerConsumption = reader.Float("PowerConsumption", 1000f);
                    beam.LuminousFlux = reader.Float("LuminousFlux", 10000f);
                    beam.ColorTemperature = reader.Float("ColorTemperature", 6000f);
                    beam.BeamAngle = reader.Float("BeamAngle", 25f);
                    beam.FieldAngle = reader.Float("FieldAngle", 25f);
                    beam.ThrowRatio = reader.Float("ThrowRatio", 1f);
                    beam.RectangleRatio = reader.Float("RectangleRatio", 1.7777f);
                    beam.BeamRadius = reader.Float("BeamRadius", 0.05f);
                    beam.BeamType = reader.Enum("BeamType", BeamType.Wash);
                    beam.ColorRenderingIndex = reader.Value<byte>("ColorRenderingIndex", TryParseByte, 100);
                    reader.Reference("EmitterSpectrum", GdtfScopes.Emitters, e => beam.EmitterSpectrum = e);
                    break;

                case Display display:
                    display.Texture = reader.String("Texture");
                    break;

                case GeometryReference reference:
                    reader.Reference("Geometry", GdtfScopes.TopLevelGeometries, g => reference.ReferencedGeometry = g, required: true);
                    foreach (var breakReader in reader.Children("Break"))
                    {
                        reference.Breaks.Add(new GeometryBreak
                        {
                            DmxOffset = breakReader.DmxAddress("DMXOffset", new DmxAddress(1)),
                            DmxBreak = breakReader.Value<byte>("DMXBreak", TryParseByte, 1)
                        });
                        breakReader.ReportUnknown();
                    }

                    break;

                case Laser laser:
                    laser.ColorType = reader.Enum("ColorType", LaserColorType.Rgb);
                    laser.Color = OptionalFloat(reader, "Color");
                    laser.OutputStrength = OptionalFloat(reader, "OutputStrength");
                    reader.Reference("Emitter", GdtfScopes.Emitters, e => laser.Emitter = e);
                    laser.BeamDiameter = OptionalFloat(reader, "BeamDiameter");
                    laser.BeamDivergenceMin = OptionalFloat(reader, "BeamDivergenceMin");
                    laser.BeamDivergenceMax = OptionalFloat(reader, "BeamDivergenceMax");
                    laser.ScanAnglePan = OptionalFloat(reader, "ScanAnglePan");
                    laser.ScanAngleTilt = OptionalFloat(reader, "ScanAngleTilt");
                    laser.ScanSpeed = OptionalFloat(reader, "ScanSpeed");
                    if (laser.ColorType == LaserColorType.SingleWaveLength && laser.Color == null)
                        context.Warning("A SingleWaveLength laser needs a Color (wavelength).", reader.Element);
                    foreach (var protocolReader in reader.Children("Protocol"))
                    {
                        laser.Protocols.Add(protocolReader.String("Name", string.Empty));
                        protocolReader.ReportUnknown();
                    }

                    break;

                case WiringObject wiring:
                    wiring.ConnectorType = reader.String("ConnectorType");
                    wiring.ComponentType = reader.OptionalEnum<WiringComponentType>("ComponentType");
                    wiring.SignalType = reader.String("SignalType");
                    wiring.PinCount = OptionalInt(reader, "PinCount");
                    wiring.ElectricalPayLoad = OptionalFloat(reader, "ElectricalPayLoad");
                    wiring.VoltageRangeMax = OptionalFloat(reader, "VoltageRangeMax");
                    wiring.VoltageRangeMin = OptionalFloat(reader, "VoltageRangeMin");
                    wiring.FrequencyRangeMax = OptionalFloat(reader, "FrequencyRangeMax");
                    wiring.FrequencyRangeMin = OptionalFloat(reader, "FrequencyRangeMin");
                    wiring.MaxPayLoad = OptionalFloat(reader, "MaxPayLoad");
                    wiring.Voltage = OptionalFloat(reader, "Voltage");
                    wiring.SignalLayer = OptionalInt(reader, "SignalLayer");
                    wiring.CosPhi = OptionalFloat(reader, "CosPhi");
                    wiring.FuseCurrent = OptionalFloat(reader, "FuseCurrent");
                    wiring.FuseRating = reader.OptionalEnum<FuseRating>("FuseRating");
                    wiring.Orientation = reader.OptionalEnum<PinOrientation>("Orientation");
                    wiring.WireGroup = reader.String("WireGroup");
                    foreach (var patchReader in reader.Children("PinPatch"))
                    {
                        var patch = new PinPatch { FromPin = patchReader.Int("FromPin"), ToPin = patchReader.Int("ToPin") };
                        var target = patchReader.Element.Attribute("ToWiringObject");
                        patchReader.Reference("ToWiringObject", GdtfScopes.Geometries, g =>
                        {
                            if (g is WiringObject to) patch.ToWiringObject = to;
                            else context.Error($"'{g.Name}' is a {g.ElementName}, not a WiringObject.", target);
                        }, required: true);
                        patchReader.ReportUnknown();
                        wiring.PinPatches.Add(patch);
                    }

                    break;

                case Inventory inventory:
                    inventory.Count = OptionalInt(reader, "Count");
                    break;

                case Structure structure:
                    reader.Reference("LinkedGeometry", GdtfScopes.Geometries, g => structure.LinkedGeometry = g);
                    structure.StructureType = reader.OptionalEnum<StructureType>("StructureType");
                    structure.CrossSectionType = reader.OptionalEnum<CrossSectionType>("CrossSectionType");
                    structure.CrossSectionHeight = OptionalFloat(reader, "CrossSectionHeight");
                    structure.CrossSectionWallThickness = OptionalFloat(reader, "CrossSectionWallThickness");
                    structure.TrussCrossSection = reader.String("TrussCrossSection");
                    break;

                case Support support:
                    support.SupportType = reader.OptionalEnum<SupportType>("SupportType");
                    support.RopeCrossSection = reader.String("RopeCrossSection");
                    support.RopeOffset = reader.OptionalVector3("RopeOffset");
                    support.CapacityX = OptionalFloat(reader, "CapacityX");
                    support.CapacityY = OptionalFloat(reader, "CapacityY");
                    support.CapacityZ = OptionalFloat(reader, "CapacityZ");
                    support.CapacityXX = OptionalFloat(reader, "CapacityXX");
                    support.CapacityYY = OptionalFloat(reader, "CapacityYY");
                    support.CapacityZZ = OptionalFloat(reader, "CapacityZZ");
                    support.ResistanceX = OptionalFloat(reader, "ResistanceX");
                    support.ResistanceY = OptionalFloat(reader, "ResistanceY");
                    support.ResistanceZ = OptionalFloat(reader, "ResistanceZ");
                    support.ResistanceXX = OptionalFloat(reader, "ResistanceXX");
                    support.ResistanceYY = OptionalFloat(reader, "ResistanceYY");
                    support.ResistanceZZ = OptionalFloat(reader, "ResistanceZZ");
                    break;
            }

            var childIndex = 0;
            foreach (var childReader in reader.ChildrenInOrder(GeometryFactories.Keys))
                geometry.Geometries.Add(ReadGeometry(childReader, ++childIndex));

            reader.ReportUnknown();
            return geometry;
        }

        private static float? OptionalFloat(ElementReader reader, string name) => reader.Optional<float>(name, GdtfFormat.TryParseFloat);

        private static int? OptionalInt(ElementReader reader, string name) => reader.Optional<int>(name, GdtfFormat.TryParseInt);
    }
}
