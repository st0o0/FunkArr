## Why

A local override currently discards the entire community ruleset — "community is fine, I just want one more rule" is not expressible. Generated shadows community entirely, so auto-generation suppresses curated rules rather than complementing them. The merge concept was sketched (`OverrideConfig` with `Mode`, `Base`, `Add`, `Remove`) but never implemented, and the sketch had two defects: removal by rule index (breaks on reorder) and an untyped `Base` string. The design document §5 mandates a declared merge chain with per-rule identity.

## What Changes

- **Rule.Id required**: Add a stable `string Id` property to `Rule`. Declared, not derived. All 61 community ruleset JSON files get IDs. `RuleSetGenerator` assigns IDs to generated rules.
- **OverrideConfig redesign**: Replace `OverrideMode`/index-based removal with `RuleSetLayer` enum, `Add`/`Replace`/`Remove` as separate lists, keyed by rule ID. Delete `OverrideMode` enum.
- **RuleSetMerger**: New pure Core function `RuleSetMerger.Resolve(layers) → EffectiveRuleSet`. Walks the declared chain, applies Add/Replace/Remove by ID.
- **Validation**: Pure Core function validating Replace-ID exists in base, Add-ID does NOT exist, Remove-ID exists, no ID in multiple lists, unique IDs within a document, no cycles.
- **Provenance**: Each effective rule carries its `RuleSetLayer` origin. `ActiveRuleSource` (single string) replaced by per-rule provenance.
- **MediaRuleSetActorState**: `RecomputeEffectiveRules` uses `RuleSetMerger.Resolve` instead of `Local ?? Generated ?? Community`.
- **BREAKING**: Community ruleset JSON schema changes (id field added). `OverrideConfig` schema changes.

## Capabilities

### New Capabilities
- `ruleset-merger`: Pure Core function for resolving rule-set layer chains with merge, replace, and add/remove operations. Includes validation and provenance tracking.

### Modified Capabilities
- `ruleset-data-model`: `Rule` gains required `Id` field. `OverrideConfig` redesigned with `RuleSetLayer` enum, separate Add/Replace/Remove lists keyed by ID. `OverrideMode` enum deleted.
- `community-dataset`: All 61 community ruleset JSON files gain `"id"` on each rule.
- `ruleset-auto-generation`: Generator assigns stable IDs to generated rules.
- `media-ruleset-actor`: `RecomputeEffectiveRules` uses `RuleSetMerger.Resolve` instead of winner-takes-all. `ActiveRuleSource` replaced by provenance.

## Impact

- **Core**: New `RuleSetMerger` class, new `EffectiveRuleSet`/`EffectiveRule` types, `Rule.Id` added, `OverrideConfig` redesigned, `OverrideMode` deleted.
- **Data**: All 61 community JSON files updated with rule IDs. JSON schema updated.
- **Generator**: `RuleSetGenerator` assigns IDs to generated rules in both `Generate` and `GenerateForMovie`.
- **Actor state**: `MediaRuleSetActorState.RecomputeEffectiveRules` rewritten to use `RuleSetMerger`.
- **Tests**: New tests for `RuleSetMerger` (validation, merge, provenance). Updated tests for `Rule.Id`. Community ruleset tests updated for new schema.
- **API**: No external API contract changes — provenance is internal, summary derived.
