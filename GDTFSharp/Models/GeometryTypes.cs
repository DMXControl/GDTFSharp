using System.Collections.Generic;
using System.Numerics;
using System.Xml.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    public enum LampType
    {
        Discharge,
        Tungsten,
        Halogen,
        [XmlEnum("LED")] Led
    }

    /// <summary>How a beam is rendered.</summary>
    public enum BeamType
    {
        /// <summary>Conical beam with soft edges and softened field projection.</summary>
        Wash,

        /// <summary>Conical beam with hard edges.</summary>
        Spot,

        /// <summary>No beam is drawn; only the geometry emits light.</summary>
        None,

        /// <summary>Pyramid-shaped beam with hard edges.</summary>
        Rectangle,

        [XmlEnum("PC")] Pc,
        Fresnel,

        /// <summary>No beam is drawn; only the geometry emits light.</summary>
        Glow
    }

    /// <summary>
    /// The light output of a device, usually the position of the lens (XML node <c>Beam</c>). The beam emits light into -Z
    /// (Y up).
    /// </summary>
    public sealed class Beam : Geometry
    {
        public Beam(Name name) : base(name) { }
        public override string ElementName => "Beam";

        /// <summary>Default: <see cref="Models.LampType.Discharge"/>.</summary>
        public LampType LampType { get; set; } = LampType.Discharge;

        /// <summary>Power consumption in W. Default: 1000.</summary>
        public float PowerConsumption { get; set; } = 1000f;

        /// <summary>Intensity of all represented light emitters in lumen. Default: 10000.</summary>
        public float LuminousFlux { get; set; } = 10000f;

        /// <summary>Color temperature in K. Default: 6000.</summary>
        public float ColorTemperature { get; set; } = 6000f;

        /// <summary>Beam angle in degrees. Default: 25.</summary>
        public float BeamAngle { get; set; } = 25f;

        /// <summary>Field angle in degrees. Default: 25.</summary>
        public float FieldAngle { get; set; } = 25f;

        /// <summary>Throw ratio of the lens for <see cref="Models.BeamType.Rectangle"/>. Default: 1.</summary>
        public float ThrowRatio { get; set; } = 1f;

        /// <summary>Ratio from width to height of a <see cref="Models.BeamType.Rectangle"/> beam. Default: 1.7777.</summary>
        public float RectangleRatio { get; set; } = 1.7777f;

        /// <summary>Beam radius at the starting point in meters. Default: 0.05.</summary>
        public float BeamRadius { get; set; } = 0.05f;

        /// <summary>Default: <see cref="Models.BeamType.Wash"/>.</summary>
        public BeamType BeamType { get; set; } = BeamType.Wash;

        /// <summary>TM-30 color rendering index. Default: 100.</summary>
        public byte ColorRenderingIndex { get; set; } = 100;

        /// <summary>
        /// Optional emitter defining the white light source of a subtractive color mixing system. Without one, the spectrum
        /// is a black body with <see cref="ColorTemperature"/>.
        /// </summary>
        public Emitter? EmitterSpectrum { get; set; }
    }

    public enum LaserColorType
    {
        [XmlEnum("RGB")] Rgb,
        SingleWaveLength
    }

    /// <summary>The light output of a laser (XML node <c>Laser</c>).</summary>
    public sealed class Laser : Geometry
    {
        public Laser(Name name) : base(name) { }
        public override string ElementName => "Laser";

        /// <summary>Default: <see cref="LaserColorType.Rgb"/>.</summary>
        public LaserColorType ColorType { get; set; } = LaserColorType.Rgb;

        /// <summary>Wavelength in nm; required when <see cref="ColorType"/> is <see cref="LaserColorType.SingleWaveLength"/>.</summary>
        public float? Color { get; set; }

        /// <summary>Output strength in W.</summary>
        public float? OutputStrength { get; set; }

        /// <summary>Optional emitter group.</summary>
        public Emitter? Emitter { get; set; }

        /// <summary>Beam diameter where it leaves the projector, in meters.</summary>
        public float? BeamDiameter { get; set; }

        /// <summary>Minimum beam divergence in mrad.</summary>
        public float? BeamDivergenceMin { get; set; }

        /// <summary>Maximum beam divergence in mrad.</summary>
        public float? BeamDivergenceMax { get; set; }

        /// <summary>Total pan scan angle in degrees, assuming symmetrical output.</summary>
        public float? ScanAnglePan { get; set; }

        /// <summary>Total tilt scan angle in degrees, assuming symmetrical output.</summary>
        public float? ScanAngleTilt { get; set; }

        /// <summary>Scan speed in kilo points per second.</summary>
        public float? ScanSpeed { get; set; }

        /// <summary>Names of the supported laser protocols (XML nodes <c>Protocol</c>).</summary>
        public IList<string> Protocols { get; } = new List<string>();
    }

    public enum WiringComponentType
    {
        Input,
        Output,
        PowerSource,
        Consumer,
        Fuse,
        NetworkProvider,
        NetworkInput,
        NetworkOutput,
        NetworkInOut
    }

    public enum FuseRating
    {
        B,
        C,
        D,
        K,
        Z
    }

    public enum PinOrientation
    {
        Left,
        Right,
        Top,
        Bottom
    }

    /// <summary>
    /// An electrical part that can be wired, for power or data (XML node <c>WiringObject</c>). Its name is also the name of
    /// the interface to the outside. Replaces the obsolete <see cref="Connector"/>.
    /// </summary>
    public sealed class WiringObject : Geometry
    {
        public WiringObject(Name name) : base(name) { }
        public override string ElementName => "WiringObject";

        /// <summary>Connector type (predefined types in Annex D, or custom such as "Loose End"). Not for fuses.</summary>
        public string? ConnectorType { get; set; }

        public WiringComponentType? ComponentType { get; set; }

        /// <summary>Signal type: "Power", "DMX512", "Protocol", "AES", "AnalogVideo", "AnalogAudio" or a custom protocol.</summary>
        public string? SignalType { get; set; }

        /// <summary>Number of pins available for internal wiring.</summary>
        public int? PinCount { get; set; }

        /// <summary>Consumers: electrical consumption in W.</summary>
        public float? ElectricalPayLoad { get; set; }

        /// <summary>Consumers: maximum voltage in V.</summary>
        public float? VoltageRangeMax { get; set; }

        /// <summary>Consumers: minimum voltage in V.</summary>
        public float? VoltageRangeMin { get; set; }

        /// <summary>Consumers: maximum frequency in Hz.</summary>
        public float? FrequencyRangeMax { get; set; }

        /// <summary>Consumers: minimum frequency in Hz.</summary>
        public float? FrequencyRangeMin { get; set; }

        /// <summary>Power sources: maximum electrical payload in VA.</summary>
        public float? MaxPayLoad { get; set; }

        /// <summary>Power sources: output voltage in V.</summary>
        public float? Voltage { get; set; }

        /// <summary>Wiring objects with the same signal layer are connected; 0 connects to all.</summary>
        public int? SignalLayer { get; set; }

        /// <summary>Consumers: power factor.</summary>
        public float? CosPhi { get; set; }

        /// <summary>Fuses: fuse current in A.</summary>
        public float? FuseCurrent { get; set; }

        public FuseRating? FuseRating { get; set; }

        /// <summary>Where the pins are placed on the object.</summary>
        public PinOrientation? Orientation { get; set; }

        /// <summary>Name of the group this wiring object belongs to.</summary>
        public string? WireGroup { get; set; }

        /// <summary>How the pins of this wiring object connect to pins of other wiring objects.</summary>
        public IList<PinPatch> PinPatches { get; } = new List<PinPatch>();
    }

    /// <summary>A connection from a pin of a <see cref="WiringObject"/> to a pin of another one (XML node <c>PinPatch</c>).</summary>
    public sealed class PinPatch
    {
        public WiringObject? ToWiringObject { get; set; }

        /// <summary>Pin of the parent wiring object.</summary>
        public int FromPin { get; set; }

        /// <summary>Pin of <see cref="ToWiringObject"/>.</summary>
        public int ToPin { get; set; }
    }

    /// <summary>An additional item that can be used with the fixture, like a rain cover (XML node <c>Inventory</c>).</summary>
    public sealed class Inventory : Geometry
    {
        public Inventory(Name name) : base(name) { }
        public override string ElementName => "Inventory";

        /// <summary>The default count for new objects.</summary>
        public int? Count { get; set; }
    }

    public enum StructureType
    {
        CenterLineBased,
        Detail
    }

    public enum CrossSectionType
    {
        TrussFramework,
        Tube
    }

    /// <summary>The internal framing of an object, like truss members (XML node <c>Structure</c>).</summary>
    public sealed class Structure : Geometry
    {
        public Structure(Name name) : base(name) { }
        public override string ElementName => "Structure";

        public Geometry? LinkedGeometry { get; set; }

        public StructureType? StructureType { get; set; }

        public CrossSectionType? CrossSectionType { get; set; }

        /// <summary>Tubes: height of the cross section in meters.</summary>
        public float? CrossSectionHeight { get; set; }

        /// <summary>Tubes: wall thickness of the cross section in meters.</summary>
        public float? CrossSectionWallThickness { get; set; }

        /// <summary>Trusses: name of the truss cross section.</summary>
        public string? TrussCrossSection { get; set; }
    }

    public enum SupportType
    {
        Rope,
        GroundSupport
    }

    /// <summary>A support like a base plate or a hoist (XML node <c>Support</c>). Capacities follow the Eurocode.</summary>
    public sealed class Support : Geometry
    {
        public Support(Name name) : base(name) { }
        public override string ElementName => "Support";

        public SupportType? SupportType { get; set; }

        /// <summary>Ropes: name of the rope cross section.</summary>
        public string? RopeCrossSection { get; set; }

        /// <summary>Ropes: offset of the rope from bottom to top in meters.</summary>
        public Vector3? RopeOffset { get; set; }

        /// <summary>Allowable force on the X axis in N.</summary>
        public float? CapacityX { get; set; }
        public float? CapacityY { get; set; }
        public float? CapacityZ { get; set; }

        /// <summary>Allowable moment around the X axis in N/m.</summary>
        public float? CapacityXX { get; set; }
        public float? CapacityYY { get; set; }
        public float? CapacityZZ { get; set; }

        /// <summary>Ground supports: compression ratio along the X axis in N/m.</summary>
        public float? ResistanceX { get; set; }
        public float? ResistanceY { get; set; }
        public float? ResistanceZ { get; set; }

        /// <summary>Ground supports: compression ratio around the X axis in N/m.</summary>
        public float? ResistanceXX { get; set; }
        public float? ResistanceYY { get; set; }
        public float? ResistanceZZ { get; set; }
    }
}
