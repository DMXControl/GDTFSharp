using GDTFSharp.Models;

namespace GDTFSharp.Serialization
{
    /// <summary>The reference scopes of GDTF Node attributes, one per starting point defined by the spec.</summary>
    internal static class GdtfScopes
    {
        /// <summary>Starting point: the ActivationGroups node. Path: <c>ActivationGroup</c>.</summary>
        public static readonly ReferenceScope<ActivationGroup> ActivationGroups = new ReferenceScope<ActivationGroup>("ActivationGroups");

        /// <summary>Starting point: the FeatureGroups node. Path: <c>FeatureGroup.Feature</c>.</summary>
        public static readonly ReferenceScope<Feature> Features = new ReferenceScope<Feature>("FeatureGroups");

        /// <summary>Starting point: the Attributes node. Path: <c>Attribute</c>.</summary>
        public static readonly ReferenceScope<GdtfAttribute> Attributes = new ReferenceScope<GdtfAttribute>("Attributes");

        /// <summary>Starting point: the Wheels node. Path: <c>Wheel</c>.</summary>
        public static readonly ReferenceScope<Wheel> Wheels = new ReferenceScope<Wheel>("Wheels");

        /// <summary>Starting point: the Emitters node. Path: <c>Emitter</c>.</summary>
        public static readonly ReferenceScope<Emitter> Emitters = new ReferenceScope<Emitter>("Emitters");

        /// <summary>Starting point: the Filters node. Path: <c>Filter</c>.</summary>
        public static readonly ReferenceScope<Filter> Filters = new ReferenceScope<Filter>("Filters");

        /// <summary>The default and the additional color spaces. Path: <c>ColorSpace</c>.</summary>
        public static readonly ReferenceScope<ColorSpace> ColorSpaces = new ReferenceScope<ColorSpace>("ColorSpaces");

        /// <summary>Starting point: the Gamuts node. Path: <c>Gamut</c>.</summary>
        public static readonly ReferenceScope<Gamut> Gamuts = new ReferenceScope<Gamut>("Gamuts");

        /// <summary>Starting point: the DMXProfiles node. Path: <c>DMXProfile</c>.</summary>
        public static readonly ReferenceScope<DmxProfile> DmxProfiles = new ReferenceScope<DmxProfile>("DMXProfiles");

        /// <summary>Starting point: the Connectors node. Path: <c>Connector</c>.</summary>
        public static readonly ReferenceScope<Connector> Connectors = new ReferenceScope<Connector>("Connectors");

        /// <summary>Every geometry of the tree, by its unique name.</summary>
        public static readonly ReferenceScope<Geometry> Geometries = new ReferenceScope<Geometry>("Geometries");

        /// <summary>Top-level geometries only, by name (GeometryReference, DMXMode).</summary>
        public static readonly ReferenceScope<Geometry> TopLevelGeometries = new ReferenceScope<Geometry>("top-level Geometries");

        /// <summary>Starting point: the DMXModes node. Path: <c>DMXMode</c>.</summary>
        public static readonly ReferenceScope<DmxMode> DmxModes = new ReferenceScope<DmxMode>("DMXModes");

        /// <summary>Starting point: the Models node. Path: <c>Model</c>.</summary>
        public static readonly ReferenceScope<Model> Models = new ReferenceScope<Model>("Models");
    }
}
