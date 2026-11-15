## 1. API Input Validation Tests (F.2)

- [x] 1.1 Add `RuleSetValidationTests` in FunkArr.Api.Tests: test CreateRuleSetRequest regex — valid kebab-case IDs pass, uppercase/spaces/trailing-hyphen/empty fail. Test Topic and Rules required.
- [x] 1.2 Add `DownloadHistoryValidationTests` in FunkArr.Api.Tests: test DownloadHistoryRequest Range annotations — negative Start fails, zero Limit fails, valid values pass.
- [x] 1.3 Add `PriorityValidationTests` in FunkArr.Api.Tests: test PriorityRequest Required — valid priorities pass, test that Unknown (0) maps to throw in ToMessage.
- [x] 1.4 Add `NzbEdgeCaseTests` in FunkArr.ArrApi.Tests: empty NZB content returns null, whitespace-only returns null (extends existing NzbParserTests pattern).

## 2. Download Persistence Mapping Tests (G.1)

- [x] 2.1 Add `DownloadPersistenceMappingTests` in FunkArr.Download.Tests: roundtrip tests for MediaType (Show, Movie), DownloadPriority (Low, Normal, High), DownloadStatus (Queued, Processing, Completed, Failed, Cancelled), FailureKind (Transient, Permanent). Verify ToPersistence→ToDomain preserves value.
- [x] 2.2 Add enum member count assertions: Messages enum non-Unknown count equals Persistence enum non-Unknown count for each pair.

## 3. Feed Persistence Mapping Tests (G.1)

- [x] 3.1 Add `FeedPersistenceMappingTests` in FunkArr.Feed.Tests: roundtrip tests for MediaType (Show, Movie), CrawlOutcome (Completed, CompletedWithGap, Unchanged, Failed). Verify ToPersistence→ToDomain preserves value.
- [x] 3.2 Add enum member count assertions for Feed domain enum pairs.

## 4. Feed Event Verify Tests (G.3)

- [x] 4.1 Add FeedCursorAdvanced shape + roundtrip tests in FeedEventVerifyTests
- [x] 4.2 Add FeedRunFailed shape + roundtrip tests
- [x] 4.3 Add FeedEntriesAdded shape + roundtrip tests (with sample PersistedFeedEntry)
- [x] 4.4 Add FeedEntriesExpired shape + roundtrip tests
- [x] 4.5 Add PersistedFeedCrawlState shape + roundtrip tests
- [x] 4.6 Add PersistedFeedStoreState shape + roundtrip tests

## 5. Download Event Verify Tests (G.3)

- [x] 5.1 Add DownloadMoved, DownloadSwapped, DownloadPriorityChanged shape + roundtrip tests in DownloadEventVerifyTests
- [x] 5.2 Add DownloadsPaused, DownloadsResumed shape + roundtrip tests
- [x] 5.3 Add DownloadAttemptStarted shape + roundtrip tests
- [x] 5.4 Add PersistedDownloadManagerState shape + roundtrip tests

## 6. Build and verify

- [x] 6.1 Run `dotnet build src/FunkArr.slnx` — zero errors, zero warnings
- [x] 6.2 Run all affected test projects — all new tests pass (pre-existing failures in DownloadManagerRecoveryTests and FeedCrawlManagerRecoveryTests from prior changes)
