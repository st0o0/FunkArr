## 1. Messages cleanup (cross-domain, do first)

- [x] 1.1 Flatten `EvaluateSeries`: replace `RuleSetResolved? RuleSet` with `string? RuleSetId`, `string? MediaName`, `EnrichmentConfig? Enrichment` in `FunkArr.Messages/Search/EvaluateSeries.cs`
- [x] 1.2 Flatten `EvaluateMovies`: same change in `FunkArr.Messages/Search/EvaluateMovies.cs`
- [x] 1.3 Update `TvEvaluationState.Init` to read flat fields (`cmd.RuleSetId`, `cmd.MediaName`, `cmd.Enrichment`) instead of destructuring `cmd.RuleSet`
- [x] 1.4 Update `MovieEvaluationState.Init` to read flat fields instead of destructuring `cmd.RuleSet`
- [x] 1.5 Update `FeedCrawlPageActor` evaluation dispatch to pass flat fields when constructing `EvaluateSeries`/`EvaluateMovies`
- [x] 1.6 Update Search test files (`TvEvaluationWorkerTests`, `TvEvaluationStateTests`, `MovieEvaluationWorkerTests`) to use flat fields instead of `RuleSet: new RuleSetResolved(...)`
- [x] 1.7 Build and run affected tests to verify

## 2. Delete `RuleSetLookup` and absorb into `FeedCrawlRunState`

- [x] 2.1 Rewrite `FeedCrawlRunState`: flat fields (ListTimestamp, LowerBound, UpperBound, Truncated, PageSize, MaxPages, MinConfidence, StartedAt, TopicTargets), `static Init(...)` factory, `Apply(IReadOnlyList<RegisteredRuleSetEntry>)` for topic-target dictionary building
- [x] 2.2 Move `RuleSetTarget` record from `RuleSetLookup.cs` to `Crawl/RuleSetTarget.cs`
- [x] 2.3 Delete `RuleSetLookup.cs`
- [x] 2.4 Delete `CrawlInput` and `CrawlWindow` records from `CrawlResult.cs`
- [x] 2.5 Update `FeedCrawlManager`: use `FeedCrawlRunState.Init(...)` after probe, `_runState.Apply(entries)` after rulesets loaded, read flat fields in `StartCrawling()` and error paths
- [x] 2.6 Flatten `RuleSetsLoaded` PipeTo wrapper: `RuleSetsLoaded(IReadOnlyList<RegisteredRuleSetEntry> Entries)`, extract `.Entries` in PipeTo success mapper
- [x] 2.7 Update `ComputeWindow` to return tuple `(long lower, long upper, bool truncated)` instead of `CrawlWindow`
- [x] 2.8 Delete `RuleSetLookupTests.cs`, update/add `FeedCrawlRunState` tests for `Init` + `Apply(entries)` + `GetCrawlResult`
- [x] 2.9 Build and run Feed tests to verify

## 3. Simplify `CrawlResult`

- [x] 3.1 Remove factory methods (`ForCompleted`, `ForCompletedPartial`, `ForFailed`, `ForUnchanged`) from `CrawlResult` record
- [x] 3.2 Update `FeedCrawlRunState.GetCrawlResult(duration)` to use `new CrawlResult(...)` directly
- [x] 3.3 Update `FeedCrawlManager` error paths (CrawlPagesFailed, PublishFailed) to use `new CrawlResult(...)` directly
- [x] 3.4 Update `CrawlResultTests` to test via `new CrawlResult(...)` construction
- [x] 3.5 Build and run Feed tests to verify

## 4. Inline `FeedGate` into `FeedCrawlPageActor`

- [x] 4.1 Move `StableIdFor`, `Evaluate`, and `Reject` as `private static` methods into `FeedCrawlPageActor`
- [x] 4.2 Move `GateResult` record declaration to actor file level (or private nested)
- [x] 4.3 Delete `FeedGate.cs`
- [x] 4.4 Delete `FeedGateTests.cs` (coverage now via actor-level tests)
- [x] 4.5 Build and run Feed tests to verify

## 5. Flatten `FetchCompleted` PipeTo wrapper

- [x] 5.1 Change `FetchCompleted` to `FetchCompleted(MediathekItem[] Items, long ListTimestamp, ProcessPage Request)`, extract fields in PipeTo success mapper
- [x] 5.2 Update Fetching handler to read `msg.Items` and `msg.ListTimestamp` instead of `msg.Result.Items` and `msg.Result.ListTimestamp`
- [x] 5.3 Build and run Feed tests to verify

## 6. Final verification

- [x] 6.1 Run `dotnet build src/FunkArr.slnx` — zero warnings
- [x] 6.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 6.3 Run all Feed and Search test projects
