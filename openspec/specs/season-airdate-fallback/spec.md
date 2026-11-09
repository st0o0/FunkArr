# season-airdate-fallback Specification

## Purpose
TBD - created by archiving change fix-newznab-title-search. Update Purpose after archive.
## Requirements
### Requirement: Remap season to air year after enrichment

When TVDB enrichment resolves an episode but the resolved season does not match
the requested season, AND the source item's `AiredAt.Year` equals the requested
season: remap `Identity.Season` to `AiredAt.Year.ToString()`. This handles TVDB
layouts where Tatort is season 1 but Sonarr sends season 2026.

### Requirement: Airdate season fallback in Newznab attributes

When `Metadata.Season` is null and `item.AiredAt` is available, emit
`newznab:attr name="season"` with `AiredAt.Year` as value. No fallback episode
attribute is emitted (no reliable way to derive episode number from date alone).

