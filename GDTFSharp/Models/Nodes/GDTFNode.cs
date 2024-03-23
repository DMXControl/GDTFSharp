using System;
using System.Collections.Generic;
using System.Text;

namespace GDTFSharp.Nodes
{
    public class GDTFNode : INode
    {
        public DataVersion DataVersion { get; }
        public IEnumerable<INode> Children => _children;

        private List<INode> _children;
        
        public GDTFNode(DataVersion dataVersion)
        {
            DataVersion = dataVersion;
            this._children = new List<INode>(1);
        }

        public void AddChildren(INode node)
        {
            if (!(node is FixtureTypeNode))
            {
                throw new Exception("Node must be of type FixtureTypeNode");
            }
            if (this._children.Count > 0)
            {
                throw new Exception("GDTFNode can only have one child of type FixtureTypeNode");
            }
            
            this._children.Add(node);
        }

        public string ToXml()
        {
            var builder = new StringBuilder();
            builder.Append($"GDTFData Version=\"{DataVersion.Major}.{DataVersion.Minor}\">");
            foreach (var child in Children)
            {
                builder.Append(child.ToXml());
            }
            builder.Append("</GDTFData>");
            return builder.ToString();
        }
    }
}