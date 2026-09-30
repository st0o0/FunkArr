## Context

Three places fan-out Ask calls to `IHistoryRegion` for `ScoringStatsResult`:

1. **DownloadManager.HandleQueryQueue** - uses Akka.Streams `.Ask()` with bounded
   parallelism (8), `ResumingDecider`, `Sink.Seq`, `PipeTo`. The established pattern.
2. **StatsCollector.HandleBackfillRuleSets** - foreach loop firing N concurrent Ask+PipeTo
   calls. No backpressure, no completion signal, results trickle individually.
3. **RuleSetManager.HandleQueryListWithStats** - `Task.WhenAll` with N async lambdas
   inside `ReceiveAsync`. Blocks the actor mailbox for the entire fan-out duration.

Separately, `RuleSetUpdater.FindRelease()` manually walks `JsonElement` with
`TryGetProperty` to parse GitHub releases API responses, while every other external
API client (Mediathek, TVDB, TMDB, SABnzbd) uses typed record models.

## Goals / Non-Goals

**Goals:**
- Standardize all fan-out-Ask patterns on the DownloadManager's Akka.Streams approach
- Replace `JsonElement` traversal with typed GitHub API models and `ReadFromJsonAsync`
- Fix testability gap: raw `Directory.Exists` bypassing `IDataFiles` abstraction

**Non-Goals:**
- Extracting a separate `GitHubClient` service (single consumer, not justified)
- Adding backfill telemetry instruments (can be done later if needed)
- Changing `ScoringStatsResult` itself (stays a clean value type)
- Changing the RuleSetManager handler from `ReceiveAsync` to `Receive` is a natural
  consequence but not a goal in itself

## Decisions

### 1. Wrapper type `ScoringStatsQueryResult` over modifying `ScoringStatsResult`

The `.Ask()` stream operator returns only the response type. DownloadManager works
because `WorkerStatusResult` carries `DownloadId`. `ScoringStatsResult` has no
`RuleSetId`.

**Chosen**: New `ScoringStatsQueryResult(string RuleSetId, ScoringStatsResult Stats)`
wrapper in Messages/History.

**Alternative rejected**: Adding `RuleSetId` directly to `ScoringStatsResult`. This
creates redundancy in `AllStatsResult` (dictionary keyed by RuleSetId with RuleSetId
also inside the value) and in `UpdateStats(string RuleSetId, ScoringStatsResult Stats)`.
Also, `HistoryState.ComputeStats` constructs `ScoringStatsResult` internally where
no entity-level RuleSetId is in scope.

### 2. GitHub models as internal records in FunkArr.RuleSet

Two records: `GitHubRelease` and `GitHubReleaseAsset` with `JsonPropertyName` attributes.
Only the fields we consume (`tag_name`, `assets[].name`, `assets[].browser_download_url`).
`System.Text.Json` ignores unknown properties by default.

**Placement**: `GitHubModels.cs` in `FunkArr.RuleSet/` as `internal` records. Only
`RuleSetUpdater` uses them. No reason for wider visibility.

**Alternative considered**: Private nested records inside `RuleSetUpdater`. Rejected
because they represent an external API contract, not actor internals. Same distinction
as `MediathekApiModels.cs` being a top-level file in Search.

### 3. Parallelism of 4 for both StatsCollector and RuleSetManager streams

DownloadManager uses parallelism 8 for queue queries where responsiveness matters
(user-facing API). StatsCollector backfill is startup-only, and RuleSetManager stats
enrichment is per-request but lower urgency. Parallelism 4 balances throughput with
shard region load.

### 4. StatsCollector: bulk BackfillComplete replaces individual BackfillStatsResult

Current pattern: N individual `BackfillStatsResult` messages, each doing one
`ImmutableDictionary.SetItem`. Stream pattern: one `BackfillComplete` message with
all results, single handler applies them all. Cleaner semantics - backfill either
completes or fails as a unit.

### 5. RuleSetManager: handler changes from ReceiveAsync to Receive

`HandleQueryListWithStats` currently uses `ReceiveAsync` because it `await`s
`Task.WhenAll`. With streams, the handler becomes synchronous (fire the stream,
PipeTo delivers result later). The `ReceiveAsync` registration changes to `Receive`
with a separate `Receive` for the stream completion message.

### 6. RuleSetManager: IWithTimers for debounce

The debounce timer currently uses manual `ICancelable? _flushSchedule` with
`Context.System.Scheduler.ScheduleTellOnceCancelable`, null-checking in
`ScheduleFlushIfNeeded`, null-reset in `HandleFlush`, and manual cancel in `PostStop`.

`IWithTimers` replaces all of this:
- `Timers.StartSingleTimer("flush", new FlushChanges(), _debounceWindow)` replaces
  the `ScheduleTellOnceCancelable` call
- `Timers.IsTimerActive("flush")` replaces the null-check
- No cleanup needed in `HandleFlush` (single timer auto-clears after firing)
- No manual cancel in `PostStop` (`IWithTimers` handles cleanup)
- Removes the `ICancelable?` field entirely

RuleSetUpdater already uses `IWithTimers` for its refresh timer. This makes both
RuleSet singleton actors consistent.

## Risks / Trade-offs

- **[Stream materialization overhead]** Each stream run creates a lightweight
  materializer graph. For the small collection sizes here (tens of rulesets), this
  is negligible. The DownloadManager already does this per queue query without issues.
  Mitigation: none needed.

- **[ResumingDecider silently drops failures]** Individual Ask failures within the
  stream are skipped. The current StatsCollector also silently drops failures
  (`BackfillStatsResult` with null stats is ignored). Net behavior is equivalent, but
  failures are less visible. Mitigation: acceptable for stats backfill; could add
  logging later if needed.

- **[RuleSetManager response on stream failure]** If the entire stream fails (not
  individual elements), the PipeTo failure handler must still respond to `Sender`.
  Current code catches per-element and always responds. Stream version needs an
  explicit failure message type that responds with a fallback (stats-less list).
