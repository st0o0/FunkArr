## Context

The RuleSet domain has three actors that don't follow the redesigned naming/pattern conventions: `RuleSetRegistryActor` (should be `RuleSetCoordinator`), inline HTTP refresh (should be a `RefreshWorker` child), and `MatchLedgerActor` (standalone, in-memory — should be event-sourced `MatchQualityWorker` under the coordinator).

## Goals / Non-Goals

**Goals:**
- Rename `RuleSetRegistryActor` → `RuleSetCoordinator`
- Extract GitHub refresh into `RefreshWorker` permanent child
- Replace `MatchLedgerActor` with `MatchQualityWorker` (event-sourced Tier 2, child of RuleSetCoordinator)
- Rename `RuleSetGeneratorActor` → `RuleSetGeneratorWorker`
- Update all references (controllers, SearchCoordinator, registration)

**Non-Goals:**
- Changing the matching logic or ruleset data model
- Changing the RuleSetCoordinator's index structure

## Decisions

### D1: MatchQualityWorker replaces MatchLedgerActor

**Decision:** MatchQualityWorker is a `ReceivePersistentActor` child of RuleSetCoordinator with `PersistenceId: "match-quality"`. It keeps the same message API as MatchLedgerActor but persists match records and uses time-based eviction.

**Why:** Match statistics surviving restarts is valuable for ruleset tuning. The existing API (RecordMatchResult, GetRecentMatches, GetTopicStats, GetUnmatched) stays the same.

### D2: SearchCoordinator still resolves by type

**Decision:** SearchCoordinator resolves `RuleSetCoordinator` (renamed from RuleSetRegistryActor) and MatchQualityWorker (renamed from MatchLedgerActor) via `Context.GetActorAsync<T>()`.

**Why:** The Servus resolvable actor pattern uses type-based resolution. Both are registered in the actor system setup.

### D3: RuleSetCoordinator forwards match records to its child

**Decision:** RuleSetCoordinator receives `RecordMatchResult` from SearchCoordinator and forwards it to its `MatchQualityWorker` child.

**Why:** Cross-coordinator messaging goes through the parent (architecture-redesign.md §3.4). SearchCoordinator tells RuleSetCoordinator, not MatchQualityWorker directly.

### D4: RefreshWorker is a simple ReceiveActor child

**Decision:** RefreshWorker wraps `GitHubReleaseClient.RefreshAsync`. On completion it tells the parent `RefreshComplete(bool updated)`. Parent triggers reload.

**Why:** Consistent with the actor-per-HTTP-step pattern. The timer stays in the parent.
