## Context

The Dashboard (`Home.vue`) currently shows 3 stat cards derived from the SSE queue stream (queued count, downloading count, speed), a health widget polling every 30s, and a top-3 active downloads widget. The History view fetches up to 1000 items to derive category names for its filter dropdown.

The backend already holds richer data in actor state:
- `DownloadManagerState` tracks queued vs. dispatched lists separately, but the API flattens them
- `DownloadHistoryManagerState` holds all records in memory - aggregates are trivially derivable
- The SABnzbd adapter builds disk space info for Sonarr/Radarr, but the internal API has no equivalent
- `MetadataResolverManager` already handles `QueryCacheStats` - just needs an endpoint
- `DownloadHistoryManagerState.Records` contains all categories, no need for the UI to fetch all items

## Goals / Non-Goals

**Goals:**
- Surface queue split (active/queued/total slots) on Dashboard and queue response
- Expose download history aggregate stats (totals, success rate, avg time)
- Expose disk space info on the internal API
- Add dedicated history categories endpoint
- Wire existing `QueryCacheStats` to an endpoint
- Enrich Dashboard UI with all the above

**Non-Goals:**
- Historical trends / time-series charts (future work)
- Modifying the SABnzbd adapter API
- Real-time SSE for stats (poll on Dashboard mount is sufficient)
- Changing the existing queue SSE stream structure (extend only)

## Decisions

### 1. Queue split: extend existing response vs. separate endpoint

**Decision**: Extend `DownloadQueueResponse` with `QueuedCount` and `ActiveCount` fields.

The `QueueResult` message already carries `TotalItems` which is unused. Adding counts to the existing response means the SSE stream automatically includes them - no additional endpoint needed. The Dashboard and Queue page both consume the same stream.

**Alternative considered**: Separate `/api/downloads/queue/stats` endpoint. Rejected - duplicates the Ask to the same actor and the SSE stream already refreshes every 3s.

### 2. History stats: new message in DownloadHistoryManager

**Decision**: Add `QueryHistoryStats` message → `HistoryStatsResult` response. Compute aggregates from in-memory `DownloadHistoryManagerState.Records`.

The state already holds all records. Computing totals/averages is O(n) over the list on each query but history sizes are bounded (thousands, not millions). A new `ToHistoryStats` extension method on the state keeps the pattern consistent with `ToHistoryResult`.

**Alternative considered**: Cache aggregates incrementally on each Apply. Rejected - premature optimization for bounded data sizes.

### 3. History categories: new message in DownloadHistoryManager

**Decision**: Add `QueryHistoryCategories` message → `HistoryCategoriesResult(string[] Categories)`. Derive distinct categories from in-memory state.

Same rationale as stats - the data is already there, just needs a thin query path.

### 4. Disk space: new endpoint in SetupApiEndpoints

**Decision**: Add `GET /api/health/storage` returning disk space for configured complete and incomplete directories. Use `DriveInfo` to get available/total space.

The SABnzbd adapter computes `diskspace1`/`diskspace2` as strings in GB. For the internal API, return structured data with bytes so the UI can format it. Place it under `/api/health/` since it's system status, not download-specific.

**Alternative considered**: Reuse SABnzbd's computation. Rejected - it's in the adapter project (wrong dependency direction). The computation itself is trivial (`DriveInfo`).

### 5. Cache stats: wire existing message to new endpoint

**Decision**: Add `GET /api/health/cache` that sends `QueryCacheStats` to `MetadataResolverManager` and returns the `CacheStatsResult`.

Zero domain work needed - the actor already handles this message. Only an API endpoint and response model.

### 6. Dashboard UI: stats from multiple sources

**Decision**: Dashboard fetches stats via one-shot API calls on mount (history stats, storage, cache) alongside the existing SSE stream (queue data). No new SSE streams.

Stats data changes slowly - polling on mount (and optionally on a 60s interval) is sufficient. The SSE stream covers the fast-changing queue data.

## Risks / Trade-offs

- **History stats are computed on every request** - For large histories this could become slow, but records are bounded and the query is rare (Dashboard mount only). If it becomes a problem, caching can be added later.
- **Disk space requires `DriveInfo` permission** - In Docker containers the paths are mounted volumes, so `DriveInfo` reflects the host filesystem. This is correct behavior for monitoring.
- **Dashboard makes 3 additional API calls on mount** - Acceptable latency since they're parallel and fast (all in-memory data). SSE stream still handles the real-time portion.
