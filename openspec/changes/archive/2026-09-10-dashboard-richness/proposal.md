## Why

The Dashboard is the first thing users see but only shows 3 stat cards (queued count, downloading count, speed) plus a health widget and top-3 active downloads. The backend holds significantly more data in actor state that never reaches the UI - queue position, history aggregates, disk space, metadata cache stats. Surfacing this data makes the Dashboard informative at a glance and fixes an inefficiency where the History view fetches 1000 items just to derive category names.

## What Changes

- Expose queued vs. active download counts separately (DownloadManagerState already tracks both)
- Add history aggregate stats endpoint (total completed/failed, total bytes, avg download time, success rate)
- Expose disk space on the internal API (SABnzbd adapter already computes this for Sonarr/Radarr)
- Add dedicated history categories endpoint to replace client-side derivation from 1000 items
- Wire existing `QueryCacheStats` message to a new endpoint for TMDB/TVDB cache visibility
- Enrich the Dashboard UI with richer stat cards, a stats row, storage indicator, and cache info
- Update the History view to use the new categories endpoint

## Capabilities

### New Capabilities
- `dashboard-stats`: Aggregated download statistics endpoint and Dashboard stat cards (queue split, history totals, success rate)
- `storage-status`: Disk space endpoint for complete/incomplete directories, surfaced on Dashboard
- `history-categories`: Dedicated endpoint returning distinct category values from download history
- `metadata-cache-status`: Endpoint exposing TMDB/TVDB cache entry counts and age

### Modified Capabilities
- `download-api-internal`: Add queue split fields (`QueuedCount`, `ActiveCount`) to queue response
- `download-history`: Add aggregate stats query message and response
- `download-queue-ui`: Richer stat cards showing active/queued split and total speed
- `download-history-ui`: Use categories endpoint instead of fetching all items for filter dropdown

## Impact

- **Messages**: New query/response records in Download and MetadataResolver domains
- **Api**: 3 new endpoints (`/api/downloads/history/stats`, `/api/downloads/history/categories`, `/api/health/storage`), 1 wired to existing message (`/api/health/cache`)
- **Api models**: Extended `DownloadQueueResponse`, new `HistoryStatsResponse`, `StorageStatusResponse`
- **Download domain**: New message handling in DownloadHistoryManager
- **UI**: Dashboard view rewrite (stat cards, stats row, storage bar), History view categories fix
- **No breaking changes** to existing API contracts - all additions
