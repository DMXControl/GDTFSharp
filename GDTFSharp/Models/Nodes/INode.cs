using System.Collections.Generic;

namespace GDTFSharp.Nodes
{
    public interface INode
    {
        public IEnumerable<INode> Children { get; }
        
        public void AddChildren(INode node);
        public string ToXml();
    }
}