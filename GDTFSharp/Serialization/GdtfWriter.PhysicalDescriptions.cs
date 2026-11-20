using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    public static partial class GdtfWriter
    {
        private static XElement WritePhysicalDescriptions(PhysicalDescriptions descriptions)
        {
            var writer = new ElementWriter("PhysicalDescriptions")
                .Child(new ElementWriter("Emitters").Children(descriptions.Emitters.Select(emitter => (XElement)new ElementWriter("Emitter")
                    .Name(emitter.Name)
                    .Color("Color", emitter.Color)
                    .Float("DominantWaveLength", emitter.DominantWaveLength)
                    .String("DiodePart", emitter.DiodePart)
                    .Children(WriteMeasurements(emitter.Measurements)))))
                .Child(new ElementWriter("Filters").Children(descriptions.Filters.Select(filter => (XElement)new ElementWriter("Filter")
                    .Name(filter.Name)
                    .Color("Color", filter.Color)
                    .Children(WriteMeasurements(filter.Measurements)))));

            if (descriptions.ColorSpace != null) writer.Child(WriteColorSpace(descriptions.ColorSpace));

            writer
                .Child(new ElementWriter("AdditionalColorSpaces").Children(descriptions.AdditionalColorSpaces.Select(WriteColorSpace)))
                .Child(new ElementWriter("Gamuts").Children(descriptions.Gamuts.Select(gamut => (XElement)new ElementWriter("Gamut")
                    .Name(gamut.Name)
                    .String("Points", GdtfFormat.FormatColorArray(gamut.Points)))))
                .Child(new ElementWriter("DMXProfiles").Children(descriptions.DmxProfiles.Select(profile => (XElement)new ElementWriter("DMXProfile")
                    .Name(profile.Name)
                    .Children(profile.Points.Select(point => (XElement)new ElementWriter("Point")
                        .Float("DMXPercentage", point.DmxPercentage)
                        .Float("CFC0", point.Cfc0)
                        .Float("CFC1", point.Cfc1)
                        .Float("CFC2", point.Cfc2)
                        .Float("CFC3", point.Cfc3))))))
                .Child(new ElementWriter("CRIs").Children(descriptions.Cris.Select(group => (XElement)new ElementWriter("CRIGroup")
                    .Float("ColorTemperature", group.ColorTemperature)
                    .Children(group.Cris.Select(cri => (XElement)new ElementWriter("CRI")
                        .String("CES", "CES" + cri.ColorSample.ToString("00", CultureInfo.InvariantCulture))
                        .Int("ColorRenderingIndex", cri.ColorRenderingIndex))))));

            // Obsolete since 1.2: only written when the fixture still has connectors.
            if (descriptions.Connectors.Count > 0)
            {
                writer.Child(new ElementWriter("Connectors").Children(descriptions.Connectors.Select(connector => (XElement)new ElementWriter("Connector")
                    .Name(connector.Name)
                    .Name("Type", connector.Type)
                    .Int("DMXBreak", connector.DmxBreak)
                    .Int("Gender", connector.Gender)
                    .Float("Length", connector.Length))));
            }

            return writer.Child(WriteProperties(descriptions.Properties));
        }

        private static IEnumerable<XElement> WriteMeasurements(IEnumerable<Measurement> measurements) =>
            measurements.Select(measurement => (XElement)new ElementWriter("Measurement")
                .Float("Physical", measurement.Physical)
                .Float("LuminousIntensity", measurement.LuminousIntensity)
                .Float("Transmission", measurement.Transmission)
                .Enum("InterpolationTo", measurement.InterpolationTo)
                .Children(measurement.Points.Select(point => (XElement)new ElementWriter("MeasurementPoint")
                    .Float("WaveLength", point.WaveLength)
                    .Float("Energy", point.Energy))));

        private static XElement WriteColorSpace(ColorSpace colorSpace) =>
            new ElementWriter("ColorSpace")
                .Name(colorSpace.Name)
                .Enum("Mode", colorSpace.Mode)
                .Color("Red", colorSpace.Red)
                .Color("Green", colorSpace.Green)
                .Color("Blue", colorSpace.Blue)
                .Color("WhitePoint", colorSpace.WhitePoint);

        private static XElement WriteProperties(DeviceProperties properties)
        {
            var writer = new ElementWriter("Properties");
            if (properties.OperatingTemperature is { } temperature)
                writer.Child(new ElementWriter("OperatingTemperature").Float("Low", temperature.Low).Float("High", temperature.High));
            if (properties.Weight != null) writer.Child(new ElementWriter("Weight").Float("Value", properties.Weight));
            writer.Children(properties.PowerConsumptions.Select(power => (XElement)new ElementWriter("PowerConsumption")
                .Float("Value", power.Value)
                .Float("PowerFactor", power.PowerFactor)
                .Name("Connector", power.Connector?.Name)
                .Float("VoltageLow", power.VoltageLow)
                .Float("VoltageHigh", power.VoltageHigh)
                .Float("FrequencyLow", power.FrequencyLow)
                .Float("FrequencyHigh", power.FrequencyHigh)));
            if (properties.LegHeight != null) writer.Child(new ElementWriter("LegHeight").Float("Value", properties.LegHeight));
            return writer;
        }
    }
}
