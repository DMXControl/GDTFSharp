using GDTFSharp.Models;

namespace GDTFSharp.Serialization
{
    /// <summary>The reference scopes of GDTF Node attributes, one per starting point defined by the spec.</summary>
    public static class GdtfScopes
    {
        /// <summary>Starting point: the ActivationGroups node. Path: <c>ActivationGroup</c>.</summary>
        public static readonly ReferenceScope<ActivationGroup> ActivationGroups = new ReferenceScope<ActivationGroup>("ActivationGroups");

        /// <summary>Starting point: the FeatureGroups node. Path: <c>FeatureGroup.Feature</c>.</summary>
        public static readonly ReferenceScope<Feature> Features = new ReferenceScope<Feature>("FeatureGroups");

        /// <summary>Starting point: the Attributes node. Path: <c>Attribute</c>.</summary>
        public static readonly ReferenceScope<GdtfAttribute> Attributes = new ReferenceScope<GdtfAttribute>("Attributes");
    }
}
