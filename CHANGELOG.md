# Changelog

## 0.1.0 – unreleased

First release: the complete GDTF 1.2 format (DIN SPEC 15800:2022).

- Read and write `.gdtf` archives and `description.xml`; resources (thumbnails, wheel media, models) are kept.
- Typed model of every section: attribute definitions, wheels, physical descriptions, models, geometries (all 18
  types), DMX modes, revisions, presets and protocols. References are resolved to objects and written back as paths.
- Lenient reader with diagnostics (severity, location, line) and a strict mode; unknown content is kept verbatim.
- GDTF 1.0 and 1.1 files are read and upgraded; output is always GDTF 1.2.
- DMX helpers: computed DMX ranges, channel instances across geometry references, DMX footprints, DMX value conversion.
- Validation of fixtures against the spec (`GdtfValidator`, `GdtfArchive.Validate()`).
