namespace GDTFSharp.Types
{
    /// <summary>The <c>Type</c> of a SubPhysicalUnit.</summary>
    public enum SubPhysicalUnitType
    {
        /// <summary>Placement offset.</summary>
        PlacementOffset,
        /// <summary>Amplitude.</summary>
        Amplitude,
        /// <summary>Minimum amplitude.</summary>
        AmplitudeMin,
        /// <summary>Maximum amplitude.</summary>
        AmplitudeMax,
        /// <summary>Duration.</summary>
        Duration,
        /// <summary>Fraction of a period in which the value is not 0 (Annex F).</summary>
        DutyCycle,
        /// <summary>Offset of the end of a pulse as a fraction of the period (Annex F).</summary>
        TimeOffset,
        /// <summary>Minimum opening.</summary>
        MinimumOpening,
        /// <summary>Value.</summary>
        Value,
        /// <summary>Horizontal ratio.</summary>
        RatioHorizontal,
        /// <summary>Vertical ratio.</summary>
        RatioVertical
    }
}
