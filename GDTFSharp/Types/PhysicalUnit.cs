using System.Xml.Serialization;

namespace GDTFSharp.Types
{
    /// <summary>Physical unit of an Attribute or SubPhysicalUnit. Default: <see cref="None"/>.</summary>
    public enum PhysicalUnit
    {
        None,
        Percent,
        Length,
        Mass,
        Time,
        Temperature,
        LuminousIntensity,
        Angle,
        Force,
        Frequency,
        Current,
        Voltage,
        Power,
        Energy,
        Area,
        Volume,
        Speed,
        Acceleration,
        AngularSpeed,

        /// <summary>Written as <c>AngularAccc</c>, which is the spelling used by DIN SPEC 15800 and the XSD.</summary>
        [XmlEnum("AngularAccc")] AngularAcceleration,

        WaveLength,
        ColorComponent
    }

    public static class PhysicalUnitExtensions
    {
        /// <summary>The SI symbol of the unit's base unit, e.g. "m/s" for <see cref="PhysicalUnit.Speed"/>.</summary>
        public static string ToSymbol(this PhysicalUnit unit)
        {
            switch (unit)
            {
                case PhysicalUnit.Percent: return "%";
                case PhysicalUnit.Length: return "m";
                case PhysicalUnit.Mass: return "kg";
                case PhysicalUnit.Time: return "s";
                case PhysicalUnit.Temperature: return "K";
                case PhysicalUnit.LuminousIntensity: return "cd";
                case PhysicalUnit.Angle: return "deg";
                case PhysicalUnit.Force: return "N";
                case PhysicalUnit.Frequency: return "Hz";
                case PhysicalUnit.Current: return "A";
                case PhysicalUnit.Voltage: return "V";
                case PhysicalUnit.Power: return "W";
                case PhysicalUnit.Energy: return "J";
                case PhysicalUnit.Area: return "m^2";
                case PhysicalUnit.Volume: return "m^3";
                case PhysicalUnit.Speed: return "m/s";
                case PhysicalUnit.Acceleration: return "m/s^2";
                case PhysicalUnit.AngularSpeed: return "deg/s";
                case PhysicalUnit.AngularAcceleration: return "deg/s^2";
                case PhysicalUnit.WaveLength: return "nm";
                default: return "";
            }
        }
    }
}
