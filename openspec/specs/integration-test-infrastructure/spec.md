# integration-test-infrastructure Specification

## Purpose

Shared test infrastructure for HTTP-level integration tests: FunkArrFixture, collection-per-domain isolation, and TestData builders for realistic domain objects.

## Requirements

### Requirement: FunkArrFixture provides shared test server
The `FunkArrFixture` SHALL implement `IAsyncLifetime`, start a `WebApplication` with `TestServer`, register `TestProbe` actors for all actor interfaces, and expose `HttpClient` and `GetProbe<T>()`. Each `ICollectionFixture<FunkArrFixture>` instance SHALL have its own isolated server and probe set.

#### Scenario: Fixture starts and stops cleanly
- **WHEN** a test collection using `FunkArrFixture` runs
- **THEN** the fixture SHALL start the server in `InitializeAsync` and dispose all resources in `DisposeAsync`

#### Scenario: Probes are registered for all actor interfaces
- **WHEN** the fixture initializes
- **THEN** TestProbes SHALL be registered for IDownloadManager, IDownloadHistoryManager, ISearchManager, IMediathekManager, IScoringManager, IRuleSetResolver, IRuleSetManager, IRuleSetRegion, IRuleSetUpdater, IHistoryRegion, IStatsCollector, IEnrichmentManager, IDownloadScheduler

### Requirement: Collection-per-domain isolation
Each API domain SHALL have its own xUnit collection definition. Tests within a collection SHALL run sequentially. Collections SHALL run in parallel.

#### Scenario: Downloads collection runs independently
- **WHEN** Downloads and RuleSets test collections run
- **THEN** they SHALL use separate FunkArrFixture instances with no shared state

### Requirement: TestData builders produce realistic domain objects
The `TestData` class SHALL provide static factory methods for all domain types used in test probe responses, with required parameters for commonly-asserted fields and sensible defaults for the rest.

#### Scenario: QueueItem builder
- **WHEN** `TestData.QueueItem("Title", DownloadStatus.Processing)` is called
- **THEN** it SHALL return a fully populated `QueueItem` with the given title and status

#### Scenario: HistoryItem builder
- **WHEN** `TestData.HistoryItem("Title", DownloadStatus.Completed)` is called
- **THEN** it SHALL return a fully populated `HistoryItem` with completion data
