using GDTFSharp.Types;

namespace GDTFSharp.Tests.Types;

public class NameTests
{
    [Theory]
    [InlineData("Dimmer")]
    [InlineData("Beam 1")]
    [InlineData("Gobo(1)+#%'\"*-/:;<=>@_`")]
    [InlineData("Größe äöü 灯")]
    [InlineData("")]
    public void Valid(string value)
    {
        Assert.True(Name.IsValid(value));
        Assert.Equal(value, new Name(value).Value);
    }

    [Theory]
    [InlineData("Dimmer.Dimmer")]
    [InlineData("A,B")]
    [InlineData("Hey!")]
    [InlineData("$")]
    [InlineData("&")]
    [InlineData("?")]
    [InlineData("[x]")]
    [InlineData("a\\b")]
    [InlineData("^")]
    [InlineData("{x}")]
    [InlineData("a|b")]
    [InlineData("~")]
    [InlineData("tab\there")]
    [InlineData("\u001F")]
    public void Invalid(string value)
    {
        Assert.False(Name.IsValid(value));
        Assert.Throws<ArgumentException>(() => new Name(value));
        Assert.False(Name.TryCreate(value, out _));
    }

    [Fact]
    public void EqualityIsOrdinal()
    {
        Assert.Equal(new Name("Pan"), new Name("Pan"));
        Assert.NotEqual(new Name("Pan"), new Name("pan"));
        Assert.Equal(string.Empty, default(Name).Value);
    }
}

public class NodePathTests
{
    [Fact]
    public void ParseAndFormat()
    {
        var path = NodePath.Parse("Dimmer.Dimmer");
        Assert.Equal(new[] { new Name("Dimmer"), new Name("Dimmer") }, path.Segments);
        Assert.Equal("Dimmer.Dimmer", path.ToString());
        Assert.Equal(path, new NodePath(new Name("Dimmer"), new Name("Dimmer")));
        Assert.Equal(path.GetHashCode(), NodePath.Parse("Dimmer.Dimmer").GetHashCode());
    }

    [Theory]
    [InlineData("A..B")]
    [InlineData(".A")]
    [InlineData("A!")]
    public void RejectsInvalid(string value) => Assert.False(NodePath.TryParse(value, out _));

    [Fact]
    public void EmptyStringIsEmptyPath()
    {
        Assert.True(NodePath.TryParse("", out var path));
        Assert.True(path.IsEmpty);
        Assert.Equal(default, path);
    }

    [Fact]
    public void Append() => Assert.Equal("A.B", NodePath.Parse("A").Append(new Name("B")).ToString());
}

public class DataVersionTests
{
    [Fact]
    public void ParseAndCompare()
    {
        Assert.Equal(DataVersion.V1_2, DataVersion.Parse("1.2"));
        Assert.True(DataVersion.V1_0 < DataVersion.V1_1);
        Assert.True(DataVersion.Parse("1.10") > DataVersion.V1_2);
        Assert.Equal("1.2", DataVersion.V1_2.ToString());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.2.3")]
    [InlineData("256.0")]
    [InlineData("a.b")]
    public void RejectsInvalid(string value) => Assert.False(DataVersion.TryParse(value, out _));
}

public class ColorCIETests
{
    [Fact]
    public void ParseAndFormat()
    {
        TestData.WithGermanCulture(() =>
        {
            var color = ColorCIE.Parse("0.3127,0.329,100");
            Assert.Equal(0.3127f, color.X);
            Assert.Equal(0.329f, color.Y);
            Assert.Equal(100f, color.Luminance);
            Assert.Equal("0.3127,0.329,100", color.ToString());
        });
    }

    [Fact]
    public void AcceptsSpaces() => Assert.Equal(new ColorCIE(0.1f, 0.2f, 3f), ColorCIE.Parse("0.1, 0.2, 3"));

    [Fact]
    public void RejectsWrongCount() => Assert.False(ColorCIE.TryParse("0.1,0.2", out _));
}

public class DmxValueTests
{
    [Theory]
    [InlineData("255/1", 255UL, 1, false)]
    [InlineData("65535/2", 65535UL, 2, false)]
    [InlineData("255/1s", 255UL, 1, true)]
    [InlineData("0/4", 0UL, 4, false)]
    public void Parse(string text, ulong value, int bytes, bool shifting)
    {
        var dmx = DmxValue.Parse(text);
        Assert.Equal(value, dmx.Value);
        Assert.Equal(bytes, dmx.ByteCount);
        Assert.Equal(shifting, dmx.ByteShifting);
        Assert.Equal(text, dmx.ToString());
    }

    [Theory]
    [InlineData("256/1")]
    [InlineData("1/0")]
    [InlineData("1/9")]
    [InlineData("-1/1")]
    [InlineData("255")]
    [InlineData("/1")]
    [InlineData("1/1x")]
    public void RejectsInvalid(string text) => Assert.False(DmxValue.TryParse(text, out _));

    // Examples from DIN SPEC 15800 Table 1: 255/1 in a 16 bit channel is 65535 (mirroring) or 65280 (shifting).
    [Theory]
    [InlineData("255/1", 2, 65535UL)]
    [InlineData("255/1s", 2, 65280UL)]
    [InlineData("128/1", 2, 0x8080UL)]
    [InlineData("128/1s", 2, 0x8000UL)]
    [InlineData("255/1", 3, 0xFFFFFFUL)]
    [InlineData("4660/2", 3, 0x123412UL)]
    [InlineData("4660/2", 4, 0x12341234UL)]
    [InlineData("65535/2", 1, 255UL)]
    [InlineData("4660/2", 1, 0x12UL)]
    [InlineData("42/1", 1, 42UL)]
    public void ToResolution(string text, int bytes, ulong expected) =>
        Assert.Equal(expected, DmxValue.Parse(text).ToResolution(bytes));
}

public class DmxAddressTests
{
    [Theory]
    [InlineData("1", 1, 1, 1)]
    [InlineData("512", 512, 1, 512)]
    [InlineData("513", 513, 2, 1)]
    [InlineData("2.1", 513, 2, 1)]
    [InlineData("3.512", 1536, 3, 512)]
    public void Parse(string text, int absolute, int universe, int address)
    {
        var dmx = DmxAddress.Parse(text);
        Assert.Equal(absolute, dmx.Absolute);
        Assert.Equal(universe, dmx.Universe);
        Assert.Equal(address, dmx.Address);
    }

    [Theory]
    [InlineData("0.1")]
    [InlineData("1.0")]
    [InlineData("1.513")]
    [InlineData("x")]
    public void RejectsInvalid(string text) => Assert.False(DmxAddress.TryParse(text, out _));
}

public class DmxBreakTests
{
    [Fact]
    public void ParseNumberAndOverwrite()
    {
        Assert.Equal(2, DmxBreak.Parse("2").Number);
        Assert.True(DmxBreak.Parse("Overwrite").IsOverwrite);
        Assert.Equal("Overwrite", DmxBreak.Overwrite.ToString());
        Assert.Throws<InvalidOperationException>(() => DmxBreak.Overwrite.Number);
        Assert.False(DmxBreak.TryParse("overwrite!", out _));
    }
}

public class RotationMatrixTests
{
    [Fact]
    public void RoundTrips()
    {
        const string text = "{0,-1,0}{1,0,0}{0,0,1}";
        var rotation = RotationMatrix.Parse(text);
        Assert.Equal(-1f, rotation.M12);
        Assert.Equal(1f, rotation[1, 0]);
        Assert.Equal(text, rotation.ToString());
    }

    [Fact]
    public void NoneIsIdentity() => Assert.Equal(RotationMatrix.Identity, RotationMatrix.Parse("None"));

    [Fact]
    public void RejectsWrongShape() => Assert.False(RotationMatrix.TryParse("{1,0,0}{0,1,0}", out _));
}
