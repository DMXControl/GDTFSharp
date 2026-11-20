using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    // Wheels, Models, Revisions, FTPresets and Protocols.
    public static partial class GdtfReader
    {
        private static void ReadWheels(ElementReader reader, FixtureType fixtureType)
        {
            var wheelIndex = 0;
            foreach (var wheelReader in reader.Children("Wheel"))
            {
                var wheel = new Wheel(ReadName(wheelReader, "Wheel", ++wheelIndex));
                var slotIndex = 0;
                foreach (var slotReader in wheelReader.Children("Slot"))
                {
                    wheel.Slots.Add(ReadSlot(slotReader, ++slotIndex));
                }

                wheelReader.ReportUnknown();
                reader.Context.Register(GdtfScopes.Wheels, new NodePath(wheel.Name), wheel, wheelReader.Element);
                fixtureType.Wheels.Add(wheel);
            }

            reader.ReportUnknown();
        }

        private static WheelSlot ReadSlot(ElementReader reader, int index)
        {
            var mediaFileName = reader.String("MediaFileName");
            var slot = new WheelSlot(ReadName(reader, "Slot", index))
            {
                Color = reader.OptionalColor("Color"),
                MediaFileName = string.IsNullOrEmpty(mediaFileName) ? null : mediaFileName
            };
            reader.Reference("Filter", GdtfScopes.Filters, f => slot.Filter = f);
            if (slot.Color != null && reader.Has("Filter"))
                reader.Context.Warning("A slot with a Filter should not define a Color.", reader.Element);

            foreach (var facetReader in reader.Children("Facet"))
            {
                slot.Facets.Add(new PrismFacet(facetReader.Rotation("Rotation", required: true))
                {
                    Color = facetReader.OptionalColor("Color")
                });
                facetReader.ReportUnknown();
            }

            var animation = reader.Child("AnimationSystem");
            if (animation != null)
            {
                slot.AnimationSystem = new AnimationSystem
                {
                    P1 = animation.Vector2("P1", required: true),
                    P2 = animation.Vector2("P2", required: true),
                    P3 = animation.Vector2("P3", required: true),
                    Radius = animation.Float("Radius", required: true)
                };
                animation.ReportUnknown();
                if (slot.Facets.Count > 0)
                    reader.Context.Warning("A slot can have prism facets or an animation system, not both.", reader.Element);
            }

            reader.ReportUnknown();
            return slot;
        }

        private static void ReadModels(ElementReader reader, FixtureType fixtureType)
        {
            var index = 0;
            foreach (var modelReader in reader.Children("Model"))
            {
                var file = modelReader.String("File");
                var model = new Model(ReadName(modelReader, "Model", ++index))
                {
                    Length = modelReader.Float("Length"),
                    Width = modelReader.Float("Width"),
                    Height = modelReader.Float("Height"),
                    PrimitiveType = modelReader.Enum("PrimitiveType", PrimitiveType.Undefined),
                    File = string.IsNullOrEmpty(file) ? null : file,
                    SvgOffsetX = modelReader.Float("SVGOffsetX"),
                    SvgOffsetY = modelReader.Float("SVGOffsetY"),
                    SvgSideOffsetX = modelReader.Float("SVGSideOffsetX"),
                    SvgSideOffsetY = modelReader.Float("SVGSideOffsetY"),
                    SvgFrontOffsetX = modelReader.Float("SVGFrontOffsetX"),
                    SvgFrontOffsetY = modelReader.Float("SVGFrontOffsetY")
                };
                modelReader.ReportUnknown();
                reader.Context.Register(GdtfScopes.Models, new NodePath(model.Name), model, modelReader.Element);
                fixtureType.Models.Add(model);
            }

            reader.ReportUnknown();
        }

        private static void ReadRevisions(ElementReader reader, FixtureType fixtureType)
        {
            foreach (var revisionReader in reader.Children("Revision"))
            {
                fixtureType.Revisions.Add(new Revision
                {
                    Text = revisionReader.String("Text", string.Empty),
                    Date = revisionReader.Date("Date"),
                    UserId = revisionReader.UInt("UserID"),
                    ModifiedBy = revisionReader.String("ModifiedBy", string.Empty)
                });
                revisionReader.ReportUnknown();
            }

            reader.ReportUnknown();
        }

        private static void ReadFTPresets(ElementReader reader, FixtureType fixtureType)
        {
            // The spec has not defined FTPreset yet; keep them verbatim.
            foreach (var preset in reader.Children("FTPreset")) fixtureType.FTPresets.Add(new XElement(preset.Element));
            reader.ReportUnknown();
        }

        private static void ReadProtocols(ElementReader reader, Protocols protocols)
        {
            var rdm = reader.Child("FTRDM");
            if (rdm != null)
            {
                protocols.Rdm = new RdmProtocol
                {
                    ManufacturerId = rdm.Hex("ManufacturerID"),
                    DeviceModelId = rdm.Hex("DeviceModelID")
                };
                foreach (var versionReader in rdm.Children("SoftwareVersionID"))
                {
                    var version = new RdmSoftwareVersion { Value = versionReader.Hex("Value") };
                    foreach (var personalityReader in versionReader.Children("DMXPersonality"))
                    {
                        var personality = new DmxPersonality { Value = personalityReader.Hex("Value") };
                        personalityReader.Reference("DMXMode", GdtfScopes.DmxModes, m => personality.DmxMode = m);
                        version.DmxPersonalities.Add(personality);
                        personalityReader.ReportUnknown();
                    }

                    versionReader.ReportUnknown();
                    protocols.Rdm.SoftwareVersions.Add(version);
                }

                rdm.ReportUnknown();
            }

            protocols.ArtNet = ReadValueMappedProtocol(reader.Child("Art-Net"));
            protocols.Sacn = ReadValueMappedProtocol(reader.Child("sACN"));

            // Everything else is kept verbatim: PosiStageNet, OpenSoundControl and CITP are not defined by the spec yet.
            foreach (var other in reader.Element.Elements())
            {
                var name = other.Name.LocalName;
                if (name == "FTRDM" || name == "Art-Net" || name == "sACN") continue;
                if (name != "PosiStageNet" && name != "OpenSoundControl" && name != "CITP")
                    reader.Context.Info($"Unknown protocol '{name}' is kept as is.", other);
                protocols.Other.Add(new XElement(other));
            }

            reader.ReportUnknownAttributes();
        }

        private static ValueMappedProtocol? ReadValueMappedProtocol(ElementReader? reader)
        {
            if (reader == null) return null;
            var protocol = new ValueMappedProtocol();
            foreach (var mapReader in reader.Children("Map"))
            {
                protocol.Maps.Add(new ValueMap(mapReader.UInt("Key", required: true), mapReader.UInt("Value", required: true)));
                mapReader.ReportUnknown();
            }

            reader.ReportUnknown();
            return protocol;
        }
    }
}
