# Tasks: Uniform Actor Naming

## Tasks

- [x] Dissolve `Search/Pipelines/` — move files to `Search/`, update namespace from `FunkArr.Search.Pipelines` to `FunkArr.Search`, remove stale `using` statements
- [x] Rename Search actors: `BrowseCoordinator` → `BrowseActor`, `MediathekGatewayWorker` → `MediathekGatewayActor`, `SearchPipelineBase` → `SearchActorBase`, `TextSearchPipeline` → `TextSearchActor`, `TvSearchPipeline` → `TvSearchActor`, `MovieSearchPipeline` → `MovieSearchActor`
- [x] Rename RuleSet actors: `RuleSetCoordinator` → `RuleSetActor`, `RuleSetGeneratorWorker` → `RuleSetGeneratorActor`, `RefreshWorker` → `RefreshActor`, `MatchQualityWorker` → `MatchQualityActor`
- [x] Rename DownloadClient actors: `QueueCoordinator` → `QueueActor`, `DownloadCoordinator` → `DownloadActor`, `DownloadRequestTracker` → `DownloadRequestActor`, `HlsDownloadWorker` → `HlsDownloadActor`, `Mp4DownloadWorker` → `Mp4DownloadActor`, `RemuxWorker` → `RemuxActor`, `SubtitleDownloadWorker` → `SubtitleDownloadActor`, `SubtitleConvertWorker` → `SubtitleConvertActor`, `SubtitleExtractWorker` → `SubtitleExtractActor`
- [x] Verify all PersistenceId strings unchanged (DownloadCoordinator, DownloadRequestTracker, QueueCoordinator, MatchQualityWorker, SeriesResolver, MovieResolver)
- [x] Verify shard region type names — pin explicit `typeName` strings if they derived from class name
- [x] Update `SearchCoordinatorMessages.cs` references if any actor types are mentioned
- [x] Update CLAUDE.md naming convention section
- [x] Update OpenSpec main specs that reference old actor names
- [x] Build + run tests
