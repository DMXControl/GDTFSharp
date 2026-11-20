using System;

namespace GDTFSharp.Models
{
    /// <summary>One revision in the history of the fixture type. GDTF Share adds one on every upload.</summary>
    public sealed class Revision
    {
        /// <summary>User-defined text for this revision.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Revision date and time in UTC.</summary>
        public DateTime? Date { get; set; }

        /// <summary>GDTF Share user ID of the uploader. Default: 0.</summary>
        public uint UserId { get; set; }

        /// <summary>Name of the software that modified this revision.</summary>
        public string ModifiedBy { get; set; } = string.Empty;

        public override string ToString() => $"{Date:yyyy-MM-dd} {Text}";
    }
}
