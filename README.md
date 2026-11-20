# GDTFSharp

A C# implementation of the [GDTF](https://gdtf.eu/gdtf/prologue/introduction/) file format specification.


Targets GDTF 1.2 (DIN SPEC 15800:2022). Older files (1.0/1.1) are read; files are always written as 1.2.

## Status

| Area | State |
|---|---|
| Value types (Name, Node, DMXValue, DMXAddress, ColorCIE, Matrix, Rotation, …) | done |
| `.gdtf` archive read/write, `description.xml` reader/writer, diagnostics | done |
| FixtureType header | done |
| AttributeDefinitions (ActivationGroups, FeatureGroups/Features, Attributes, SubPhysicalUnits) | done |
| Wheels (slots, prism facets, animation systems), Models, Revisions | done |
| Protocols (RDM, Art-Net, sACN) | done; PosiStageNet, OpenSoundControl and CITP are undefined in the spec and kept verbatim |
| FTPresets | undefined in the spec; kept verbatim |
| PhysicalDescriptions (emitters, filters, measurements, color spaces, gamuts, DMX profiles, CRIs, connectors, properties) | done |
| Geometries (all 18 geometry types, geometry references with breaks, laser protocols, pin patches) | done |
| DMXModes (channels, logical channels, channel functions, channel sets, sub channel sets, relations, macros) | done |
| Unknown or vendor-specific sections | kept verbatim on read and written back unchanged |
| Validation (`GdtfValidator`, `GdtfArchive.Validate()`) | done |

## Usage

```csharp
var result = GdtfArchive.Load("Robe@BMFL Spot.gdtf");
foreach (var diagnostic in result.Diagnostics) Console.WriteLine(diagnostic);

var fixtureType = result.Archive.Document.FixtureType;
Console.WriteLine($"{fixtureType.Manufacturer} {fixtureType.Name}");

foreach (var mode in fixtureType.DmxModes)
{
    Console.WriteLine($"{mode.Name}: {string.Join(", ", mode.GetFootprint().Select(f => $"break {f.Key}: {f.Value} ch"))}");
    foreach (var channel in mode.Channels)
        Console.WriteLine($"  {string.Join(",", channel.Offsets)} {channel.Name}");
}

result.Archive.Save("copy.gdtf");
```

Geometry positions are `System.Numerics.Matrix4x4` transforms in the usual .NET convention (translation in
M41–M43, `Vector3.Transform` applies them, `child * parent` composes them). The reader and writer convert from and to
GDTF's matrix layout, which is the transpose. `geometry.AbsolutePosition` gives the transform relative to the fixture.

DMX helpers: `ChannelFunction.DmxTo` / `ChannelSet.DmxTo` give the computed end of a DMX range, and
`DmxMode.GetChannelInstances()` / `GetFootprint()` expand channels across geometry references into real addresses.
Files older than GDTF 1.1 are upgraded on read: the DMX channel `Default` moves to the matching channel function.

### Validation

The reader reports problems in the file; the validator checks a model however it was made (read, edited or built in
code) against the rules of the spec before you write it:

```csharp
var result = archive.Validate();          // or GdtfValidator.Validate(document)
if (!result.IsValid)
    foreach (var error in result.Errors) Console.WriteLine(error);
```

It checks unique names, required and dangling references (including links to objects of another fixture type),
geometry references and their breaks, overlapping DMX addresses, DMX ranges of channel functions and channel sets,
ModeMasters, relations and macros, physical descriptions, RDM values, and that the resources the model names exist in
the archive. Errors are spec violations, warnings are discouraged or ambiguous constructs.

Reading is lenient by default: problems are collected as diagnostics. Pass `new GdtfReadOptions { Strict = true }`
to throw on the first error.

## Development

```sh
./scripts/fetch-testdata.sh   # optional: gdtf.xsd and real fixture files (third-party licenses, not committed)
dotnet test
```

Tests that need the fetched files are skipped when they are missing.
