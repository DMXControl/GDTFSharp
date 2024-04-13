using System.Text;

namespace GDTFSharp.Nodes
{
    public class SubphysicalUnit: ChildlessNode<SubphysicalUnit>
    {
        public Type Type { get; }
        public PhysicalUnit PhysicalUnit { get; }
        public float PhysicalFrom { get; }
        public float PhysicalTo { get; }

        public SubphysicalUnit(Type type, PhysicalUnit physicalUnit, float physicalFrom, float physicalTo)
        {
            this.Type = type;
            this.PhysicalUnit = physicalUnit;
            this.PhysicalFrom = physicalFrom;
            this.PhysicalTo = physicalTo;
        }
        
        public override StringBuilder ToXmlBuilder(StringBuilder builder)
        {
            builder.Append(
                $"<SubPhysicalUnit Type=\"{Type.ToString()}\" PhysicalUnit=\"{PhysicalUnit.ToNameString()}\" PhysicalFrom=\"{PhysicalFrom}\" PhysicalTo=\"{PhysicalTo}\" />");
            return builder;
        }
    }
}