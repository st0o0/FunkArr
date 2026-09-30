## Why

SetupContainers have no clear ownership principle. ServiceSetupContainer is a grab-bag with registrations from multiple domains. Options from Scoring/History leak into RuleSetSetupContainer. ArrApi services live inside DownloadSetupContainer. AkkaSetupContainer registers every actor from every domain in one monolithic chain. Separately, the download remux layer splits across three classes with three interfaces and long parameter lists, and fails on SRF/ORF HLS streams due to missing stream filtering and corrupt ADTS tolerance.

## What Changes

### SetupContainer Redesign

- **New** `CoreSetupContainer` with FunkArrOptions, PostgresOptions, DataPaths, IDataFiles, IFileSystem, RoutingOptions, IRouteResolver, JSON config, directory health checks
- **Rename** `MediathekSetupContainer` to `SearchSetupContainer`, add Search domain actors (SearchManager, MediathekViewWebManager, TvSearchWorker, MovieSearchWorker) and MediathekViewWeb health check
- **Rework** `DownloadSetupContainer` to own only DownloadOptions, Remuxer, `/api/downloads` endpoints, Download domain actors (DownloadManager, DownloadScheduler, DownloadHistoryManager, DownloadWorker), FFmpeg health check
- **New** `ScoringSetupContainer` with ScoringOptions, ScoringHistoryOptions, Scoring/History actors (ScoringManager, StatsCollector, HistoryWorker)
- **Rework** `RuleSetSetupContainer` to own only RuleSetUpdaterOptions, IRuleSetValidator, RuleSetStore, GitHub HttpClient, `/api/rulesets` endpoints, RuleSet actors (RuleSetResolver, RuleSetManager, RuleSetUpdater, RuleSetWorker)
- **Rename** `MetadataSetupContainer` to `EnrichmentSetupContainer`, add Enrichment actors (EnrichmentManager, TvdbEnrichmentActor, TmdbEnrichmentActor)
- **New** `ArrApiSetupContainer` with Newznab/SABnzbd services, ApiKeyFilter, ArrApiClient, controller mapping
- **Slim** `AkkaSetupContainer` to only actor system, persistence, remoting, clustering. No actor registrations.
- **Remove** `ServiceSetupContainer` (contents distributed to domain containers)
- **Unchanged**: LoggingSetupContainer, TelemetrySetupContainer, ApplicationSetupContainer

### Remuxer Redesign

- **Remove** `IFfmpegRunner` / `FfmpegRunner` (absorbed into Remuxer)
- **Remove** `ISubtitlePreparer` / `SubtitlePreparer` (absorbed into Remuxer)
- **Keep** `IRemuxer` as the public interface (signature unchanged)
- **Add** `RemuxOptions` record with fluent builder for internal parameter passing
- **Rewrite** FFmpeg argument building: single flow, explicit `-map 0:v:0 -map 0:a:0`, HLS BSF tolerance (`-bsf:a aac_adtstoasc=no_validation=1`), FFMpegCore fluent API

## Capabilities

### New Capabilities

- `setup-container-layout`: Defines the per-domain container ownership principle and which registrations belong where
- `remux-options`: Fluent RemuxOptions builder for FFmpeg argument configuration

### Modified Capabilities

- `application-bootstrap`: Setup container chain changes (new names, new containers, removed ServiceSetupContainer)
- `ffmpeg-runner`: Interface removed, functionality absorbed into Remuxer
- `ffmpeg-process`: Argument building rewritten with stream mapping and HLS awareness
- `remuxer`: Absorbs FFmpegRunner and SubtitlePreparer, uses RemuxOptions internally
- `subtitle-preparer`: Interface removed, functionality absorbed into Remuxer

## Impact

- `src/FunkArr/Configuration/` - all SetupContainer files restructured
- `src/FunkArr/Program.cs` - updated container chain
- `src/FunkArr.Download/FfmpegRunner.cs` - removed
- `src/FunkArr.Download/IFfmpegRunner.cs` - removed
- `src/FunkArr.Download/SubtitlePreparer.cs` - removed (code moves to Remuxer)
- `src/FunkArr.Download/ISubtitlePreparer.cs` - removed
- `src/FunkArr.Download/Remuxer.cs` - rewritten with absorbed functionality
- `src/FunkArr.Download/DownloadServiceExtensions.cs` - simplified (fewer registrations)
- `src/FunkArr.Download.Tests/` - new BuildArguments tests, updated DI in existing tests
- No API changes, no persistence changes, no message changes
