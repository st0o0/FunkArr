## ADDED Requirements

### Requirement: QualityProbeWorker actor wrapping QualityProbeService
`QualityProbeWorker` SHALL be a permanent child of `SearchCoordinator` that wraps `QualityProbeService`. It SHALL respond to `ProbeUrls(SearchResult[])` with `UrlsProbed(SearchResult[])`.

#### Scenario: Probe request via Ask
- **WHEN** `SearchCoordinator` asks `QualityProbeWorker` with `ProbeUrls(results)`
- **THEN** `QualityProbeWorker` SHALL invoke `QualityProbeService.ExpandWithProbingAsync` and reply with `UrlsProbed(enrichedResults)`

### Requirement: In-memory probe cache with deduplication
`QualityProbeWorker` SHALL maintain an in-memory `Dictionary<string, QualityInfo>` cache keyed by URL. Duplicate probe requests for the same URL SHALL return cached data.

#### Scenario: URL already probed
- **WHEN** `QualityProbeWorker` receives a `ProbeUrls` batch containing a URL that was previously probed
- **THEN** it SHALL use the cached `QualityInfo` for that URL without making a network request

#### Scenario: Inflight deduplication
- **WHEN** two concurrent `ProbeUrls` batches contain the same URL
- **THEN** only one probe request SHALL be made for that URL

### Requirement: Tier 2 event-sourced persistence
`QualityProbeWorker` SHALL be a `ReceivePersistentActor` with `PersistenceId: "quality-probe"`. It SHALL persist `UrlProbed(url, qualityInfo)` events. Snapshots SHALL be taken every 500 events.

#### Scenario: Cache warm on recovery
- **WHEN** `QualityProbeWorker` restarts after a crash
- **THEN** it SHALL recover its probe cache from the latest snapshot + replayed events

#### Scenario: Snapshot every 500 events
- **WHEN** 500 probe events have been persisted since the last snapshot
- **THEN** `QualityProbeWorker` SHALL save a snapshot of the current cache state
