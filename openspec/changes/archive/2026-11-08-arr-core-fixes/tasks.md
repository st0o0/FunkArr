## Tasks

### History ordering (newest-first)

- [x] In `DownloadHistoryManagerState.ToHistoryResult`, reverse the record order before applying Skip/Take so newest records come first
- [x] Add unit test: history with 20 records, query limit=5 returns the 5 newest
- [x] Add unit test: history with offset=5 limit=5 skips the 5 newest, returns next 5

### Category mapping fix

- [x] Change `SabnzbdResponseMapper.MapMediaTypeToCategory` to return `"movies"` for `MediaType.Movie`
- [x] Update `SabnzbdQueueService.GetConfig` category classification to match (movie_categories contains categories with "movie" or "movies") — already correct
- [x] Update existing unit tests for the new category string (`"movie"` → `"movies"`)

### pubDate fallback

- [x] In `NewznabSearchService`, when building RSS items, fall back to `DateTimeOffset.UtcNow` when `AiredAt` is null or 0
- [x] Add unit test: search result with null AiredAt gets a valid pubDate

### Safe retry (delete history entry only on success)

- [x] In `SabnzbdDownloadService.RetryDownload`, reorder: send retry command → await response → only on success remove history entry
- [x] Add unit test: successful retry removes history entry
- [x] Add unit test: failed retry preserves history entry
