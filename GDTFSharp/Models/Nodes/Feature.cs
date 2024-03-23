using System.Text;

namespace GDTFSharp.Nodes
{
    public class Feature: ChildlessNode<Feature>
    {
        public Name Name { get; }
        
        public Feature(Name name)
        {
            Name = name;
        }
        
        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append($"<Feature Name=\"{Name}\"/>");
            return builder;
        }
    }
}