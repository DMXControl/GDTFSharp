using System.Linq;
using System.Xml.Linq;
using GDTFSharp.Models;

namespace GDTFSharp.Serialization
{
    // Wheels, Models, Revisions, FTPresets and Protocols.
    public static partial class GdtfWriter
    {
        private static XElement WriteWheels(FixtureType fixtureType) =>
            new ElementWriter("Wheels").Children(fixtureType.Wheels.Select(wheel =>
                (XElement)new ElementWriter("Wheel").Name(wheel.Name).Children(wheel.Slots.Select(WriteSlot))));

        private static XElement WriteSlot(WheelSlot slot)
        {
            var writer = new ElementWriter("Slot")
                .Name(slot.Name)
                .Color("Color", slot.Color)
                .Name("Filter", slot.Filter?.Name)
                .String("MediaFileName", slot.MediaFileName)
                .Children(slot.Facets.Select(facet => (XElement)new ElementWriter("Facet")
                    .Color("Color", facet.Color)
                    .Rotation("Rotation", facet.Rotation)));

            if (slot.AnimationSystem is { } animation)
            {
                writer.Child(new ElementWriter("AnimationSystem")
                    .Vector2("P1", animation.P1)
                    .Vector2("P2", animation.P2)
                    .Vector2("P3", animation.P3)
                    .Float("Radius", animation.Radius));
            }

            return writer;
        }

        private static XElement WriteModels(FixtureType fixtureType) =>
            new ElementWriter("Models").Children(fixtureType.Models.Select(model => (XElement)new ElementWriter("Model")
                .Name(model.Name)
                .Float("Length", model.Length)
                .Float("Width", model.Width)
                .Float("Height", model.Height)
                .Enum("PrimitiveType", model.PrimitiveType)
                .String("File", model.File)
                .Float("SVGOffsetX", model.SvgOffsetX)
                .Float("SVGOffsetY", model.SvgOffsetY)
                .Float("SVGSideOffsetX", model.SvgSideOffsetX)
                .Float("SVGSideOffsetY", model.SvgSideOffsetY)
                .Float("SVGFrontOffsetX", model.SvgFrontOffsetX)
                .Float("SVGFrontOffsetY", model.SvgFrontOffsetY)));

        private static XElement WriteRevisions(FixtureType fixtureType) =>
            new ElementWriter("Revisions").Children(fixtureType.Revisions.Select(revision => (XElement)new ElementWriter("Revision")
                .String("Text", revision.Text)
                .Date("Date", revision.Date)
                .Int("UserID", revision.UserId)
                .String("ModifiedBy", revision.ModifiedBy)));

        private static XElement WriteFTPresets(FixtureType fixtureType) =>
            new ElementWriter("FTPresets").Children(fixtureType.FTPresets.Select(p => new XElement(p)));

        private static XElement WriteProtocols(Protocols protocols)
        {
            var writer = new ElementWriter("Protocols");
            if (protocols.Rdm is { } rdm)
            {
                writer.Child(new ElementWriter("FTRDM")
                    .Hex("ManufacturerID", rdm.ManufacturerId)
                    .Hex("DeviceModelID", rdm.DeviceModelId)
                    .Children(rdm.SoftwareVersions.Select(version => (XElement)new ElementWriter("SoftwareVersionID")
                        .Hex("Value", version.Value)
                        .Children(version.DmxPersonalities.Select(personality => (XElement)new ElementWriter("DMXPersonality")
                            .Hex("Value", personality.Value)
                            .Name("DMXMode", personality.DmxMode?.Name))))));
            }

            writer.Child(WriteValueMappedProtocol("Art-Net", protocols.ArtNet));
            writer.Child(WriteValueMappedProtocol("sACN", protocols.Sacn));
            return writer.Children(protocols.Other.Select(o => new XElement(o)));
        }

        private static XElement? WriteValueMappedProtocol(string name, ValueMappedProtocol? protocol) =>
            protocol == null
                ? null
                : (XElement)new ElementWriter(name).Children(protocol.Maps.Select(map => (XElement)new ElementWriter("Map")
                    .Int("Key", map.Key)
                    .Int("Value", map.Value)));
    }
}
