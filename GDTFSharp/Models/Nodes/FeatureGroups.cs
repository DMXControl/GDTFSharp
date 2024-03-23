using System.Collections.Generic;
using System.Text;

namespace GDTFSharp.Nodes
{
    public class FeatureGroups: AbstractNode<FeatureGroup>
    {
        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append("<FeatureGroups>");
            foreach (var child in Children)
            {
                child.ToXmlBuilder(builder);
            }
            builder.Append("</FeatureGroups>");
            return builder;
        }
    }
}