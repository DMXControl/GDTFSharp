using System.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>A logical grouping of Fixture Type Attributes, e.g. <c>Position</c> with the features PanTilt and XYZ.</summary>
    public sealed class FeatureGroup
    {
        public Name Name { get; set; }

        /// <summary>Display name of the feature group.</summary>
        public string Pretty { get; set; }

        public ChildCollection<FeatureGroup, Feature> Features { get; }

        public FeatureGroup(Name name, string? pretty = null)
        {
            Name = name;
            Pretty = pretty ?? name.Value;
            Features = new ChildCollection<FeatureGroup, Feature>(this, f => f.Group, (f, g) => f.Group = g);
        }

        public Feature? FindFeature(Name name) => Features.FirstOrDefault(f => f.Name == name);

        public override string ToString() => Name.Value;
    }

    /// <summary>A logical control unit inside a <see cref="FeatureGroup"/>, e.g. PanTilt inside Position.</summary>
    public sealed class Feature
    {
        public Name Name { get; set; }

        /// <summary>The feature group this feature is in; set when it is added to <see cref="FeatureGroup.Features"/>.</summary>
        public FeatureGroup? Group { get; internal set; }

        public Feature(Name name)
        {
            Name = name;
        }

        /// <summary>The path used to reference this feature: <c>FeatureGroup.Feature</c>.</summary>
        public NodePath Path => Group == null ? new NodePath(Name) : new NodePath(Group.Name, Name);

        public override string ToString() => Path.ToString();
    }
}
