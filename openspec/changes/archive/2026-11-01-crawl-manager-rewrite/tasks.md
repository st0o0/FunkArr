# Tasks: Crawl Manager Rewrite

## 1. New records and outcome hierarchy

Create the new internal records in `FunkArr.Feed`:

- [x] `Signal` unit record
- [x] `ProbeResult(long ListTimestamp)`
- [x] `CrawlContext` record with all fields and helper methods (port from `FeedCrawlPlan`: `PageOffsets`, `PageQuery`, `Read`, `WithinLead`, `Match`, `Group`, `EvaluateSeries`, `EvaluateMovies`)
- [x] `BatchResult(int Groups, int FailedGroups, int Published)`
- [x] `CrawlTotals` with `Empty` static field and `Add(BatchResult)` method
- [x] `FeedCrawlOutcome` abstract + `FeedCrawlCompleted` / `FeedCrawlUnchanged` / `FeedCrawlFailed`
- [x] Progress records: `FeedItemCollected`, `FeedItemMatched`, `FeedPageCrawled(bool ReachedEnd)`
- [x] `OfferResult(IQueueOfferResult?, Exception?)` private message
- [x] Port `FeedCrawlResponses` helper methods (`ItemsOrThrow`, `PublishedOrThrow`) — keep as static class or move to `CrawlContext`

File: new `CrawlContext.cs` (replaces `FeedCrawlPlan.cs`), new `FeedCrawlOutcome.cs` (replaces `FeedCrawlRun.cs`)

## 2. Rewrite FeedCrawlManager stream and handlers

Rewrite `FeedCrawlManager.cs`:

- [x] Replace `Source.Queue<FeedCrawlPlan>` with `Source.Queue<Signal>(1, DropNew)`
- [x] Add `Probe()` async method: asks mediathek with `CrawlContext.ProbeQuery()`, returns `ProbeResult`
- [x] Add `Dispatch(ProbeResult)` method: branches unchanged vs full crawl
- [x] Add `LoadContext(long listTimestamp)` async method: asks ruleset resolver, reads FeedOptions, builds CrawlWindow, returns `CrawlContext`
- [x] Refactor `Crawl(FeedCrawlPlan)` → `RunCrawl(CrawlContext)`: same inner pipeline but with `Aggregate` at the end, progress events without RunId
- [x] Wire outer stream: `Queue → SelectAsync(Probe) → ConcatMany(Dispatch) → Sink.ActorRef`
- [x] Replace `_run` field with `_collected`/`_matched` counters
- [x] Simplify `Tick`/`StartFeedCrawl` handlers: check enabled → offer Signal → PipeTo OfferResult
- [x] Add `OfferResult` handler: if not enqueued, schedule next tick
- [x] Add progress event handlers: increment counters + telemetry
- [x] Add outcome handlers: `FeedCrawlCompleted` → combine with counters → HandleOutcome, `FeedCrawlUnchanged` → schedule, `FeedCrawlFailed` → HandleOutcome
- [x] Remove all RunId-related code: `WhenCurrent`, `Probed`, `RuleSetsLoaded`, `RuleSetsFailed`, `CrawlOffered` handlers
- [x] Keep persistence, snapshot, timer logic unchanged

## 3. Update FeedCrawlManagerState if needed

- [x] Check if `FeedRunOutcome` record needs adjustment (it's built from `_collected`/`_matched` + stream outcome now instead of from `FeedCrawlRun.ToOutcome()`)
- [x] Update `Decide()` if the outcome shape changed — `FeedCrawlFailed` needs `ListTimestamp` for the three-failure cursor advance rule
- [x] Verify `ToStatus()` still works (the `running` param was `_run is not null` — now needs a different signal, e.g. track via OfferResult/outcome cycle)

## 4. Delete old files

- [x] Delete `FeedCrawlPlan.cs` (replaced by `CrawlContext.cs`)
- [x] Delete `FeedCrawlRun.cs` (replaced by `FeedCrawlOutcome.cs`)

## 5. Update tests

- [x] **FeedCrawlManagerTests**: Adapt integration tests — the TestProbe interaction pattern changes:
  - No more separate probe → rulesets → offer sequence from the actor side; the stream drives it
  - Mediathek probe is now an Ask inside the stream's `SelectAsync`, not a PipeTo — TestProbe still intercepts it the same way
  - `Second_start_while_running_is_ignored` → verify via `_running` flag (kept for simplicity)
- [x] **FeedCrawlPlanTests** → rename to `CrawlContextTests`, update record construction
- [x] **FeedCrawlRunTests** → delete (FeedCrawlRun is gone). Replace with:
  - `CrawlTotalsTests`: test `Add`/fold logic
- [x] **FeedCrawlPlannerTests** → unchanged (pure functions, no dependency on Plan/Run)
- [x] **FeedCrawlManagerStateTests** → verify `Decide()` still passes, update if outcome shape changed

## 6. Build and format

- [x] `dotnet build src/FunkArr.slnx`
- [x] `dotnet format src/FunkArr.slnx --verify-no-changes` (pre-existing CRLF issues in other projects, no new issues)
- [x] Run all Feed tests: `dotnet run --project src/FunkArr.Feed.Tests/FunkArr.Feed.Tests.csproj`
- [x] Run architecture tests: `dotnet run --project src/FunkArr.Architecture.Tests/FunkArr.Architecture.Tests.csproj`
