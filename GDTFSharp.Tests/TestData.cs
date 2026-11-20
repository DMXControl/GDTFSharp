using GDTFSharp.Serialization;

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

    /// <summary>
    /// Defects in the third-party sample files that the reader is right to report, by file name: a substring of the
    /// diagnostic message and a substring of the XML path where the written file legitimately differs.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Message, string Path)[]> KnownDefects =
        new Dictionary<string, (string, string)[]>
        {
            // A slot color written by a C++ tool from NaN values; the reader drops it, so the slot is white.
            ["allWorking.gdtf"] = new[]
            {
                ("'-nan(ind),-nan(ind),0.000000'", "Slot[Tokyo Blue]@Color"),
                // Negative DMX values; the reader drops them, so those channels have no highlight.
                ("is not a valid value for 'Highlight'", "DMXChannel@Highlight")
            },
            // Two channel functions named "NoFeature" in one logical channel, so their paths clash.
            // A channel set name with commas, which a Name does not allow (kept as is).
            ["Robe lighting s.r.o.@BMFL Spot.gdtf"] = new[]
            {
                ("Duplicate name 'Head_Gobo1Pos.Gobo1Pos.NoFeature'", "\u0000"),
                ("'Time from 0,2 s to 25,4 sec / Speed from max to min' contains characters", "\u0000")
            }
        };

    public static bool IsKnownDefect(string fileName, GdtfDiagnostic diagnostic) =>
        KnownDefects.TryGetValue(fileName, out var defects) && defects.Any(d => diagnostic.Message.Contains(d.Message));

    public static Func<string, bool> KnownDifferences(string fileName) => path =>
        KnownDefects.TryGetValue(fileName, out var defects) && defects.Any(d => path.Contains(d.Path));

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
