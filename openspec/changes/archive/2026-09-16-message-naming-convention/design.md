## Context

FunkArr.Messages contains ~90 record types across 6 domain namespaces. Each domain evolved independently, leading to inconsistent naming. The `clean-message-interfaces` change standardized marker interfaces but left naming patterns untouched.

Current state by domain:

| Domain | Commands | Queries | Responses |
|---|---|---|---|
| Search | `*Command` suffix | — | `*Completed`/`*Failed` |
| Download | bare verbs | `Query*` prefix | `*Result` |
| RuleSet | bare verbs | `Query*` prefix | `*Result`/domain-specific |
| Scoring | bare verbs | `Query*` prefix | `*Completed`/`*Failed`/`*Result` |
| Mediathek | `Query*` prefix | — | `*Completed`/`*Failed` |
| MetadataResolver | `Resolve*` prefix | `Query*` prefix | `*Resolved`/`*Failed`/`*Result` |

## Goals / Non-Goals

**Goals:**
- Define a single, unambiguous naming convention for all message types
- Rename existing messages to follow the convention
- Add missing `IDownloadResponse` marker interface
- Convention should be intuitive — reading the type name tells you what kind of message it is

**Non-Goals:**
- Changing message field names or types (that's a different concern)
- Changing the namespace structure (already clean)
- Renaming persistence DTOs (already consistent with past-tense events)
- Renaming data carrier records (ScoreCandidate, MediathekItem, etc.)

## Decisions

### Decision 1: Naming Convention

After analyzing the codebase, we adopt a convention that minimizes renames while being consistent:

**Commands** (trigger an action, expect a response):
- Pattern: **bare verb phrase** — `VerbNoun` or `VerbNounNoun`
- Examples: `StartDownload`, `CancelDownload`, `AddDownload`, `DeleteDownload`, `RegisterRuleSet`, `DeregisterRuleSet`, `ResolveRuleSet`, `ResolveEpisodes`, `ResolveMovie`, `ScoreItems`
- Rationale: Download domain's bare-verb style is the most natural in C# and already the majority. Search's `*Command` suffix is redundant — the verb already signals a command.
- **Renames**: `SearchCommand` → `Search`, `TvSearchCommand` → `TvSearch`, `MovieSearchCommand` → `MovieSearch`. Or keep as-is since they're internal routing messages (TvSearch/MovieSearch are internal to the Search domain).

**Queries** (request data, always expect a response):
- Pattern: **`Query` prefix** — `QueryNoun`
- Examples: `QueryQueue`, `QueryHistory`, `QueryWorkerStatus`, `QueryMediathek`, `QueryRuleSetDetail`, `QueryScoringHistory`, `QueryCacheStats`
- Rationale: Already the dominant pattern. Clear and consistent. `Query` prefix immediately signals "read-only, returns data."
- **Renames**: None needed — all queries already use `Query*` prefix.

**Responses to commands** (async operation completed or failed):
- Success: **past-tense verb or `*Completed`** — pattern depends on domain semantics
- Failure: **`*Failed`** — always use `Failed` suffix with `Reason` + `Cause`
- Examples: `SearchCompleted`/`SearchFailed`, `DownloadAdded`, `ScoreCompleted`/`ScoringFailed`, `RuleSetResolved`/`RuleSetNotFound`
- Rationale: These already work well. `Completed`/`Failed` for async operations, past-tense for immediate acknowledgments.
- **Renames**: None needed.

**Responses to queries** (data returned):
- Pattern: **`*Result`** — `NounResult` or `NounNounResult`
- Examples: `QueueResult`, `HistoryResult`, `WorkerStatusResult`, `ScoringHistoryResult`, `CacheStatsResult`, `RuleSetDetailResult`, `RegisteredRuleSetsResult`
- Rationale: Already the dominant pattern. `*Result` clearly signals "this is data in response to a query."
- **Renames**: None needed.

**Internal signals** (actor-to-actor, no response expected):
- Pattern: **bare noun or verb phrase** — `SlotFree`, `RecordDownload`, `RemoveHistoryEntry`, `RecordScoringResult`, `RemoveMatchingConfig`
- Rationale: These are fire-and-forget internal messages. No suffix needed.
- **Renames**: None needed.

**Data carriers** (nested types, no protocol role):
- Pattern: **descriptive noun** — `MediathekItem`, `ScoreCandidate`, `ScoredItem`, `QueueItem`, `HistoryItem`, `SearchResultItem`
- Rationale: Already consistent. No changes.

### Decision 2: Search domain command renames

The main rename is in the Search domain:
- `SearchCommand` → `SearchCommand` (keep — it's the public entry point and the `Command` suffix distinguishes it from the general concept of "search")
- `TvSearchCommand` → `TvSearch` (internal routing message, drop suffix)
- `MovieSearchCommand` → `MovieSearch` (internal routing message, drop suffix)

Actually, reconsidering: `SearchCommand` is the only public-facing command. `TvSearchCommand` and `MovieSearchCommand` are internal shard-routed messages. The `Command` suffix on the public entry point is fine — it's the canonical "initiate a search" message. The internal ones should drop it since they're routing artifacts, not commands from outside.

### Decision 3: IDownloadResponse marker interface

Add `IDownloadResponse` marker interface in `FunkArr.Messages.Download`. Apply to:
- `DownloadAdded`
- `QueueResult`
- `HistoryResult`
- `HistoryStatsResult`
- `HistoryCategoriesResult`
- `DeleteDownloadResult`
- `RetryDownloadResult`
- `WorkerStatusResult`

This enables typed `Ask<IDownloadResponse>` calls in FunkArr.Api download endpoints.

### Decision 4: Rename scope is minimal

After careful analysis, the actual rename count is small:
1. `TvSearchCommand` → `TvSearch`
2. `MovieSearchCommand` → `MovieSearch`

Everything else already follows the convention. The real value of this change is:
- Formalizing the convention as a spec (so new messages follow it)
- Adding the missing `IDownloadResponse` marker interface
- Updating all `Ask<object>` calls in Download endpoints to `Ask<IDownloadResponse>`

## Risks / Trade-offs

- **[Breaking changes]** Renames break internal compilation across projects. 0.x means no external consumers to worry about.
- **[Minimal renames]** The convention mostly codifies what exists. This is a feature, not a bug — the codebase already landed on good patterns organically. The value is preventing future drift.
- **[SearchCommand keeps suffix]** We could drop it to be consistent with bare verbs, but it's the public entry point and `Search` alone is too generic as a type name.
