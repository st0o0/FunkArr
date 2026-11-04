## 1. FunkArr.Core

- [x] 1.1 Create subfolders: `Actors/`, `Data/` (Configuration/ already exists)
- [x] 1.2 Update namespaces in `Configuration/` files from `FunkArr.Core` to `FunkArr.Core.Configuration`
- [x] 1.3 Move `ActorKeys.cs`, `ShardMessageExtractor.cs` to `Actors/`, update namespaces to `FunkArr.Core.Actors`
- [x] 1.4 Move `DataPaths.cs`, `DataFiles.cs`, `IDataFiles.cs` to `Data/`, update namespaces to `FunkArr.Core.Data`
- [x] 1.5 Update `using` statements solution-wide for moved Core types
- [x] 1.6 `dotnet build src/FunkArr.slnx` — verify zero errors/warnings
- [x] 1.7 `dotnet format src/FunkArr.slnx --verify-no-changes`

## 2. FunkArr.Download + Tests

- [x] 2.1 Create subfolders in FunkArr.Download: `Manager/`, `Scheduler/`, `History/`, `Worker/`, `Ffmpeg/`, `Subtitles/`
- [x] 2.2 Move `DownloadManager.cs` + `DownloadManagerState.cs` to `Manager/`, update namespaces
- [x] 2.3 Move `DownloadScheduler.cs` + `DownloadScheduleHelper.cs` to `Scheduler/`, update namespaces
- [x] 2.4 Move `DownloadHistoryManager.cs` + `DownloadHistoryManagerState.cs` to `History/`, update namespaces
- [x] 2.5 Move `DownloadWorker.cs` + `DownloadWorkerState.cs` to `Worker/`, update namespaces
- [x] 2.6 Move FFmpeg files to `Ffmpeg/`, update namespaces to `FunkArr.Download.Ffmpeg`
- [x] 2.7 Move Subtitle files to `Subtitles/`, update namespaces to `FunkArr.Download.Subtitles`
- [x] 2.8 Create matching subfolders in FunkArr.Download.Tests: `Manager/`, `Worker/`, `Scheduler/`, `Ffmpeg/`, `Subtitles/`
- [x] 2.9 Move test files to matching subfolders, update namespaces to `FunkArr.Download.Tests.<Subfolder>`
- [x] 2.10 Update `using` statements solution-wide for moved Download types
- [x] 2.11 `dotnet build` + `dotnet format` — verify clean

## 3. FunkArr.Search + Tests

- [x] 3.1 Create subfolders in FunkArr.Search: `Manager/`, `Workers/`, `Client/`, `Models/`
- [x] 3.2 Move `SearchManager.cs` + `SearchManagerState.cs` to `Manager/`, update namespaces
- [x] 3.3 Move all 4 worker + state files to `Workers/`, update namespaces to `FunkArr.Search.Workers`
- [x] 3.4 Move Mediathek client files to `Client/`, update namespaces to `FunkArr.Search.Client`
- [x] 3.5 Move domain model files to `Models/`, update namespaces to `FunkArr.Search.Models`
- [x] 3.6 Create matching subfolders in FunkArr.Search.Tests: `Manager/`, `Workers/`, `Client/`, `Models/`
- [x] 3.7 Move test files to matching subfolders, update namespaces to `FunkArr.Search.Tests.<Subfolder>`
- [x] 3.8 Update `using` statements solution-wide for moved Search types
- [x] 3.9 `dotnet build` + `dotnet format` — verify clean

## 4. FunkArr.Feed + Tests

- [x] 4.1 Create subfolders in FunkArr.Feed: `Crawl/`, `Store/`, `Eval/`, `Page/`, `Scheduler/`
- [x] 4.2 Move crawl actors + state to `Crawl/`, update namespaces to `FunkArr.Feed.Crawl`
- [x] 4.3 Move `FeedStoreManager.cs` + state to `Store/`, update namespaces to `FunkArr.Feed.Store`
- [x] 4.4 Move `FeedEvalActor.cs` + `FeedEvalMessages.cs` to `Eval/`, update namespaces to `FunkArr.Feed.Eval`
- [x] 4.5 Move `FeedPageActor.cs` + `FeedPageMessages.cs` to `Page/`, update namespaces to `FunkArr.Feed.Page`
- [x] 4.6 Move `FeedScheduler.cs` to `Scheduler/`, update namespace to `FunkArr.Feed.Scheduler`
- [x] 4.7 Create matching subfolders in FunkArr.Feed.Tests: `Crawl/`, `Store/`
- [x] 4.8 Move test files to matching subfolders, update namespaces to `FunkArr.Feed.Tests.<Subfolder>`
- [x] 4.9 Update `using` statements solution-wide for moved Feed types
- [x] 4.10 `dotnet build` + `dotnet format` — verify clean

## 5. FunkArr.Enrichment + Tests

- [x] 5.1 Create subfolders in FunkArr.Enrichment: `Tvdb/`, `Tmdb/`
- [x] 5.2 Move `TvdbEnrichmentActor.cs` to `Tvdb/`, update namespace to `FunkArr.Enrichment.Tvdb`
- [x] 5.3 Move `TmdbEnrichmentActor.cs` to `Tmdb/`, update namespace to `FunkArr.Enrichment.Tmdb`
- [x] 5.4 Create matching subfolders in FunkArr.Enrichment.Tests: `Tvdb/`, `Tmdb/`
- [x] 5.5 Move test files to matching subfolders, update namespaces to `FunkArr.Enrichment.Tests.<Subfolder>`
- [x] 5.6 Update `using` statements solution-wide for moved Enrichment types
- [x] 5.7 `dotnet build` + `dotnet format` — verify clean

## 6. FunkArr.RuleSet + Tests

- [x] 6.1 Create subfolders in FunkArr.RuleSet: `Manager/`, `Resolver/`, `Worker/`, `Updater/`
- [x] 6.2 Move `RuleSetManager.cs` + `RuleSetManagerState.cs` to `Manager/`, update namespaces
- [x] 6.3 Move `RuleSetResolver.cs` + `RuleSetResolverState.cs` to `Resolver/`, update namespaces
- [x] 6.4 Move `RuleSetWorker.cs` + `RuleSetWorkerState.cs` to `Worker/`, update namespaces
- [x] 6.5 Move `RuleSetUpdater.cs` to `Updater/`, update namespace to `FunkArr.RuleSet.Updater`
- [x] 6.6 Create matching subfolders in FunkArr.RuleSet.Tests: `Manager/`, `Resolver/`, `Worker/`, `DiskModel/`
- [x] 6.7 Move test files to matching subfolders, update namespaces to `FunkArr.RuleSet.Tests.<Subfolder>`
- [x] 6.8 Update `using` statements solution-wide for moved RuleSet types
- [x] 6.9 `dotnet build` + `dotnet format` — verify clean

## 7. Verification

- [x] 7.1 Run all test projects to verify nothing broke
- [x] 7.2 Check ArchUnitNET tests for namespace-dependent assertions, update if needed
