using System;
using System.Collections.Generic;
using System.Text;

namespace GDTFSharp.Nodes
{
    public class GDTFNode : AbstractNode<FixtureTypeNode>
    {
        public DataVersion DataVersion { get; }
        
        public GDTFNode(DataVersion dataVersion)
        {
            DataVersion = dataVersion;
        }

        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append($"GDTFData Version=\"{DataVersion.Major}.{DataVersion.Minor}\">");
            foreach (var child in Children)
            {
                child.ToXmlBuilder(builder);
            }
            builder.Append("</GDTFData>");
            return builder;
        }
    }
}