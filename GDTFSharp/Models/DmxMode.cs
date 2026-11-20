using System.Collections.Generic;
using System.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Models
{
    /// <summary>
    /// The logical control of the device in one DMX mode (XML node <c>DMXMode</c>). Firmware revisions that change the
    /// DMX footprint should be separate modes.
    /// </summary>
    public sealed class DmxMode
    {
        public Name Name { get; set; }

        /// <summary>Description of the DMX mode.</summary>
        public string? Description { get; set; }

        /// <summary>The first geometry of the device in this mode; only top-level geometries are allowed.</summary>
        public Geometry? Geometry { get; set; }

        /// <summary>The DMX channels; adding one sets its <see cref="DmxChannel.Mode"/>.</summary>
        public ChildCollection<DmxMode, DmxChannel> Channels { get; }

        /// <summary>Dependencies between channels, such as a virtual dimmer multiplying color channels.</summary>
        public IList<Relation> Relations { get; } = new List<Relation>();

        /// <summary>Manufacturer macros: DMX sequences to be run by the control system.</summary>
        public IList<FTMacro> Macros { get; } = new List<FTMacro>();

        public DmxMode(Name name)
        {
            Name = name;
            Channels = new ChildCollection<DmxMode, DmxChannel>(this, c => c.Mode, (c, m) => c.Mode = m);
        }

        /// <summary>Finds a DMX channel by its name (<c>Geometry_Attribute</c>).</summary>
        public DmxChannel? FindChannel(string name) => Channels.FirstOrDefault(c => c.Name == name);

        /// <summary>Finds a channel function by its path <c>Channel.LogicalChannel.ChannelFunction</c>.</summary>
        public ChannelFunction? FindChannelFunction(NodePath path) =>
            Channels.SelectMany(c => c.ChannelFunctions).FirstOrDefault(f => f.Path == path);

        /// <summary>
        /// The channels as they are patched: channels of geometries in this mode's geometry tree once, and channels of
        /// referenced geometries once per <see cref="GeometryReference"/> in the tree, with the reference's DMX offsets
        /// applied. Channels with <see cref="DmxBreak.Overwrite"/> take break and offset from the reference's last
        /// <see cref="GeometryBreak"/>. Virtual channels (no offsets) are included with no addresses.
        /// </summary>
        public IReadOnlyList<DmxChannelInstance> GetChannelInstances()
        {
            var instances = new List<DmxChannelInstance>();
            if (Geometry == null) return instances;

            var tree = new HashSet<Geometry>(new[] { Geometry }.Concat(Geometry.Descendants()));
            foreach (var channel in Channels)
            {
                if (channel.Geometry != null && tree.Contains(channel.Geometry) && !channel.DmxBreak.IsOverwrite)
                    instances.Add(new DmxChannelInstance(channel, channel.Geometry, null, channel.DmxBreak.Number, channel.Offsets.ToList()));
            }

            foreach (var reference in tree.OfType<GeometryReference>().Where(r => r.ReferencedGeometry != null))
            {
                foreach (var channel in Channels.Where(c => c.Geometry != null && c.Geometry.Root == reference.ReferencedGeometry))
                {
                    GeometryBreak? geometryBreak;
                    int breakNumber;
                    if (channel.DmxBreak.IsOverwrite)
                    {
                        geometryBreak = reference.Breaks.LastOrDefault();
                        breakNumber = geometryBreak?.DmxBreak ?? 1;
                    }
                    else
                    {
                        breakNumber = channel.DmxBreak.Number;
                        geometryBreak = reference.Breaks.FirstOrDefault(b => b.DmxBreak == breakNumber);
                    }

                    var shift = (geometryBreak?.DmxOffset.Absolute ?? 1) - 1;
                    instances.Add(new DmxChannelInstance(channel, channel.Geometry!, reference, breakNumber,
                        channel.Offsets.Select(o => o + shift).ToList()));
                }
            }

            return instances;
        }

        /// <summary>Number of DMX addresses this mode uses per DMX break (the highest address used).</summary>
        public IReadOnlyDictionary<int, int> GetFootprint() =>
            GetChannelInstances()
                .Where(i => i.Addresses.Count > 0)
                .GroupBy(i => i.DmxBreak)
                .ToDictionary(g => g.Key, g => g.Max(i => i.Addresses.Max()));

        public override string ToString() => Name.Value;
    }

    /// <summary>A DMX channel patched for one geometry instance; see <see cref="DmxMode.GetChannelInstances"/>.</summary>
    public sealed class DmxChannelInstance
    {
        /// <summary>The channel definition.</summary>
        public DmxChannel Channel { get; }

        /// <summary>The geometry the channel controls (inside the referenced geometry for referenced instances).</summary>
        public Geometry Geometry { get; }

        /// <summary>The geometry reference this instance belongs to, or <c>null</c> for channels of the mode's own tree.</summary>
        public GeometryReference? Reference { get; }

        /// <summary>The DMX break the addresses are in.</summary>
        public int DmxBreak { get; }

        /// <summary>1-based DMX addresses within the break, from most to least significant byte.</summary>
        public IReadOnlyList<int> Addresses { get; }

        public DmxChannelInstance(DmxChannel channel, Geometry geometry, GeometryReference? reference, int dmxBreak, IReadOnlyList<int> addresses)
        {
            Channel = channel;
            Geometry = geometry;
            Reference = reference;
            DmxBreak = dmxBreak;
            Addresses = addresses;
        }

        public override string ToString() =>
            $"{Reference?.Name.Value ?? Geometry.Name.Value} {Channel.LogicalChannels.FirstOrDefault()?.Name} @ {DmxBreak}.{string.Join(",", Addresses)}";
    }

    /// <summary>Either a <see cref="DmxChannel"/> or a <see cref="ChannelFunction"/>: what a ModeMaster can link to.</summary>
    public interface IModeMaster
    {
    }

    /// <summary>
    /// A DMX channel (XML node <c>DMXChannel</c>). Its name is not user-defined: it is the geometry name and the attribute
    /// of the first logical channel, separated by "_", and unique in the DMX mode.
    /// </summary>
    public sealed class DmxChannel : IModeMaster
    {
        /// <summary>DMX break of the channel, or <see cref="DmxBreak.Overwrite"/> to take it from the geometry reference. Default: 1.</summary>
        public DmxBreak DmxBreak { get; set; } = DmxBreak.Default;

        /// <summary>
        /// Relative DMX addresses (1-based), from most to least significant byte; the count is the channel's resolution.
        /// Empty for a virtual channel.
        /// </summary>
        public IList<int> Offsets { get; } = new List<int>();

        /// <summary>The channel function activated by default; <c>null</c> means the first one (see <see cref="EffectiveInitialFunction"/>).</summary>
        public ChannelFunction? InitialFunction { get; set; }

        /// <summary>Highlight value, or <c>null</c> for none.</summary>
        public DmxValue? Highlight { get; set; }

        /// <summary>The geometry this channel controls.</summary>
        public Geometry? Geometry { get; set; }

        /// <summary>The mutually exclusive logical channels; adding one sets its <see cref="LogicalChannel.Channel"/>.</summary>
        public ChildCollection<DmxChannel, LogicalChannel> LogicalChannels { get; }

        /// <summary>The DMX mode; set when the channel is added to <see cref="DmxMode.Channels"/>.</summary>
        public DmxMode? Mode { get; internal set; }

        public DmxChannel(Geometry? geometry = null)
        {
            Geometry = geometry;
            LogicalChannels = new ChildCollection<DmxChannel, LogicalChannel>(this, l => l.Channel, (l, c) => l.Channel = c);
        }

        /// <summary><c>Geometry_Attribute</c> of the first logical channel.</summary>
        public string Name => $"{Geometry?.Name.Value}_{LogicalChannels.FirstOrDefault()?.Name}";

        /// <summary>True for a channel without offsets, i.e. without DMX addresses of its own.</summary>
        public bool IsVirtual => Offsets.Count == 0;

        /// <summary>
        /// Resolution in bytes: the number of offsets. Virtual channels use the largest byte count of their
        /// channel functions' DMXFrom values (at least 1).
        /// </summary>
        public int ByteCount => Offsets.Count > 0
            ? Offsets.Count
            : ChannelFunctions.Select(f => f.DmxFrom.ByteCount).DefaultIfEmpty(1).Max();

        /// <summary>The highest DMX value of the channel in its resolution, e.g. 255 or 65535.</summary>
        public ulong MaxDmxValue => DmxValue.MaxValueFor(ByteCount);

        /// <summary>All channel functions of all logical channels, in document order.</summary>
        public IEnumerable<ChannelFunction> ChannelFunctions => LogicalChannels.SelectMany(l => l.ChannelFunctions);

        /// <summary><see cref="InitialFunction"/> or the first channel function.</summary>
        public ChannelFunction? EffectiveInitialFunction => InitialFunction ?? ChannelFunctions.FirstOrDefault();

        public override string ToString() => Name;
    }

    /// <summary>Whether a logical channel jumps to new values instead of fading.</summary>
    public enum Snap
    {
        /// <summary>Fade between values. The default.</summary>
        No,
        /// <summary>Jump to new values.</summary>
        Yes,
        /// <summary>Jump to new values (same as <see cref="Yes"/>).</summary>
        On,
        /// <summary>Fade between values (same as <see cref="No"/>).</summary>
        Off
    }

    /// <summary>Which master control of the control system a logical channel reacts to.</summary>
    public enum LogicalChannelMaster
    {
        /// <summary>No master.</summary>
        None,
        /// <summary>Reacts to the grand master.</summary>
        Grand,
        /// <summary>Reacts to group masters.</summary>
        Group
    }

    /// <summary>
    /// A logical channel of a DMX channel (XML node <c>LogicalChannel</c>); logical channels of one DMX channel are mutually
    /// exclusive. Its name is the attribute name.
    /// </summary>
    public sealed class LogicalChannel
    {
        public GdtfAttribute? Attribute { get; set; }

        /// <summary>When snapping, values jump instead of fading. Default: <see cref="Models.Snap.No"/>.</summary>
        public Snap Snap { get; set; } = Snap.No;

        /// <summary>Whether the channel functions react to a grand or group master. Default: none.</summary>
        public LogicalChannelMaster Master { get; set; } = LogicalChannelMaster.None;

        /// <summary>Minimum fade time for moves in black, in seconds.</summary>
        public float MibFade { get; set; }

        /// <summary>Minimum fade time for changing DMX values, in seconds.</summary>
        public float DmxChangeTimeLimit { get; set; }

        /// <summary>The channel functions in DMX order; adding one sets its <see cref="ChannelFunction.LogicalChannel"/>.</summary>
        public ChildCollection<LogicalChannel, ChannelFunction> ChannelFunctions { get; }

        /// <summary>The DMX channel; set when the logical channel is added to <see cref="DmxChannel.LogicalChannels"/>.</summary>
        public DmxChannel? Channel { get; internal set; }

        public LogicalChannel(GdtfAttribute? attribute = null)
        {
            Attribute = attribute;
            ChannelFunctions = new ChildCollection<LogicalChannel, ChannelFunction>(this, f => f.LogicalChannel, (f, l) => f.LogicalChannel = l);
        }

        public string Name => Attribute?.Name.Value ?? ChannelFunction.NoFeature;

        public override string ToString() => Name;
    }

    /// <summary>The function of a DMX range of a logical channel (XML node <c>ChannelFunction</c>).</summary>
    public sealed class ChannelFunction : IModeMaster
    {
        /// <summary>The attribute name used when a channel function has no attribute.</summary>
        public const string NoFeature = "NoFeature";

        /// <summary>The name, or <c>null</c> for the default; see <see cref="EffectiveName"/>.</summary>
        public Name? Name { get; set; }

        /// <summary>The attribute; <c>null</c> means "NoFeature".</summary>
        public GdtfAttribute? Attribute { get; set; }

        /// <summary>The manufacturer's original name of the attribute.</summary>
        public string OriginalAttribute { get; set; } = string.Empty;

        /// <summary>Start DMX value. The end is the next function's start - 1 or the channel maximum (<see cref="DmxTo"/>). Default: 0/1.</summary>
        public DmxValue DmxFrom { get; set; } = new DmxValue(0, 1);

        private DmxValue? _default;

        /// <summary>DMX value set when the control system activates this function. Until it is set, it is <see cref="DmxFrom"/>.</summary>
        public DmxValue Default
        {
            get => _default ?? DmxFrom;
            set => _default = value;
        }

        /// <summary>Physical start value. Default: 0.</summary>
        public float PhysicalFrom { get; set; }

        /// <summary>Physical end value. Default: 1.</summary>
        public float PhysicalTo { get; set; } = 1f;

        /// <summary>Seconds to move from min to max. Default: 0.</summary>
        public float RealFade { get; set; }

        /// <summary>Seconds to accelerate from stop to maximum velocity. Default: 0.</summary>
        public float RealAcceleration { get; set; }

        /// <summary>The wheel the channel sets select slots of.</summary>
        public Wheel? Wheel { get; set; }
        /// <summary>The emitter this function controls (additive color mixing).</summary>
        public Emitter? Emitter { get; set; }
        /// <summary>The filter this function controls (subtractive color mixing).</summary>
        public Filter? Filter { get; set; }
        /// <summary>The color space for indirect color control.</summary>
        public ColorSpace? ColorSpace { get; set; }
        /// <summary>The gamut of the colors this function can produce.</summary>
        public Gamut? Gamut { get; set; }

        /// <summary>The DMX channel or channel function that has to be within <see cref="ModeFrom"/>..<see cref="ModeTo"/> for this function to be active.</summary>
        public IModeMaster? ModeMaster { get; set; }

        /// <summary>Start of the ModeMaster range. Default: 0/1.</summary>
        public DmxValue ModeFrom { get; set; } = new DmxValue(0, 1);

        /// <summary>End of the ModeMaster range. Default: 0/1.</summary>
        public DmxValue ModeTo { get; set; } = new DmxValue(0, 1);

        /// <summary>Nonlinear correlation between DMX value and physical output.</summary>
        public DmxProfile? DmxProfile { get; set; }

        /// <summary>Minimum physical value used for the DMX profile; <c>null</c> means <see cref="PhysicalFrom"/>.</summary>
        public float? Min { get; set; }

        /// <summary>Maximum physical value used for the DMX profile; <c>null</c> means <see cref="PhysicalTo"/>.</summary>
        public float? Max { get; set; }

        /// <summary>Name for command based protocols like OSC; <c>null</c> means the node path.</summary>
        public string? CustomName { get; set; }

        /// <summary>The channel sets in DMX order; adding one sets its <see cref="ChannelSet.ChannelFunction"/>.</summary>
        public ChildCollection<ChannelFunction, ChannelSet> ChannelSets { get; }

        /// <summary>Physical ranges of the attribute's sub physical units.</summary>
        public IList<SubChannelSet> SubChannelSets { get; } = new List<SubChannelSet>();

        /// <summary>The logical channel; set when the function is added to <see cref="LogicalChannel.ChannelFunctions"/>.</summary>
        public LogicalChannel? LogicalChannel { get; internal set; }

        public ChannelFunction(GdtfAttribute? attribute = null)
        {
            Attribute = attribute;
            ChannelSets = new ChildCollection<ChannelFunction, ChannelSet>(this, s => s.ChannelFunction, (s, f) => s.ChannelFunction = f);
        }

        /// <summary><see cref="Name"/>, or the default: the attribute name and the 1-based index in the logical channel, e.g. "Dimmer 1".</summary>
        public string EffectiveName =>
            Name?.Value is { Length: > 0 } name
                ? name
                : $"{Attribute?.Name.Value ?? NoFeature} {(LogicalChannel == null ? 1 : LogicalChannel.ChannelFunctions.IndexOf(this) + 1)}";

        /// <summary>The path used to reference this function from its DMX mode: <c>Channel.LogicalChannel.ChannelFunction</c>.</summary>
        public NodePath Path => NodePath.ParseUnchecked(
            $"{LogicalChannel?.Channel?.Name}.{LogicalChannel?.Name}.{EffectiveName}");

        /// <summary><see cref="Min"/> or <see cref="PhysicalFrom"/>.</summary>
        public float EffectiveMin => Min ?? PhysicalFrom;
        /// <summary><see cref="Max"/> or <see cref="PhysicalTo"/>.</summary>
        public float EffectiveMax => Max ?? PhysicalTo;

        /// <summary>
        /// The end DMX value in the channel's resolution: the start of the next channel function of the DMX channel - 1, or
        /// the channel maximum when there is no next function or it does not start later.
        /// </summary>
        public ulong DmxTo
        {
            get
            {
                var channel = LogicalChannel?.Channel;
                if (channel == null) return DmxValue.MaxValueFor(DmxFrom.ByteCount);
                var bytes = channel.ByteCount;
                var functions = channel.ChannelFunctions.ToList();
                var index = functions.IndexOf(this);
                if (index >= 0 && index + 1 < functions.Count)
                {
                    var next = functions[index + 1].DmxFrom.ToResolution(bytes);
                    if (next > DmxFrom.ToResolution(bytes)) return next - 1;
                }

                return channel.MaxDmxValue;
            }
        }

        public override string ToString() => EffectiveName;
    }

    /// <summary>A named DMX range of a channel function, e.g. a gobo or a "closed" position (XML node <c>ChannelSet</c>).</summary>
    public sealed class ChannelSet
    {
        public Name Name { get; set; }

        /// <summary>Start DMX value. Default: 0/1.</summary>
        public DmxValue DmxFrom { get; set; } = new DmxValue(0, 1);

        /// <summary>Physical start value; <c>null</c> means the channel function's.</summary>
        public float? PhysicalFrom { get; set; }

        /// <summary>Physical end value; <c>null</c> means the channel function's.</summary>
        public float? PhysicalTo { get; set; }

        /// <summary>1-based slot of the channel function's wheel; see <see cref="WheelSlot"/>.</summary>
        public int? WheelSlotIndex { get; set; }

        /// <summary>The channel function; set when the set is added to <see cref="ChannelFunction.ChannelSets"/>.</summary>
        public ChannelFunction? ChannelFunction { get; internal set; }

        public ChannelSet(Name name = default)
        {
            Name = name;
        }

        /// <summary><see cref="PhysicalFrom"/> or that of the channel function.</summary>
        public float EffectivePhysicalFrom => PhysicalFrom ?? ChannelFunction?.PhysicalFrom ?? 0f;
        /// <summary><see cref="PhysicalTo"/> or that of the channel function.</summary>
        public float EffectivePhysicalTo => PhysicalTo ?? ChannelFunction?.PhysicalTo ?? 1f;

        /// <summary>The wheel slot selected by <see cref="WheelSlotIndex"/> on the channel function's wheel.</summary>
        public WheelSlot? WheelSlot => WheelSlotIndex is { } index ? ChannelFunction?.Wheel?.GetSlot(index) : null;

        /// <summary>The end DMX value in the channel's resolution: the next set's start - 1, or the end of the channel function.</summary>
        public ulong DmxTo
        {
            get
            {
                var function = ChannelFunction;
                if (function == null) return DmxValue.MaxValueFor(DmxFrom.ByteCount);
                var bytes = function.LogicalChannel?.Channel?.ByteCount ?? DmxFrom.ByteCount;
                var index = function.ChannelSets.IndexOf(this);
                if (index + 1 < function.ChannelSets.Count)
                {
                    var next = function.ChannelSets[index + 1].DmxFrom.ToResolution(bytes);
                    if (next > DmxFrom.ToResolution(bytes)) return next - 1;
                }

                return function.DmxTo;
            }
        }

        public override string ToString() => Name.Value;
    }

    /// <summary>A physical range of a sub physical unit of the channel function's attribute (XML node <c>SubChannelSet</c>).</summary>
    public sealed class SubChannelSet
    {
        public Name Name { get; set; }

        public float? PhysicalFrom { get; set; }
        public float? PhysicalTo { get; set; }

        /// <summary>The sub physical unit, of the channel function's attribute.</summary>
        public SubPhysicalUnit? SubPhysicalUnit { get; set; }

        public DmxProfile? DmxProfile { get; set; }

        public SubChannelSet(Name name = default)
        {
            Name = name;
        }
    }

    /// <summary>How a <see cref="Relation"/> follower depends on its master.</summary>
    public enum RelationType
    {
        /// <summary>The follower is multiplied by the master, e.g. a virtual dimmer.</summary>
        Multiply,
        /// <summary>The master overrides the follower.</summary>
        Override
    }

    /// <summary>A dependency of a channel function on a master DMX channel (XML node <c>Relation</c>).</summary>
    public sealed class Relation
    {
        public Name Name { get; set; }

        /// <summary>The master DMX channel.</summary>
        public DmxChannel? Master { get; set; }

        /// <summary>The channel function that follows the master.</summary>
        public ChannelFunction? Follower { get; set; }

        public RelationType Type { get; set; }

        public Relation(Name name, RelationType type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString() => $"{Name}: {Master} {Type} {Follower}";
    }

    /// <summary>A manufacturer macro (XML node <c>FTMacro</c>).</summary>
    public sealed class FTMacro
    {
        public Name Name { get; set; }

        /// <summary>Optional channel function the macro belongs to.</summary>
        public ChannelFunction? ChannelFunction { get; set; }

        /// <summary>The DMX sequence (XML node <c>MacroDMX</c>), or <c>null</c>.</summary>
        public IList<MacroDmxStep>? DmxSteps { get; set; }

        public FTMacro(Name name)
        {
            Name = name;
        }

        public override string ToString() => Name.Value;
    }

    /// <summary>One step of a macro's DMX sequence (XML node <c>MacroDMXStep</c>).</summary>
    public sealed class MacroDmxStep
    {
        /// <summary>Duration of the step in seconds. Default: 1.</summary>
        public float Duration { get; set; } = 1f;

        /// <summary>The DMX channel values of the step.</summary>
        public IList<MacroDmxValue> Values { get; } = new List<MacroDmxValue>();
    }

    /// <summary>The value of one DMX channel in a macro step (XML node <c>MacroDMXValue</c>).</summary>
    public sealed class MacroDmxValue
    {
        public DmxValue Value { get; set; }

        /// <summary>The DMX channel the value is for.</summary>
        public DmxChannel? DmxChannel { get; set; }

        public MacroDmxValue(DmxValue value, DmxChannel? channel = null)
        {
            Value = value;
            DmxChannel = channel;
        }
    }
}
