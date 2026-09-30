## Why

Flat records with 10-17 positional parameters create maintenance burden and obscure which data belongs together. The same field groups (media identity, external IDs, download progress) are duplicated across records in Messages, Persistence, and domain projects, leading to manual field-by-field copying at construction sites. Extracting shared record types makes the data model self-documenting and reduces constructor noise.

This is the final persistence-breaking change. After this, persistence records are extend-only.

## What Changes

- **BREAKING**: Persistence records restructured to embed shared sub-records instead of flat fields
- Extract `ExternalIds` record (`TvdbId`, `ImdbId`, `TmdbId`) used across Search, RuleSet, and Enrichment domains (~10 consuming records)
- Extract `DownloadMedia` record (`Title`, `VideoUrl`, `SubtitleUrl`, `Channel`, `Duration`, `Size`, `Category`) used across Download messages and persistence (~4 records, reducing 8-10 params to 2-4)
- Extract `DownloadProgress` record (`BytesDownloaded`, `CurrentTimeUs`, `Speed`) from worker state and status records (~3 records)
- Extract `DownloadCompletion` record for history data (`Title`, `Category`, `Size`, `Status`, `RelativePath`, `FailMessage`, `DownloadTimeSeconds`, `CompletedAt`) across 3 nearly identical records
- Embed `ScoreCandidate` in `ItemTrace`/`PersistedItemTrace` instead of flattening with `Candidate*` prefix (13 params to 7)
- Extract `MatchMetadata` from `SearchResultItem` for optional enrichment fields (17 params to ~10)

## Capabilities

### New Capabilities
- `shared-record-types`: Defines the shared record types (ExternalIds, DownloadMedia, DownloadProgress, DownloadCompletion, MatchMetadata) in FunkArr.Messages, with persistence mirrors in FunkArr.Persistence

### Modified Capabilities
- `search-pipeline-types`: SearchResultItem restructured to embed MatchMetadata and ExternalIds
- `scoring-trace-persistence`: ItemTrace/PersistedItemTrace restructured to embed ScoreCandidate
- `match-history-persistence`: DownloadHistoryRecorded restructured to embed DownloadCompletion

## Impact

- **FunkArr.Messages**: New shared records, updated record definitions across Download, Search, Scoring, RuleSet, Enrichment
- **FunkArr.Persistence**: Mirrored persistence records, updated PersistedItemTrace, DownloadInitialized, DownloadHistoryRecorded
- **Domain projects**: All construction sites updated (Search workers, ScoringEngine, DownloadWorker state, persistence mappings)
- **Test projects**: All test record construction updated
- **Serialization**: Akka persistence journal incompatible with previous events -- requires journal wipe for existing installations
- **API models**: No external API changes (Newznab/SABnzbd responses unchanged, internal API models may reference shared types)
