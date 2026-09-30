## Context

FunkArr exposes two API surfaces for *arr ecosystem integration: a Newznab-compatible indexer API (`/index/api`) and a SABnzbd-compatible download client API (`/download/api`). Prowlarr, Sonarr, and Radarr each run a multi-step TestConnection sequence when a user adds FunkArr. The current implementation passes all critical checks, but several gaps reduce compatibility:

- Radarr primarily uses TMDB IDs for movie lookups, but FunkArr's Caps only advertise `q,imdbid` for movie-search
- SABnzbd `get_config` response is missing fields that Sonarr reads (`enable_date_sorting`, category lists)
- Caps XML lacks `<server>` and `<book-search>` elements that Prowlarr parses

All changes are confined to the two adapter projects (IndexerApi, DownloadApi) — no domain logic is affected.

## Goals / Non-Goals

**Goals:**
- Pass TestConnection in Prowlarr, Sonarr, and Radarr with zero warnings (except "no results" which is expected until Search domain is built)
- Ensure `get_config` response is complete enough that future *arr versions won't break on missing fields
- Advertise `tmdbid` support so Radarr can use its primary ID system when search is wired up

**Non-Goals:**
- Implementing actual `tmdbid` search logic (that's a Search domain concern, not adapter)
- Changing the auth error HTTP status code (403 matches real SABnzbd behavior)
- Adding `rid` or `traktid` to supportedParams (no data source for these)

## Decisions

### 1. Add `tmdbid` to movie-search supportedParams

Radarr checks for `q`, `tmdbid`, or `imdbid` in movie-search capabilities. While `imdbid` already satisfies the test, Radarr's primary ID system is TMDB. Advertising `tmdbid` now means the IndexerRequest model is ready when the Search domain implements TMDB-based lookups.

Alternative: Leave as-is — test passes with `imdbid` alone. Rejected because it would require a separate change later just to add a query parameter.

### 2. Add `<server>` element to Caps XML

Prowlarr displays the server title in its indexer list. Format: `<server title="FunkArr"/>`. This is a new XML model class `Server` added to `Caps`. Minimal change, improves Prowlarr UX.

### 3. Add `<book-search>` to Caps XML

Prowlarr parses all five search types including `book-search`. Adding it as `available="no"` prevents any future Prowlarr version from failing on a missing element. Follows the existing `audio-search` pattern.

### 4. Complete `get_config` misc fields

Sonarr reads `enable_date_sorting` and the `*_categories` arrays from `config.misc`. These are only checked when the corresponding `enable_*_sorting` is `true` (all false in FunkArr), but including them:
- Prevents null-reference issues if *arr deserialization changes
- Makes the response match real SABnzbd's structure exactly

All new fields use safe defaults: `enable_date_sorting: false`, empty arrays for `*_categories`.

### 5. Accept `output` query parameter on DownloadApi

SABnzbd proxy appends `&output=json` to every request. FunkArr already returns JSON unconditionally, but the parameter should be explicitly bound to avoid ASP.NET logging warnings about unknown query parameters. The value is accepted but ignored.

## Risks / Trade-offs

- [Advertising `tmdbid` before search supports it] → Radarr may send `tmdbid`-only searches that return empty results. Mitigation: this is the same behavior as today (empty results), and the warning is non-fatal.
- [Hardcoded SABnzbd version "4.3.3"] → Future *arr versions may raise the minimum. Mitigation: version is easily configurable later; 4.3.3 is well above the 0.7.0 minimum and below any bleeding-edge version checks.
