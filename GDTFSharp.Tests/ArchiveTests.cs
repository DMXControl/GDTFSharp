using System.IO.Compression;
using System.Xml.Linq;
using GDTFSharp.Models;
using GDTFSharp.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Tests;

public class ArchiveTests
{
    private static GdtfArchive CreateArchive()
    {
        var fixture = new FixtureType(new Name("Test"), Guid.NewGuid()) { Thumbnail = "thumbnail" };
        var archive = new GdtfArchive(new GdtfDocument(fixture));
        archive.Resources["thumbnail.png"] = new byte[] { 1, 2, 3 };
        archive.Resources["thumbnail.svg"] = new byte[] { 4 };
        archive.Resources["wheels/gobo1.png"] = new byte[] { 5 };
        archive.Resources["models/gltf/base.glb"] = new byte[] { 6 };
        archive.Resources["models/3ds/base.3ds"] = new byte[] { 7 };
        return archive;
    }

    private static GdtfArchiveReadResult SaveAndLoad(GdtfArchive archive)
    {
        using var stream = new MemoryStream();
        archive.Save(stream);
        stream.Position = 0;
        return GdtfArchive.Load(stream);
    }

    [Fact]
    public void SaveAndLoadRoundTrips()
    {
        var original = CreateArchive();
        var result = SaveAndLoad(original);

        Assert.False(result.HasErrors);
        Assert.Equal(original.Document.FixtureType.FixtureTypeId, result.Archive.Document.FixtureType.FixtureTypeId);
        Assert.Equal(original.Resources.Keys.OrderBy(k => k), result.Archive.Resources.Keys.OrderBy(k => k));
        foreach (var (path, content) in original.Resources) Assert.Equal(content, result.Archive.Resources[path]);
    }

    [Fact]
    public void DescriptionIsTheFirstEntry()
    {
        using var stream = new MemoryStream();
        CreateArchive().Save(stream);
        stream.Position = 0;
        using var zip = new ZipArchive(stream);
        Assert.Equal("description.xml", zip.Entries[0].FullName);
    }

    [Fact]
    public void ResourceLookup()
    {
        var archive = CreateArchive();
        Assert.Equal(new byte[] { 1, 2, 3 }, archive.GetThumbnail("png"));
        Assert.Equal(new byte[] { 4 }, archive.GetThumbnail("svg"));
        Assert.Equal(new byte[] { 5 }, archive.GetWheelMedia("gobo1"));
        Assert.Equal(new byte[] { 5 }, archive.GetWheelMedia("GOBO1"));
        Assert.Null(archive.GetWheelMedia("gobo2"));
        Assert.Equal(new byte[] { 6 }, archive.GetModelFile("gltf", "base"));
        Assert.Equal(new byte[] { 7 }, archive.GetModelFile("3ds", "base", "3ds"));
        Assert.Null(archive.GetModelFile("svg", "base"));
        Assert.False(archive.TryGetResource("", "base", null, out _, out _));
    }

    [Fact]
    public void NormalizesEntryPaths()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var writer = new StreamWriter(zip.CreateEntry("./description.xml").Open()))
                writer.Write(GdtfWriter.WriteToString(new GdtfDocument(new FixtureType(new Name("X"), Guid.NewGuid()))));
            zip.CreateEntry("wheels/");
            using (var entry = zip.CreateEntry("wheels\\gobo.png").Open()) entry.WriteByte(9);
        }

        stream.Position = 0;
        var archive = GdtfArchive.Load(stream).Archive;
        Assert.Equal(new[] { "wheels/gobo.png" }, archive.Resources.Keys);
    }

    [Fact]
    public void MissingDescriptionThrows()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) zip.CreateEntry("other.txt");
        stream.Position = 0;
        Assert.Throws<GdtfReadException>(() => GdtfArchive.Load(stream));
    }

    [Fact]
    public void NotAZipThrows() =>
        Assert.Throws<GdtfReadException>(() => GdtfArchive.Load(new MemoryStream(new byte[] { 1, 2, 3, 4 })));

    // ---- Real-world files (scripts/fetch-testdata.sh) ------------------------------------------------------------

    [Theory]
    [MemberData(nameof(TestData.ExternalFixtureNames), MemberType = typeof(TestData))]
    public void ReadsRealFixturesWithoutErrors(string fileName)
    {
        var result = GdtfArchive.Load(TestData.RequireExternal(fileName));
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error);
        Assert.NotEmpty(result.Archive.Document.FixtureType.UnparsedSections);
    }

    [Theory]
    [MemberData(nameof(TestData.ExternalFixtureNames), MemberType = typeof(TestData))]
    public void RealFixturesRoundTripWithoutLoss(string fileName)
    {
        var original = GdtfArchive.Load(TestData.RequireExternal(fileName)).Archive;
        var reread = SaveAndLoad(original).Archive;

        var a = original.Document.FixtureType;
        var b = reread.Document.FixtureType;
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Description, b.Description);
        Assert.Equal(a.FixtureTypeId, b.FixtureTypeId);
        Assert.Equal(a.UnparsedSections.Select(s => s.Name.LocalName), b.UnparsedSections.Select(s => s.Name.LocalName));
        for (var i = 0; i < a.UnparsedSections.Count; i++)
            Assert.True(XNode.DeepEquals(a.UnparsedSections[i], b.UnparsedSections[i]), a.UnparsedSections[i].Name.LocalName);

        Assert.Equal(original.Resources.Keys.OrderBy(k => k), reread.Resources.Keys.OrderBy(k => k));
        foreach (var (path, content) in original.Resources) Assert.Equal(content, reread.Resources[path]);
    }
}
