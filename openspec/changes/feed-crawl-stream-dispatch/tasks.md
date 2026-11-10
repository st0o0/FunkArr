## 1. PageActor Convention Cleanup

- [x] 1.1 Create `FeedCrawlPageActorState.cs` — sealed class with `_pendingGroups`, `_totalNewItems`, `_totalUnmatched`, `_reachedEnd`, `_accepted`, `_rejectedByReason` as mutable fields, Apply methods per event type (FetchCompleted, EvalCompleted, EvalFailed), static `Empty` property, and `GetResult()` method that returns a `PageProcessed`
- [x] 1.2 Rewrite `FeedCrawlPageActor.cs` — remove IWithUnboundedStash, replace mutable fields with `_state = FeedCrawlPageActorState.Empty`, capture `Sender` as `_replyTo` in HandleProcessPage, reply via `_replyTo.Tell` instead of `_manager.Tell`, remove `_manager` field, rename static readonly fields to PascalCase (`MediathekTimeout`, `EvaluationTimeout`, `MaxLead`), remove Stash.Stash()/UnstashAll() from Fetching/Evaluating states, create fresh state via `Empty` per ProcessPage instead of manual field clearing

## 2. FeedCrawlRunState Slimming

- [x] 2.1 Remove dispatch fields and methods from `FeedCrawlRunState.cs` — delete `PendingPages`, `NextOffset`, `PagesFired`, `HasMorePages`, `StartCrawling()`, `NextPageRequest()`. Add `ApplyAll(IReadOnlyList<PageProcessed>)` that iterates `Apply` per result. Add `bool ReachedEnd` property (was computed from PageProcessed results)

## 3. Manager Stream Dispatch + State Collapse

- [x] 3.1 Add stream internal messages to `FeedCrawlManager` — `CrawlPagesCompleted(IImmutableList<PageProcessed> Results)` and `CrawlPagesFailed(string Reason)` as private sealed records
- [x] 3.2 Rewrite `StartCrawling()` — replace Tell-loop with `Source.From → SelectAsync(maxConcurrency, Ask) → TakeWhile(!ReachedEnd, inclusive) → Sink.Seq → PipeTo(Self, CrawlPagesCompleted/CrawlPagesFailed)`. Remove manual PendingPages initialization
- [x] 3.3 Merge Probing + LoadingRuleSets into single Probing Become-state — inline ProbeResult handler (Ask rulesets), RuleSetsLoaded handler (StartCrawling), ProbeFailed/RuleSetsFailed handlers. Remove `StartLoadingRuleSets()`, `LoadingRuleSets()`, `HandleRuleSetsLoaded()`, `HandleRuleSetsFailed()` as separate methods
- [x] 3.4 Merge Crawling + Publishing into single Crawling Become-state — CrawlPagesCompleted handler: ApplyAll + inline publish Ask if entries exist. Inline PublishCompleted/PublishFailed/CrawlPagesFailed handlers. Remove `StartPublishing()`, `Publishing()`, `HandlePageProcessed()`, `HandlePublishCompleted()`, `HandlePublishFailed()` as separate methods
- [x] 3.5 Remove `_pagePool` field creation via `Context.ResolveChildActor` with SmallestMailboxPool — replace with pool creation that does not require the actor to hold a separate `_pagePool` field if it can be captured in the stream closure, OR keep `_pagePool` as a simple field without router configuration changes. Verify pool size matches SelectAsync parallelism

## 4. Verification

- [x] 4.1 Update `FeedCrawlManager` tests to reflect new Become-states and batch result delivery (CrawlPagesCompleted instead of individual PageProcessed messages)
- [x] 4.2 Update `FeedCrawlPageActor` tests — verify reply goes to Sender (not hardcoded manager), verify no Stash behavior, verify State object usage
- [x] 4.3 Build solution (`dotnet build src/FunkArr.slnx`) — zero warnings
- [x] 4.4 Run Feed tests (`dotnet run --project src/FunkArr.Feed.Tests/FunkArr.Feed.Tests.csproj`) — all pass
- [x] 4.5 Run format check (`dotnet format src/FunkArr.slnx --verify-no-changes`)
