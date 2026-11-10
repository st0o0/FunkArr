## Why

FeedCrawlPageActor's messages violate project conventions and the pool/stream integration has correctness issues. `ProcessPage` carries behavioral objects (`RuleSetLookup`, `CrawlWindow`) instead of flat data. `PageProcessed` uses nullable fields instead of the abstract response + Completed/Failed pattern. The Manager uses a hand-rolled actor array with manual round-robin instead of an Akka router, and the stream pipeline accesses actor context in lambdas (threading violation).

## What Changes

- **Rewrite `ProcessPage`/`PageProcessed` messages**: Replace behavioral objects with flat data (`RuleSetTarget[]`, primitive bounds). Introduce proper `abstract ProcessPageResponse` with `ProcessPageCompleted`/`ProcessPageFailed` subtypes.
- **Add `IWithUnboundedStash` to FeedCrawlPageActor**: Stash incoming `ProcessPage` in Fetching/Evaluating states, UnstashAll on return to Ready. Prevents message loss when SmallestMailboxPool routes to a busy actor.
- **Replace manual actor pool with permanent `SmallestMailboxPool` router**: Router created in RecoveryCompleted, lives for the Manager's lifetime. Dynamic resize via `AdjustPoolSize` on config change.
- **Replace `SelectAsync` + manual `Ask` with stream-native `Ask` stage**: Build `ProcessPage[]` array in actor thread before stream starts. Use `Source.From(messages).Ask<ProcessPageResponse>(router, timeout, concurrency)`. No actor context access inside stream.
- **Pattern-match on response type in `FeedCrawlRunState`**: Replace nullable error checks with `switch` on `ProcessPageCompleted`/`ProcessPageFailed`.

## Capabilities

### New Capabilities

_(none — this is a refactor of existing internal behavior)_

### Modified Capabilities

- `feed-crawl-page-actor`: Message shapes change (ProcessPage carries flat data, response becomes abstract hierarchy with Completed/Failed), actor gains IWithUnboundedStash, pool becomes SmallestMailboxPool router with AdjustPoolSize lifecycle.

## Impact

- `src/FunkArr.Feed/Crawl/FeedCrawlPageMessages.cs` — rewrite messages
- `src/FunkArr.Feed/Crawl/FeedCrawlPageActor.cs` — stash, build lookup from targets, new response types
- `src/FunkArr.Feed/Crawl/FeedCrawlPageActorState.cs` — return ProcessPageCompleted/Failed
- `src/FunkArr.Feed/Crawl/FeedCrawlManager.cs` — permanent SmallestMailboxPool, stream Ask, AdjustPoolSize
- `src/FunkArr.Feed/Crawl/FeedCrawlRunState.cs` — pattern match on response type
- `src/FunkArr.Feed/Crawl/RuleSetLookup.cs` — add ToTargets()
- No public API changes. No persistence changes. Internal-only refactor.
