using System;
using System.Collections.Generic;
using System.Text;

namespace GDTFSharp.Nodes
{
    public class FeatureGroup: AbstractNode<Feature>
    {
        
        public Name Name { get; }
        public string Pretty { get; }
        
        public FeatureGroup(Name name, string pretty)
        {
            Name = name;
            Pretty = pretty;
        }

        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append($"<FeatureGroup Name=\"{Name}\" Pretty=\"{Pretty}\">");
            foreach (var child in _children)
            {
                child.ToXmlBuilder(builder);
            }
            builder.Append("</FeatureGroup>");
            return builder;
        }
    }
}