using System.Collections.Generic;
using System.Xml.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>Protocol specific information for protocols the device supports besides DMX (XML node <c>Protocols</c>).</summary>
    public sealed class Protocols
    {
        /// <summary>RDM information (XML node <c>FTRDM</c>), or <c>null</c> if the device does not support RDM.</summary>
        public RdmProtocol? Rdm { get; set; }

        /// <summary>Art-Net value mapping, or <c>null</c> if the device does not support Art-Net.</summary>
        public ValueMappedProtocol? ArtNet { get; set; }

        /// <summary>Streaming ACN value mapping, or <c>null</c> if the device does not support sACN.</summary>
        public ValueMappedProtocol? Sacn { get; set; }

        /// <summary>
        /// Protocol elements whose content the spec has not defined yet (<c>PosiStageNet</c>, <c>OpenSoundControl</c>,
        /// <c>CITP</c>) and unknown ones, kept verbatim.
        /// </summary>
        public IList<XElement> Other { get; } = new List<XElement>();
    }

    /// <summary>RDM information of the fixture type (XML node <c>FTRDM</c>).</summary>
    public sealed class RdmProtocol
    {
        /// <summary>Manufacturer ESTA ID.</summary>
        public ulong ManufacturerId { get; set; }

        /// <summary>Unique device model ID.</summary>
        public ulong DeviceModelId { get; set; }

        public IList<RdmSoftwareVersion> SoftwareVersions { get; } = new List<RdmSoftwareVersion>();
    }

    /// <summary>A supported software version (XML node <c>SoftwareVersionID</c>).</summary>
    public sealed class RdmSoftwareVersion
    {
        public ulong Value { get; set; }

        public IList<DmxPersonality> DmxPersonalities { get; } = new List<DmxPersonality>();
    }

    /// <summary>An RDM DMX personality of a software version, linked to a DMX mode.</summary>
    public sealed class DmxPersonality
    {
        public ulong Value { get; set; }

        /// <summary>
        /// Name of the DMX mode that can be used with this software version, as written in the file.
        /// Kept as a name until DMX modes are modelled; it will then become a reference to the mode.
        /// </summary>
        public Name DmxModeName { get; set; }
    }

    /// <summary>A protocol whose values map 1:1 to DMX values unless custom maps are given (Art-Net, sACN).</summary>
    public sealed class ValueMappedProtocol
    {
        /// <summary>Custom mappings from protocol values to DMX values; empty means all values map 1:1.</summary>
        public IList<ValueMap> Maps { get; } = new List<ValueMap>();
    }

    /// <summary>A custom mapping of one protocol value (<see cref="Key"/>) to a DMX value (<see cref="Value"/>).</summary>
    public sealed class ValueMap
    {
        public uint Key { get; set; }
        public uint Value { get; set; }

        public ValueMap(uint key, uint value)
        {
            Key = key;
            Value = value;
        }
    }
}
