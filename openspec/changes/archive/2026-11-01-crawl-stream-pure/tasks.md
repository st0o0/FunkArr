# Tasks: Pure Crawl Stream

## 1. New records

- [x] `CrawlInput(long Cursor, FeedOptions Options, long StartedAt)` — queue element, replaces Signal
- [x] `ProbeResult(CrawlInput Input, long ListTimestamp, string? Error)` — probe output, error as data
- [x] `CrawlStep` hierarchy: `CrawlReady(CrawlInput, CrawlContext)` / `CrawlUnchanged(CrawlInput, long)` / `CrawlProbeFailed(CrawlInput, long, string)` — branch point
- [x] `EvalResult(RuleSetGroup Group, SearchResultItem[]? Items, string? Error)` — replaces FeedEvaluation
- [x] `GatedResult(string RuleSetId, FeedEntry[] Accepted, string? Error)` — replaces FeedGated
- [x] `CrawlBatchResult` with `Error?` field — replaces current BatchResult
- [x] `CrawlAccumulator(int Groups, int FailedGroups, int Published, string? FirstError)` with `Empty`, `Add`, `ToResult` — replaces CrawlTotals
- [x] `CrawlStatus` enum (`Completed`, `Unchanged`, `Failed`) and `CrawlResult` record with factory methods — replaces FeedCrawlOutcome + FeedRunOutcome + FeedRunStatus
- [x] `EvalResult.Gate(double minConfidence)` method — port gating logic from FeedEvaluation
- [x] Add `Error?` field and `Failed(string)` factory to `PageEvaluation`

## 2. StreamSupervision

- [x] Create `StreamSupervision` static class with `LoggingDecider(ILoggingAdapter)` → `Attributes` that resumes on all exceptions

## 3. Rewrite FeedCrawlManager

- [x] Replace `Source.Queue<Signal>` with `Source.Queue<CrawlInput>`
- [x] Replace nested ConcatMany with: `SelectAsync(1, Probe)` → `SelectAsync(1, LoadOrSkip)` → `Via(ConcatMany(Run))`
- [x] Add `.WithAttributes(StreamSupervision.LoggingDecider(_log))` before Sink
- [x] Implement `Probe(CrawlInput)` → `CrawlProbeResult` (error as data, no exceptions)
- [x] Implement `LoadOrSkip(CrawlProbeResult)` → `CrawlStep` (error as data)
- [x] Implement `Run(CrawlStep)` → `Source<CrawlResult>` (single ConcatMany dispatch)
- [x] Implement `RunPipeline(CrawlInput, CrawlContext)` with closure-captured `collected`/`matched`/`pageError` counters
- [x] All SelectAsync stages use error-as-data pattern (try/catch → error field)
- [x] `Aggregate(CrawlAccumulator.Empty, fold)` → `Select(acc.ToResult(...))`
- [x] Implement `Elapsed(CrawlInput)` helper using TimeProvider
- [x] Simplify `StartCrawl`: build `CrawlInput(cursor, options, startedAt)` → `OfferAsync`
- [x] Replace `HandleCompleted`/`HandleUnchanged`/`HandleFailed` with single `HandleResult(CrawlResult)`
- [x] Move three-failure cursor advance logic inline into HandleResult (from FeedCrawlManagerState.Decide)
- [x] Remove `_collected`, `_matched`, `_crawlStartedAt` fields
- [x] Remove all progress event handlers (`FeedItemCollected`, `FeedItemMatched`, `FeedPageCrawled`)
- [x] Remove `ResetCounters`, `Elapsed` (old version), `RecordTelemetry(FeedRunOutcome)`
- [x] Add `RecordTelemetry(CrawlResult)` using CrawlResult fields directly
- [x] Track running state: set `_running = true` in OfferResult when Enqueued, clear in HandleResult

## 4. Update FeedCrawlManagerState

- [x] Remove `Decide(FeedRunOutcome, DateTimeOffset)` — logic moved to actor's HandleResult
- [x] Remove `FeedRunOutcome` record and `FeedRunStatus` enum
- [x] Keep `Apply`, `WithUnchangedRun`, `ToStatus`, `GetPersistenceState`, `FromPersistence`
- [x] Update `ToStatus` if needed (running param stays)

## 5. Delete replaced files/records

- [x] Delete `FeedCrawlOutcome.cs` (FeedCrawlCompleted, FeedCrawlUnchanged, FeedCrawlFailed, FeedItemCollected, FeedItemMatched, FeedPageCrawled)
- [x] Delete `FeedRunOutcome.cs` (FeedRunOutcome, FeedRunStatus)
- [x] Remove `FeedEvaluation`, `FeedGated` from CrawlContext.cs (replaced by EvalResult, GatedResult)
- [x] Remove `CrawlTotals` from CrawlContext.cs (replaced by CrawlAccumulator)
- [x] Remove old `BatchResult` from CrawlContext.cs (replaced by CrawlBatchResult)

## 6. Update tests

- [x] **FeedCrawlManagerTests**: Update integration tests — CrawlResult is the only outcome type, no progress events
- [x] **CrawlContextTests**: Unchanged (CrawlContext helper methods stay)
- [x] **CrawlTotalsTests** → renamed to `CrawlAccumulatorTests`, added error-field and ToResult tests
- [x] **FeedCrawlManagerStateTests**: Removed Decide-related tests, kept Apply/ToStatus tests
- [x] **FeedCrawlPlannerTests**: Unchanged
- [x] Add `EvalResultTests`: test Gate method with error-as-data
- [x] Add `CrawlResultTests`: test factory methods and status mapping

## 7. Build and verify

- [x] `dotnet build src/FunkArr.slnx`
- [x] `dotnet format src/FunkArr.slnx --verify-no-changes` (pre-existing CRLF issues, no new issues)
- [x] Run Feed tests: `dotnet run --project src/FunkArr.Feed.Tests/FunkArr.Feed.Tests.csproj`
- [x] Run architecture tests: `dotnet run --project src/FunkArr.Architecture.Tests/FunkArr.Architecture.Tests.csproj`
