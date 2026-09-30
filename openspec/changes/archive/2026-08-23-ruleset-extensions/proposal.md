## Why

The RuleSet domain needs three targeted improvements to align with the redesigned architecture: (1) community refresh HTTP work should follow the actor-per-HTTP-step pattern by moving into a dedicated `RefreshWorker`; (2) `MatchLedgerActor` (currently a standalone top-level actor with an in-memory bounded buffer) should become `MatchQualityWorker` under `RuleSetCoordinator` with event-sourced persistence so match statistics survive restarts; (3) `RuleSetRegistryActor` renames to `RuleSetCoordinator` following the naming convention. These changes complete the architecture redesign by bringing the RuleSet domain in line with the Coordinator/Worker/Tracker naming convention and persistence tiers.

## What Changes

- Rename `RuleSetRegistryActor` → `RuleSetCoordinator` (naming convention alignment)
- Extract GitHub community refresh into `RefreshWorker` (permanent child) — timer-triggered by parent, dedicated HTTP worker consistent with actor-per-HTTP-step pattern
- Replace `MatchLedgerActor` (standalone, in-memory) with `MatchQualityWorker` (permanent child of RuleSetCoordinator, event-sourced Tier 2)
  - Events: `MatchRecorded(matchRecord)`, `MatchesExpired(olderThan)`
  - Time-based eviction via periodic timer persisting `MatchesExpired` delete events
  - Snapshot every 500 events for fast recovery
  - Serves Match Intelligence API and Ruleset overview with per-topic match rates
- `RuleSetGeneratorWorker` stays as-is (already follows the pattern) — just renamed from `RuleSetGeneratorActor`
- Cross-coordinator messaging: `SearchCoordinator` tells `RuleSetCoordinator` with `RecordMatchResult` (fire & forget), RuleSetCoordinator forwards to `MatchQualityWorker`
- Supervision: Stop for Generator (failure expected, handled via GenerationFailed), Restart for RefreshWorker (stateless HTTP), Restart for MatchQualityWorker (recovers from persistence)

## Capabilities

### New Capabilities
- `refresh-worker`: Permanent child of RuleSetCoordinator for GitHub community ruleset refresh — timer-triggered HTTP download with comparison and report-back to parent
- `match-quality-worker`: Event-sourced (Tier 2) permanent child of RuleSetCoordinator collecting match statistics — time-based eviction, snapshot every 500 events, serves Match Intelligence and Ruleset overview APIs

### Modified Capabilities
- `ruleset-registry`: Renamed to RuleSetCoordinator, community refresh delegated to RefreshWorker child, MatchQualityWorker added as child
- `match-ledger`: MatchLedgerActor replaced by MatchQualityWorker under RuleSetCoordinator with event-sourced persistence
- `match-intelligence-api`: Match Intelligence API reads from MatchQualityWorker (via RuleSetCoordinator) instead of MatchLedgerActor
- `github-release-refresh`: Refresh logic moves from inline `ReceiveAsync` in RuleSetRegistryActor to dedicated RefreshWorker actor

## Impact

- **Actors**: `RuleSetRegistryActor` → `RuleSetCoordinator` (rename + refactor); `MatchLedgerActor` deleted, replaced by `MatchQualityWorker` child; new `RefreshWorker` child
- **Registration**: `FunkArrActorSystemSetup` removes `MatchLedgerActor` registration; RuleSetCoordinator remains a Singleton
- **Persistence**: New `PersistenceId: "match-quality"` for MatchQualityWorker (Tier 2); new event DTOs for MatchRecorded/MatchesExpired
- **Controllers**: `MatchIntelligenceController` resolves RuleSetCoordinator instead of MatchLedgerActor
- **Tests**: MatchLedgerActor tests replaced by MatchQualityWorker tests; RuleSetRegistryActor tests updated for new name and child delegation
