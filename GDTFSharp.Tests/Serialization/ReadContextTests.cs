using System.Xml.Linq;
using GDTFSharp.Serialization;
using GDTFSharp.Types;

namespace GDTFSharp.Tests.Serialization;

public class ReadContextTests
{
    private sealed class FakeNode
    {
        public string Label = "";
        public FakeNode? Link;
    }

    private static readonly ReferenceScope<FakeNode> Scope = new("FakeNodes");
    private static readonly ReferenceScope<FakeNode> OtherScope = new("OtherNodes");

    [Fact]
    public void ResolvesForwardReferencesInPassTwo()
    {
        var context = new GdtfReadContext();
        var source = new FakeNode { Label = "source" };
        context.Resolve(Scope, NodePath.Parse("Group.Target"), n => source.Link = n);

        var target = new FakeNode { Label = "target" };
        context.Register(Scope, NodePath.Parse("Group.Target"), target);

        Assert.Null(source.Link);
        context.ResolveReferences();
        Assert.Same(target, source.Link);
        Assert.Empty(context.Diagnostics);
    }

    [Fact]
    public void ScopesAreSeparate()
    {
        var context = new GdtfReadContext();
        context.Register(OtherScope, NodePath.Parse("A"), new FakeNode());
        context.Resolve(Scope, NodePath.Parse("A"), _ => Assert.Fail("must not resolve"));
        context.ResolveReferences();
        Assert.Single(context.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error);
    }

    [Fact]
    public void DanglingReferenceIsAnErrorWithLocation()
    {
        var xml = XDocument.Parse("<GDTF><FixtureType><Attribute Name=\"Pan\" Feature=\"Position.PanTilt\" /></FixtureType></GDTF>",
            LoadOptions.SetLineInfo);
        var element = xml.Root!.Element("FixtureType")!.Element("Attribute")!;
        var context = new GdtfReadContext();
        new ElementReader(element, context).Reference("Feature", Scope, _ => { });
        context.ResolveReferences();

        var diagnostic = Assert.Single(context.Diagnostics);
        Assert.Equal(GdtfDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Position.PanTilt", diagnostic.Message);
        Assert.Equal("FixtureType/Attribute[Pan]@Feature", diagnostic.Location);
        Assert.Equal(1, diagnostic.Line);
    }

    [Fact]
    public void ReferencePrefixIsPrepended()
    {
        var element = XElement.Parse("<X Ref=\"Child\" />");
        var context = new GdtfReadContext();
        var target = new FakeNode();
        context.Register(Scope, NodePath.Parse("Parent.Child"), target);
        FakeNode? resolved = null;
        new ElementReader(element, context).Reference("Ref", Scope, n => resolved = n, prefix: NodePath.Parse("Parent"));
        context.ResolveReferences();
        Assert.Same(target, resolved);
    }

    [Fact]
    public void DuplicateRegistrationKeepsFirstAndReportsError()
    {
        var context = new GdtfReadContext();
        var first = new FakeNode();
        context.Register(Scope, NodePath.Parse("A"), first);
        context.Register(Scope, NodePath.Parse("A"), new FakeNode());

        Assert.True(context.TryLookup(Scope, NodePath.Parse("A"), out var found));
        Assert.Same(first, found);
        Assert.Single(context.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error);
    }

    [Fact]
    public void StrictModeThrowsOnFirstError()
    {
        var context = new GdtfReadContext(new GdtfReadOptions { Strict = true });
        context.Warning("only a warning");
        var exception = Assert.Throws<GdtfReadException>(() => context.Error("boom"));
        Assert.Equal("boom", exception.Diagnostic!.Message);
    }
}

public class ElementReaderTests
{
    private static (ElementReader Reader, GdtfReadContext Context) Create(string xml)
    {
        var context = new GdtfReadContext();
        return (new ElementReader(XElement.Parse(xml), context), context);
    }

    [Fact]
    public void MissingOptionalAttributeGivesDefaultWithoutDiagnostic()
    {
        var (reader, context) = Create("<X />");
        Assert.Equal(7.5f, reader.Float("Value", 7.5f));
        Assert.True(reader.YesNo("Flag", true));
        Assert.Null(reader.Guid("RefFT"));
        Assert.Equal(PhysicalUnit.None, reader.Enum("PhysicalUnit", PhysicalUnit.None));
        Assert.Empty(context.Diagnostics);
    }

    [Fact]
    public void MissingRequiredAttributeIsAnError()
    {
        var (reader, context) = Create("<X />");
        reader.Name(required: true);
        Assert.Contains(context.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error && d.Message.Contains("'Name'"));
    }

    [Fact]
    public void InvalidValueGivesDefaultAndError()
    {
        var (reader, context) = Create("<X Value=\"1,5\" />");
        Assert.Equal(2f, reader.Float("Value", 2f));
        var diagnostic = Assert.Single(context.Diagnostics);
        Assert.Equal(GdtfDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.EndsWith("@Value", diagnostic.Location);
    }

    [Fact]
    public void InvalidNameIsKeptWithWarning()
    {
        var (reader, context) = Create("<X Name=\"Bad.Name!\" />");
        Assert.Equal("Bad.Name!", reader.Name().Value);
        Assert.Equal(GdtfDiagnosticSeverity.Warning, Assert.Single(context.Diagnostics).Severity);
    }

    [Fact]
    public void EmptyRequiredGuidIsAnError()
    {
        var (reader, context) = Create("<X FixtureTypeID=\"\" />");
        Assert.Null(reader.Guid("FixtureTypeID", required: true));
        Assert.Single(context.Diagnostics, d => d.Severity == GdtfDiagnosticSeverity.Error);
    }

    [Fact]
    public void OptionalDmxValueNone()
    {
        var (reader, context) = Create("<X Highlight=\"None\" Default=\"128/1\" />");
        Assert.Null(reader.OptionalDmxValue("Highlight"));
        Assert.Equal(DmxValue.Parse("128/1"), reader.OptionalDmxValue("Default"));
        reader.ReportUnknownAttributes();
        Assert.Empty(context.Diagnostics);
    }

    [Fact]
    public void ReportsUnreadAttributesAsInfo()
    {
        var (reader, context) = Create("<X Name=\"A\" Extra=\"1\" />");
        reader.Name();
        reader.ReportUnknownAttributes();
        var diagnostic = Assert.Single(context.Diagnostics);
        Assert.Equal(GdtfDiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("Extra", diagnostic.Message);
    }
}
