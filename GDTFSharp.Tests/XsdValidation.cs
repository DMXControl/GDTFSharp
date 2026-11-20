using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace GDTFSharp.Tests;

internal static class XsdValidation
{
    private static readonly Lazy<XmlSchemaSet> Schemas = new(() =>
    {
        var set = new XmlSchemaSet();
        using var reader = XmlReader.Create(TestData.Xsd);
        set.Add(null, reader);
        set.Compile();
        return set;
    });

    public static IReadOnlyList<string> Validate(XDocument document)
    {
        var errors = new List<string>();
        // Validate a copy: Validate() annotates the tree with schema info.
        new XDocument(document).Validate(Schemas.Value, (_, e) =>
        {
            // gdtf.xsd declares keyrefs on FixtureType that refer to unique constraints declared on its children.
            // That is valid XSD 1.0 (xmllint accepts it), but System.Xml cannot resolve such keyrefs and reports
            // this error for every one of them, so it carries no information about the document.
            if (e.Message.Contains("cannot find the referred key or unique in scope")) return;
            errors.Add($"{e.Severity}: {e.Message}");
        });
        return errors;
    }

    public static void AssertValid(XDocument document)
    {
        var errors = Validate(document);
        Assert.True(errors.Count == 0, "XSD validation failed:\n" + string.Join("\n", errors) + "\n\n" + document);
    }
}
