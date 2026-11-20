using System.Globalization;
using GDTFSharp.Models;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    public static partial class GdtfReader
    {
        private static void ReadPhysicalDescriptions(ElementReader reader, PhysicalDescriptions descriptions)
        {
            var context = reader.Context;

            var emitters = reader.Child("Emitters");
            if (emitters != null)
            {
                var index = 0;
                foreach (var emitterReader in emitters.Children("Emitter"))
                {
                    var emitter = new Emitter(ReadName(emitterReader, "Emitter", ++index))
                    {
                        Color = emitterReader.OptionalColor("Color"),
                        DominantWaveLength = emitterReader.Optional<float>("DominantWaveLength", GdtfFormat.TryParseFloat),
                        DiodePart = emitterReader.String("DiodePart")
                    };
                    if (emitter.Color == null && emitter.DominantWaveLength == null)
                        context.Warning("An emitter without Color needs a DominantWaveLength.", emitterReader.Element);
                    ReadMeasurements(emitterReader, emitter.Measurements);
                    emitterReader.ReportUnknown();
                    context.Register(GdtfScopes.Emitters, new NodePath(emitter.Name), emitter, emitterReader.Element);
                    descriptions.Emitters.Add(emitter);
                }

                emitters.ReportUnknown();
            }

            var filters = reader.Child("Filters");
            if (filters != null)
            {
                var index = 0;
                foreach (var filterReader in filters.Children("Filter"))
                {
                    var filter = new Filter(ReadName(filterReader, "Filter", ++index)) { Color = filterReader.OptionalColor("Color") };
                    ReadMeasurements(filterReader, filter.Measurements);
                    filterReader.ReportUnknown();
                    context.Register(GdtfScopes.Filters, new NodePath(filter.Name), filter, filterReader.Element);
                    descriptions.Filters.Add(filter);
                }

                filters.ReportUnknown();
            }

            var colorSpace = reader.Child("ColorSpace");
            if (colorSpace != null) descriptions.ColorSpace = ReadColorSpace(colorSpace);

            var additionalColorSpaces = reader.Child("AdditionalColorSpaces");
            if (additionalColorSpaces != null)
            {
                foreach (var colorSpaceReader in additionalColorSpaces.Children("ColorSpace"))
                    descriptions.AdditionalColorSpaces.Add(ReadColorSpace(colorSpaceReader));
                additionalColorSpaces.ReportUnknown();
            }

            var gamuts = reader.Child("Gamuts");
            if (gamuts != null)
            {
                var index = 0;
                foreach (var gamutReader in gamuts.Children("Gamut"))
                {
                    var gamut = new Gamut(ReadName(gamutReader, "Gamut", ++index));
                    foreach (var point in gamutReader.Value<ColorCIE[]>("Points", GdtfFormat.TryParseColorArray, new ColorCIE[0]))
                        gamut.Points.Add(point);
                    gamutReader.ReportUnknown();
                    context.Register(GdtfScopes.Gamuts, new NodePath(gamut.Name), gamut, gamutReader.Element);
                    descriptions.Gamuts.Add(gamut);
                }

                gamuts.ReportUnknown();
            }

            var profiles = reader.Child("DMXProfiles");
            if (profiles != null)
            {
                var index = 0;
                foreach (var profileReader in profiles.Children("DMXProfile"))
                {
                    var profile = new DmxProfile(ReadName(profileReader, "DMXProfile", ++index));
                    foreach (var pointReader in profileReader.Children("Point"))
                    {
                        profile.Points.Add(new DmxProfilePoint
                        {
                            DmxPercentage = pointReader.Float("DMXPercentage"),
                            Cfc0 = pointReader.Float("CFC0"),
                            Cfc1 = pointReader.Float("CFC1"),
                            Cfc2 = pointReader.Float("CFC2"),
                            Cfc3 = pointReader.Float("CFC3")
                        });
                        pointReader.ReportUnknown();
                    }

                    profileReader.ReportUnknown();
                    context.Register(GdtfScopes.DmxProfiles, new NodePath(profile.Name), profile, profileReader.Element);
                    descriptions.DmxProfiles.Add(profile);
                }

                profiles.ReportUnknown();
            }

            var cris = reader.Child("CRIs");
            if (cris != null)
            {
                foreach (var groupReader in cris.Children("CRIGroup"))
                {
                    var group = new CriGroup { ColorTemperature = groupReader.Float("ColorTemperature", 6000f) };
                    foreach (var criReader in groupReader.Children("CRI"))
                    {
                        group.Cris.Add(new Cri
                        {
                            ColorSample = criReader.Value<int>("CES", TryParseColorSample, 1),
                            ColorRenderingIndex = criReader.Value<byte>("ColorRenderingIndex", TryParseByte, 100)
                        });
                        criReader.ReportUnknown();
                    }

                    groupReader.ReportUnknown();
                    descriptions.Cris.Add(group);
                }

                cris.ReportUnknown();
            }

            var connectors = reader.Child("Connectors");
            if (connectors != null)
            {
                var index = 0;
                foreach (var connectorReader in connectors.Children("Connector"))
                {
                    var connector = new Connector(ReadName(connectorReader, "Connector", ++index), connectorReader.Name("Type", required: true))
                    {
                        DmxBreak = connectorReader.Optional<uint>("DMXBreak", GdtfFormat.TryParseUInt),
                        Gender = connectorReader.Int("Gender"),
                        Length = connectorReader.Float("Length")
                    };
                    connectorReader.ReportUnknown();
                    context.Register(GdtfScopes.Connectors, new NodePath(connector.Name), connector, connectorReader.Element);
                    descriptions.Connectors.Add(connector);
                }

                connectors.ReportUnknown();
            }

            var properties = reader.Child("Properties");
            if (properties != null) ReadProperties(properties, descriptions.Properties);

            reader.ReportUnknown();
        }

        private static void ReadMeasurements(ElementReader reader, System.Collections.Generic.IList<Measurement> measurements)
        {
            foreach (var measurementReader in reader.Children("Measurement"))
            {
                var measurement = new Measurement
                {
                    Physical = measurementReader.Float("Physical", required: true),
                    LuminousIntensity = measurementReader.Optional<float>("LuminousIntensity", GdtfFormat.TryParseFloat),
                    Transmission = measurementReader.Optional<float>("Transmission", GdtfFormat.TryParseFloat),
                    InterpolationTo = measurementReader.Enum("InterpolationTo", InterpolationTo.Linear)
                };
                foreach (var pointReader in measurementReader.Children("MeasurementPoint"))
                {
                    measurement.Points.Add(new MeasurementPoint(
                        pointReader.Float("WaveLength", required: true),
                        pointReader.Float("Energy", required: true)));
                    pointReader.ReportUnknown();
                }

                measurementReader.ReportUnknown();
                measurements.Add(measurement);
            }
        }

        private static ColorSpace ReadColorSpace(ElementReader reader)
        {
            var colorSpace = new ColorSpace
            {
                Name = reader.Name("Name", new Name(ColorSpace.DefaultName)),
                Mode = reader.Enum("Mode", ColorSpaceMode.SRgb),
                Red = reader.OptionalColor("Red"),
                Green = reader.OptionalColor("Green"),
                Blue = reader.OptionalColor("Blue"),
                WhitePoint = reader.OptionalColor("WhitePoint")
            };
            if (colorSpace.Name.IsEmpty) colorSpace.Name = new Name(ColorSpace.DefaultName);
            reader.ReportUnknown();
            reader.Context.Register(GdtfScopes.ColorSpaces, new NodePath(colorSpace.Name), colorSpace, reader.Element);
            return colorSpace;
        }

        private static void ReadProperties(ElementReader reader, DeviceProperties properties)
        {
            var temperature = reader.Child("OperatingTemperature");
            if (temperature != null)
            {
                properties.OperatingTemperature = new OperatingTemperature
                {
                    Low = temperature.Float("Low", 0f),
                    High = temperature.Float("High", 40f)
                };
                temperature.ReportUnknown();
            }

            var weight = reader.Child("Weight");
            if (weight != null)
            {
                properties.Weight = weight.Float("Value");
                weight.ReportUnknown();
            }

            var legHeight = reader.Child("LegHeight");
            if (legHeight != null)
            {
                properties.LegHeight = legHeight.Float("Value");
                legHeight.ReportUnknown();
            }

            foreach (var powerReader in reader.Children("PowerConsumption"))
            {
                var power = new PowerConsumption
                {
                    Value = powerReader.Optional<float>("Value", GdtfFormat.TryParseFloat),
                    PowerFactor = powerReader.Optional<float>("PowerFactor", GdtfFormat.TryParseFloat),
                    VoltageLow = powerReader.Optional<float>("VoltageLow", GdtfFormat.TryParseFloat),
                    VoltageHigh = powerReader.Optional<float>("VoltageHigh", GdtfFormat.TryParseFloat),
                    FrequencyLow = powerReader.Optional<float>("FrequencyLow", GdtfFormat.TryParseFloat),
                    FrequencyHigh = powerReader.Optional<float>("FrequencyHigh", GdtfFormat.TryParseFloat)
                };
                powerReader.Reference("Connector", GdtfScopes.Connectors, c => power.Connector = c, required: true);
                powerReader.ReportUnknown();
                properties.PowerConsumptions.Add(power);
            }

            reader.ReportUnknown();
        }

        /// <summary>Parses a TM-30 color evaluation sample, <c>CES01</c> to <c>CES99</c>.</summary>
        internal static bool TryParseColorSample(string? value, out int sample)
        {
            sample = 0;
            var text = value?.Trim();
            return text != null && text.Length == 5 && text.StartsWith("CES", System.StringComparison.Ordinal) &&
                   int.TryParse(text.Substring(3), NumberStyles.None, CultureInfo.InvariantCulture, out sample) &&
                   sample >= 1 && sample <= 99;
        }

        private static bool TryParseByte(string? value, out byte result) =>
            byte.TryParse(value?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out result);
    }
}
