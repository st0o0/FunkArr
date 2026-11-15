# Design: API Validation & Data Integrity Tests

## F.2 API Input Validation Tests

**Approach**: Test the ValidationEndpointFilter with real request model instances.
The filter uses DataAnnotations, so tests construct invalid models and verify
ProblemHttpResult responses. The existing `ValidationEndpointFilterTests.cs`
provides the pattern.

### RuleSet ID regex (F.2.1)
- CreateRuleSetRequest has `[RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$")]`
- Test IDs: `"r1"` (valid short), `"UPPER"` (uppercase), `"1start"` (starts with digit - actually valid per regex), `"has spaces"`, `"trailing-"`
- These are pure DataAnnotation validation tests

### Enum validation (F.2.2)
- PriorityRequest requires `[Required] QueuePriority Priority`
- Test with `QueuePriority.Unknown` (0) — DataAnnotation `[Required]` on value types doesn't reject 0
- The actual rejection happens in the mapping: `ToMessage()` throws ArgumentOutOfRangeException
- Test this via the enum mapping tests (already covered in EnumMappingExtensionsTests)

### History request range (F.2.3)
- DownloadHistoryRequest has `[Range(0, int.MaxValue)]` on Start, `[Range(1, 1000)]` on Limit
- Test negative values and zero limit

### NZB edge cases (F.2.5-F.2.7)
- NzbParserTests already tests invalid XML → null
- Add: empty content, whitespace-only, extremely large NZB payload handling
- These test the parser behavior, not HTTP-level rejection

## G.1 Enum Mapping Exhaustiveness

**Already covered**: EnumMappingExtensionsTests covers all Api↔Messages pairs.
PersistenceMappingTests in History.Tests covers all History domain enum pairs.

**Gaps to fill**:
- Download domain: MediaType, DownloadPriority, DownloadStatus, FailureKind
  persistence roundtrips (PersistenceMapping.cs in FunkArr.Download)
- Feed domain: MediaType, CrawlOutcome persistence roundtrips
  (PersistenceMapping.cs in FunkArr.Feed)

**Pattern**: Follow History.Tests.PersistenceMappingTests — Theory with InlineData
for all enum members, verify ToPersistence→ToDomain roundtrip.

## G.3 Persistence Event Snapshots

**Pattern**: Follow existing DownloadEventVerifyTests and FeedEventVerifyTests.
Each event gets `*_shape()` (Verify snapshot) and `*_roundtrip()` (JSON serialize/
deserialize assertion).

**Feed events to add** (currently only FeedUnchangedRun):
- FeedCursorAdvanced, FeedRunFailed, FeedEntriesAdded, FeedEntriesExpired
- PersistedFeedCrawlState, PersistedFeedStoreState

**Download events to add** (already has Enqueued, Dequeued, Dispatched, Started,
Initialized, Succeeded, Faulted, HistoryRecorded, HistoryRemoved):
- DownloadMoved, DownloadSwapped, DownloadPriorityChanged
- DownloadsPaused, DownloadsResumed, DownloadAttemptStarted
- PersistedDownloadManagerState (snapshot record)
