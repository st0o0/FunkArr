## ADDED Requirements

### Requirement: Core project folder structure
FunkArr.Core SHALL organize files into the following subfolders with matching namespaces:
- `Options/` → all `*Options` classes and `*Validator` classes → `FunkArr.Core.Options`
- `Actors/` → `ActorKeys`, `ShardMessageExtractor` → `FunkArr.Core.Actors`
- `Data/` → `DataPaths`, `DataFiles`, `IDataFiles` → `FunkArr.Core.Data`
- Root → remaining abstractions, extensions, and markers → `FunkArr.Core`

#### Scenario: Options grouped in Options folder
- **WHEN** a file defines an `*Options` or `*OptionsValidator` class in FunkArr.Core
- **THEN** the file SHALL reside in the `Options/` subfolder with namespace `FunkArr.Core.Options`

#### Scenario: Actor infrastructure grouped in Actors folder
- **WHEN** a file defines actor infrastructure types (ActorKeys, ShardMessageExtractor) in FunkArr.Core
- **THEN** the file SHALL reside in the `Actors/` subfolder with namespace `FunkArr.Core.Actors`

#### Scenario: Data abstractions grouped in Data folder
- **WHEN** a file defines data path or data file types in FunkArr.Core
- **THEN** the file SHALL reside in the `Data/` subfolder with namespace `FunkArr.Core.Data`

### Requirement: Download project folder structure
FunkArr.Download SHALL organize files into role-based subfolders:
- `Manager/` → `DownloadManager` + `DownloadManagerState` → `FunkArr.Download.Manager`
- `Scheduler/` → `DownloadScheduler` + `DownloadScheduleHelper` → `FunkArr.Download.Scheduler`
- `History/` → `DownloadHistoryManager` + `DownloadHistoryManagerState` → `FunkArr.Download.History`
- `Worker/` → `DownloadWorker` + `DownloadWorkerState` → `FunkArr.Download.Worker`
- `Ffmpeg/` → all FFmpeg-related types → `FunkArr.Download.Ffmpeg`
- `Subtitles/` → all subtitle-related types → `FunkArr.Download.Subtitles`
- Root → `PersistenceMapping`, `QueueEntry`, `DownloadServiceExtensions`, `Telemetry`, `AssemblyMarker`

#### Scenario: Actor and state colocated in role folder
- **WHEN** an actor class and its state class exist in FunkArr.Download
- **THEN** both files SHALL reside in the same role folder (e.g. `Manager/` contains both `DownloadManager.cs` and `DownloadManagerState.cs`)

#### Scenario: FFmpeg types grouped
- **WHEN** a file defines FFmpeg-related types (FfmpegProcess, Remuxer, RemuxOptions, FfmpegInput)
- **THEN** the file SHALL reside in the `Ffmpeg/` subfolder with namespace `FunkArr.Download.Ffmpeg`

#### Scenario: Subtitle types grouped
- **WHEN** a file defines subtitle-related types (SubtitleDownloader, format parsers, SubtitleCue, SubtitleTrack)
- **THEN** the file SHALL reside in the `Subtitles/` subfolder with namespace `FunkArr.Download.Subtitles`

### Requirement: Search project folder structure
FunkArr.Search SHALL organize files into role-based subfolders:
- `Manager/` → `SearchManager` + `SearchManagerState` → `FunkArr.Search.Manager`
- `Workers/` → all 4 search/evaluation workers + states → `FunkArr.Search.Workers`
- `Client/` → `MediathekClient`, `MediathekQueryBuilder`, `MediathekApiModels`, `MediathekQueryResult` → `FunkArr.Search.Client`
- `Models/` → `EnrichedItem`, `MatchInfo`, `MediaIdentity`, `SceneRelease`, `ReleaseDisplay`, `SourceInfo`, `VideoQuality` → `FunkArr.Search.Models`
- Root → `MediathekViewWebManager` + state, `VariantDetector`, `Telemetry`, `AssemblyMarker`

#### Scenario: Search workers in shared Workers folder
- **WHEN** a file defines a search or evaluation worker actor in FunkArr.Search
- **THEN** the file SHALL reside in the `Workers/` subfolder with namespace `FunkArr.Search.Workers`

#### Scenario: Mediathek client types grouped
- **WHEN** a file defines MediathekViewWeb API client or query types
- **THEN** the file SHALL reside in the `Client/` subfolder with namespace `FunkArr.Search.Client`

#### Scenario: Domain model types grouped
- **WHEN** a file defines a search domain model (EnrichedItem, MatchInfo, etc.)
- **THEN** the file SHALL reside in the `Models/` subfolder with namespace `FunkArr.Search.Models`

### Requirement: Feed project folder structure
FunkArr.Feed SHALL organize files into role-based subfolders:
- `Crawl/` → `FeedCrawlManager` + state, `FeedCrawlWorker` → `FunkArr.Feed.Crawl`
- `Store/` → `FeedStoreManager` + state → `FunkArr.Feed.Store`
- `Eval/` → `FeedEvalActor` + `FeedEvalMessages` → `FunkArr.Feed.Eval`
- `Page/` → `FeedPageActor` + `FeedPageMessages` → `FunkArr.Feed.Page`
- `Scheduler/` → `FeedScheduler` → `FunkArr.Feed.Scheduler`
- Root → `FeedGate`, `CrawlResult`, `RuleSetLookup`, `PersistenceMapping`, `Telemetry`, `AssemblyMarker`

#### Scenario: Crawl actors colocated
- **WHEN** an actor participates in the feed crawl pipeline (FeedCrawlManager, FeedCrawlWorker)
- **THEN** the file SHALL reside in the `Crawl/` subfolder with namespace `FunkArr.Feed.Crawl`

#### Scenario: Internal message types colocated with their actor
- **WHEN** a file defines internal actor messages (FeedEvalMessages, FeedPageMessages)
- **THEN** the file SHALL reside in the same folder as the actor that uses them

### Requirement: Enrichment project folder structure
FunkArr.Enrichment SHALL organize files into provider-based subfolders:
- `Tvdb/` → `TvdbEnrichmentActor` → `FunkArr.Enrichment.Tvdb`
- `Tmdb/` → `TmdbEnrichmentActor` → `FunkArr.Enrichment.Tmdb`
- Root → `EnrichmentManager` + state, `Telemetry`, `AssemblyMarker`

#### Scenario: Provider actors in provider folders
- **WHEN** a file defines a provider-specific enrichment actor
- **THEN** the file SHALL reside in the provider subfolder (Tvdb/ or Tmdb/) with the corresponding namespace

### Requirement: RuleSet project folder structure
FunkArr.RuleSet SHALL organize files into role-based subfolders:
- `Manager/` → `RuleSetManager` + `RuleSetManagerState` → `FunkArr.RuleSet.Manager`
- `Resolver/` → `RuleSetResolver` + `RuleSetResolverState` → `FunkArr.RuleSet.Resolver`
- `Worker/` → `RuleSetWorker` + `RuleSetWorkerState` → `FunkArr.RuleSet.Worker`
- `Updater/` → `RuleSetUpdater` → `FunkArr.RuleSet.Updater`
- `DiskModel/` → unchanged → `FunkArr.RuleSet.DiskModel` (already exists)
- Root → `RuleSetStore`, `RuleSetValidator`, `RuleSetMerger`, `RuleSetEnumMapping`, `GitHubModels`, `Telemetry`, `AssemblyMarker`

#### Scenario: RuleSet actor and state colocated
- **WHEN** a RuleSet actor has a corresponding state class
- **THEN** both files SHALL reside in the same role folder

#### Scenario: DiskModel folder unchanged
- **WHEN** a file already resides in the `DiskModel/` subfolder
- **THEN** the file SHALL remain in `DiskModel/` with no changes

### Requirement: Namespace matches folder path
Every .cs file in the reorganized projects SHALL have a namespace declaration that matches its folder path relative to the project root, using the pattern `{ProjectNamespace}.{Subfolder}`.

#### Scenario: File moved to subfolder gets updated namespace
- **WHEN** a .cs file is moved from project root to a subfolder
- **THEN** its `namespace` declaration SHALL be updated to `{ProjectRootNamespace}.{SubfolderName}`

#### Scenario: File remaining in root keeps root namespace
- **WHEN** a .cs file stays in the project root after reorganization
- **THEN** its `namespace` declaration SHALL remain as the project root namespace

### Requirement: Test projects mirror domain folder structure
Each test project SHALL mirror the subfolder structure of its corresponding domain project. Test files SHALL be placed in the subfolder that matches the domain type they test.

Mirrored test projects:
- `FunkArr.Download.Tests` → Manager/, Worker/, Scheduler/, Ffmpeg/, Subtitles/
- `FunkArr.Search.Tests` → Manager/, Workers/, Client/, Models/
- `FunkArr.Feed.Tests` → Crawl/, Store/
- `FunkArr.RuleSet.Tests` → Manager/, Resolver/, Worker/, DiskModel/
- `FunkArr.Enrichment.Tests` → Tvdb/, Tmdb/

Test files that don't map to a specific domain subfolder (e.g. options validators, shared test data, root-level type tests) stay in the test project root.

#### Scenario: Test file mirrors domain subfolder
- **WHEN** a test file tests a type that resides in a domain subfolder (e.g. `FunkArr.Download.Ffmpeg.Remuxer`)
- **THEN** the test file SHALL reside in the matching subfolder of the test project (e.g. `FunkArr.Download.Tests/Ffmpeg/`) with namespace `FunkArr.Download.Tests.Ffmpeg`

#### Scenario: Test file for root-level domain type stays in test root
- **WHEN** a test file tests a type that remains in the domain project root
- **THEN** the test file SHALL remain in the test project root

#### Scenario: Test project reorganized together with domain project
- **WHEN** a domain project's reorganization is complete
- **THEN** its corresponding test project SHALL be reorganized in the same step, before the build verification

### Requirement: Using statements updated solution-wide
All `using` statements across the entire solution (including test projects, Api, ArrApi, Host) SHALL be updated to reference the new namespaces of moved types.

#### Scenario: Consumer updated after type moves
- **WHEN** a type's namespace changes due to folder reorganization
- **THEN** every file that references that type SHALL have its `using` statements updated to the new namespace

#### Scenario: Solution builds after each project
- **WHEN** one project's reorganization is complete
- **THEN** `dotnet build src/FunkArr.slnx` SHALL succeed with zero errors and zero warnings
