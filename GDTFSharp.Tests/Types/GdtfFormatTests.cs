using System.Numerics;
using GDTFSharp.Types;

namespace GDTFSharp.Tests.Types;

public class GdtfFormatTests
{
    [Theory]
    [InlineData(1.5f, "1.5")]
    [InlineData(-0.25f, "-0.25")]
    [InlineData(100f, "100")]
    [InlineData(0.00001f, "0.00001")]
    [InlineData(1e20f, "100000000000000000000")]
    public void FormatFloat_IsInvariantAndAvoidsExponents(float value, string expected)
    {
        TestData.WithGermanCulture(() => Assert.Equal(expected, GdtfFormat.FormatFloat(value)));
    }

    [Fact]
    public void FormatFloat_RoundTrips()
    {
        foreach (var value in new[] { 0.1f, 1f / 3f, 0.3127f, 123456.7f, -9.87654e-6f })
        {
            Assert.True(GdtfFormat.TryParseFloat(GdtfFormat.FormatFloat(value), out var parsed));
            Assert.Equal(value, parsed);
        }
    }

    [Fact]
    public void ParseFloat_UsesDotRegardlessOfCulture()
    {
        TestData.WithGermanCulture(() =>
        {
            Assert.True(GdtfFormat.TryParseFloat("1.5", out var value));
            Assert.Equal(1.5f, value);
            Assert.False(GdtfFormat.TryParseFloat("1,5", out _));
        });
    }

    [Theory]
    [InlineData("0x1A2B", 0x1A2BUL)]
    [InlineData("0X00ff", 0xFFUL)]
    [InlineData("1A2B", 0x1A2BUL)]
    public void ParseHex(string text, ulong expected)
    {
        Assert.True(GdtfFormat.TryParseHex(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0x")]
    [InlineData("0xZZ")]
    public void ParseHex_RejectsInvalid(string text) => Assert.False(GdtfFormat.TryParseHex(text, out _));

    [Fact]
    public void FormatHex_HasPrefixAndRoundTrips()
    {
        Assert.Equal("0x2A", GdtfFormat.FormatHex(42));
        Assert.True(GdtfFormat.TryParseHex(GdtfFormat.FormatHex(0xDEADBEEF), out var value));
        Assert.Equal(0xDEADBEEFUL, value);
    }

    [Theory]
    [InlineData("Yes", true)]
    [InlineData("No", false)]
    [InlineData("yes", true)]
    public void ParseYesNo(string text, bool expected)
    {
        Assert.True(GdtfFormat.TryParseYesNo(text, out var value));
        Assert.Equal(expected, value);
    }

    [Fact]
    public void ParseYesNo_RejectsOther() => Assert.False(GdtfFormat.TryParseYesNo("true", out _));

    [Fact]
    public void Date_RoundTripsAsUtc()
    {
        Assert.True(GdtfFormat.TryParseDate("2016-06-21T11:22:48", out var date));
        Assert.Equal(new DateTime(2016, 6, 21, 11, 22, 48, DateTimeKind.Utc), date);
        Assert.Equal(DateTimeKind.Utc, date.Kind);
        Assert.Equal("2016-06-21T11:22:48", GdtfFormat.FormatDate(date));
    }

    [Fact]
    public void Date_AcceptsOtherIsoVariants()
    {
        Assert.True(GdtfFormat.TryParseDate("2016-06-21T13:22:48+02:00", out var date));
        Assert.Equal(new DateTime(2016, 6, 21, 11, 22, 48, DateTimeKind.Utc), date);
    }

    [Fact]
    public void Guid_EmptyIsValidAndNull()
    {
        Assert.True(GdtfFormat.TryParseGuid("", out var empty));
        Assert.Null(empty);
        Assert.True(GdtfFormat.TryParseGuid("308EA87D-7164-42DE-8106-A6D273F57A51", out var guid));
        Assert.Equal("308EA87D-7164-42DE-8106-A6D273F57A51", GdtfFormat.FormatGuid(guid!.Value));
        Assert.False(GdtfFormat.TryParseGuid("xx308EA87D-7164-42DE-8106-A6D273F57A51", out _));
    }

    [Theory]
    [InlineData("1,2.5,-3")]
    [InlineData("{1,2.5,-3}")]
    [InlineData(" 1, 2.5 , -3 ")]
    public void ParseVector3(string text)
    {
        Assert.True(GdtfFormat.TryParseVector3(text, out var vector));
        Assert.Equal(new Vector3(1, 2.5f, -3), vector);
    }

    [Fact]
    public void Matrix_ConvertsGdtfLayoutToNumericsConvention()
    {
        // GDTF: translation in the 4th column of the written rows.
        const string text = "{1,0,0,0.5}{0,1,0,-0.25}{0,0,1,2}{0,0,0,1}";
        Assert.True(GdtfFormat.TryParseMatrix(text, out var matrix));
        Assert.Equal(Matrix4x4.CreateTranslation(0.5f, -0.25f, 2f), matrix);
        Assert.Equal(new Vector3(1.5f, -0.25f, 2f), Vector3.Transform(Vector3.UnitX, matrix));
        Assert.Equal(text, GdtfFormat.FormatMatrix(matrix));
    }

    [Fact]
    public void Matrix_RotationMatchesSpecLayout()
    {
        // 90° around Z in GDTF (column-vector) layout: first column is the image of X = (0,1,0).
        Assert.True(GdtfFormat.TryParseMatrix("{0,-1,0,0}{1,0,0,0}{0,0,1,0}{0,0,0,1}", out var matrix));
        var x = Vector3.Transform(Vector3.UnitX, matrix);
        Assert.Equal(0f, x.X, 5);
        Assert.Equal(1f, x.Y, 5);
        // Matrix4x4.CreateRotationZ produces the same transform.
        var rotation = Matrix4x4.CreateRotationZ(MathF.PI / 2);
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 4; column++)
            Assert.Equal(matrix[row, column], rotation[row, column], 5);
    }

    [Theory]
    [InlineData("{1,0,0,0}{0,1,0,0}{0,0,1,0}")]
    [InlineData("{1,0,0}{0,1,0,0}{0,0,1,0}{0,0,0,1}")]
    [InlineData("x{1,0,0,0}{0,1,0,0}{0,0,1,0}{0,0,0,1}")]
    public void Matrix_RejectsWrongShape(string text) => Assert.False(GdtfFormat.TryParseMatrix(text, out _));

    [Fact]
    public void Matrix_NoneIsIdentity()
    {
        Assert.True(GdtfFormat.TryParseMatrix("None", out var matrix));
        Assert.Equal(Matrix4x4.Identity, matrix);
    }

    [Fact]
    public void IntList()
    {
        Assert.True(GdtfFormat.TryParseIntList("1,2", out var list));
        Assert.Equal(new[] { 1, 2 }, list);
        Assert.True(GdtfFormat.TryParseIntList("None", out var none));
        Assert.Empty(none);
        Assert.Equal("None", GdtfFormat.FormatIntList(Array.Empty<int>()));
        Assert.Equal("1,2", GdtfFormat.FormatIntList(new[] { 1, 2 }));
    }

    [Fact]
    public void Enum_UsesXmlNames()
    {
        Assert.Equal("AngularAccc", GdtfFormat.FormatEnum(PhysicalUnit.AngularAcceleration));
        Assert.Equal("WaveLength", GdtfFormat.FormatEnum(PhysicalUnit.WaveLength));
        Assert.True(GdtfFormat.TryParseEnum<PhysicalUnit>("AngularAccc", out var unit));
        Assert.Equal(PhysicalUnit.AngularAcceleration, unit);
    }

    [Fact]
    public void Enum_AcceptsCSharpNameAndOtherCase()
    {
        Assert.True(GdtfFormat.TryParseEnum<PhysicalUnit>("AngularAcceleration", out var unit));
        Assert.Equal(PhysicalUnit.AngularAcceleration, unit);
        Assert.True(GdtfFormat.TryParseEnum<PhysicalUnit>("wavelength", out unit));
        Assert.Equal(PhysicalUnit.WaveLength, unit);
        Assert.False(GdtfFormat.TryParseEnum<PhysicalUnit>("Parsec", out _));
    }
}
