using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>Something that holds child geometries: the <see cref="FixtureType"/> (top level) or a <see cref="Geometry"/>.</summary>
    public interface IGeometryContainer
    {
        /// <summary>The child geometries; adding one sets its <see cref="Geometry.Parent"/>.</summary>
        ChildCollection<IGeometryContainer, Geometry> Geometries { get; }
    }

    /// <summary>
    /// A physically separated part of the device. This class is the general geometry (XML node <c>Geometry</c>) and
    /// the base of all specific geometry types; <see cref="ElementName"/> is the XML node name.
    /// Geometry names are unique within the whole fixture type.
    /// </summary>
    public class Geometry : IGeometryContainer
    {
        public Name Name { get; set; }

        /// <summary>The model of this part, from <see cref="FixtureType.Models"/>.</summary>
        public Model? Model { get; set; }

        /// <summary>
        /// Position relative to the parent geometry, as a System.Numerics transform: build it with
        /// <see cref="Matrix4x4.CreateTranslation(Vector3)"/> and friends, apply it with <see cref="Vector3.Transform(Vector3, Matrix4x4)"/>.
        /// The reader and writer convert from and to the GDTF matrix layout (which is the transpose). Default: identity.
        /// Coordinates: X left to right, Y front to back, Z bottom to top, in meters; devices are drawn hanging.
        /// </summary>
        public Matrix4x4 Position { get; set; } = Matrix4x4.Identity;

        /// <summary>Child geometries in document order.</summary>
        public ChildCollection<IGeometryContainer, Geometry> Geometries { get; }

        /// <summary>The fixture type (for top-level geometries) or the parent geometry; set when added to a collection.</summary>
        public IGeometryContainer? Parent { get; internal set; }

        /// <summary>The parent geometry, or <c>null</c> for a top-level geometry.</summary>
        public Geometry? ParentGeometry => Parent as Geometry;

        /// <summary>True for direct children of <see cref="FixtureType.Geometries"/>.</summary>
        public bool IsTopLevel => Parent is FixtureType;

        /// <summary>The XML node name of this geometry type.</summary>
        public virtual string ElementName => "Geometry";

        public Geometry(Name name)
        {
            Name = name;
            Geometries = CreateGeometryCollection(this);
        }

        internal static ChildCollection<IGeometryContainer, Geometry> CreateGeometryCollection(IGeometryContainer owner) =>
            new ChildCollection<IGeometryContainer, Geometry>(owner, g => g.Parent, (g, p) => g.Parent = p);

        /// <summary>All geometries below this one, depth first in document order.</summary>
        public IEnumerable<Geometry> Descendants() => Geometries.SelectMany(g => new[] { g }.Concat(g.Descendants()));

        /// <summary>The top-level geometry this geometry belongs to (itself when it is top level).</summary>
        public Geometry Root
        {
            get
            {
                var current = this;
                while (current.ParentGeometry != null) current = current.ParentGeometry;
                return current;
            }
        }

        /// <summary>
        /// The transform relative to the fixture origin: this position combined with those of all parent geometries
        /// (System.Numerics order: own position × parent's absolute position).
        /// </summary>
        public Matrix4x4 AbsolutePosition => ParentGeometry == null ? Position : Position * ParentGeometry.AbsolutePosition;

        public override string ToString() => $"{ElementName} {Name}";
    }

    /// <summary>Geometry with a rotation axis, e.g. Yoke or Head (XML node <c>Axis</c>).</summary>
    public sealed class Axis : Geometry
    {
        public Axis(Name name) : base(name) { }
        public override string ElementName => "Axis";
    }

    /// <summary>Geometry with a beam filter, e.g. BarnDoor or Iris (XML node <c>FilterBeam</c>).</summary>
    public sealed class FilterBeam : Geometry
    {
        public FilterBeam(Name name) : base(name) { }
        public override string ElementName => "FilterBeam";
    }

    /// <summary>Geometry with a color filter (XML node <c>FilterColor</c>).</summary>
    public sealed class FilterColor : Geometry
    {
        public FilterColor(Name name) : base(name) { }
        public override string ElementName => "FilterColor";
    }

    /// <summary>Geometry with gobo wheels (XML node <c>FilterGobo</c>).</summary>
    public sealed class FilterGobo : Geometry
    {
        public FilterGobo(Name name) : base(name) { }
        public override string ElementName => "FilterGobo";
    }

    /// <summary>Geometry with a shaper (XML node <c>FilterShaper</c>).</summary>
    public sealed class FilterShaper : Geometry
    {
        public FilterShaper(Name name) : base(name) { }
        public override string ElementName => "FilterShaper";
    }

    /// <summary>Media representation layer of a media device (XML node <c>MediaServerLayer</c>).</summary>
    public sealed class MediaServerLayer : Geometry
    {
        public MediaServerLayer(Name name) : base(name) { }
        public override string ElementName => "MediaServerLayer";
    }

    /// <summary>Camera or output layer of a media device; the view points into +Y (XML node <c>MediaServerCamera</c>).</summary>
    public sealed class MediaServerCamera : Geometry
    {
        public MediaServerCamera(Name name) : base(name) { }
        public override string ElementName => "MediaServerCamera";
    }

    /// <summary>Master control of one or several media devices (XML node <c>MediaServerMaster</c>).</summary>
    public sealed class MediaServerMaster : Geometry
    {
        public MediaServerMaster(Name name) : base(name) { }
        public override string ElementName => "MediaServerMaster";
    }

    /// <summary>A point where other geometries should be attached (XML node <c>Magnet</c>).</summary>
    public sealed class Magnet : Geometry
    {
        public Magnet(Name name) : base(name) { }
        public override string ElementName => "Magnet";
    }

    /// <summary>A self-emitting surface that displays visual media (XML node <c>Display</c>).</summary>
    public sealed class Display : Geometry
    {
        public Display(Name name) : base(name) { }
        public override string ElementName => "Display";

        /// <summary>Name of the texture in the model file that is swapped out for the media resource.</summary>
        public string? Texture { get; set; }
    }

    /// <summary>Several instances of the same top-level geometry, e.g. the pixels of an LED panel (XML node <c>GeometryReference</c>).</summary>
    public sealed class GeometryReference : Geometry
    {
        public GeometryReference(Name name) : base(name) { }
        public override string ElementName => "GeometryReference";

        /// <summary>
        /// The referenced geometry; only top-level geometries can be referenced. <see cref="Geometry.Model"/>, when set,
        /// replaces only the model of the referenced geometry itself, not those of its children.
        /// </summary>
        public Geometry? ReferencedGeometry { get; set; }

        /// <summary>DMX offsets, one per DMX break used by the DMX channels of the referenced geometry.</summary>
        public IList<GeometryBreak> Breaks { get; } = new List<GeometryBreak>();
    }

    /// <summary>The DMX offset of a <see cref="GeometryReference"/> for one DMX break (XML node <c>Break</c>).</summary>
    public sealed class GeometryBreak
    {
        /// <summary>DMX offset; 1 means no offset. Default: 1.</summary>
        public DmxAddress DmxOffset { get; set; } = new DmxAddress(1);

        /// <summary>The DMX break this offset applies to. Default: 1.</summary>
        public byte DmxBreak { get; set; } = 1;
    }
}
