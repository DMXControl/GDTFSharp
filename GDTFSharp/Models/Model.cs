using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>Type of a <see cref="Model"/>, used when there is no model file or to pick a built-in mesh.</summary>
    public enum PrimitiveType
    {
        /// <summary>No primitive; use the model file.</summary>
        Undefined,
        /// <summary>Cube.</summary>
        Cube,
        /// <summary>Cylinder.</summary>
        Cylinder,
        /// <summary>Sphere.</summary>
        Sphere,
        /// <summary>Moving head base (GDTF 1.0 mesh).</summary>
        Base,
        /// <summary>Moving head yoke.</summary>
        Yoke,
        /// <summary>Moving head head.</summary>
        Head,
        /// <summary>Scanner (GDTF 1.0 mesh).</summary>
        Scanner,
        /// <summary>Conventional (GDTF 1.0 mesh).</summary>
        Conventional,
        /// <summary>Pigtail cable.</summary>
        Pigtail,
        /// <summary>Moving head base, GDTF 1.1 mesh with the origin at the mounting plate.</summary>
        Base1_1,
        /// <summary>Scanner, GDTF 1.1 mesh with the origin at the mounting plate.</summary>
        Scanner1_1,
        /// <summary>Conventional, GDTF 1.1 mesh with the origin at the mounting plate.</summary>
        Conventional1_1
    }

    /// <summary>
    /// Type and dimensions of the model of a fixture part (body, yoke, head …). The mesh is always scaled to
    /// <see cref="Length"/> (X), <see cref="Width"/> (Y) and <see cref="Height"/> (Z), whatever its own size.
    /// </summary>
    public sealed class Model
    {
        public Name Name { get; set; }

        /// <summary>Dimension on the X axis in meters.</summary>
        public float Length { get; set; }

        /// <summary>Dimension on the Y axis in meters.</summary>
        public float Width { get; set; }

        /// <summary>Dimension on the Z axis in meters.</summary>
        public float Height { get; set; }

        public PrimitiveType PrimitiveType { get; set; }

        /// <summary>
        /// Optional resource name (without extension and folder) of the model files: 3DS or GLB for 3D,
        /// SVG for 2D symbols, in the folders listed in <see cref="GdtfArchive.ModelSubfolders"/>.
        /// See <see cref="GdtfArchive.GetModelFile"/>.
        /// </summary>
        public string? File { get; set; }

        /// <summary>Insertion point of the top view SVG, from its 0,0 point, in SVG units.</summary>
        public float SvgOffsetX { get; set; }

        public float SvgOffsetY { get; set; }

        /// <summary>Insertion point of the side view SVG, from its 0,0 point, in SVG units.</summary>
        public float SvgSideOffsetX { get; set; }

        public float SvgSideOffsetY { get; set; }

        /// <summary>Insertion point of the front view SVG, from its 0,0 point, in SVG units.</summary>
        public float SvgFrontOffsetX { get; set; }

        public float SvgFrontOffsetY { get; set; }

        public Model(Name name)
        {
            Name = name;
        }

        public override string ToString() => Name.Value;
    }
}
