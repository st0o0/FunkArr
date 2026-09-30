## Why

Prowlarr, Sonarr, and Radarr each run a multi-step TestConnection sequence when a user adds FunkArr as an indexer or download client. While the current implementation passes the critical checks, several gaps cause suboptimal behavior: missing `tmdbid` support limits Radarr's primary search capability, incomplete `get_config` fields risk deserialization issues in future *arr versions, and the Caps XML lacks metadata elements that Prowlarr uses for display. Fixing these now ensures a clean, warning-free integration experience.

## What Changes

- Add `tmdbid` to movie-search `supportedParams` in Newznab Caps (Radarr's primary ID system)
- Add `tmdbid` query parameter binding to `IndexerRequest`
- Add `<server title="FunkArr"/>` element to Caps XML
- Add `<book-search available="no" supportedParams=""/>` to Caps XML (Prowlarr checks it)
- Add `enable_date_sorting`, `tv_categories`, `movie_categories`, `date_categories` fields to SABnzbd `get_config` response
- Add `output` query parameter binding to `DownloadGetRequest` (accepted, ignored — FunkArr always returns JSON)
- Add tests verifying the exact TestConnection request/response sequences for all three *arr apps

## Capabilities

### New Capabilities

_None — all changes are within existing capability boundaries._

### Modified Capabilities

- `newznab-indexer-api`: Add `tmdbid` to movie-search supportedParams, add `<server>` and `<book-search>` elements to Caps XML, add `tmdbid` to IndexerRequest parameter binding
- `sabnzbd-download-api`: Add missing `get_config` fields (`enable_date_sorting`, `tv_categories`, `movie_categories`, `date_categories`), accept `output` query parameter

## Impact

- **FunkArr.IndexerApi**: `Caps.cs` (add Server and BookSearch), `IndexerRequest.cs` (add TmdbId param), `CapsJsonProjection.cs` (add server/book-search to JSON)
- **FunkArr.DownloadApi**: `DownloadApiEndpoints.cs` (extend `BuildConfig`), `DownloadGetRequest.cs` (add Output param)
- **FunkArr.IndexerApi.Tests**: New/updated tests for Caps structure, tmdbid parameter
- **FunkArr.DownloadApi.Tests**: New/updated tests for get_config completeness
- No breaking changes, no new dependencies, no domain project changes
