## Context

The Feed crawl pipeline in `FunkArr.Feed` uses several intermediate types (`CrawlInput`, `CrawlWindow`, `RuleSetLookup`, `FeedGate`, `CrawlResult`) that fragment state and logic. `FeedCrawlRunState` mixes config snapshots, a helper class, and mutable accumulation state. Two cross-domain messages (`EvaluateSeries`, `EvaluateMovies`) embed `RuleSetResolved?` — a response record used as a command property.

Current `FeedCrawlRunState` holds:
- `CrawlInput` — a 9-field bag mixing window computation params (used once before state creation), run params (used during crawl), and 2 dead fields (`PagePrefetch`, `MaxConcurrency`)
- `CrawlWindow` — 3 fields computed from `CrawlInput` + `listTimestamp`
- `RuleSetLookup` — dictionary wrapper with dead `TryMatch`/`Match` methods; only `ToDictionary()` is called
- Mutable counters and entry accumulation

## Goals / Non-Goals

**Goals:**
- `FeedCrawlRunState` is a single, self-contained state for an active crawl run with flat fields and a `static Init` factory
- No intermediate wrapper types (`CrawlInput`, `CrawlWindow`, `RuleSetLookup`) — their data lives directly on the state
- `FeedGate` logic moves into `FeedCrawlPageActor` as private static methods (only caller)
- `CrawlResult` is a plain DTO record without factory methods
- PipeTo wrapper messages extract data, not wrap entire messages
- `EvaluateSeries`/`EvaluateMovies` carry flat properties instead of `RuleSetResolved?`

**Non-Goals:**
- Changing the crawl lifecycle state machine (Idle/Probing/Crawling/Publishing) or persistence
- Changing `ProcessPage`/`ProcessPageResponse` message shapes
- Changing `FeedCrawlManagerState` (persistent state)
- Restructuring the page actor's Become states

## Decisions

### Decision 1: Two-phase `FeedCrawlRunState` initialization

The Manager creates `_runState` in two steps because a `Become` boundary separates the probe result from the ruleset response:

1. **After probe:** `FeedCrawlRunState.Init(listTimestamp, lower, upper, truncated, pageSize, maxPages, minConfidence, startedAt)` — creates state with all run parameters, `TopicTargets` is `null`.
2. **After rulesets loaded:** `_runState.Apply(entries)` — builds the topic→target dictionary internally from `RegisteredRuleSetEntry[]`.

**Why not a single Init:** The probe and ruleset-load are separate `Become` phases. Parking the window values as loose fields on the actor would scatter run state across actor and state object — exactly what we're cleaning up.

**Why not pass `FeedOptions` to Init:** The state shouldn't know about options types. The Manager extracts the values it needs and passes primitives.

### Decision 2: `CrawlWindow` computation stays on the Manager

`ComputeWindow()` remains a private method on `FeedCrawlManager` and returns a tuple `(long lower, long upper, bool truncated)`. The state receives these as flat parameters.

**Why:** The window computation uses the persistent cursor (`_state.Cursor`) which belongs to the Manager. The run state shouldn't reach back into the persistent state.

### Decision 3: `FeedGate` logic moves into `FeedCrawlPageActor`

- `StableIdFor(channel, url)` → `private static string StableIdFor(...)` on the actor
- `Evaluate(results, sources, mediaType, minConfidence)` → `private static GateResult Evaluate(...)` on the actor
- `Reject(...)` → `private static RejectionReason? Reject(...)` on the actor
- `GateResult` → stays as a record, declared at actor file level or as private nested

**Why not on the state:** These are stateless pure functions operating on evaluation results. They don't mutate state. Private static methods on the actor keep them close to the only call site.

### Decision 4: Flatten `EvaluateSeries`/`EvaluateMovies` messages

Replace `RuleSetResolved? RuleSet` with:
- `string? RuleSetId`
- `string? MediaName`
- `EnrichmentConfig? Enrichment`

The evaluation state's `Init` reads these directly instead of destructuring `RuleSetResolved`.

**Why:** `RuleSetResolved` is a response from `ResolveRuleSet`. Embedding a response record as a command property violates message conventions. The command should carry the data it needs, not a handle to how that data was obtained.

### Decision 5: Flatten PipeTo wrapper messages

- `RuleSetsLoaded(RegisteredRuleSetsResult Result)` → `RuleSetsLoaded(IReadOnlyList<RegisteredRuleSetEntry> Entries)` with PipeTo extracting `.Entries`
- `FetchCompleted(QueryMediathekCompleted Result, ProcessPage Request)` → extract the fields the actor actually reads: `FetchCompleted(MediathekItem[] Items, long ListTimestamp, ProcessPage Request)` — `ProcessPage` stays because the actor needs the full request context for evaluation routing

**Why keep ProcessPage on FetchCompleted:** The page actor uses `Request.TopicTargets`, `Request.MinConfidence`, and `Request.LowerBound`/`Request.UpperBound` during evaluation setup. Extracting all of those into the wrapper would be worse than keeping the request.

### Decision 6: `CrawlResult` keeps the record, drops factories

`CrawlResult` stays as a sealed record (it's consumed by `CompleteCrawl` and `RecordTelemetry` — a useful data boundary). The 4 static factory methods are removed; callers use `new CrawlResult(...)` directly. `GetCrawlResult(duration)` on the run state builds the record inline.

### Decision 7: `RuleSetTarget` record stays, moves to its own file

`RuleSetTarget(string RuleSetId, MediaType MediaType, ExternalIds Ids)` is used on `ProcessPage.TopicTargets` — it's a real domain type. It moves from `RuleSetLookup.cs` (deleted) to `Crawl/RuleSetTarget.cs` in `FunkArr.Feed`.

## Risks / Trade-offs

- **9 parameters on `FeedCrawlRunState.Init`**: Acceptable for a factory called in one place. The alternative (introducing a parameter record) would recreate `CrawlInput`. Positional record constructor keeps it compact.
- **`FeedGate` test coverage moves to actor-level tests**: `FeedGateTests` tested the static methods in isolation. After inlining, we either make the methods `internal` for testing or rely on `FeedCrawlPageActorTests` covering the same paths. Recommendation: keep as `private static` — the actor tests already cover these paths via `ProcessPage` → `ProcessPageCompleted` flows.
- **`RuleSetLookupTests` deleted**: The dictionary-building logic now lives inside `FeedCrawlRunState.Apply(entries)`. The existing `FeedCrawlRunState` test coverage should include a test that verifies topic→target mapping after `Apply(entries)`.
