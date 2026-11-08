## Why

Sonarr and Radarr poll FunkArr's SABnzbd API for queue/history updates. Several data-layer bugs prevent correct import detection: history returns oldest-first (so new completions are invisible beyond the poll limit), the raw category from `addfile` is lost (Radarr never matches its own items), `pubDate` is empty when `AiredAt` is null (Arr drops the whole feed), and SABnzbd retry deletes the failed history entry before confirming the retry succeeded (losing the record on failure).

## What Changes

- **Reverse history ordering**: `ToHistoryResult` returns newest-first so Arr always sees recent completions within its `limit` window.
- **Fix category mapping**: Change `MapMediaTypeToCategory` to return `"movies"` (plural) for Movie, matching what `get_config.movie_categories` advertises and what Radarr registers with. This fixes the Radarr category mismatch without threading a raw string through the pipeline.
- **pubDate fallback**: When `AiredAt` is null, fall back to the current UTC time so every search result item has a valid `pubDate` and the feed never triggers `UnsupportedFeedException` in Arr.
- **Safe retry**: Move history entry deletion to after the retry succeeds (DownloadManager confirms re-queue) instead of deleting before dispatching.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `download-history`: History query results are returned newest-first. Trimming still removes oldest records.
- `sabnzbd-download-api`: `MapMediaTypeToCategory` returns `"movies"` for Movie (matching config and Radarr registration).
- `download-retry`: SABnzbd retry removes the history entry only after the retry is confirmed successful by DownloadManager.
- `newznab-indexer-api`: Search result items always have a valid `pubDate` (falls back to current UTC when `AiredAt` is null).

## Impact

- **ArrApi**: `SabnzbdResponseMapper.MapMediaTypeToCategory` returns `"movies"` for Movie. No message or persistence changes needed.
- **Download domain**: `DownloadHistoryManagerState.ToHistoryResult` reverses before Skip/Take.
- **Search domain**: `NewznabSearchService` pubDate fallback.
- **Tests**: Unit tests for history ordering, category echo, pubDate fallback, retry safety.
