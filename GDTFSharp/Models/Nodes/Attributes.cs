using System.Text;

namespace GDTFSharp.Nodes
{
    public class Attributes : AbstractNode<Attribute>
    {
        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append("<Attributes>");
            foreach (var child in Children)
            {
                child.ToXmlBuilder(builder);
            }
            builder.Append("</Attributes>");
            return builder;
        }
    }
}