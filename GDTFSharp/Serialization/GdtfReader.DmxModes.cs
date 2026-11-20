using System.Collections.Generic;
using System.Linq;
using GDTFSharp.Models;
using GDTFSharp.Types;

namespace GDTFSharp.Serialization
{
    public static partial class GdtfReader
    {
        private static void ReadDmxModes(ElementReader reader, FixtureType fixtureType)
        {
            var index = 0;
            foreach (var modeReader in reader.Children("DMXMode"))
            {
                var mode = ReadDmxMode(modeReader, ++index);
                reader.Context.Register(GdtfScopes.DmxModes, new NodePath(mode.Name), mode, modeReader.Element);
                fixtureType.DmxModes.Add(mode);
            }

            reader.ReportUnknown();
        }

        private static DmxMode ReadDmxMode(ElementReader reader, int index)
        {
            var context = reader.Context;
            var mode = new DmxMode(ReadName(reader, "DMXMode", index)) { Description = reader.String("Description") };
            reader.Reference("Geometry", GdtfScopes.TopLevelGeometries, g => mode.Geometry = g, required: true);

            // DMX channels, logical channels and channel functions are referenced by paths starting at the DMX mode.
            var scope = new ReferenceScope<object>($"DMXMode '{mode.Name}'");

            var channels = reader.Child("DMXChannels");
            if (channels == null) context.Warning("Mandatory child 'DMXChannels' is missing.", reader.Element);
            else
            {
                foreach (var channelReader in channels.Children("DMXChannel"))
                    mode.Channels.Add(ReadDmxChannel(channelReader, scope));
                channels.ReportUnknown();
            }

            var relations = reader.Child("Relations");
            if (relations != null)
            {
                var relationIndex = 0;
                foreach (var relationReader in relations.Children("Relation"))
                {
                    var relation = new Relation(ReadName(relationReader, "Relation", ++relationIndex),
                        relationReader.Enum("Type", RelationType.Multiply, required: true));
                    ResolveInMode<DmxChannel>(relationReader, "Master", scope, c => relation.Master = c, "DMX channel", required: true);
                    ResolveInMode<ChannelFunction>(relationReader, "Follower", scope, f => relation.Follower = f, "channel function", required: true);
                    relationReader.ReportUnknown();
                    mode.Relations.Add(relation);
                }

                relations.ReportUnknown();
            }

            var macros = reader.Child("FTMacros");
            if (macros != null)
            {
                var macroIndex = 0;
                foreach (var macroReader in macros.Children("FTMacro"))
                {
                    var macro = new FTMacro(ReadName(macroReader, "FTMacro", ++macroIndex));
                    ResolveInMode<ChannelFunction>(macroReader, "ChannelFunction", scope, f => macro.ChannelFunction = f, "channel function");
                    var dmx = macroReader.Child("MacroDMX");
                    if (dmx != null)
                    {
                        macro.DmxSteps = new List<MacroDmxStep>();
                        foreach (var stepReader in dmx.Children("MacroDMXStep"))
                        {
                            var step = new MacroDmxStep { Duration = stepReader.Float("Duration", 1f) };
                            foreach (var valueReader in stepReader.Children("MacroDMXValue"))
                            {
                                var value = new MacroDmxValue(valueReader.DmxValue("Value", new DmxValue(0, 1), required: true));
                                ResolveInMode<DmxChannel>(valueReader, "DMXChannel", scope, c => value.DmxChannel = c, "DMX channel", required: true);
                                valueReader.ReportUnknown();
                                step.Values.Add(value);
                            }

                            stepReader.ReportUnknown();
                            macro.DmxSteps.Add(step);
                        }

                        dmx.ReportUnknown();
                    }

                    macroReader.ReportUnknown();
                    mode.Macros.Add(macro);
                }

                macros.ReportUnknown();
            }

            reader.ReportUnknown();
            return mode;
        }

        private static DmxChannel ReadDmxChannel(ElementReader reader, ReferenceScope<object> scope)
        {
            var context = reader.Context;
            var channel = new DmxChannel
            {
                DmxBreak = reader.DmxBreak("DMXBreak", DmxBreak.Default),
                Highlight = reader.OptionalDmxValue("Highlight")
            };
            foreach (var offset in reader.IntList("Offset")) channel.Offsets.Add(offset);
            reader.Reference("Geometry", GdtfScopes.Geometries, g => channel.Geometry = g, required: true);

            // GDTF 1.0 had the default on the DMX channel; 1.1 moved it to the channel functions.
            var legacyDefault = reader.OptionalDmxValue("Default");
            var hasInitialFunction = reader.Has("InitialFunction");
            ResolveInMode<ChannelFunction>(reader, "InitialFunction", scope, f => channel.InitialFunction = f, "channel function");

            // Paths are built from the raw XML values, so registration does not depend on resolved references.
            var logicalReaders = reader.Children("LogicalChannel").ToList();
            var channelName = $"{reader.Element.Attribute("Geometry")?.Value}_{logicalReaders.FirstOrDefault()?.Element.Attribute("Attribute")?.Value ?? ChannelFunction.NoFeature}";
            context.Register(scope, new NodePath(Name.Unchecked(channelName)), channel, reader.Element);

            var functionsWithoutDefault = new List<ChannelFunction>();
            foreach (var logicalReader in logicalReaders)
            {
                var logical = new LogicalChannel
                {
                    Snap = logicalReader.Enum("Snap", Snap.No),
                    Master = logicalReader.Enum("Master", LogicalChannelMaster.None),
                    MibFade = logicalReader.Float("MibFade"),
                    DmxChangeTimeLimit = logicalReader.Float("DMXChangeTimeLimit")
                };
                logicalReader.Reference("Attribute", GdtfScopes.Attributes, a => logical.Attribute = a, required: true);
                var logicalName = logicalReader.Element.Attribute("Attribute")?.Value ?? ChannelFunction.NoFeature;
                var logicalPath = new NodePath(Name.Unchecked(channelName), Name.Unchecked(logicalName));
                context.Register(scope, logicalPath, logical, logicalReader.Element);

                var functionIndex = 0;
                foreach (var functionReader in logicalReader.Children("ChannelFunction"))
                {
                    functionIndex++;
                    var function = ReadChannelFunction(functionReader, scope, out var hasDefault);
                    if (!hasDefault) functionsWithoutDefault.Add(function);
                    logical.ChannelFunctions.Add(function);

                    var rawName = functionReader.Element.Attribute("Name")?.Value;
                    var functionName = string.IsNullOrEmpty(rawName)
                        ? $"{functionReader.Element.Attribute("Attribute")?.Value ?? ChannelFunction.NoFeature} {functionIndex}"
                        : rawName!;
                    context.Register(scope, logicalPath.Append(Name.Unchecked(functionName)), function, functionReader.Element);
                }

                logicalReader.ReportUnknown();
                channel.LogicalChannels.Add(logical);
            }

            ApplyDefaults(channel, functionsWithoutDefault, legacyDefault, hasInitialFunction, reader);
            reader.ReportUnknown();
            return channel;
        }

        /// <summary>
        /// Gives channel functions without a <c>Default</c> their default value. For a GDTF 1.0 channel <c>Default</c>, the
        /// function whose DMX range contains it gets that value and becomes the initial function; the others start at their
        /// DMXFrom. Without a channel default every function defaults to its DMXFrom.
        /// </summary>
        private static void ApplyDefaults(DmxChannel channel, List<ChannelFunction> functionsWithoutDefault, DmxValue? legacyDefault,
            bool hasInitialFunction, ElementReader reader)
        {
            foreach (var function in functionsWithoutDefault) function.Default = function.DmxFrom;
            if (legacyDefault == null) return;

            if (reader.Context.DataVersion >= DataVersion.V1_1)
                reader.Context.Warning("DMXChannel 'Default' is GDTF 1.0; it is moved to the channel functions.", reader.Element);

            var value = legacyDefault.Value.ToResolution(channel.ByteCount);
            var target = channel.ChannelFunctions.FirstOrDefault(f => f.DmxFrom.ToResolution(channel.ByteCount) <= value && value <= f.DmxTo);
            if (target == null) return;
            if (functionsWithoutDefault.Contains(target)) target.Default = legacyDefault.Value;
            if (!hasInitialFunction && target != channel.ChannelFunctions.First()) channel.InitialFunction = target;
        }

        private static ChannelFunction ReadChannelFunction(ElementReader reader, ReferenceScope<object> scope, out bool hasDefault)
        {
            var context = reader.Context;
            var function = new ChannelFunction
            {
                Name = ReadChannelFunctionName(reader),
                OriginalAttribute = reader.String("OriginalAttribute", string.Empty),
                DmxFrom = reader.DmxValue("DMXFrom", new DmxValue(0, 1)),
                PhysicalFrom = reader.Float("PhysicalFrom", 0f),
                PhysicalTo = reader.Float("PhysicalTo", 1f),
                RealFade = reader.Float("RealFade"),
                RealAcceleration = reader.Float("RealAcceleration"),
                ModeFrom = reader.DmxValue("ModeFrom", new DmxValue(0, 1)),
                ModeTo = reader.DmxValue("ModeTo", new DmxValue(0, 1)),
                Min = OptionalFloat(reader, "Min"),
                Max = OptionalFloat(reader, "Max"),
                CustomName = reader.String("CustomName")
            };

            hasDefault = reader.Has("Default");
            if (hasDefault) function.Default = reader.DmxValue("Default", function.DmxFrom);

            // "NoFeature" is the spec default and need not be defined as an attribute.
            var attributeValue = reader.Element.Attribute("Attribute")?.Value;
            if (attributeValue == ChannelFunction.NoFeature)
            {
                reader.String("Attribute");
                context.AfterResolve(() =>
                {
                    if (context.TryLookup(GdtfScopes.Attributes, new NodePath(Name.Unchecked(ChannelFunction.NoFeature)), out var noFeature))
                        function.Attribute = noFeature;
                });
            }
            else
            {
                reader.Reference("Attribute", GdtfScopes.Attributes, a => function.Attribute = a);
            }

            reader.Reference("Wheel", GdtfScopes.Wheels, w => function.Wheel = w);
            reader.Reference("Emitter", GdtfScopes.Emitters, e => function.Emitter = e);
            reader.Reference("Filter", GdtfScopes.Filters, f => function.Filter = f);
            reader.Reference("ColorSpace", GdtfScopes.ColorSpaces, c => function.ColorSpace = c);
            // The official XSD spells it "Gammut".
            reader.Reference(reader.Has("Gamut") || !reader.Has("Gammut") ? "Gamut" : "Gammut", GdtfScopes.Gamuts, g => function.Gamut = g);
            reader.Reference("DMXProfile", GdtfScopes.DmxProfiles, p => function.DmxProfile = p);

            var modeMaster = reader.Element.Attribute("ModeMaster");
            reader.Reference("ModeMaster", scope, target =>
            {
                if (target is IModeMaster master) function.ModeMaster = master;
                else context.Error("ModeMaster must link to a DMX channel or a channel function.", modeMaster);
            });

            var setIndex = 0;
            foreach (var setReader in reader.Children("ChannelSet"))
            {
                setIndex++;
                function.ChannelSets.Add(new ChannelSet(setReader.Name("Name", default))
                {
                    DmxFrom = setReader.DmxValue("DMXFrom", new DmxValue(0, 1)),
                    PhysicalFrom = OptionalFloat(setReader, "PhysicalFrom"),
                    PhysicalTo = OptionalFloat(setReader, "PhysicalTo"),
                    WheelSlotIndex = OptionalInt(setReader, "WheelSlotIndex")
                });
                setReader.ReportUnknown();
            }

            foreach (var subReader in reader.Children("SubChannelSet"))
            {
                var subSet = new SubChannelSet(subReader.Name("Name", default))
                {
                    PhysicalFrom = OptionalFloat(subReader, "PhysicalFrom"),
                    PhysicalTo = OptionalFloat(subReader, "PhysicalTo")
                };
                subReader.Reference("DMXProfile", GdtfScopes.DmxProfiles, p => subSet.DmxProfile = p);
                var unitPath = subReader.Node("SubPhysicalUnit");
                var unitAttribute = subReader.Element.Attribute("SubPhysicalUnit");
                if (!unitPath.IsEmpty)
                {
                    // libMVRgdtf writes only the type ("Duration"); "Attribute.Type" is accepted as well.
                    context.AfterResolve(() =>
                    {
                        var typeName = unitPath.Segments[unitPath.Segments.Count - 1].Value;
                        var attribute = unitPath.Segments.Count > 1
                            ? (context.TryLookup(GdtfScopes.Attributes, new NodePath(unitPath.Segments[0]), out var a) ? a : null)
                            : function.Attribute;
                        var unit = GdtfFormat.TryParseEnum<SubPhysicalUnitType>(typeName, out var type)
                            ? attribute?.SubPhysicalUnits.FirstOrDefault(u => u.Type == type)
                            : null;
                        if (unit != null) subSet.SubPhysicalUnit = unit;
                        else context.Error($"Sub physical unit '{unitPath}' not found on attribute '{attribute?.Name.Value ?? ChannelFunction.NoFeature}'.", unitAttribute);
                    });
                }

                subReader.ReportUnknown();
                function.SubChannelSets.Add(subSet);
            }

            reader.ReportUnknown();
            return function;
        }

        /// <summary>Before GDTF 1.1 (#111) channel function names were Strings, so their characters are not checked there.</summary>
        private static Name? ReadChannelFunctionName(ElementReader reader)
        {
            if (reader.Context.DataVersion >= DataVersion.V1_1) return reader.OptionalName("Name");
            var value = reader.String("Name");
            return string.IsNullOrEmpty(value) ? (Name?)null : Name.Unchecked(value!);
        }

        /// <summary>Resolves a Node path inside the DMX mode and checks the type of the target.</summary>
        private static void ResolveInMode<T>(ElementReader reader, string attribute, ReferenceScope<object> scope, System.Action<T> assign,
            string what, bool required = false) where T : class
        {
            var source = reader.Element.Attribute(attribute);
            var context = reader.Context;
            reader.Reference(attribute, scope, target =>
            {
                if (target is T typed) assign(typed);
                else context.Error($"'{source?.Value}' is not a {what}.", source);
            }, required);
        }
    }
}
