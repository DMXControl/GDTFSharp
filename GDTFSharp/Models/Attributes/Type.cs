using System;

namespace GDTFSharp
{
    public enum Type
    {
        PlacementOffset,
        Amplitude,
        AmplitudeMin,
        AmplitudeMax,
        Duration,
        DutyCycle,
        TimeOffset,
        MinimumOpening,
        Value,
        RatioHorizontal,
        RatioVertical
    }

    public static class TypeExtensions
    {
        public static string ToString(this Type type)
        {
            return type switch
            {
                Type.PlacementOffset => "PlacementOffset",
                Type.Amplitude => "Amplitude",
                Type.AmplitudeMin => "AmplitudeMin",
                Type.AmplitudeMax => "AmplitudeMax",
                Type.Duration => "Duration",
                Type.DutyCycle => "DutyCycle",
                Type.TimeOffset => "TimeOffset",
                Type.MinimumOpening => "MinimumOpening",
                Type.Value => "Value",
                Type.RatioHorizontal => "RatioHorizontal",
                Type.RatioVertical => "RatioVertical",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}