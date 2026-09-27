# shared-record-types Specification

## Purpose
TBD - created by archiving change record-consolidation. Update Purpose after archive.
## Requirements
### Requirement: ExternalIds groups external database identifiers

ExternalIds SHALL be a sealed record in `FunkArr.Messages/Shared/` with fields: TvdbId (int?), ImdbId (string?), TmdbId (int?). A corresponding `PersistedExternalIds` SHALL exist in `FunkArr.Persistence/Events/Shared/` with identical fields. Domain projects SHALL use ExternalIds wherever the full set of external IDs can be present: ResolveRuleSet, RegisterRuleSet, RegisteredRuleSetEntry, RuleSetIdentity, SearchResultItem, QueryRegisteredRuleSets, LocalRuleSetCommands.

#### Scenario: RuleSet registration uses ExternalIds
- **WHEN** a RegisterRuleSet command is created with TvdbId=83214, ImdbId="tt0806910", TmdbId=null
- **THEN** it SHALL contain an ExternalIds property with those values instead of three separate parameters

#### Scenario: ExternalIds with all nulls is valid
- **WHEN** an ExternalIds is created with TvdbId=null, ImdbId=null, TmdbId=null
- **THEN** it SHALL be a valid instance representing no external IDs

#### Scenario: Persistence roundtrip preserves ExternalIds
- **WHEN** a record containing PersistedExternalIds is serialized and deserialized
- **THEN** all three ID fields SHALL be preserved exactly

### Requirement: DownloadMedia groups download media identity

DownloadMedia SHALL be a sealed record in `FunkArr.Messages/Shared/` with fields: Title (string), VideoUrl (string), SubtitleUrl (string?), Channel (string), Duration (int), Size (long), Category (MediaType). A corresponding `PersistedDownloadMedia` SHALL exist in `FunkArr.Persistence/Events/Shared/` with Category as PersistedMediaType. Records AddDownload, InitDownload, DownloadInitialized, and DownloadWorkerState SHALL embed DownloadMedia instead of repeating these seven fields.

#### Scenario: AddDownload uses DownloadMedia
- **WHEN** a download is added with Title="Tatort", VideoUrl="https://example.com/v.mp4", Channel="ARD", Duration=5400, Size=1200000000, Category=Show
- **THEN** AddDownload SHALL contain a DownloadMedia property with those values plus Priority as a separate field

#### Scenario: DownloadWorkerState.Apply initializes from DownloadMedia
- **WHEN** a DownloadInitialized event is applied to DownloadWorkerState
- **THEN** the state's Media property SHALL be populated from the event's PersistedDownloadMedia, converted to domain types

#### Scenario: DownloadMedia persistence roundtrip
- **WHEN** a DownloadInitialized with PersistedDownloadMedia is serialized and deserialized
- **THEN** all seven fields including Category SHALL be preserved

### Requirement: DownloadProgress groups download progress metrics

DownloadProgress SHALL be a sealed record in `FunkArr.Messages/Shared/` with fields: BytesDownloaded (long), CurrentTimeUs (long), Speed (double). DownloadWorkerState, WorkerStatusResult, and QueueItem SHALL embed DownloadProgress instead of repeating these three fields.

#### Scenario: DownloadWorkerState tracks progress
- **WHEN** a DownloadWorker updates progress with BytesDownloaded=50000, CurrentTimeUs=120000000, Speed=2.5
- **THEN** the state's Progress property SHALL contain those values

#### Scenario: Progress resets on new attempt
- **WHEN** a DownloadAttemptStarted event is applied
- **THEN** the state's Progress SHALL be reset to BytesDownloaded=0, CurrentTimeUs=0, Speed=0.0

#### Scenario: QueueItem exposes progress
- **WHEN** a QueueItem snapshot is created from DownloadWorkerState
- **THEN** the QueueItem's Progress property SHALL contain the current BytesDownloaded, CurrentTimeUs, and Speed

### Requirement: DownloadCompletion groups download history data

DownloadCompletion SHALL be a sealed record in `FunkArr.Messages/Shared/` with fields: Title (string), Category (MediaType), Size (long), Status (DownloadStatus), RelativePath (string?), FailMessage (string?), DownloadTimeSeconds (int), CompletedAt (long). A corresponding `PersistedDownloadCompletion` SHALL exist in `FunkArr.Persistence/Events/Shared/` with Category as PersistedMediaType and Status as PersistedDownloadStatus. RecordDownload, HistoryItem, and DownloadHistoryRecorded SHALL embed the completion record with DownloadId remaining as a separate field.

#### Scenario: RecordDownload uses DownloadCompletion
- **WHEN** a download completes with Title="Tatort", Category=Show, Size=1200000000, Status=Completed, DownloadTimeSeconds=120
- **THEN** RecordDownload SHALL contain DownloadId and a DownloadCompletion property with those values

#### Scenario: HistoryItem uses DownloadCompletion
- **WHEN** a history query returns items
- **THEN** each HistoryItem SHALL contain DownloadId and a DownloadCompletion property

#### Scenario: DownloadCompletion persistence roundtrip
- **WHEN** a DownloadHistoryRecorded with PersistedDownloadCompletion is serialized and deserialized
- **THEN** all eight fields SHALL be preserved

### Requirement: MatchMetadata groups optional enrichment fields on SearchResultItem

MatchMetadata SHALL be a sealed record in `FunkArr.Messages/Shared/` with fields: TvdbId (int?), ImdbId (string?), TmdbId (int?), Season (string?), Episode (string?), MatchConfidence (float?), MatchMethod (MatchMethod?). SearchResultItem SHALL embed MatchMetadata as a nullable property replacing the seven individual optional fields. SubtitleUrl remains a separate field on SearchResultItem.

#### Scenario: SearchResultItem with enrichment
- **WHEN** a search result has TvdbId=83214, Season="2", Episode="5", MatchConfidence=0.95
- **THEN** SearchResultItem SHALL have a non-null MatchMetadata property containing those values

#### Scenario: SearchResultItem without enrichment
- **WHEN** a search result has no enrichment data
- **THEN** SearchResultItem.Metadata SHALL be null

#### Scenario: ReleaseVariant.ToResultItem maps MatchMetadata
- **WHEN** a ReleaseVariant with Match and Identity is converted to SearchResultItem
- **THEN** the MatchMetadata SHALL be populated from the ReleaseVariant's Match and Identity fields

### Requirement: Mapping extensions convert between Messages and Persistence shared types

Each domain project that maps between Messages and Persistence records SHALL have mapping extension methods for the shared types. The mappings SHALL follow the existing pattern: `ToDomain()` on persistence records, `ToPersistence()` or explicit construction for the reverse direction.

#### Scenario: PersistedDownloadMedia to DownloadMedia
- **WHEN** a PersistedDownloadMedia with PersistedMediaType.Show is converted
- **THEN** ToDomain() SHALL return a DownloadMedia with Category=MediaType.Show

#### Scenario: PersistedExternalIds to ExternalIds
- **WHEN** a PersistedExternalIds is converted
- **THEN** ToDomain() SHALL return an ExternalIds with identical field values

#### Scenario: PersistedDownloadCompletion to DownloadCompletion
- **WHEN** a PersistedDownloadCompletion with PersistedDownloadStatus.Completed is converted
- **THEN** ToDomain() SHALL return a DownloadCompletion with Status=DownloadStatus.Completed

