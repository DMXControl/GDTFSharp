using System.Collections.Generic;
using System.Text;

namespace GDTFSharp.Nodes
{
    public class ActivationGroup : ChildlessNode<ActivationGroup>
    {
        public Name Name { get; }
        
        public ActivationGroup(Name name)
        {
            this.Name = name;
        }

        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append($"<ActivationGroup Name=\"{Name}\" />");
            return builder;
        }
    }
}