using System.Xml.Linq;
using GDTFSharp.Types;

namespace GDTFSharp.Tests;

/// <summary>
/// Checks that XML the writer produced carries the same information as the original file, allowing for differences
/// that do not change meaning: number formatting ("0.225000" vs "0.225"), attributes the writer adds with their spec
/// default value, and empty optional elements the writer adds.
/// </summary>
internal static class XmlEquivalence
{
    /// <summary>(element, attribute) → spec default the writer may add when the original omitted it.</summary>
    public static readonly IReadOnlyDictionary<(string Element, string Attribute), string> Defaults =
        new Dictionary<(string, string), string>
        {
            [("Attribute", "PhysicalUnit")] = "None",
            [("SubPhysicalUnit", "PhysicalUnit")] = "None",
            [("SubPhysicalUnit", "PhysicalFrom")] = "0",
            [("SubPhysicalUnit", "PhysicalTo")] = "1",
        };

    /// <summary>Elements the writer always writes, even when empty.</summary>
    public static readonly ISet<string> AlwaysWritten = new HashSet<string> { "ActivationGroups" };

    public static void AssertEquivalent(XElement original, XElement written)
    {
        var differences = new List<string>();
        Compare(original, written, original.Name.LocalName, differences);
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
            if (!Defaults.TryGetValue((written.Name.LocalName, attribute.Name.LocalName), out var defaultValue) ||
                !SameValue(defaultValue, attribute.Value))
                differences.Add($"{path}@{attribute.Name}: '{attribute.Value}' was added");
        }

        var originalChildren = original.Elements().ToList();
        var writtenChildren = written.Elements()
            .Where(w => !(AlwaysWritten.Contains(w.Name.LocalName) && !w.HasElements && original.Element(w.Name) == null))
            .ToList();
        if (originalChildren.Count != writtenChildren.Count)
        {
            differences.Add($"{path}: {originalChildren.Count} children were written as {writtenChildren.Count}");
            return;
        }

        for (var i = 0; i < originalChildren.Count; i++)
        {
            var child = originalChildren[i];
            var name = child.Attribute("Name")?.Value;
            Compare(child, writtenChildren[i], $"{path}/{child.Name.LocalName}{(name != null ? $"[{name}]" : "")}", differences);
        }
    }

    private static bool SameValue(string original, string written)
    {
        if (original == written) return true;
        var a = original.Split(',');
        var b = written.Split(',');
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
        {
            if (!GdtfFormat.TryParseFloat(a[i], out var x) || !GdtfFormat.TryParseFloat(b[i], out var y) || x != y) return false;
        }

        return true;
    }
}
