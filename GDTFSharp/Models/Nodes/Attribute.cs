using System.Linq;
using System.Text;

namespace GDTFSharp.Nodes
{
    public class Attribute: AbstractNode<SubphysicalUnit>
    {
        public Name Name { get; }
        public string Pretty { get; }
        public ActivationGroup? ActivationGroup { get; }
        public Feature Feature { get; }
        public Attribute? MainAttribute { get; }
        public PhysicalUnit PhysicalUnit { get; }
        public ColorCIE? Color { get; }

        public Attribute(Name name, string pretty, ActivationGroup? activationGroup, Feature feature,
            Attribute? mainAttribute, PhysicalUnit physicalUnit, ColorCIE? color)
        {
            this.Name = name;
            this.Pretty = pretty;
            this.ActivationGroup = activationGroup;
            this.Feature = feature;
            this.MainAttribute = mainAttribute;
            this.PhysicalUnit = physicalUnit;
            this.Color = color;
        }
        
        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append($"<Attribute Name=\"{Name}\" Pretty=\"{Pretty}\" Feature=\"{Feature.Name}\" PhysicalUnit=\"{PhysicalUnit.ToNameString()}\"");
            if (ActivationGroup != null)
            {
                builder.Append($"ActivationGroup=\"{ActivationGroup.Name}\"");
            }
            if (MainAttribute != null)
            {
                builder.Append($"MainAttribute=\"{MainAttribute.Name}\"");
            }
            if (Color != null)
            {
                builder.Append($"Color=\"{Color.floatx},{Color.floaty},{Color.floatY}\"");
            }

            if (Children.Any())
            {
                builder.Append(">");
                foreach (var child in Children)
                {
                    child.ToXmlBuilder(builder);
                }
                builder.Append("</Attribute>");
            }
            else
            {
                builder.Append(" />");
            }
            return builder;
        }
    }
}