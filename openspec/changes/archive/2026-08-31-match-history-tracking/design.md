## Context

The MatchMagic scoring pipeline evaluates MediathekViewWeb candidates against RuleSet configurations and returns a flat `ScoredItem(Index, Score, Matched)` result. All intermediate evaluation state — which rule matched, which filters passed/failed, what was identified — is discarded after scoring. Users tuning RuleSets have no visibility into match decisions.

The system already uses sharded entity actors (RuleSetWorker) and event-sourced persistence (T2 tier with snapshots). The MatchMagicManager is a singleton that routes to a stateless MatchMagicActor pool. Messages flow: SearchWorker → MatchMagicManager → MatchMagicActor → ScoreCompleted back to SearchWorker.

## Goals / Non-Goals

**Goals:**
- Full scoring trace capturing every filter condition evaluation (with actual values) and identification attempt per rule per item
- Persistent history of scoring requests per RuleSet, queryable by the UI API
- Clean separation between scoring response path (stays slim) and history path (fire-and-forget)
- Versioned persistence DTOs with JSON snapshot tests from day one
- Configurable retention (max count + max age)

**Non-Goals:**
- UI endpoints (separate change — this delivers the actor/message/persistence layer)
- Automatic RuleSet quality scoring based on history data
- Real-time streaming of trace data (history is query-based, not push)
- Trace data in Newznab/SABnzbd adapter responses

## Decisions

### 1. Separate MatchHistoryWorker over enriched ScoreCompleted

**Choice:** MatchMagicActor builds the trace internally and fire-and-forgets a `RecordScoringResult` to the MatchHistoryWorker. ScoreCompleted stays slim (`ScoredItem[]` + `RequestId`).

**Why over enriching ScoreCompleted:** The Newznab/SABnzbd path doesn't need trace data. Enriching ScoreCompleted would force all consumers to carry the overhead. The history concern is orthogonal to the scoring response — a separate actor keeps the responsibilities clean.

**Why fire-and-forget:** History recording must never block or fail the scoring response. If the MatchHistoryWorker is temporarily unavailable, the scoring result still reaches the caller. Lost history entries are acceptable; lost search results are not.

### 2. Sharded by RuleSetId

**Choice:** MatchHistoryWorker is a sharded entity keyed by RuleSetId (naming convention: `*Worker`).

**Why over singleton:** Each RuleSet's history is independent. A singleton would bottleneck when multiple RuleSets score simultaneously and would accumulate unbounded state across all RuleSets. Sharding gives natural partitioning, independent passivation, and bounded state per actor.

**PersistenceId:** `match-history-{ruleSetId}`

**Passivation:** 5 minutes of inactivity. Most RuleSets score intermittently (Sonarr RSS polls every 15-30 min). Short enough to save memory, long enough to absorb burst scoring without repeated recovery.

### 3. Full filter trace with short-circuit markers (Option C + Option A)

**Choice:** Trace every filter condition that was actually evaluated. Short-circuited conditions (e.g., remaining `All` conditions after first failure) are marked `Skipped = true` without evaluation.

**Why full trace:** Users debugging RuleSets need to see which specific filter condition failed and what the actual item value was. "FilterFailed" without detail forces them to guess.

**Why short-circuit (not full evaluation):** Evaluating all conditions regardless of short-circuit changes semantics — regex filters that would never run in production would run for tracing, potentially masking performance characteristics. The trace should show what the engine actually did, not a hypothetical. The UI can display skipped conditions clearly.

### 4. Persistence DTOs separate from Messages

**Choice:** All trace data persisted via dedicated DTOs in `FunkArr.Persistence/MatchHistory/`, mapped from Message records in the MatchHistoryWorker.

**Why:** Messages and persistence have different lifecycles. A Message record can be freely renamed or restructured across versions. A persistence DTO is extend-only with stable `[JsonProperty]` strings. Coupling them means either Messages can't evolve or persistence breaks.

**Versioning:** Each DTO has a `Version` property. Recovery code handles all versions >= 1. New nullable properties with defaults for forward compatibility.

### 5. ScoreItems redesign with ScoringOrigin

**Choice:** `ScoreItems(Guid RequestId, string RuleSetId, ScoringOrigin Origin, ScoreCandidate[] Candidates)` where `ScoringOrigin(string Source, string Query)`.

**Why nested Origin over flat fields:** RequestId and RuleSetId are consumed by the scoring engine. Source and Query are pass-through for the history actor — different consumers, different concern. The nesting makes this explicit. MatchMagicActor passes Origin through to RecordScoringResult without inspecting it.

**Why extend ScoreCandidate with Description/Timestamp:** The filter system defines Description and Timestamp as filterable fields, but ScoreCandidate doesn't carry them — `ResolveField` returns hardcoded null/"0". For the trace to show meaningful `ActualValue`, the candidate must carry the real data. Building the message correctly now avoids a trace that lies.

### 6. ExecuteScoring carries HistoryRef

**Choice:** The internal `ExecuteScoring` message (MatchMagicManager → MatchMagicActor) gains `RequestId`, `Origin`, and `IActorRef HistoryRef` (the MatchHistory ShardRegion).

**Why pass HistoryRef vs. actor resolution in MatchMagicActor:** Pool workers are created by the router, not via DI. They cannot resolve actors from `IActorRegistry`. The Manager resolves the ShardRegion once and passes it through. This follows the existing pattern where ExecuteScoring already carries the Config.

### 7. Retention: dual policy, configurable

**Choice:** Both max snapshot count and max age, whichever triggers first. Configured via `appsettings.json` under `FunkArr:MatchHistory`.

**Defaults:** MaxSnapshots = 100, MaxAgeDays = 30, SnapshotInterval = 20 (Akka persistence snapshots).

**Trimming:** Applied on recovery (after replaying events) and after each new persist. Ensures state never exceeds bounds even if the actor was passivated for a long time.

### 8. JSON snapshot tests for persistence DTOs

**Choice:** Golden-file tests that serialize a fully-populated DTO to JSON, compare against a checked-in `.json` file, and verify roundtrip deserialization produces an equal object.

**Why from day one:** Persistence DTOs are the most dangerous code to change accidentally. A renamed `[JsonProperty]` silently breaks recovery of all existing journal entries. Snapshot tests catch this at compile/test time rather than in production.

**Location:** `FunkArr.MatchMagic.Tests/Snapshots/` with golden files per DTO version.

## Risks / Trade-offs

**Trace size in persistence** — A scoring request with 50 items × 5 rules × 5 filter conditions produces ~1250 condition traces. At ~100 bytes per condition trace, that's ~125KB per snapshot. With 100 snapshots retention, each shard holds ~12.5MB. Acceptable for SQLite, but worth monitoring.
→ Mitigation: Retention policy bounds growth. SnapshotInterval (20) means recovery replays at most 20 events before hitting a snapshot.

**Fire-and-forget reliability** — If MatchHistoryWorker crashes during persist, the trace is lost. No retry, no dead-letter recovery.
→ Mitigation: Acceptable trade-off. History is diagnostic, not transactional. The scoring result (the thing that matters) is already delivered. Structured logging of persist failures provides observability.

**ScoreCandidate expansion** — Adding Description and Timestamp to ScoreCandidate means SearchWorkers must map these fields from MediathekItem. If MediathekViewWeb doesn't return a description for some items, it's null — which is correct and honest in the trace.
→ Mitigation: Both fields are nullable/have defaults. No breaking change to callers that don't populate them.

**Message shape changes break existing tests** — ScoreItems, ScoreCompleted, ExecuteScoring all change signature. Every existing test in MatchMagic.Tests and Search.Tests needs updating.
→ Mitigation: Mechanical update — add new required fields to test constructors. No logic changes in existing tests.
