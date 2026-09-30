## Why

11 show rulesets match Mediathek content correctly but never produce downloads because FunkArr assigns season/episode numbers that don't match TVDB's numbering scheme. Sonarr searches for TVDB episode numbers (e.g. S1E110 for Das Traumschiff) but FunkArr returns Mediathek-native numbers (e.g. S31E03). The root cause: enrichment (TVDB lookup) only runs when scoring produces no S/E at all — if the regex already extracted S/E from the Mediathek title, enrichment is skipped even though those numbers are wrong for TVDB.

A second group of 4 shows uses title-only or airdate strategies that extract no S/E at all, but their enrichment either isn't enabled or fails silently, leaving Newznab results without season/episode attributes that Sonarr can't match.

## What Changes

- Always run enrichment for matched items when enrichment is enabled and a TVDB ID exists, even when scoring already extracted S/E — enrichment results override regex-extracted S/E since TVDB is the authoritative source for Sonarr
- Handle absolute episode numbering in search results: when only an episode number exists without a season, populate the Newznab season attribute (default to season 1) so Sonarr can match absolute-numbered shows
- Improve enrichment candidate selection: include items that already have S/E from regex, not just items with null S/E

## Capabilities

### New Capabilities

### Modified Capabilities
- `episode-resolution`: Change enrichment candidate filter to include items with regex-extracted S/E, allowing enrichment to override Mediathek numbering with TVDB numbering
- `search-worker-state`: Update `TryGetEnrichmentRequest` to send all matched items for enrichment (not just those without S/E), and update `Apply(EnrichEpisodesCompleted)` to override existing S/E with enrichment results

## Impact

- **FunkArr.Search/TvSearchWorkerState.cs**: `TryGetEnrichmentRequest()` line 216 filter change, `Apply(EnrichEpisodesCompleted)` override logic
- **FunkArr.Search/ReleaseVariant.cs**: Handle absolute episode numbering (episode without season)
- **FunkArr.Enrichment**: No changes — the enrichment actor already resolves correctly, the problem is that it's never asked
- **FunkArr.Messages**: No changes — existing types support the flow
- **Existing tests**: Filter tests in TvSearchWorkerState need updating for new candidate selection
