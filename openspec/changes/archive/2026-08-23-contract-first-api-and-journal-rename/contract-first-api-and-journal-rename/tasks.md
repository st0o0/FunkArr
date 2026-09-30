## 1. Journal Rename (Persistence)

- [x] 1.1 Rename `QueueCoordinatorEventDtos.cs` to `QueueCoordinatorJournal.cs`: remove `Dto` suffix from types (`QueueJobEnqueuedDto` → `QueueJobEnqueued` etc.), convert `QueueCoordinatorEventDtoMapping` static class to `static class QueueCoordinatorJournalExtensions` with extension methods (`this` on domain events for `ToJournal()`, `this` on journal types for `ToDomain()`)
- [x] 1.2 Rename `DownloadCoordinatorEventDtos.cs` to `DownloadCoordinatorJournal.cs`: same pattern — remove `Dto` suffix, convert static mapping to extension methods
- [x] 1.3 Rename `DownloadRequestTrackerEventDtos.cs` to `DownloadRequestTrackerJournal.cs`: same pattern
- [x] 1.4 Rename `MatchQualityEventDtos.cs` to `MatchQualityJournal.cs`: same pattern
- [x] 1.5 Update call-sites in `QueueCoordinator.cs`: change `Persist(QueueCoordinatorEventDtoMapping.ToDto(evt), ...)` → `Persist(evt.ToJournal(), ...)` and `Recover<QueueJobEnqueuedDto>(dto => ...)` → `Recover<QueueJobEnqueued>(j => j.ToDomain())`
- [x] 1.6 Update call-sites in `DownloadCoordinator.cs`: same pattern for all `Persist()` and `Recover<>()` calls
- [x] 1.7 Update call-sites in `DownloadRequestTracker.cs`: same pattern
- [x] 1.8 Update call-sites in `MatchQualityWorker.cs`: same pattern
- [x] 1.9 Build and run existing tests to verify no regressions

## 2. OpenAPI Specs

- [x] 2.1 Create `openapi/funkArr-v1.yaml` root spec with info, servers, security (apikey query param), and `$ref` paths to feature files
- [x] 2.2 Create `openapi/queue.yaml` with paths for `GET /api/v1/queue` and `GET /api/v1/history` and their response schemas (matching current `QueueItemResponse` / `HistoryItemResponse` JSON shapes)
- [x] 2.3 Create `openapi/setup.yaml` with paths for all setup endpoints and their request/response schemas (matching current `SetupResponses.cs` shapes)
- [x] 2.4 Create `openapi/rulesets.yaml` with paths for all ruleset endpoints and request/response schemas — define new contract schemas for `RuleSetDetail` (replaces `RuleSetFile` exposure), `TestRulesRequest`, `TestRulesResult` (replaces `MatchTrace` exposure), `RulesetSummary`
- [x] 2.5 Create `openapi/match-intelligence.yaml` with paths for all match endpoints — define new contract schemas for `MatchSummary` (replaces `MatchRecord`), `TopicSummary` (replaces `TopicStats`), `UnmatchedSummary` (replaces `MatchQualityWorker.UnmatchedGroup`)

## 3. NSwag Code Generation

- [x] 3.1 Add `NSwag.MSBuild` to `Directory.Packages.props` and `FunkArr.csproj` with MSBuild target
- [x] 3.2 Configure NSwag MSBuild target in csproj targeting bundled `openapi/funkArr-v1.yaml`, generating to `Api/Generated/Contracts.g.cs` with namespace `FunkArr.Api.Contracts`
- [x] 3.3 Run NSwag generation and verify `Contracts.g.cs` is produced with correct types
- [x] 3.4 Build to verify generated types compile

## 4. API Contract Mapping and Controller Rewire

- [x] 4.1 Create `Api/Mapping/ContractMappingExtensions.cs` with `ToContract()` extensions for queue domain types → generated contract types
- [x] 4.2 Add `ToContract()` extensions for setup domain types
- [x] 4.3 Add `ToContract()` / `ToDomain()` extensions for ruleset domain types (`RuleSetFile` ↔ contract, `MatchTrace` variants → contract)
- [x] 4.4 Add `ToContract()` extensions for match intelligence domain types (`MatchRecord`, `TopicStats`, `UnmatchedGroup`)
- [x] 4.5 Rewire `QueueController` to use generated contract types via `.ToContract()` extensions
- [x] 4.6 Rewire `SetupController` to use generated contract types
- [x] 4.7 Rewire `RulesetController` to use generated contract types — fix `RuleSetFile` direct exposure and `Rule`/`MatchTrace` leaks
- [x] 4.8 Rewire `MatchIntelligenceController` to use generated contract types — fix `MatchRecord`, `TopicStats`, `MatchQualityWorker.UnmatchedGroup` direct exposure
- [x] 4.9 Move `SabnzbdResponses.cs` from `Api/Models/` to `Api/Contracts/Sabnzbd/`
- [x] 4.10 Move `ErrorResponse.cs` to `Api/Contracts/` (covered by NSwag-generated ErrorResponse)
- [x] 4.11 Delete `Api/Models/` directory (all remaining types replaced by generated contracts)
- [x] 4.12 Build and run existing tests to verify no regressions

## 5. Contract Tests

- [x] 5.1 Create `Contracts/JournalRoundTripSpec.cs` with round-trip tests for all QueueCoordinator journal types (serialize → deserialize → assert equality) and Verify snapshots of JSON wire format
- [x] 5.2 Add round-trip tests and snapshots for DownloadCoordinator journal types
- [x] 5.3 Add round-trip tests and snapshots for DownloadRequestTracker journal types
- [x] 5.4 Add round-trip tests and snapshots for MatchQuality journal types
- [x] 5.5 Add unknown-field tolerance tests for all journal types (extra JSON fields ignored)
- [x] 5.6 Create `Contracts/SabnzbdContractSpec.cs` with Verify snapshots for all SABnzbd response types (version, config, queue, history, addfile, error)
- [x] 5.7 Create `Contracts/NewznabContractSpec.cs` with Verify snapshots for caps, tvsearch, movie, and empty search XML responses
- [x] 5.8 Run all tests, approve `.verified.txt` snapshots

## 6. Cleanup and Verification

- [x] 6.1 Run `dotnet format` on all modified files
- [x] 6.2 Full build and test run — verify zero failures (452 tests, 0 failures)
- [x] 6.3 Verify Scalar API reference still renders correctly (run the app, check `/scalar/v1`)
