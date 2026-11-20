# GDTFSharp

Read, write, create and validate [GDTF](https://gdtf.eu/) fixture type files in .NET.

GDTF (General Device Type Format, DIN SPEC 15800) describes lighting fixtures: their attributes, wheels, geometry,
physical data and DMX modes. GDTFSharp gives you the whole of GDTF 1.2 as a typed object model with resolved
references, reads and writes `.gdtf` archives, computes DMX footprints and checks fixtures against the spec.

- Targets **GDTF 1.2** (DIN SPEC 15800:2022). Files of GDTF 1.0 and 1.1 are read and upgraded; files are written as 1.2.
- `netstandard2.1`, no dependencies.
- Lenient reader with diagnostics (file, location, line) and an optional strict mode.
- Nothing is lost: unknown or vendor-specific content is kept and written back.

## Installation

```sh
dotnet add package GDTFSharp
```

## Reading a fixture

```csharp
using GDTFSharp;

var result = GdtfArchive.Load("Robe lighting s.r.o.@BMFL Spot.gdtf");
foreach (var diagnostic in result.Diagnostics)
    Console.WriteLine(diagnostic);   // e.g. "Error: '-1/1' is not a valid value for 'Highlight' at FixtureType[…]/… (line 812)"

var fixtureType = result.Archive.Document.FixtureType;
Console.WriteLine($"{fixtureType.Manufacturer} {fixtureType.Name}");

foreach (var mode in fixtureType.DmxModes)
{
    Console.WriteLine($"{mode.Name}: {mode.GetFootprint()[1]} channels");
    foreach (var channel in mode.Channels)
    {
        var function = channel.ChannelFunctions.First();
        Console.WriteLine($"  {string.Join(",", channel.Offsets),-6} {channel.Name}  " +
                          $"{function.Attribute?.Name} {function.PhysicalFrom}..{function.PhysicalTo} {function.Attribute?.PhysicalUnit}");
    }
}
```

Reading is lenient: problems are collected as diagnostics and a sensible value is used. Pass
`new GdtfReadOptions { Strict = true }` to throw a `GdtfReadException` on the first error instead.
`GdtfReader.Read(stream)` reads a bare `description.xml`.

## The model

`FixtureType` mirrors the spec, section by section. All references are real object references, resolved when reading
and turned back into GDTF node paths when writing:

| Section | Entry point |
|---|---|
| Attribute definitions | `AttributeDefinitions.ActivationGroups`, `.FeatureGroups` → `Features`, `.Attributes` → `SubPhysicalUnits` |
| Wheels | `Wheels` → `Slots` (with `Facets`, `AnimationSystem`, `Filter`, `MediaFileName`) |
| Physical descriptions | `PhysicalDescriptions.Emitters`, `Filters`, `ColorSpace`, `AdditionalColorSpaces`, `Gamuts`, `DmxProfiles`, `Cris`, `Connectors`, `Properties` |
| Models | `Models` |
| Geometries | `Geometries` → a tree of `Geometry` and its 17 subtypes (`Axis`, `Beam`, `Laser`, `WiringObject`, `GeometryReference`, …) |
| DMX modes | `DmxModes` → `Channels` → `LogicalChannels` → `ChannelFunctions` → `ChannelSets` / `SubChannelSets`, plus `Relations` and `Macros` |
| History, presets, protocols | `Revisions`, `FTPresets`, `Protocols.Rdm`, `.ArtNet`, `.Sacn` |

Child collections keep parent pointers (`feature.Group`, `geometry.Parent`, `channelFunction.LogicalChannel`, …), so
paths such as `Feature.Path` (`Position.PanTilt`) or `ChannelFunction.Path` (`Head_Dimmer.Dimmer.Dimmer 1`) are always
available. `Find…` helpers look nodes up by name.

### DMX helpers

- `ChannelFunction.DmxTo` and `ChannelSet.DmxTo`: the computed end of a DMX range, in the channel's resolution.
- `DmxChannel.ByteCount`, `MaxDmxValue`, `IsVirtual`, `EffectiveInitialFunction`; `ChannelFunction.EffectiveName`.
- `DmxMode.GetChannelInstances()`: channels as patched, expanded across `GeometryReference`s with their DMX offsets
  (`Overwrite` channels take the reference's last break); `GetFootprint()`: addresses used per DMX break.
- `DmxValue.ToResolution(bytes)`: converts `n/b` values with byte mirroring or (`n/bs`) byte shifting.
- `ChannelSet.WheelSlot`, `DmxProfile.Evaluate(x)`.

### Geometry positions

`Geometry.Position` is a `System.Numerics.Matrix4x4` in the usual .NET convention: build it with
`Matrix4x4.CreateTranslation`/`CreateRotationZ`, apply it with `Vector3.Transform`, compose with `child * parent`.
The reader and writer convert from and to GDTF's matrix layout, which is the transpose. `AbsolutePosition` gives the
transform relative to the fixture origin.

## Creating a fixture

```csharp
using GDTFSharp;
using GDTFSharp.Models;
using GDTFSharp.Types;

var fixture = new FixtureType(new Name("LED Par"), Guid.NewGuid()) { Manufacturer = "Generic", ShortName = "Par" };

var group = new FeatureGroup(new Name("Dimmer"));
var feature = new Feature(new Name("Dimmer"));
group.Features.Add(feature);
fixture.AttributeDefinitions.FeatureGroups.Add(group);
var dimmer = new GdtfAttribute(new Name("Dimmer"), feature) { PhysicalUnit = PhysicalUnit.LuminousIntensity };
fixture.AttributeDefinitions.Attributes.Add(dimmer);

var body = new Geometry(new Name("Body"));
body.Geometries.Add(new Beam(new Name("Beam")) { LampType = LampType.Led, BeamAngle = 25 });
fixture.Geometries.Add(body);

var mode = new DmxMode(new Name("1 channel")) { Geometry = body };
var channel = new DmxChannel(body);
channel.Offsets.Add(1);
var logical = new LogicalChannel(dimmer);
logical.ChannelFunctions.Add(new ChannelFunction(dimmer) { PhysicalTo = 1 });
channel.LogicalChannels.Add(logical);
mode.Channels.Add(channel);
fixture.DmxModes.Add(mode);

var archive = new GdtfArchive(new GdtfDocument(fixture));
archive.Resources["thumbnail.png"] = File.ReadAllBytes("par.png");
fixture.Thumbnail = "thumbnail";
archive.Save("Generic@LED Par.gdtf");
```

## Validation

The reader reports problems in a file; the validator checks a model however it was made (read, edited or built in
code) before you write it:

```csharp
var validation = archive.Validate();      // or GdtfValidator.Validate(document) for the model only
foreach (var diagnostic in validation.Diagnostics)
    Console.WriteLine(diagnostic);        // "Error: Address 3 of break 1 is used by 'Body_Gobo1' and … at FixtureType/DMXModes/…"
if (!validation.IsValid) return;
```

It checks unique names, required and dangling references (including links to objects of another fixture type),
geometry references and their breaks, overlapping DMX addresses and addresses above 512, DMX ranges of channel
functions and channel sets, ModeMasters, relations and macros, physical descriptions and RDM values; the archive
variant also checks that the thumbnail, wheel media and model files exist. Errors are spec violations, warnings are
discouraged or ambiguous constructs.

## Notes on the spec

Where DIN SPEC 15800 leaves room, GDTFSharp follows [libMVRgdtf](https://github.com/mvrdevelopment/libMVRgdtf), the
reference implementation by the GDTF maintainers:

- DMX ranges of channel functions chain across all logical channels of a DMX channel, in document order.
- `Overwrite` DMX channels use the last `Break` of a geometry reference; `WheelSlotIndex="0"` means "no slot".
- Gamut points are written as `{x,y,Y}{x,y,Y}…`; SubChannelSet sub physical units by their type name.
- GDTF 1.0 files: the DMX channel `Default` moves to the channel function whose range contains it (which becomes the
  `InitialFunction`); channel function names are not checked against the `Name` rules.

The [official XSD](https://github.com/mvrdevelopment/tools) disagrees with the spec in a few places; GDTFSharp follows
the spec, so such files are valid GDTF but fail the XSD: several Art-Net/sACN `Map`s, several `Gamut`s, several DMX
profile `Point`s, filter measurements with `Physical="0"`, several `PinPatch`es, and Inventory/Structure/Support
geometries without a `Geometry` attribute. The XSD's `Gammut` spelling is read, `Gamut` is written.

## Development

```sh
./scripts/fetch-testdata.sh   # gdtf.xsd and sample fixtures (third-party licenses, not committed)
dotnet test
```

Tests that need the fetched files are skipped when they are missing. The test project runs on
Microsoft.Testing.Platform (xUnit v3), configured in `global.json`.

## Releasing

1. Set `<Version>` in `GDTFSharp/GDTFSharp.csproj` and move the `CHANGELOG.md` entry out of "unreleased".
2. Push a tag `v<version>`, e.g. `v0.1.0`. CI builds, tests, packs and pushes the package and its symbols to nuget.org
   with [trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing): no API key is stored,
   the job exchanges its GitHub OIDC token for a short-lived key.

One-time setup:

- On nuget.org (as the account or organization that should own the package): *Trusted Publishing* → add a policy with
  repository owner `DMXControl`, repository `GDTFSharp`, workflow file `ci.yml`, environment `nuget`, and a scope that
  allows publishing new packages (for the first release of `GDTFSharp`).
- On GitHub: create the environment `nuget` and add the secret `NUGET_USER` with the nuget.org profile name (not the
  email address) of the policy owner.
- A new policy can be only temporarily active for 7 days until the first successful publish binds it to the
  repository; publish within that window or restart it on nuget.org.

## License

MIT, see [LICENSE.md](LICENSE.md).
