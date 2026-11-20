using System;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>The content of a GDTF <c>description.xml</c>: the <c>GDTF</c> root node and its fixture type.</summary>
    public sealed class GdtfDocument
    {
        /// <summary>
        /// DataVersion of the root node. When reading, this is the version of the file;
        /// the writer always writes <see cref="Types.DataVersion.Current"/>, since that is the format it produces.
        /// </summary>
        public DataVersion DataVersion { get; set; } = DataVersion.Current;

        /// <summary>The fixture type described by the document.</summary>
        public FixtureType FixtureType { get; set; }

        public GdtfDocument(FixtureType fixtureType)
        {
            FixtureType = fixtureType ?? throw new ArgumentNullException(nameof(fixtureType));
        }
    }
}
