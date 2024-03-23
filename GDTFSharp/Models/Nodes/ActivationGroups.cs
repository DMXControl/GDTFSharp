using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GDTFSharp.Nodes
{
    public class ActivationGroups : AbstractNode<ActivationGroup>
    {
        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            
            builder.Append("<ActivationGroups>");
            foreach (var child in Children)
            {
                child.ToXmlBuilder(builder);
            }
            builder.Append("</ActivationGroups>");
            return builder;
        }
    }
}