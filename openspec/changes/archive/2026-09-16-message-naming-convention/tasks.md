## 1. Add IDownloadResponse marker interface

- [x] 1.1 Create `IDownloadResponse` marker interface in `FunkArr.Messages.Download`
- [x] 1.2 Add `IDownloadResponse` to `DownloadAdded`, `QueueResult`, `HistoryResult`, `HistoryStatsResult`, `HistoryCategoriesResult`, `DeleteDownloadResult`, `RetryDownloadResult`, `WorkerStatusResult`
- [x] 1.3 Update Download API endpoint `Ask<object>` calls to `Ask<IDownloadResponse>` in `DownloadsApiEndpoints`

## 2. Rename Search internal routing messages

- [x] 2.1 Rename `TvSearchCommand` to `TvSearch` in `FunkArr.Messages.Search` (file and record)
- [x] 2.2 Rename `MovieSearchCommand` to `MovieSearch` in `FunkArr.Messages.Search` (file and record)
- [x] 2.3 Update `SearchManager` references to `TvSearch` and `MovieSearch`
- [x] 2.4 Update `TvSearchWorker` Receive handlers for `TvSearch`
- [x] 2.5 Update `MovieSearchWorker` Receive handlers for `MovieSearch`
- [x] 2.6 Update `ShardMessageExtractor` pattern match for renamed types
- [x] 2.7 Update test projects referencing `TvSearchCommand` / `MovieSearchCommand`

## 3. Verify and format

- [x] 3.1 Run `dotnet build src/FunkArr.slnx` and fix any compilation errors
- [x] 3.2 Run `dotnet format src/FunkArr.slnx` to apply code style
- [x] 3.3 Run test projects to verify no regressions
