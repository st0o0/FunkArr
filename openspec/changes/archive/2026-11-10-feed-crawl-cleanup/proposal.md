## Why

The Feed crawl pipeline has accumulated intermediate types (`CrawlInput`, `CrawlWindow`, `RuleSetLookup`, `FeedGate`, `CrawlResult`) that fragment state and logic across too many small wrappers. `FeedCrawlRunState` is a mix of config params, a helper class, and actual mutable state. Several actor-private PipeTo messages wrap entire response messages instead of extracting the data they need. Two cross-domain messages (`EvaluateSeries`, `EvaluateMovies`) embed `RuleSetResolved?` — a response record used as a command property — violating message conventions.

## What Changes

- **Flatten `FeedCrawlRunState`**: Absorb `CrawlInput` and `CrawlWindow` fields directly onto the state. Delete both records. Add `static Init(...)` factory that takes raw parameters and ruleset entries. The state owns all data for an active crawl run.
- **Delete `RuleSetLookup`**: Inline the dictionary-building logic into `FeedCrawlRunState.Init`. Remove dead `TryMatch`/`Match` methods. `RuleSetTarget` record stays.
- **Delete `FeedGate`**: Move `StableIdFor`, `Evaluate`, and `Reject` as private static methods into `FeedCrawlPageActor`. `GateResult` stays as a local record on the actor.
- **Simplify `CrawlResult`**: Remove factory methods (`ForCompleted`, `ForCompletedPartial`, `ForFailed`, `ForUnchanged`). Keep the record as a plain DTO with direct construction.
- **Flatten PipeTo wrappers**: `RuleSetsLoaded` extracts `Entries` instead of wrapping `RegisteredRuleSetsResult`. `FetchCompleted` extracts the fields the actor needs instead of wrapping two messages.
- **Flatten `EvaluateSeries`/`EvaluateMovies`**: Replace `RuleSetResolved? RuleSet` with flat properties (`string? RuleSetId`, `EnrichmentConfig? Enrichment`). Remove the response-as-property pattern.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `feed-crawl-lifecycle`: `FeedCrawlRunState` shape changes (flat fields, `static Init`, two-phase initialization with `Apply(entries)`). `CrawlResult` loses factory methods.
- `feed-eval-gate`: `FeedGate` is deleted as a standalone class; logic moves into `FeedCrawlPageActor` as private static methods. `GateResult` becomes actor-local.
- `feed-crawl-page-actor`: `ProcessPage.TopicTargets` type stays `IReadOnlyDictionary<string, RuleSetTarget>`. `FetchCompleted` PipeTo wrapper flattened. `FeedGate` methods become actor-private.

## Impact

- **FunkArr.Feed**: `CrawlInput.cs`, `RuleSetLookup.cs`, `FeedGate.cs` deleted. `FeedCrawlRunState.cs`, `FeedCrawlManager.cs`, `FeedCrawlPageActor.cs` modified.
- **FunkArr.Messages**: `EvaluateSeries.cs`, `EvaluateMovies.cs` modified (flat properties replace `RuleSetResolved?`).
- **FunkArr.Search**: Callers that construct `EvaluateSeries`/`EvaluateMovies` with `RuleSet:` parameter updated to pass flat fields.
- **FunkArr.Feed.Tests**: `CrawlResultTests`, `RuleSetLookupTests`, `FeedGateTests` updated or removed. `FeedCrawlRunState` tests updated for new `Init` + `Apply(entries)` pattern.
- **No persistence changes**: No DTO or journal format changes.
- **No API changes**: No external-facing behavior changes.
