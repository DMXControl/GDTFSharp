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
| Wheels, PhysicalDescriptions, Models, Geometries, DMXModes, Revisions, FTPresets, Protocols | not yet modelled; kept verbatim on read and written back unchanged |

## Usage

```csharp
var result = GdtfArchive.Load("Robe@BMFL Spot.gdtf");
foreach (var diagnostic in result.Diagnostics) Console.WriteLine(diagnostic);

var fixtureType = result.Archive.Document.FixtureType;
Console.WriteLine($"{fixtureType.Manufacturer} {fixtureType.Name}");

foreach (var attribute in fixtureType.AttributeDefinitions.Attributes)
    Console.WriteLine($"{attribute.Name} ({attribute.Feature?.Path}, {attribute.PhysicalUnit})");

result.Archive.Save("copy.gdtf");
```

Reading is lenient by default: problems are collected as diagnostics. Pass `new GdtfReadOptions { Strict = true }`
to throw on the first error.

## Development

```sh
./scripts/fetch-testdata.sh   # optional: gdtf.xsd and real fixture files (third-party licenses, not committed)
dotnet test
```

Tests that need the fetched files are skipped when they are missing.
