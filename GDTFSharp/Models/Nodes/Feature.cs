using System.Text;

namespace GDTFSharp.Nodes
{
    public class Feature: ChildlessNode<Feature>
    {
        public Name Name { get; }
        public Name GroupName { get; }
        
        public Feature(Name name, Name groupName)
        {
            Name = name;
            GroupName = groupName;
        }
        
        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append($"<Feature Name=\"{Name}\"/>");
            return builder;
        }
    }
}