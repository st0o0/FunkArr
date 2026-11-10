## 1. Messages

- [x] 1.1 Rewrite `FeedCrawlPageMessages.cs`: replace `ProcessPage` with flat data (Offset, PageSize, LowerBound, UpperBound, Targets, MinConfidence), add `abstract ProcessPageResponse` with `ProcessPageCompleted` and `ProcessPageFailed` subtypes. Remove old `PageProcessed` record.

## 2. RuleSetLookup

- [x] 2.1 Add `ToDictionary()` method to `RuleSetLookup` that returns `IReadOnlyDictionary<string, RuleSetTarget>` (exposes topic→target mapping as flat data).
- [x] 2.2 ProcessPage carries `IReadOnlyDictionary<string, RuleSetTarget> TopicTargets` — PageActor uses it directly, no `From(RuleSetTarget[])` needed.

## 3. FeedCrawlPageActor

- [x] 3.1 Add `IWithUnboundedStash` to FeedCrawlPageActor. Add `Receive<ProcessPage>(_ => Stash.Stash())` in Fetching and Evaluating states. Call `Stash.UnstashAll()` before `Become(Ready)` in `ReplyAndBecomeReady`.
- [x] 3.2 Update Ready handler: build `RuleSetLookup` from `ProcessPage.Targets` internally, construct `CrawlWindow` from `LowerBound`/`UpperBound` (or use primitives directly in filtering logic).
- [x] 3.3 Update response: return `ProcessPageCompleted`/`ProcessPageFailed` instead of `PageProcessed`. FeedCrawlPageActorState.GetResult() returns `ProcessPageCompleted`, fetch failures return `ProcessPageFailed`.

## 4. FeedCrawlPageActorState

- [x] 4.1 Change `GetResult()` return type to `ProcessPageCompleted`. Make `RejectedByReason` always non-nullable (return empty dictionary instead of null).

## 5. FeedCrawlManager — Pool

- [x] 5.1 Add `_router` (IActorRef) and `_poolSize` (int) fields. Create `SmallestMailboxPool` router in `RecoveryCompleted` callback with initial `MaxConcurrency` from FeedOptions.
- [x] 5.2 Update `OptionsChanged` handler in Idle: send `AdjustPoolSize(newSize - _poolSize)` when MaxConcurrency changes. In Crawling/Probing: defer resize flag, apply in `ReturnToIdle`.

## 6. FeedCrawlManager — Stream Pipeline

- [x] 6.1 Rewrite `StartCrawling`: build `ProcessPage[]` array from run state in actor thread. Replace `SelectAsync` + manual Ask with `Source.From(messages).Ask<ProcessPageResponse>(_router, _pageTimeout, _poolSize)`.
- [x] 6.2 Update `TakeWhile` to pattern match: `r is not ProcessPageCompleted { ReachedEnd: true }`.
- [x] 6.3 Update `CrawlPagesCompleted` record to carry `IImmutableList<ProcessPageResponse>` instead of `IImmutableList<PageProcessed>`.

## 7. FeedCrawlRunState

- [x] 7.1 Change `Apply` to accept `ProcessPageResponse` and pattern match: `ProcessPageCompleted` → accumulate stats, `ProcessPageFailed` → record error. Remove nullable error field checks. Update `ApplyAll` accordingly.

## 8. Verification

- [x] 8.1 Build solution (`dotnet build src/FunkArr.slnx`), fix any compilation errors from the message type changes.
- [x] 8.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes` and fix violations.
- [x] 8.3 Run Feed tests (`dotnet run --project src/FunkArr.Feed.Tests/FunkArr.Feed.Tests.csproj`) and fix any failures from the changed message types.
