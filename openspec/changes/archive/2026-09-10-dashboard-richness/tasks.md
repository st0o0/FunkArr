## 1. Messages - New query/response records

- [x] 1.1 Add `QueryHistoryStats` and `HistoryStatsResult(int TotalCompleted, int TotalFailed, long TotalBytes, int AverageDownloadTimeSeconds, double SuccessRate)` to `FunkArr.Messages/Download/`
- [x] 1.2 Add `QueryHistoryCategories` and `HistoryCategoriesResult(string[] Categories)` to `FunkArr.Messages/Download/`

## 2. Download domain - DownloadHistoryManager extensions

- [x] 2.1 Add `ToHistoryStats` extension method on `DownloadHistoryManagerState` computing aggregates from records
- [x] 2.2 Add `ToHistoryCategories` extension method on `DownloadHistoryManagerState` returning distinct sorted categories
- [x] 2.3 Add `Receive<QueryHistoryStats>` and `Receive<QueryHistoryCategories>` handlers in `DownloadHistoryManager`
- [x] 2.4 Add tests for `ToHistoryStats` and `ToHistoryCategories` in `DownloadHistoryManagerStateTests`

## 3. API models and queue response extension

- [x] 3.1 Add `ActiveCount` and `QueuedCount` fields to `DownloadQueueResponse`
- [x] 3.2 Update `ToQueueResponse` in `QueueApiEndpoints` to populate the new fields from `QueueResult.Items` status counts
- [x] 3.3 Add `HistoryStatsResponse` record to `FunkArr.Api/Models/`
- [x] 3.4 Add `StorageStatusResponse` record with `StorageDirectory(string Path, long AvailableBytes, long TotalBytes)` to `FunkArr.Api/Models/`
- [x] 3.5 Add `CacheStatsResponse` record to `FunkArr.Api/Models/`

## 4. API endpoints - History stats and categories

- [x] 4.1 Add `GET /api/downloads/history/stats` endpoint in `QueueApiEndpoints` - Ask DownloadHistoryManager for `HistoryStatsResult`, map to `HistoryStatsResponse`
- [x] 4.2 Add `GET /api/downloads/history/categories` endpoint in `QueueApiEndpoints` - Ask DownloadHistoryManager for `HistoryCategoriesResult`, return string array

## 5. API endpoints - Storage and cache

- [x] 5.1 Add `GET /api/health/storage` endpoint in `SetupApiEndpoints` - Use `DriveInfo` for configured complete/incomplete directories, return `StorageStatusResponse`
- [x] 5.2 Add `GET /api/health/cache` endpoint in `SetupApiEndpoints` - Ask MetadataResolverManager for `CacheStatsResult`, map to `CacheStatsResponse`

## 6. Frontend API layer

- [x] 6.1 Add `fetchHistoryStats()`, `fetchHistoryCategories()` to `api/downloads.ts`
- [x] 6.2 Add `fetchStorageStatus()`, `fetchCacheStats()` to `api/setup.ts`

## 7. Frontend - Dashboard enrichment

- [x] 7.1 Update Dashboard stat cards to show "Active: N / M" and "Queued: N" from SSE stream's `activeCount`/`queuedCount`/`totalSlots`
- [x] 7.2 Add stats row section to Dashboard showing history aggregates (total completed, failed, success rate, total size, avg time)
- [x] 7.3 Add storage indicator to Dashboard showing disk usage bar with formatted labels and warning color at >90%
- [x] 7.4 Add cache stats display to Dashboard showing TVDB/TMDB entry counts

## 8. Frontend - History categories fix

- [x] 8.1 Replace `fetchCategories()` in `History.vue` (which loads 1000 items) with call to `GET /api/downloads/history/categories`

## 9. API endpoint tests

- [x] 9.1 Add tests for history stats and categories endpoints in `FunkArr.Api.Tests`
