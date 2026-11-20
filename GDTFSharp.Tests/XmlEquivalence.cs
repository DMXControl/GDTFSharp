using System.Xml.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Tests;

/// <summary>
/// Checks that XML the writer produced carries the same information as the original file, allowing for differences
/// that do not change meaning: number formatting ("0.225000" vs "0.225"), attributes the writer adds with their spec
/// default value, empty optional elements the writer adds, and the order of children with different tags.
/// </summary>
internal static class XmlEquivalence
{
    /// <summary>(element, attribute) → spec default the writer may add when the original omitted it.</summary>
    public static readonly IReadOnlyDictionary<(string Element, string Attribute), string> Defaults =
        new Dictionary<(string, string), string>
        {
            [("*", "Position")] = "{1,0,0,0}{0,1,0,0}{0,0,1,0}{0,0,0,1}",
            [("Beam", "LampType")] = "Discharge",
            [("Beam", "PowerConsumption")] = "1000",
            [("Beam", "LuminousFlux")] = "10000",
            [("Beam", "ColorTemperature")] = "6000",
            [("Beam", "BeamAngle")] = "25",
            [("Beam", "FieldAngle")] = "25",
            [("Beam", "ThrowRatio")] = "1",
            [("Beam", "RectangleRatio")] = "1.7777",
            [("Beam", "BeamRadius")] = "0.05",
            [("Beam", "BeamType")] = "Wash",
            [("Beam", "ColorRenderingIndex")] = "100",
            [("Laser", "ColorType")] = "RGB",
            [("Break", "DMXOffset")] = "1",
            [("Break", "DMXBreak")] = "1",
            [("FixtureType", "CanHaveChildren")] = "Yes",
            [("FixtureType", "ThumbnailOffsetX")] = "0",
            [("FixtureType", "ThumbnailOffsetY")] = "0",
            [("Attribute", "PhysicalUnit")] = "None",
            [("ColorSpace", "Name")] = "Default",
            [("ColorSpace", "Mode")] = "sRGB",
            [("Measurement", "InterpolationTo")] = "Linear",
            [("Point", "DMXPercentage")] = "0",
            [("Point", "CFC0")] = "0",
            [("Point", "CFC1")] = "0",
            [("Point", "CFC2")] = "0",
            [("Point", "CFC3")] = "0",
            [("CRIGroup", "ColorTemperature")] = "6000",
            [("CRI", "CES")] = "CES01",
            [("CRI", "ColorRenderingIndex")] = "100",
            [("Connector", "Gender")] = "0",
            [("Connector", "Length")] = "0",
            [("OperatingTemperature", "Low")] = "0",
            [("OperatingTemperature", "High")] = "40",
            [("Weight", "Value")] = "0",
            [("LegHeight", "Value")] = "0",
            [("Model", "Length")] = "0",
            [("Model", "Width")] = "0",
            [("Model", "Height")] = "0",
            [("Model", "PrimitiveType")] = "Undefined",
            [("Model", "SVGOffsetX")] = "0",
            [("Model", "SVGOffsetY")] = "0",
            [("Model", "SVGSideOffsetX")] = "0",
            [("Model", "SVGSideOffsetY")] = "0",
            [("Model", "SVGFrontOffsetX")] = "0",
            [("Model", "SVGFrontOffsetY")] = "0",
            [("Revision", "Text")] = "",
            [("Revision", "UserID")] = "0",
            [("Revision", "ModifiedBy")] = "",
            [("SubPhysicalUnit", "PhysicalUnit")] = "None",
            [("SubPhysicalUnit", "PhysicalFrom")] = "0",
            [("SubPhysicalUnit", "PhysicalTo")] = "1",
        };

    /// <summary>Elements the writer always writes, even when empty.</summary>
    public static readonly ISet<string> AlwaysWritten = new HashSet<string>
    {
        "ActivationGroups", "Wheels", "PhysicalDescriptions", "Models", "Revisions", "FTPresets", "Protocols", "Geometries",
        "DMXModes", "Emitters", "Filters", "AdditionalColorSpaces", "Gamuts", "DMXProfiles", "CRIs", "Properties"
    };

    /// <summary>Elements the writer leaves out when they are empty (obsolete sections).</summary>
    public static readonly ISet<string> MayDropWhenEmpty = new HashSet<string> { "Connectors" };

    /// <param name="ignore">Paths (as in the failure messages) where a difference is expected.</param>
    public static void AssertEquivalent(XElement original, XElement written, Func<string, bool>? ignore = null)
    {
        var differences = new List<string>();
        Compare(original, written, original.Name.LocalName, differences);
        if (ignore != null) differences.RemoveAll(d => ignore(d.Substring(0, d.IndexOf(':'))));
        Assert.True(differences.Count == 0, string.Join("\n", differences.Take(20)));
    }

    private static void Compare(XElement original, XElement written, string path, List<string> differences)
    {
        if (original.Name != written.Name)
        {
            differences.Add($"{path}: element {original.Name} was written as {written.Name}");
            return;
        }

        foreach (var attribute in original.Attributes())
        {
            var writtenValue = written.Attribute(attribute.Name)?.Value;
            if (writtenValue == null)
            {
                // Empty optional values (e.g. RefFT="") carry no information.
                if (attribute.Value.Trim().Length > 0) differences.Add($"{path}@{attribute.Name}: '{attribute.Value}' was dropped");
            }
            else if (!SameValue(attribute.Value, writtenValue))
            {
                differences.Add($"{path}@{attribute.Name}: '{attribute.Value}' was written as '{writtenValue}'");
            }
        }

        foreach (var attribute in written.Attributes())
        {
            if (original.Attribute(attribute.Name) != null) continue;
            if (!(Defaults.TryGetValue((written.Name.LocalName, attribute.Name.LocalName), out var defaultValue) ||
                  Defaults.TryGetValue(("*", attribute.Name.LocalName), out defaultValue)) ||
                !SameValue(defaultValue, attribute.Value))
                differences.Add($"{path}@{attribute.Name}: '{attribute.Value}' was added");
        }

        // Children are matched by tag: the order of different tags does not matter (several GDTF nodes are xs:all),
        // the order of equal tags does (slots, measurements, channels ...).
        var originalGroups = original.Elements().GroupBy(e => e.Name).ToDictionary(g => g.Key, g => g.ToList());
        var writtenGroups = written.Elements().GroupBy(e => e.Name).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var name in originalGroups.Keys.Union(writtenGroups.Keys))
        {
            var originalChildren = originalGroups.TryGetValue(name, out var o) ? o : new List<XElement>();
            var writtenChildren = writtenGroups.TryGetValue(name, out var w) ? w : new List<XElement>();
            var tag = name.LocalName;
            if (originalChildren.Count == 0 && AlwaysWritten.Contains(tag) && writtenChildren.All(IsEmpty)) continue;
            if (writtenChildren.Count == 0 && MayDropWhenEmpty.Contains(tag) && originalChildren.All(IsEmpty)) continue;
            if (originalChildren.Count != writtenChildren.Count)
            {
                differences.Add($"{path}: {originalChildren.Count} <{tag}> children were written as {writtenChildren.Count}");
                continue;
            }

            for (var i = 0; i < originalChildren.Count; i++)
            {
                var child = originalChildren[i];
                var childName = child.Attribute("Name")?.Value;
                Compare(child, writtenChildren[i], $"{path}/{tag}{(childName != null ? $"[{childName}]" : "")}", differences);
            }
        }
    }

    private static bool IsEmpty(XElement element) => !element.HasElements && !element.HasAttributes;

    private static bool SameValue(string original, string written)
    {
        if (original == written) return true;
        if (GdtfFormat.TryParseHex(original, out var hexA) && original.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
            GdtfFormat.TryParseHex(written, out var hexB))
            return hexA == hexB;
        // Number lists, including bracketed matrices like {1,0,0}{0,1,0}{0,0,1}: compare the numbers.
        static string[] Numbers(string value) => value.Replace("}{", ",").Trim('{', '}').Split(',');
        if (original.Contains('{') != written.Contains('{')) return false;
        var a = Numbers(original);
        var b = Numbers(written);
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
        {
            if (!GdtfFormat.TryParseFloat(a[i], out var x) || !GdtfFormat.TryParseFloat(b[i], out var y) || x != y) return false;
        }

        return true;
    }
}
