using System.Linq;
using System.Xml.Linq;
using GDTFSharp.Models;

namespace GDTFSharp.Serialization
{
    public static partial class GdtfWriter
    {
        private static XElement WriteGeometries(FixtureType fixtureType) =>
            new ElementWriter("Geometries").Children(fixtureType.Geometries.Select(WriteGeometry));

        private static XElement WriteGeometry(Geometry geometry)
        {
            var writer = new ElementWriter(geometry.ElementName)
                .Name(geometry.Name)
                .Name("Model", geometry.Model?.Name)
                .Matrix("Position", geometry.Position);

            switch (geometry)
            {
                case Beam beam:
                    writer
                        .Enum("LampType", beam.LampType)
                        .Float("PowerConsumption", beam.PowerConsumption)
                        .Float("LuminousFlux", beam.LuminousFlux)
                        .Float("ColorTemperature", beam.ColorTemperature)
                        .Float("BeamAngle", beam.BeamAngle)
                        .Float("FieldAngle", beam.FieldAngle)
                        .Float("ThrowRatio", beam.ThrowRatio)
                        .Float("RectangleRatio", beam.RectangleRatio)
                        .Float("BeamRadius", beam.BeamRadius)
                        .Enum("BeamType", beam.BeamType)
                        .Int("ColorRenderingIndex", beam.ColorRenderingIndex)
                        .Name("EmitterSpectrum", beam.EmitterSpectrum?.Name);
                    break;
                case Display display:
                    writer.String("Texture", display.Texture);
                    break;
                case GeometryReference reference:
                    // A missing referenced geometry (required) is not written; validation reports it.
                    writer.Name("Geometry", reference.ReferencedGeometry?.Name);
                    break;
                case Laser laser:
                    writer
                        .Enum("ColorType", laser.ColorType)
                        .Float("Color", laser.Color)
                        .Float("OutputStrength", laser.OutputStrength)
                        .Name("Emitter", laser.Emitter?.Name)
                        .Float("BeamDiameter", laser.BeamDiameter)
                        .Float("BeamDivergenceMin", laser.BeamDivergenceMin)
                        .Float("BeamDivergenceMax", laser.BeamDivergenceMax)
                        .Float("ScanAnglePan", laser.ScanAnglePan)
                        .Float("ScanAngleTilt", laser.ScanAngleTilt)
                        .Float("ScanSpeed", laser.ScanSpeed);
                    break;
                case WiringObject wiring:
                    writer
                        .String("ConnectorType", wiring.ConnectorType)
                        .Enum("ComponentType", wiring.ComponentType)
                        .String("SignalType", wiring.SignalType)
                        .Int("PinCount", wiring.PinCount)
                        .Float("ElectricalPayLoad", wiring.ElectricalPayLoad)
                        .Float("VoltageRangeMax", wiring.VoltageRangeMax)
                        .Float("VoltageRangeMin", wiring.VoltageRangeMin)
                        .Float("FrequencyRangeMax", wiring.FrequencyRangeMax)
                        .Float("FrequencyRangeMin", wiring.FrequencyRangeMin)
                        .Float("MaxPayLoad", wiring.MaxPayLoad)
                        .Float("Voltage", wiring.Voltage)
                        .Int("SignalLayer", wiring.SignalLayer)
                        .Float("CosPhi", wiring.CosPhi)
                        .Float("FuseCurrent", wiring.FuseCurrent)
                        .Enum("FuseRating", wiring.FuseRating)
                        .Enum("Orientation", wiring.Orientation)
                        .String("WireGroup", wiring.WireGroup);
                    break;
                case Inventory inventory:
                    writer.Int("Count", inventory.Count);
                    break;
                case Structure structure:
                    writer
                        .Name("LinkedGeometry", structure.LinkedGeometry?.Name)
                        .Enum("StructureType", structure.StructureType)
                        .Enum("CrossSectionType", structure.CrossSectionType)
                        .Float("CrossSectionHeight", structure.CrossSectionHeight)
                        .Float("CrossSectionWallThickness", structure.CrossSectionWallThickness)
                        .String("TrussCrossSection", structure.TrussCrossSection);
                    break;
                case Support support:
                    writer
                        .Enum("SupportType", support.SupportType)
                        .String("RopeCrossSection", support.RopeCrossSection)
                        .Vector3("RopeOffset", support.RopeOffset)
                        .Float("CapacityX", support.CapacityX)
                        .Float("CapacityY", support.CapacityY)
                        .Float("CapacityZ", support.CapacityZ)
                        .Float("CapacityXX", support.CapacityXX)
                        .Float("CapacityYY", support.CapacityYY)
                        .Float("CapacityZZ", support.CapacityZZ)
                        .Float("ResistanceX", support.ResistanceX)
                        .Float("ResistanceY", support.ResistanceY)
                        .Float("ResistanceZ", support.ResistanceZ)
                        .Float("ResistanceXX", support.ResistanceXX)
                        .Float("ResistanceYY", support.ResistanceYY)
                        .Float("ResistanceZZ", support.ResistanceZZ);
                    break;
            }

            // Child geometries first, then the type specific children (the order the XSD expects).
            writer.Children(geometry.Geometries.Select(WriteGeometry));
            switch (geometry)
            {
                case GeometryReference reference:
                    writer.Children(reference.Breaks.Select(b => (XElement)new ElementWriter("Break")
                        .DmxAddress("DMXOffset", b.DmxOffset)
                        .Int("DMXBreak", b.DmxBreak)));
                    break;
                case Laser laser:
                    writer.Children(laser.Protocols.Select(p => (XElement)new ElementWriter("Protocol").String("Name", p)));
                    break;
                case WiringObject wiring:
                    writer.Children(wiring.PinPatches.Select(p => (XElement)new ElementWriter("PinPatch")
                        .Name("ToWiringObject", p.ToWiringObject?.Name)
                        .Int("FromPin", p.FromPin)
                        .Int("ToPin", p.ToPin)));
                    break;
            }

            return writer;
        }
    }
}
