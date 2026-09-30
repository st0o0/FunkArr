## 1. Interface and Extractor

- [x] 1.1 Create `IShardedMessage` interface in `Shared/IShardedMessage.cs` (`FunkArr.Shared`)
- [x] 1.2 Create `ShardedMessageExtractor` class in `Shared/ShardedMessageExtractor.cs` (`FunkArr.Shared`)
- [x] 1.3 Make `IWithNzoId` extend `IShardedMessage` with default interface method (`EntityKey => NzoId`)

## 2. Search Messages Implement IShardedMessage

- [x] 2.1 `TvSearchPipeline.Search` implements `IShardedMessage` with `EntityKey => TvdbId.ToString()`
- [x] 2.2 `MovieSearchPipeline.Search` drops `EntityKey` parameter, adds `IShardedMessage` with `EntityKey => ImdbId ?? $"q:{Query}"`
- [x] 2.3 `TextSearchPipeline.Search` implements `IShardedMessage` with `EntityKey => Query`
- [x] 2.4 Update `NewznabController` to use new `MovieSearchPipeline.Search(imdbId, q)` constructor

## 3. Remove Per-Entity Extractors

- [x] 3.1 Delete `Search/TvSearchPipelineMessageExtractor.cs`
- [x] 3.2 Delete `Search/MovieSearchPipelineMessageExtractor.cs`
- [x] 3.3 Delete `Search/TextSearchPipelineMessageExtractor.cs`
- [x] 3.4 Delete `DownloadClient/DownloadCoordinatorMessageExtractor.cs`
- [x] 3.5 Remove inline `DownloadRequestTrackerMessageExtractor` class from `DownloadRequestTracker.cs`
- [x] 3.6 Update `FunkArrActorSystemSetup` to use `ShardedMessageExtractor(20)` for search and `ShardedMessageExtractor(10)` for download shard regions

## 4. Namespace Restructuring — Search

- [x] 4.1 Create subfolders: `Search/Pipelines/`, `Search/Resolvers/`, `Search/Matching/`, `Search/Quality/`
- [x] 4.2 Move `TvSearchPipeline`, `MovieSearchPipeline`, `TextSearchPipeline`, `SearchPipelineBase` to `Search/Pipelines/` with namespace `FunkArr.Search.Pipelines`
- [x] 4.3 Move `SeriesResolver`, `MovieResolver`, `TvdbClient`, `TmdbClient` to `Search/Resolvers/` with namespace `FunkArr.Search.Resolvers`
- [x] 4.4 Move `MatchingPipeline`, `MatchContext`, `DateMatcher` to `Search/Matching/` with namespace `FunkArr.Search.Matching`
- [x] 4.5 Move `QualityProbeService`, `QualityExpander`, `HlsManifestParser`, `Mp4AtomParser`, `UrlPatternAnalyzer` to `Search/Quality/` with namespace `FunkArr.Search.Quality`

## 5. Namespace Restructuring — DownloadClient

- [x] 5.1 Create subfolders: `DownloadClient/Pipeline/`, `DownloadClient/Queue/`, `DownloadClient/Tracker/`, `DownloadClient/Ffmpeg/`
- [x] 5.2 Move `DownloadCoordinator`, `DownloadCoordinatorEvents`, `DownloadCoordinatorMessages`, `DownloadJob`, `DownloadOutcome`, `DownloadProgress`, all workers to `DownloadClient/Pipeline/` with namespace `FunkArr.DownloadClient.Pipeline`
- [x] 5.3 Move `QueueCoordinator`, `QueueCoordinatorEvents` to `DownloadClient/Queue/` with namespace `FunkArr.DownloadClient.Queue`
- [x] 5.4 Move `DownloadRequestTracker`, `DownloadRequestTrackerEvents`, `DownloadSourceType`, `FailureKind` to `DownloadClient/Tracker/` with namespace `FunkArr.DownloadClient.Tracker`
- [x] 5.5 Move `FfmpegService`, `IFfmpegService`, `FfmpegProgressParser` to `DownloadClient/Ffmpeg/` with namespace `FunkArr.DownloadClient.Ffmpeg`

## 6. Fix Using Statements and Build

- [x] 6.1 Update all `using` statements across Api, Configuration, Tests, and other referencing files
- [x] 6.2 Run `dotnet build` and fix any remaining compilation errors
- [x] 6.3 Run `dotnet format` to fix whitespace
- [x] 6.4 Run tests and verify all pass
