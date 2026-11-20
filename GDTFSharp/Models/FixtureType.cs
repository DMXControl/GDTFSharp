using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>The <c>FixtureType</c> node: the starting point of the description of a fixture type.</summary>
    public sealed class FixtureType
    {
        /// <summary>Child sections in the order the spec mandates (DIN SPEC 15800, Table 4).</summary>
        public static readonly IReadOnlyList<string> SectionOrder = new[]
        {
            "AttributeDefinitions", "Wheels", "PhysicalDescriptions", "Models", "Geometries", "DMXModes", "Revisions",
            "FTPresets", "Protocols"
        };

        /// <summary>Sections that must be present, even when empty.</summary>
        public static readonly IReadOnlyList<string> MandatorySections = new[] { "AttributeDefinitions", "Geometries", "DMXModes" };

        public Name Name { get; set; }

        /// <summary>Shortened name, e.g. an abbreviation. Any characters.</summary>
        public string ShortName { get; set; } = string.Empty;

        /// <summary>Detailed, complete name. Any characters.</summary>
        public string LongName { get; set; } = string.Empty;

        public string Manufacturer { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Guid FixtureTypeId { get; set; }

        /// <summary>Optional resource name (without extension) of <c>thumbnail.png</c>/<c>.svg</c> in the archive root.</summary>
        public string? Thumbnail { get; set; }

        /// <summary>Horizontal offset in pixels from the top left of the viewbox to the insertion point on a label.</summary>
        public int ThumbnailOffsetX { get; set; }

        /// <summary>Vertical offset in pixels from the top left of the viewbox to the insertion point on a label.</summary>
        public int ThumbnailOffsetY { get; set; }

        /// <summary>Optional GUID of the referenced fixture type.</summary>
        public Guid? RefFT { get; set; }

        /// <summary>Whether other devices can be mounted to this device. Default: true.</summary>
        public bool CanHaveChildren { get; set; } = true;

        /// <summary>Activation groups, feature groups and Fixture Type Attributes.</summary>
        public AttributeDefinitions AttributeDefinitions { get; } = new AttributeDefinitions();

        /// <summary>Physical or virtual wheels: color, gobo, prism, animation, media server content and others.</summary>
        public IList<Wheel> Wheels { get; } = new List<Wheel>();

        /// <summary>Emitters, filters, color spaces, gamuts, DMX profiles, CRIs, connectors and device properties.</summary>
        public PhysicalDescriptions PhysicalDescriptions { get; } = new PhysicalDescriptions();

        /// <summary>Models of the physically separated parts of the device.</summary>
        public IList<Model> Models { get; } = new List<Model>();

        /// <summary>The history of the fixture type.</summary>
        public IList<Revision> Revisions { get; } = new List<Revision>();

        /// <summary>
        /// Fixture type specific presets (XML node <c>FTPreset</c>), kept verbatim because the spec has not defined
        /// their content yet.
        /// </summary>
        public IList<XElement> FTPresets { get; } = new List<XElement>();

        /// <summary>Supported protocols besides DMX.</summary>
        public Protocols Protocols { get; } = new Protocols();

        public Wheel? FindWheel(Name name) => Wheels.FirstOrDefault(w => w.Name == name);

        public Model? FindModel(Name name) => Models.FirstOrDefault(m => m.Name == name);

        /// <summary>
        /// Child sections this version of the library does not model yet, kept verbatim so reading and writing a file
        /// does not lose them. A section moves out of this list once it is implemented. The writer throws if this list
        /// contains a section that is modelled.
        /// </summary>
        public IList<XElement> UnparsedSections { get; } = new List<XElement>();

        public FixtureType(Name name, Guid fixtureTypeId)
        {
            Name = name;
            FixtureTypeId = fixtureTypeId;
        }
    }
}
