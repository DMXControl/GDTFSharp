using System.Text;

namespace GDTFSharp.Nodes
{
    public class Attribute: ChildlessNode<Attribute>
    {
        public Name Name { get; }
        public string Pretty { get; }
        public ActivationGroup? ActivationGroup { get; }
        public FeatureGroup Feature { get; }
        public Attribute? MainAttribute { get; }
        
        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            throw new System.NotImplementedException();
        }
    }
}