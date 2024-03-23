using System.Collections.Generic;
using System.Text;

namespace GDTFSharp.Nodes
{
    public class AttributeDefinitions : AbstractNode<IFixtureTypeNodeChild>
    {
        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append("<AttributeDefinitions>");
            foreach (var child in Children)
            {
                child.ToXmlBuilder(builder);
            }
            builder.Append("</AttributeDefinitions>");
            return builder;
        }
    }
}