namespace GDTFSharp
{
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
        AngularAcceleration,
        Wavelength,
        ColorComponent
    }
    
    public static class PhysicalUnitExtensions
    {
        public static PhysicalUnit FromString(this PhysicalUnit _, string unit)
        {
            return unit switch
            {
                "None" => PhysicalUnit.None,
                "Percent" => PhysicalUnit.Percent,
                "Length" => PhysicalUnit.Length,
                "Mass" => PhysicalUnit.Mass,
                "Time" => PhysicalUnit.Time,
                "Temperature" => PhysicalUnit.Temperature,
                "LuminousIntensity" => PhysicalUnit.LuminousIntensity,
                "Angle" => PhysicalUnit.Angle,
                "Force" => PhysicalUnit.Force,
                "Frequency" => PhysicalUnit.Frequency,
                "Current" => PhysicalUnit.Current,
                "Voltage" => PhysicalUnit.Voltage,
                "Power" => PhysicalUnit.Power,
                "Energy" => PhysicalUnit.Energy,
                "Area" => PhysicalUnit.Area,
                "Volume" => PhysicalUnit.Volume,
                "Speed" => PhysicalUnit.Speed,
                "Acceleration" => PhysicalUnit.Acceleration,
                "AngularSpeed" => PhysicalUnit.AngularSpeed,
                "AngularAcceleration" => PhysicalUnit.AngularAcceleration,
                "Wavelength" => PhysicalUnit.Wavelength,
                "ColorComponent" => PhysicalUnit.ColorComponent,
                _ => PhysicalUnit.None
            };
        }
        public static string ToNameString(this PhysicalUnit unit)
        {
            return unit switch
            {
                PhysicalUnit.None => "None",
                PhysicalUnit.Percent => "Percent",
                PhysicalUnit.Length => "Length",
                PhysicalUnit.Mass => "Mass",
                PhysicalUnit.Time => "Time",
                PhysicalUnit.Temperature => "Temperature",
                PhysicalUnit.LuminousIntensity => "LuminousIntensity",
                PhysicalUnit.Angle => "Angle",
                PhysicalUnit.Force => "Force",
                PhysicalUnit.Frequency => "Frequency",
                PhysicalUnit.Current => "Current",
                PhysicalUnit.Voltage => "Voltage",
                PhysicalUnit.Power => "Power",
                PhysicalUnit.Energy => "Energy",
                PhysicalUnit.Area => "Area",
                PhysicalUnit.Volume => "Volume",
                PhysicalUnit.Speed => "Speed",
                PhysicalUnit.Acceleration => "Acceleration",
                PhysicalUnit.AngularSpeed => "AngularSpeed",
                PhysicalUnit.AngularAcceleration => "AngularAcceleration",
                PhysicalUnit.Wavelength => "Wavelength",
                PhysicalUnit.ColorComponent => "ColorComponent",
                _ => "None"
            };
        }
        
        public static string ToUnitString(this PhysicalUnit unit)
        {
            return unit switch
            {
                PhysicalUnit.None => "",
                PhysicalUnit.Percent => "%",
                PhysicalUnit.Length => "m",
                PhysicalUnit.Mass => "kg",
                PhysicalUnit.Time => "s",
                PhysicalUnit.Temperature => "K",
                PhysicalUnit.LuminousIntensity => "cd",
                PhysicalUnit.Angle => "deg",
                PhysicalUnit.Force => "N",
                PhysicalUnit.Frequency => "Hz",
                PhysicalUnit.Current => "A",
                PhysicalUnit.Voltage => "V",
                PhysicalUnit.Power => "W",
                PhysicalUnit.Energy => "J",
                PhysicalUnit.Area => "m^2",
                PhysicalUnit.Volume => "m^3",
                PhysicalUnit.Speed => "m/s",
                PhysicalUnit.Acceleration => "m/s^2",
                PhysicalUnit.AngularSpeed => "deg/s",
                PhysicalUnit.AngularAcceleration => "deg/s^2",
                PhysicalUnit.Wavelength => "nm",
                PhysicalUnit.ColorComponent => "",
                _ => ""
            };
        }
    }
}