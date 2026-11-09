## 1. New Persistence Enums

- [x] 1.1 Create `PersistedCrawlOutcome` enum in `FunkArr.Persistence/Events/Feed/` with `Unknown = 0, Completed = 1, CompletedWithGap = 2, Unchanged = 3, Failed = 4`.
- [x] 1.2 Create `PersistedRejectionReason` enum in `FunkArr.Persistence/Events/Feed/` with `Unknown = 0, LowConfidence = 1, MissingEpisode = 2, MissingId = 3, UnknownSource = 4, MissingTimestamp = 5`.
- [x] 1.3 Modify `PersistedFeedCrawlState` record: change `LastOutcome` from `string?` to `PersistedCrawlOutcome`, add `LastError: string?` parameter.

## 2. New Messages Enums

- [x] 2.1 Create `CrawlOutcome` enum in `FunkArr.Messages/Feed/` with `Unknown = 0, Completed = 1, CompletedWithGap = 2, Unchanged = 3, Failed = 4`.
- [x] 2.2 Create `RejectionReason` enum in `FunkArr.Messages/Feed/` with `Unknown = 0, LowConfidence = 1, MissingEpisode = 2, MissingId = 3, UnknownSource = 4, MissingTimestamp = 5`.
- [x] 2.3 Modify `FeedCrawlStatusResult` record: change `LastOutcome` from `string?` to `CrawlOutcome`, add `LastError: string?` parameter.

## 3. New Api Enums

- [x] 3.1 Create `CrawlOutcome` enum in `FunkArr.Api/Models/Feed/` with `Unknown = 0, Completed = 1, CompletedWithGap = 2, Unchanged = 3, Failed = 4`.
- [x] 3.2 Modify `FeedCrawlStatusOutput` record: change `LastOutcome` from `string?` to `CrawlOutcome`, add `LastError: string?` parameter.
- [x] 3.3 Update `FeedMappingExtensions` to map `CrawlOutcome` between Messages and Api layers.

## 4. Feed Domain — CrawlOutcome Migration

- [x] 4.1 Remove `CrawlStatus` enum from `CrawlResult.cs`. Replace `CrawlResult.Status` type with `CrawlOutcome` from `FunkArr.Messages.Feed`. Update factory methods to use `CrawlOutcome` members.
- [x] 4.2 Update `FeedCrawlManagerState`: change `LastOutcome` from `string?` to `CrawlOutcome`, add `LastError: string?` property. Update `Apply(FeedCursorAdvanced)`, `Apply(FeedRunFailed)`, `WithUnchangedRun` to set enum + error. Update `ToStatus()` and `GetPersistenceState()`.
- [x] 4.3 Update `FeedCrawlManagerState.FromPersistence()` to map `PersistedCrawlOutcome` to `CrawlOutcome`.
- [x] 4.4 Add `CrawlOutcome`↔`PersistedCrawlOutcome` mapping to `PersistenceMapping` with exhaustive switch.

## 5. Feed Domain — RejectionReason Migration

- [x] 5.1 Replace `FeedGate` string constants with `RejectionReason` enum usage. Change return type of `Reject()` from `string?` to `RejectionReason?`. Change `GateResult.RejectedByReason` from `IReadOnlyDictionary<string, int>` to `IReadOnlyDictionary<RejectionReason, int>`.
- [x] 5.2 Update `FeedEvalMessages.GroupResult.RejectedByReason` type from `IReadOnlyDictionary<string, int>?` to `IReadOnlyDictionary<RejectionReason, int>?`.
- [x] 5.3 Update `FeedEvalActor` telemetry tag to use `reason.ToString()` instead of the string constant.

## 6. PersistenceMapping Switch Fix

- [x] 6.1 Replace ternary `MediaType` conversion in `PersistenceMapping.ToPersistence()` and `ToDomain()` with exhaustive switch expressions that throw for `Unknown`.

## 7. Tests

- [x] 7.1 Update `FeedCrawlManagerStateTests` — replace string assertions (`"completed"`, `"completed_with_gap"`, `"unchanged"`, `"failed: boom"`) with enum assertions (`CrawlOutcome.Completed`, etc.) and check `LastError` where applicable.
- [x] 7.2 Update `FeedCrawlManagerTests` — replace `LastOutcome` string assertions with enum assertions, check `LastError` for failure scenarios.
- [x] 7.3 Update `FeedGateTests` — replace string rejection reason keys with `RejectionReason` enum values.
- [x] 7.4 Update integration test `FeedTests` — replace `Assert.Equal("completed", result.Ingest.LastOutcome)` with enum assertion.
- [x] 7.5 Build solution (`dotnet build src/FunkArr.slnx`) and fix any compile errors.
- [x] 7.6 Run all Feed test projects and fix failures.
- [x] 7.7 Run `dotnet format src/FunkArr.slnx --verify-no-changes` and fix violations.
