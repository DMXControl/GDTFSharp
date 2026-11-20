using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>
    /// A group of Fixture Type Attributes that are intended to be used together,
    /// e.g. Pan and Tilt, so any position can be stored and recreated.
    /// </summary>
    public sealed class ActivationGroup
    {
        public Name Name { get; set; }

        public ActivationGroup(Name name)
        {
            Name = name;
        }

        public override string ToString() => Name.Value;
    }
}
