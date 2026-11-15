# API Validation & Data Integrity Tests

## Why

The QA plan (COMPREHENSIVE-QA-PLAN.md sections F.2 and G) identifies gaps in
API input validation testing and persistence event snapshot coverage. The API
layer uses DataAnnotations + ValidationEndpointFilter but most validation rules
lack tests. The persistence layer has Verify snapshot tests for Download and
Feed events, but Feed coverage is minimal (only FeedUnchangedRun) and Download
persistence mapping has no roundtrip tests. History persistence mapping tests
are comprehensive and serve as the reference pattern.

## What

1. **F.2 API Input Validation** — test validation rules on API request models:
   - RuleSet ID regex enforcement (kebab-case pattern)
   - Enum Unknown (0) rejection at API boundary
   - Negative/out-of-range offset/limit clamping
   - NZB edge cases (empty, non-XML, oversized payloads)

2. **G.1 Enum Mapping Exhaustiveness** — the existing EnumMappingExtensionsTests
   already cover all Api↔Messages roundtrips. Add Messages↔Persistence roundtrip
   tests for Download and Feed domains (History domain already has them).

3. **G.3 Persistence Event Snapshots** — expand Verify snapshot coverage:
   - Feed events: FeedCursorAdvanced, FeedRunFailed, FeedEntriesAdded,
     FeedEntriesExpired, PersistedFeedCrawlState, PersistedFeedStoreState
   - Download events: DownloadMoved, DownloadSwapped, DownloadPriorityChanged,
     DownloadsPaused, DownloadsResumed, DownloadAttemptStarted,
     PersistedDownloadManagerState
   - Download persistence mapping roundtrips (covering all enum pairs)

## Scope

- Tests only — no production code changes needed (validation is already in place)
- Test projects: FunkArr.Api.Tests, FunkArr.ArrApi.Tests, FunkArr.Persistence.Tests,
  FunkArr.Download.Tests, FunkArr.Feed.Tests
