using System.Collections.Generic;
using System.Text;

namespace GDTFSharp.Nodes
{
    public class FixtureTypeNode: AbstractNode<IFixtureTypeNodeChild>
    {
        public Name Name { get; }
        public string ShortName { get; }
        public string LongName { get; }
        public string Manufacturer { get; }
        public string Description { get; }
        public GUID FixtureTypeID { get; }
        public string? Thumbnail { get; }
        public int ThumbnailOffsetX { get; }
        public int ThumbnailOffsetY { get; }
        public GUID? RefFT { get; }
        public bool CanHaveChildren { get; }
        

        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append($"<FixtureType Name=\"{Name.Value}\" ShortName=\"{ShortName}\" LongName=\"{LongName}\" Manufacturer=\"{Manufacturer}\" Description=\"{Description}\" FixtureTypeID=\"{FixtureTypeID.Value}\" Thumbnail=\"{Thumbnail ?? ""}\" ThumbnailOffsetX=\"{ThumbnailOffsetX}\" ThumbnailOffsetY=\"{ThumbnailOffsetY}\" RefFT=\"{RefFT?.Value ?? ""}\" CanHaveChildren=\"{(CanHaveChildren? "Yes" : "No")}\">");
            foreach (var child in Children)
            {
                child.ToXmlBuilder(builder);
            }
            builder.Append("</FixtureType>");
            return builder;
        }
    }
}