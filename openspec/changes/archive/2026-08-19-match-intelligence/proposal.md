## Why

The current RuleSet system is functionally identical to what MediathekArr and RundfunkArr (TypeScript) offer — per-show JSON rulesets with community sourcing. It provides no competitive differentiation. Worse, the system is a black box: when a search fails to match, there is zero visibility into why. Users cannot diagnose matching problems, rule authors cannot measure rule quality, and the filter system is limited to AND-only logic with no negation — forcing accessibility filtering to be hardcoded rather than configurable.

## What Changes

- **New rule format with full filter logic**: Replace AND-only filters with composable `all` / `any` / `not` filter groups. Add `channel` as a filterable field. Support per-rule confidence scores. Add topic aliasing so shows appearing under multiple Mediathek names share one ruleset. Support merge-based layer overrides instead of all-or-nothing replacement.
- **Match Ledger**: In-memory record of every search's matching process — which items matched (and via which rule), which were filtered (and why), and which fell through all rules unmatched. Time-windowed retention, no persistence across restarts.
- **Match Intelligence API**: REST endpoints exposing match ledger data — recent match results, per-topic aggregate stats (match rates, rule hit rates), and an unmatched items feed that directly enables rule improvement.
- **BREAKING**: New rule format replaces the current `RuleSetFile` / `Rule` / `Filter` model. Community ruleset parser must handle both old and new formats during migration.

## Capabilities

### New Capabilities
- `match-ledger`: In-memory match result recording with per-rule trace information, time-windowed retention, and aggregate statistics computation
- `match-intelligence-api`: REST API endpoints for match observability — recent matches, per-topic stats, unmatched items, rule hit rates

### Modified Capabilities
- `ruleset-data-model`: New rule format with AND/OR/NOT filter groups, per-rule confidence, topic aliasing, typed filter values, configurable capture groups, merge-based overrides
- `ruleset-matching-engine`: Engine must evaluate composite filter trees (all/any/not), emit match trace events to the ledger, and support the new channel filter field
- `ruleset-registry`: Support merge-based layer overrides, topic alias resolution, and backward-compatible community format parsing
- `ruleset-auto-generation`: Generator must emit rules in the new format with per-rule confidence scores

## Impact

- **RuleSet models** (`RuleSet/RuleSetModels.cs`): Complete redesign of `Rule`, `Filter`, `RuleSetFile` records
- **Matching engine** (`RuleSet/RuleSetMatchingEngine.cs`): New filter evaluation tree, match trace emission
- **Registry actor** (`RuleSet/RuleSetRegistryActor.cs`): Alias index, merge logic, backward-compat parsing
- **Generator actor** (`RuleSet/RuleSetGeneratorActor.cs`): Emit new format with per-rule confidence
- **Community parser** (`RuleSet/CommunityRuleSetParser.cs`): Parse old format into new model (bridge)
- **New actor**: `MatchLedgerActor` for in-memory match recording and stats aggregation
- **New endpoints**: Match Intelligence API routes in `FunkArrApplicationSetup`
- **SearchActor** (`Search/SearchActor.cs`): Must emit match events to ledger after each search
