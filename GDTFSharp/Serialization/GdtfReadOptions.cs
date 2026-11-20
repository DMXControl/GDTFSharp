namespace GDTFSharp.Serialization
{
    /// <summary>Options for reading GDTF files.</summary>
    public sealed class GdtfReadOptions
    {
        public static GdtfReadOptions Default => new GdtfReadOptions();

        /// <summary>
        /// Throw a <see cref="GdtfReadException"/> on the first error instead of collecting it.
        /// Off by default, because many published fixture files are not fully conforming.
        /// </summary>
        public bool Strict { get; set; }
    }
}
