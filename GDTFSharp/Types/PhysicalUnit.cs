using System.Xml.Serialization;

namespace GDTFSharp.Types
{
    /// <summary>Physical unit of an Attribute or SubPhysicalUnit. Default: <see cref="None"/>.</summary>
    public enum PhysicalUnit
    {
        /// <summary>No unit.</summary>
        None,
        /// <summary>Percent.</summary>
        Percent,
        /// <summary>Length in m.</summary>
        Length,
        /// <summary>Mass in kg.</summary>
        Mass,
        /// <summary>Time in s.</summary>
        Time,
        /// <summary>Temperature in K.</summary>
        Temperature,
        /// <summary>Luminous intensity in cd.</summary>
        LuminousIntensity,
        /// <summary>Angle in degrees.</summary>
        Angle,
        /// <summary>Force in N.</summary>
        Force,
        /// <summary>Frequency in Hz.</summary>
        Frequency,
        /// <summary>Current in A.</summary>
        Current,
        /// <summary>Voltage in V.</summary>
        Voltage,
        /// <summary>Power in W.</summary>
        Power,
        /// <summary>Energy in J.</summary>
        Energy,
        /// <summary>Area in m².</summary>
        Area,
        /// <summary>Volume in m³.</summary>
        Volume,
        /// <summary>Speed in m/s.</summary>
        Speed,
        /// <summary>Acceleration in m/s².</summary>
        Acceleration,
        /// <summary>Angular speed in degrees/s.</summary>
        AngularSpeed,

        /// <summary>Written as <c>AngularAccc</c>, which is the spelling used by DIN SPEC 15800 and the XSD.</summary>
        [XmlEnum("AngularAccc")] AngularAcceleration,

        /// <summary>Wavelength in nm.</summary>
        WaveLength,
        /// <summary>Color component (no unit).</summary>
        ColorComponent
    }

    /// <summary>Helpers for <see cref="PhysicalUnit"/>.</summary>
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
