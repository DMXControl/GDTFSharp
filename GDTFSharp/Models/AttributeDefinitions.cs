using System.Collections.Generic;
using System.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>
    /// The <c>AttributeDefinitions</c> section: activation groups, feature groups and the Fixture Type Attributes
    /// used by the fixture type. Predefined attributes and groups are listed in DIN SPEC 15800 Annex B.
    /// </summary>
    public sealed class AttributeDefinitions
    {
        /// <summary>Groups of attributes that are intended to be activated together, e.g. Pan and Tilt.</summary>
        public IList<ActivationGroup> ActivationGroups { get; } = new List<ActivationGroup>();

        /// <summary>Logical grouping of attributes into feature groups and features.</summary>
        public IList<FeatureGroup> FeatureGroups { get; } = new List<FeatureGroup>();

        /// <summary>The Fixture Type Attributes.</summary>
        public IList<GdtfAttribute> Attributes { get; } = new List<GdtfAttribute>();

        public ActivationGroup? FindActivationGroup(Name name) => ActivationGroups.FirstOrDefault(g => g.Name == name);

        public FeatureGroup? FindFeatureGroup(Name name) => FeatureGroups.FirstOrDefault(g => g.Name == name);

        /// <summary>Finds a feature by its <c>FeatureGroup.Feature</c> path, e.g. <c>Position.PanTilt</c>.</summary>
        public Feature? FindFeature(NodePath path) =>
            path.Segments.Count == 2 ? FindFeatureGroup(path.Segments[0])?.FindFeature(path.Segments[1]) : null;

        public GdtfAttribute? FindAttribute(Name name) => Attributes.FirstOrDefault(a => a.Name == name);
    }
}
