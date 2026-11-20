namespace GDTFSharp.Tests;

internal static class TestData
{
    public static string Root => Path.Combine(AppContext.BaseDirectory, "TestData");

    public static string MinimalDescription => Path.Combine(Root, "Minimal", "description.xml");

    public static string ExternalRoot => Path.Combine(Root, "External");

    public static string Xsd => Path.Combine(ExternalRoot, "gdtf.xsd");

    /// <summary>Third-party fixtures fetched by scripts/fetch-testdata.sh. Empty when not fetched.</summary>
    public static IEnumerable<string> ExternalFixtures
    {
        get
        {
            var folder = Path.Combine(ExternalRoot, "fixtures");
            return Directory.Exists(folder) ? Directory.GetFiles(folder, "*.gdtf").OrderBy(f => f) : Enumerable.Empty<string>();
        }
    }

    public static TheoryData<string> ExternalFixtureNames
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var file in ExternalFixtures) data.Add(Path.GetFileName(file));
            if (data.Count == 0) data.Add(string.Empty);
            return data;
        }
    }

    public static string RequireExternal(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) Assert.Skip("External test data missing; run scripts/fetch-testdata.sh.");
        var path = Path.Combine(ExternalRoot, "fixtures", fileName);
        if (!File.Exists(path)) Assert.Skip($"{fileName} missing; run scripts/fetch-testdata.sh.");
        return path;
    }

    /// <summary>Runs <paramref name="action"/> with a culture that uses "," as decimal separator.</summary>
    public static void WithGermanCulture(Action action)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            action();
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
