## Context

A full-project audit found 13 issues across bugs, inconsistencies, and dead code. The RuleSetManager never initializes in production (no one sends `ScanRuleSets`), pagination parameters are silently discarded in two query handlers, and several naming/pattern inconsistencies exist. This change sweeps all findings in one pass.

## Goals / Non-Goals

**Goals:**
- Fix the two production bugs (RuleSetManager init, pagination discard)
- Align all actors and state records with established project patterns
- Remove dead code that adds confusion without value

**Non-Goals:**
- Changing persistence DTOs or journal format (breaking changes)
- Adding new features beyond making existing parameters work
- Refactoring the download pipeline or search system

## Decisions

### D1: RuleSetManager gets DI + PreStart self-init

Inject `IOptionsMonitor<FunkArrOptions>` via constructor. In `PreStart()`, derive `_communityDir` and `_localDir` from Options and call the scan logic directly — no message needed for initialization.

`ScanRuleSets` becomes a parameterless internal record (just a trigger for re-scan from tests or future use), but `PreStart()` does the actual initial scan without sending a message.

**Why not keep the message-based init?** Nobody sends it today, and there's no legitimate reason for another actor to tell the RuleSetManager where its data lives — that's config, not a command.

**FunkArrOptions changes:** Add `LocalRuleSetDataPath => Path.Combine(DataPath, "local")` to complement existing `RuleSetDataPath`. The Manager derives its directories:
- `communityDir = Path.GetFullPath(Path.Combine(options.RuleSetDataPath, "rulesets"))`
- `localDir = Path.GetFullPath(Path.Combine(options.LocalRuleSetDataPath, "rulesets"))`

### D2: Implement pagination in QueryQueue and QueryHistory handlers

Both handlers already receive messages with `Start`, `Limit`, `Category` parameters — they just discard them. Implementation:

**QueryQueue** (`DownloadManagerState`): After collecting all Worker status responses, apply Category filter, then Skip(Start).Take(Limit) on the combined list. Return total count alongside the page for SABnzbd API `noofslots` field.

**QueryHistory** (`DownloadHistoryManagerState`): Filter by Category if set, then Skip(Start).Take(Limit) on the history list (newest first). Return total count for pagination.

`Limit = 0` means "all items" (existing contract from the message spec). `QueueResult` and `HistoryResult` gain a `TotalItems` (int) field for the unfiltered+filtered count to support pagination in UI/API.

### D3: Immutable collections in RuleSetManagerState

Replace `Dictionary<string, RuleSetPaths>` with `ImmutableDictionary` and `HashSet<string>` with `ImmutableHashSet`. All mutation sites switch to immutable operations (`Add`, `Remove`, `SetItem`). The `with` expressions then produce truly independent copies.

`PendingIds` becomes part of the record properly — `_state = _state with { PendingIds = _state.PendingIds.Add(id) }` instead of `_state.PendingIds.Add(id)`.

### D4: Rename DownloadHistoryActor → DownloadHistoryManager

Rename class, file, state, and state file. Update `IDownloadHistory` → `IDownloadHistoryManager` in ActorKeys. Update all references in AkkaSetupContainer, DownloadWorker, DownloadApiEndpoints, QueueApiEndpoints. Switch registration to `resolver.Props<DownloadHistoryManager>()`.

The PersistenceId stays `"download-history"` to avoid journal migration.

### D5: Remove dead code

- `IDownloadResponse` interface and all `: IDownloadResponse` implementations — no code constrains on it
- `IRuleSetUpdater` from ActorKeys — actor self-starts, nobody resolves it. Registration changes to unnamed singleton (`WithSingleton` without a type key, using `ActorRegistry.Register` directly... actually, Akka.Hosting requires a key type. Keep the key but note it's only for Akka.Hosting registration plumbing, not for resolution.)
- `_dataDirectory` field from RuleSetManager (covered by D1)
- `using Akka.IO` from MediathekViewWebManager

**Revised on IRuleSetUpdater**: Since Akka.Hosting `WithSingleton<TKey>` requires the key type for registration, we keep `IRuleSetUpdater` in ActorKeys. It serves the registration system even if nobody resolves it. Remove it from the dead-code list.

### D6: Add Path.GetFullPath at initialization

In `PreStart()` of RuleSetManager (D1), paths from Options are resolved via `Path.GetFullPath`. Similarly, `DownloadWorker.ComputePaths` wraps its path construction with `Path.GetFullPath` for the incomplete and output directories.

## Risks / Trade-offs

**[QueueResult/HistoryResult gain TotalItems field]** → This adds a field to response messages. Existing API consumers (Sonarr/Radarr) don't expect it — but these are JSON responses and extra fields are ignored by default. No breaking change.

**[IDownloadResponse removal]** → If future code needs a common download response constraint, it must be re-added. Low risk — the interface was never used in 5+ changes.

**[DownloadHistoryManager rename]** → PersistenceId stays `"download-history"` so journal compatibility is preserved. Architecture tests may need updating if they check actor class names against the naming convention.

**[Immutable collections in RuleSetManagerState]** → Slightly more allocation on each mutation. Negligible for a manager that processes file events at most every few seconds.
