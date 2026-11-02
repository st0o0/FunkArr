# Tasks: Flat Crawl Stream

## 1. Update records in CrawlResult.cs

- [x] Remove `CrawlProbeResult` — not needed, prepare uses closures
- [x] Remove `CrawlStep` hierarchy (`CrawlStep`, `CrawlReady`, `CrawlUnchanged`, `CrawlProbeFailed`) — SelectMany + shortCircuit replaces branching
- [x] Add `FeedMatch` record (moved from CrawlContext.cs)
- [x] Add `PageEvaluation` record with `Error?` field and `Failed` factory (moved from FeedCrawlPlanner.cs)
- [x] Add `CrawlWindow` record (moved from FeedCrawlPlanner.cs)
- [x] Keep: `CrawlInput`, `CrawlBatchResult`, `CrawlAccumulator`, `CrawlResult`, `CrawlStatus`, `EvalResult`, `GatedResult`

## 2. Rewrite FeedCrawlManager

- [x] Remove `Source.Queue` + `Sink.ActorRef` long-lived stream from constructor
- [x] Remove `OfferResult`/`CrawlOfferResult` handler and related types
- [x] Remove `CrawlStreamCompleted`/`CrawlStreamFailed` handlers and types
- [x] Rewrite `StartCrawl`: declare all closures, build `Source.Single(input)` → flat pipeline → `RunWith(Sink.First)` → `PipeTo(Self)`
- [x] Inline prepare stage: probe + HasUpdate check + rulesets + window computation, returns `int[]`
- [x] Inline fetch page stage: build QueryMediathek, Ask, evaluate page items against window — no `ctx.Read`, no `FeedCrawlPlanner.EvaluatePage`
- [x] Inline lead filter: `item.Timestamp <= input.LeadLimit`
- [x] Inline dedup: StatefulSelectMany with HashSet closure (was `FeedCrawlPlanner.DistinctByStableId`)
- [x] Inline match + count: `lookup.Match(item.Topic)` directly
- [x] Inline evaluate: pattern-match `EvaluateSeriesResponse`/`EvaluateMoviesResponse` directly, no `ItemsOrThrow`
- [x] Inline gate: `FeedGate.Evaluate` + telemetry directly
- [x] Inline publish: pattern-match `PublishFeedEntriesResponse` directly, no `PublishedOrThrow`
- [x] Final Select: `shortCircuit ?? acc.ToResult(...)`
- [x] PipeTo failure handler: `CrawlResult.ForFailed(...)`
- [x] Remove `_running` field — set true in StartCrawl, false in HandleResult
- [x] Keep `HandleResult`, `RecordTelemetry`, `ScheduleNext`, `MaybeSnapshot` unchanged
- [x] Remove `Probe`, `LoadOrSkip`, `Run`, `RunPipeline`, `Elapsed` methods — all inlined
- [x] Fix nested ternary in window computation — extracted to if/else

## 3. Delete files

- [x] Delete `CrawlContext.cs`
- [x] Delete `FeedCrawlPlanner.cs`
- [x] Delete `StreamSupervision.cs`

## 4. Update tests

- [x] Delete `CrawlContextTests.cs` — CrawlContext gone
- [x] Delete `FeedCrawlPlannerTests.cs` — FeedCrawlPlanner gone
- [x] **FeedCrawlManagerTests**: Adapted — TestProbe interaction unchanged (stream still Asks same actors)
- [x] **CrawlAccumulatorTests** (was CrawlTotalsTests): Kept and updated for new record types
- [x] **CrawlResultTests**: Kept — CrawlResult factories unchanged
- [x] **EvalResultTests**: Kept — EvalResult.Gate unchanged
- [x] **FeedCrawlManagerStateTests**: Kept — state logic unchanged

## 5. Build and verify

- [x] `dotnet build src/FunkArr.slnx`
- [x] `dotnet format src/FunkArr.slnx --verify-no-changes` (pre-existing CRLF issues, no new issues)
- [x] Run Feed tests: `dotnet run --project src/FunkArr.Feed.Tests/FunkArr.Feed.Tests.csproj` — 47 tests pass
- [x] Run architecture tests: `dotnet run --project src/FunkArr.Architecture.Tests/FunkArr.Architecture.Tests.csproj` — 13 tests pass
