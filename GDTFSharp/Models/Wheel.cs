using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>
    /// A physical or virtual wheel: color, gobo, prism, animation, media server content and others.
    /// Every wheel configuration of the real device has to be described.
    /// </summary>
    public sealed class Wheel
    {
        public Name Name { get; set; }

        /// <summary>The slots in wheel order. Channel sets link to a slot by its 1-based <see cref="WheelSlot.Index"/>.</summary>
        public ChildCollection<Wheel, WheelSlot> Slots { get; }

        public Wheel(Name name)
        {
            Name = name;
            Slots = new ChildCollection<Wheel, WheelSlot>(this, s => s.Wheel, (s, w) => s.Wheel = w);
        }

        /// <summary>The slot with the given 1-based wheel slot index, or <c>null</c>.</summary>
        public WheelSlot? GetSlot(int index) => index >= 1 && index <= Slots.Count ? Slots[index - 1] : null;

        public WheelSlot? FindSlot(Name name) => Slots.FirstOrDefault(s => s.Name == name);

        public override string ToString() => Name.Value;
    }

    /// <summary>A slot on a <see cref="Wheel"/> (XML node <c>Slot</c>).</summary>
    public sealed class WheelSlot
    {
        public Name Name { get; set; }

        /// <summary>
        /// Color of the slot; <c>null</c> means the spec default (white, see <see cref="EffectiveColor"/>).
        /// Luminance Y is relative to the luminous flux of the related beam. Do not set a color when a filter is used.
        /// </summary>
        public ColorCIE? Color { get; set; }

        /// <summary><see cref="Color"/> or the spec default white.</summary>
        public ColorCIE EffectiveColor => Color ?? ColorCIE.White;

        /// <summary>Optional filter from <see cref="PhysicalDescriptions.Filters"/>. Do not set a color when a filter is used.</summary>
        public Filter? Filter { get; set; }

        /// <summary>
        /// Optional PNG resource (file name without extension) in the archive's <c>wheels</c> folder, e.g. a gobo image.
        /// See <see cref="GdtfArchive.GetWheelMedia"/>.
        /// </summary>
        public string? MediaFileName { get; set; }

        /// <summary>Prism facets, for a prism slot.</summary>
        public IList<PrismFacet> Facets { get; } = new List<PrismFacet>();

        /// <summary>The animation system, for an animation wheel slot.</summary>
        public AnimationSystem? AnimationSystem { get; set; }

        /// <summary>The wheel the slot is on; set when it is added to <see cref="Models.Wheel.Slots"/>.</summary>
        public Wheel? Wheel { get; internal set; }

        /// <summary>1-based wheel slot index derived from the slot order, or 0 when the slot is not on a wheel.</summary>
        public int Index => Wheel == null ? 0 : Wheel.Slots.IndexOf(this) + 1;

        public WheelSlot(Name name)
        {
            Name = name;
        }

        public override string ToString() => Name.Value;
    }

    /// <summary>A facet of a prism slot (XML node <c>Facet</c>).</summary>
    public sealed class PrismFacet
    {
        /// <summary>Color of the facet; <c>null</c> means the spec default white.</summary>
        public ColorCIE? Color { get; set; }

        /// <summary>Rotation, translation and scaling of the facet.</summary>
        public RotationMatrix Rotation { get; set; }

        public PrismFacet(RotationMatrix rotation)
        {
            Rotation = rotation;
        }
    }

    /// <summary>The path of an animation system disk in the beam (XML node <c>AnimationSystem</c>).</summary>
    public sealed class AnimationSystem
    {
        /// <summary>First spline point, relative to the middle of the media file.</summary>
        public Vector2 P1 { get; set; }

        /// <summary>Second spline point, relative to the middle of the media file.</summary>
        public Vector2 P2 { get; set; }

        /// <summary>Third spline point, relative to the middle of the media file.</summary>
        public Vector2 P3 { get; set; }

        /// <summary>Radius of the circle that defines the section of the animation system shown in the beam.</summary>
        public float Radius { get; set; }
    }
}
