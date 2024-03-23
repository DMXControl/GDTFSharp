using System.Collections.Generic;

namespace GDTFSharp.Nodes
{
    public class FixtureTypeNode: INode
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
        public YesNo CanHaveChildren { get; }

        public IEnumerable<INode> Children { get; }
        public void AddChildren(INode node)
        {
            throw new System.NotImplementedException();
        }

        public string ToXml()
        {
            throw new System.NotImplementedException();
        }
    }
}