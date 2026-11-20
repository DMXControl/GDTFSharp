using System.Linq;
using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    public static partial class GdtfWriter
    {
        private static XElement WriteDmxModes(FixtureType fixtureType) =>
            new ElementWriter("DMXModes").Children(fixtureType.DmxModes.Select(WriteDmxMode));

        private static XElement WriteDmxMode(DmxMode mode)
        {
            var writer = new ElementWriter("DMXMode")
                .Name(mode.Name)
                .String("Description", mode.Description)
                .Name("Geometry", mode.Geometry?.Name)
                .Child(new ElementWriter("DMXChannels").Children(mode.Channels.Select(WriteDmxChannel)))
                .Child(new ElementWriter("Relations").Children(mode.Relations.Select(relation => (XElement)new ElementWriter("Relation")
                    .Name(relation.Name)
                    .String("Master", relation.Master?.Name)
                    .Node("Follower", relation.Follower?.Path)
                    .Enum("Type", relation.Type))))
                .Child(new ElementWriter("FTMacros").Children(mode.Macros.Select(WriteMacro)));
            return writer;
        }

        private static XElement WriteDmxChannel(DmxChannel channel) =>
            new ElementWriter("DMXChannel")
                .DmxBreak("DMXBreak", channel.DmxBreak)
                .IntList("Offset", channel.Offsets.ToList())
                .Node("InitialFunction", channel.InitialFunction?.Path)
                .DmxValue("Highlight", channel.Highlight)
                .Name("Geometry", channel.Geometry?.Name)
                .Children(channel.LogicalChannels.Select(logical => (XElement)new ElementWriter("LogicalChannel")
                    .String("Attribute", logical.Attribute?.Name.Value)
                    .Enum("Snap", logical.Snap)
                    .Enum("Master", logical.Master)
                    .Float("MibFade", logical.MibFade)
                    .Float("DMXChangeTimeLimit", logical.DmxChangeTimeLimit)
                    .Children(logical.ChannelFunctions.Select(WriteChannelFunction))));

        private static XElement WriteChannelFunction(ChannelFunction function)
        {
            var writer = new ElementWriter("ChannelFunction")
                .Name("Name", function.Name)
                .String("Attribute", function.Attribute?.Name.Value)
                .String("OriginalAttribute", function.OriginalAttribute)
                .DmxValue("DMXFrom", function.DmxFrom)
                .DmxValue("Default", function.Default)
                .Float("PhysicalFrom", function.PhysicalFrom)
                .Float("PhysicalTo", function.PhysicalTo)
                .Float("RealFade", function.RealFade)
                .Float("RealAcceleration", function.RealAcceleration)
                .Name("Wheel", function.Wheel?.Name)
                .Name("Emitter", function.Emitter?.Name)
                .Name("Filter", function.Filter?.Name)
                .Name("ColorSpace", function.ColorSpace?.Name)
                .Name("Gamut", function.Gamut?.Name);

            if (function.ModeMaster != null)
            {
                writer
                    .String("ModeMaster", function.ModeMaster switch
                    {
                        DmxChannel channel => channel.Name,
                        ChannelFunction master => master.Path.ToString(),
                        _ => null
                    })
                    .DmxValue("ModeFrom", function.ModeFrom)
                    .DmxValue("ModeTo", function.ModeTo);
            }

            return writer
                .Name("DMXProfile", function.DmxProfile?.Name)
                .Float("Min", function.Min)
                .Float("Max", function.Max)
                .String("CustomName", function.CustomName)
                .Children(function.ChannelSets.Select(set => (XElement)new ElementWriter("ChannelSet")
                    .Name("Name", set.Name.IsEmpty ? (Name?)null : set.Name)
                    .DmxValue("DMXFrom", set.DmxFrom)
                    .Float("PhysicalFrom", set.PhysicalFrom)
                    .Float("PhysicalTo", set.PhysicalTo)
                    .Int("WheelSlotIndex", set.WheelSlotIndex)))
                .Children(function.SubChannelSets.Select(sub => (XElement)new ElementWriter("SubChannelSet")
                    .Name("Name", sub.Name.IsEmpty ? (Name?)null : sub.Name)
                    .Float("PhysicalFrom", sub.PhysicalFrom)
                    .Float("PhysicalTo", sub.PhysicalTo)
                    .String("SubPhysicalUnit", sub.SubPhysicalUnit == null ? null : GdtfFormat.FormatEnum(sub.SubPhysicalUnit.Type))
                    .Name("DMXProfile", sub.DmxProfile?.Name)));
        }

        private static XElement WriteMacro(FTMacro macro)
        {
            var writer = new ElementWriter("FTMacro")
                .Name(macro.Name)
                .Node("ChannelFunction", macro.ChannelFunction?.Path);
            if (macro.DmxSteps != null)
            {
                writer.Child(new ElementWriter("MacroDMX").Children(macro.DmxSteps.Select(step => (XElement)new ElementWriter("MacroDMXStep")
                    .Float("Duration", step.Duration)
                    .Children(step.Values.Select(value => (XElement)new ElementWriter("MacroDMXValue")
                        .DmxValue("Value", value.Value)
                        .String("DMXChannel", value.DmxChannel?.Name))))));
            }

            return writer;
        }
    }
}
