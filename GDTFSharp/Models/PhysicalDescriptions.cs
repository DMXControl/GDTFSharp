using System.Collections.Generic;
using System.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>The physical constitution of the device (XML node <c>PhysicalDescriptions</c>).</summary>
    public sealed class PhysicalDescriptions
    {
        /// <summary>Additive light sources, such as LEDs and lamps with permanently fitted filters.</summary>
        public IList<Emitter> Emitters { get; } = new List<Emitter>();

        /// <summary>Subtractive filters, such as mixing flags and color wheel media.</summary>
        public IList<Filter> Filters { get; } = new List<Filter>();

        /// <summary>The default color space for indirect color mixing, or <c>null</c> when the file has none.</summary>
        public ColorSpace? ColorSpace { get; set; }

        /// <summary>Further color spaces. Names must be unique together with the default <see cref="ColorSpace"/>.</summary>
        public IList<ColorSpace> AdditionalColorSpaces { get; } = new List<ColorSpace>();

        public IList<Gamut> Gamuts { get; } = new List<Gamut>();

        /// <summary>Nonlinear correlations between DMX input and physical output of a channel.</summary>
        public IList<DmxProfile> DmxProfiles { get; } = new List<DmxProfile>();

        /// <summary>Color rendering (IES TM-30-15 Rf) per color temperature.</summary>
        public IList<CriGroup> Cris { get; } = new List<CriGroup>();

        /// <summary>
        /// Physical connectors. Obsolete since GDTF 1.2, where connectors are WiringObject geometries;
        /// kept for older files. Written only when not empty.
        /// </summary>
        public IList<Connector> Connectors { get; } = new List<Connector>();

        public DeviceProperties Properties { get; } = new DeviceProperties();

        public Emitter? FindEmitter(Name name) => Emitters.FirstOrDefault(e => e.Name == name);

        public Filter? FindFilter(Name name) => Filters.FirstOrDefault(f => f.Name == name);

        /// <summary>Finds the default or an additional color space by name.</summary>
        public ColorSpace? FindColorSpace(Name name) =>
            ColorSpace?.Name == name ? ColorSpace : AdditionalColorSpaces.FirstOrDefault(c => c.Name == name);

        public Gamut? FindGamut(Name name) => Gamuts.FirstOrDefault(g => g.Name == name);

        public DmxProfile? FindDmxProfile(Name name) => DmxProfiles.FirstOrDefault(p => p.Name == name);

        public Connector? FindConnector(Name name) => Connectors.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>An additive light source (XML node <c>Emitter</c>).</summary>
    public sealed class Emitter
    {
        public Name Name { get; set; }

        /// <summary>
        /// Approximate absolute color point; omitted for non-visible emitters (e.g. UV). Luminance Y is relative to the
        /// luminous flux of the related beam.
        /// </summary>
        public ColorCIE? Color { get; set; }

        /// <summary>Dominant wavelength in nm. Required if <see cref="Color"/> is omitted.</summary>
        public float? DominantWaveLength { get; set; }

        /// <summary>Manufacturer's part number of the diode.</summary>
        public string? DiodePart { get; set; }

        /// <summary>Measurements in ascending <see cref="Measurement.Physical"/> order.</summary>
        public IList<Measurement> Measurements { get; } = new List<Measurement>();

        public Emitter(Name name)
        {
            Name = name;
        }

        public override string ToString() => Name.Value;
    }

    /// <summary>A subtractive filter (XML node <c>Filter</c>).</summary>
    public sealed class Filter
    {
        public Name Name { get; set; }

        /// <summary>
        /// Approximate absolute color point when this filter is the only item fully inserted into the beam and the
        /// fixture is at maximum intensity.
        /// </summary>
        public ColorCIE? Color { get; set; }

        /// <summary>Measurements in ascending <see cref="Measurement.Physical"/> order.</summary>
        public IList<Measurement> Measurements { get; } = new List<Measurement>();

        public Filter(Name name)
        {
            Name = name;
        }

        public override string ToString() => Name.Value;
    }

    public enum InterpolationTo
    {
        Linear,
        Step,
        Log
    }

    /// <summary>The relation between the output requested by a control channel and the physically achieved output.</summary>
    public sealed class Measurement
    {
        /// <summary>
        /// Emitter: intensity DMX percentage (&gt; 0 to 100). Filter: flag insertion DMX percentage (0 = removed,
        /// 100 = fully inserted).
        /// </summary>
        public float Physical { get; set; }

        /// <summary>Emitters: overall candela value for this measurement.</summary>
        public float? LuminousIntensity { get; set; }

        /// <summary>Filters: total amount of light energy passed at this insertion percentage.</summary>
        public float? Transmission { get; set; }

        /// <summary>Interpolation from the previous measurement. Default: <see cref="Models.InterpolationTo.Linear"/>.</summary>
        public InterpolationTo InterpolationTo { get; set; }

        /// <summary>Optional spectrum of this measurement.</summary>
        public IList<MeasurementPoint> Points { get; } = new List<MeasurementPoint>();
    }

    /// <summary>The energy of one wavelength of a spectrum.</summary>
    public sealed class MeasurementPoint
    {
        /// <summary>Center wavelength in nm.</summary>
        public float WaveLength { get; set; }

        /// <summary>Light energy in W/m²/nm.</summary>
        public float Energy { get; set; }

        public MeasurementPoint(float waveLength, float energy)
        {
            WaveLength = waveLength;
            Energy = energy;
        }
    }

    public enum ColorSpaceMode
    {
        Custom,
        [System.Xml.Serialization.XmlEnum("sRGB")] SRgb,
        ProPhoto,
        [System.Xml.Serialization.XmlEnum("ANSI")] Ansi
    }

    /// <summary>The color space used for color mixing with indirect RGB, Hue/Sat, xyY or CMY control input.</summary>
    public sealed class ColorSpace
    {
        public const string DefaultName = "Default";

        public Name Name { get; set; } = new Name(DefaultName);

        /// <summary>Default: <see cref="ColorSpaceMode.SRgb"/>.</summary>
        public ColorSpaceMode Mode { get; set; } = ColorSpaceMode.SRgb;

        /// <summary>Red primary; used only when <see cref="Mode"/> is <see cref="ColorSpaceMode.Custom"/>.</summary>
        public ColorCIE? Red { get; set; }

        /// <summary>Green primary; used only when <see cref="Mode"/> is <see cref="ColorSpaceMode.Custom"/>.</summary>
        public ColorCIE? Green { get; set; }

        /// <summary>Blue primary; used only when <see cref="Mode"/> is <see cref="ColorSpaceMode.Custom"/>.</summary>
        public ColorCIE? Blue { get; set; }

        /// <summary>White point; used only when <see cref="Mode"/> is <see cref="ColorSpaceMode.Custom"/>.</summary>
        public ColorCIE? WhitePoint { get; set; }

        public override string ToString() => $"{Name} ({Mode})";
    }

    /// <summary>The set of colors the fixture can produce, as a polygon in CIE xyY.</summary>
    public sealed class Gamut
    {
        public Name Name { get; set; }

        /// <summary>Vertices of the gamut's polygon.</summary>
        public IList<ColorCIE> Points { get; } = new List<ColorCIE>();

        public Gamut(Name name)
        {
            Name = name;
        }

        public override string ToString() => Name.Value;
    }

    /// <summary>A nonlinear correlation between DMX input and physical output, defined piecewise by cubic functions.</summary>
    public sealed class DmxProfile
    {
        public Name Name { get; set; }

        public IList<DmxProfilePoint> Points { get; } = new List<DmxProfilePoint>();

        public DmxProfile(Name name)
        {
            Name = name;
        }

        /// <summary>
        /// The output at DMX percentage <paramref name="x"/>: the point with the biggest <see cref="DmxProfilePoint.DmxPercentage"/>
        /// below or equal <paramref name="x"/> defines a cubic function of (x - DMXPercentage). Without such a point the output is 0.
        /// </summary>
        public float Evaluate(float x)
        {
            DmxProfilePoint? point = null;
            foreach (var candidate in Points)
            {
                if (candidate.DmxPercentage <= x && (point == null || candidate.DmxPercentage > point.DmxPercentage)) point = candidate;
            }

            if (point == null) return 0f;
            var d = x - point.DmxPercentage;
            return ((point.Cfc3 * d + point.Cfc2) * d + point.Cfc1) * d + point.Cfc0;
        }

        public override string ToString() => Name.Value;
    }

    /// <summary>A point of a <see cref="DmxProfile"/>: from this DMX percentage on, output = CFC3·d³ + CFC2·d² + CFC1·d + CFC0.</summary>
    public sealed class DmxProfilePoint
    {
        /// <summary>DMX percentage where this piece starts.</summary>
        public float DmxPercentage { get; set; }

        /// <summary>Cubic function coefficient for x⁰.</summary>
        public float Cfc0 { get; set; }

        /// <summary>Cubic function coefficient for x¹.</summary>
        public float Cfc1 { get; set; }

        /// <summary>Cubic function coefficient for x².</summary>
        public float Cfc2 { get; set; }

        /// <summary>Cubic function coefficient for x³.</summary>
        public float Cfc3 { get; set; }
    }

    /// <summary>TM-30-15 fidelity indices for one color temperature (XML node <c>CRIGroup</c>).</summary>
    public sealed class CriGroup
    {
        /// <summary>Color temperature in Kelvin. Default: 6000.</summary>
        public float ColorTemperature { get; set; } = 6000f;

        public IList<Cri> Cris { get; } = new List<Cri>();
    }

    /// <summary>The color rendering index for one of the 99 TM-30 color evaluation samples.</summary>
    public sealed class Cri
    {
        /// <summary>The color evaluation sample, 1 to 99 (written as <c>CES01</c> … <c>CES99</c>). Default: 1.</summary>
        public int ColorSample { get; set; } = 1;

        /// <summary>Color rendering index for this sample. Default: 100.</summary>
        public byte ColorRenderingIndex { get; set; } = 100;
    }

    /// <summary>A physical connector. Obsolete since GDTF 1.2; use WiringObject geometries instead.</summary>
    public sealed class Connector
    {
        public Name Name { get; set; }

        /// <summary>Connector type, e.g. <c>XLR5</c> (predefined types in Annex D).</summary>
        public Name Type { get; set; }

        /// <summary>DMX break the connector belongs to.</summary>
        public uint? DmxBreak { get; set; }

        /// <summary>-1 male, +1 female, 0 universal; connectors whose genders add up to 0 fit together.</summary>
        public int Gender { get; set; }

        /// <summary>Length of the connector's wire in meters; 0 means built into the housing.</summary>
        public float Length { get; set; }

        public Connector(Name name, Name type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString() => Name.Value;
    }

    /// <summary>General properties of the device (XML node <c>Properties</c>).</summary>
    public sealed class DeviceProperties
    {
        /// <summary>Ambient temperature range the device can be operated in, or <c>null</c> when not given.</summary>
        public OperatingTemperature? OperatingTemperature { get; set; }

        /// <summary>Weight including all accessories in kg, or <c>null</c> when not given.</summary>
        public float? Weight { get; set; }

        /// <summary>Distance between the floor and the bottom base plate in meters, or <c>null</c> when not given.</summary>
        public float? LegHeight { get; set; }

        /// <summary>Power information per connector.</summary>
        public IList<PowerConsumption> PowerConsumptions { get; } = new List<PowerConsumption>();

        public bool IsEmpty => OperatingTemperature == null && Weight == null && LegHeight == null && PowerConsumptions.Count == 0;
    }

    public sealed class OperatingTemperature
    {
        /// <summary>Lowest operating temperature in °C. Default: 0.</summary>
        public float Low { get; set; }

        /// <summary>Highest operating temperature in °C. Default: 40.</summary>
        public float High { get; set; } = 40f;
    }

    /// <summary>
    /// Power information for a connector. The spec lists this node without defining it; the attributes follow the
    /// official XSD and are <c>null</c> when not given.
    /// </summary>
    public sealed class PowerConsumption
    {
        /// <summary>The connector this applies to (required).</summary>
        public Connector? Connector { get; set; }

        /// <summary>Power consumption in W.</summary>
        public float? Value { get; set; }

        public float? PowerFactor { get; set; }

        /// <summary>Lowest supported voltage in V.</summary>
        public float? VoltageLow { get; set; }

        /// <summary>Highest supported voltage in V.</summary>
        public float? VoltageHigh { get; set; }

        /// <summary>Lowest supported frequency in Hz.</summary>
        public float? FrequencyLow { get; set; }

        /// <summary>Highest supported frequency in Hz.</summary>
        public float? FrequencyHigh { get; set; }
    }
}
