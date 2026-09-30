## Context

After Slice 3, ruleset entities use `MediaRuleSetActor` with `MediaRuleSetActorState` holding three nullable `RuleSetFile?` slots (community, generated, local). Effective rules are computed as `Local ?? Generated ?? Community` — winner-takes-all. The existing `OverrideConfig` (Mode/Base/Add/Remove with OverrideMode enum) was sketched but never implemented, and has defects: removal by index and untyped Base string.

The `Rule` record currently has no identity field. Per-rule stats in Slice 3's `MatchStatsActor` key by rule index, which breaks when rules are reordered.

## Goals / Non-Goals

**Goals:**
- Every rule has a stable, declared string ID
- Override layers can add, replace, or remove individual rules by ID
- Merge resolution is a pure Core function, fully testable without actors
- Every effective rule carries provenance (which layer it came from)
- Validation catches misconfigurations at load time, not at match time

**Non-Goals:**
- Runtime override editing via API (future work)
- Cross-topic overrides (each layer is per-entity)
- Versioned override chains (no migration between override schemas)

## Decisions

### D1: Rule.Id as required string, not optional

**Choice:** `Rule.Id` is a required `string` property, not nullable.

**Why:** Optional IDs would require content-hash fallbacks for rules without IDs, which is wrong for merge — an override changes a rule's content by definition, so a content-derived ID would never match its base. The merge chain needs identity that survives content changes. At 0.x, requiring IDs on all existing rules is acceptable.

**ID format:** Community rulesets use descriptive slugs (e.g., `"airdate"`, `"se-main"`, `"duration-filter"`). Generator uses `"gen-{index}"` (e.g., `"gen-0"`, `"gen-1"`). Local overrides use user-chosen IDs for Add rules.

### D2: Separate Add/Replace/Remove lists

**Choice:** Three separate lists instead of one list with an operation field.

**Why:** A single list keyed by ID would allow both interpretations of each entry (add or replace), making neither mistake detectable. With separate lists: an Add whose ID collides with a base rule is a load-time validation error, and a Replace with a mistyped ID is also an error. This project's defining failure mode was silent drift; the schema that can contradict itself wins.

### D3: RuleSetLayer enum, not string Base

**Choice:** `OverrideConfig.Base` is `RuleSetLayer` enum (`Community`, `Generated`, `Local`), not an untyped string.

**Why:** The existing sketch used `string? Base` which provided no compile-time safety. The enum restricts to valid layers. The chain is declared per-layer: a Generated layer says "I sit on Community", a Local layer says "I sit on Generated" or "I sit on Community" (bypassing generated).

### D4: EffectiveRuleSet as result type with provenance

**Choice:** `RuleSetMerger.Resolve` returns `EffectiveRuleSet` containing `IReadOnlyList<EffectiveRule>` where each `EffectiveRule` wraps a `Rule` with its `RuleSetLayer` origin.

**Why:** The API needs to show where each rule came from (for the UI ruleset detail card). The single-string `ActiveRuleSource` loses this information. Per-rule provenance also enables the match trace to show which layer contributed the winning rule.

### D5: Validation as pure Core function

**Choice:** `RuleSetMerger.Validate` returns `IReadOnlyList<ValidationError>` — pure, no side effects.

**Why:** Validation runs at ruleset load time (registry push, local save) and at generation time. A pure function is testable without actors. Errors are values, not exceptions.

### D6: RecomputeEffectiveRules uses RuleSetMerger

**Choice:** `MediaRuleSetActorState.RecomputeEffectiveRules` calls `RuleSetMerger.Resolve` with the three slots, replacing the inline `Local ?? Generated ?? Community`.

**Why:** Centralizes the merge logic. The state object provides the three slots as input; the merger returns the resolved rules. State no longer needs to understand layering semantics.

**Backward compatibility:** A `RuleSetFile` without `Overrides` is treated as a standalone layer (no merge). This preserves the current behavior for all existing rulesets. Merge only activates when `Overrides` is explicitly declared.

## Risks / Trade-offs

- **All 61 community files need IDs** → Mechanical change, can be scripted. IDs are short descriptive slugs derived from strategy/purpose.
- **Schema breaking change** → Acceptable at 0.x. The `Id` field is required on `Rule`, so old JSON without it will fail deserialization. This is intentional — silent compatibility with ID-less rules would undermine the merge chain.
- **Validation at load time means rejecting bad overrides** → If a user writes a local override with a Replace referencing a non-existent ID, it is rejected at save time. The user gets a clear error, not silent misbehavior.
- **Provenance increases per-rule memory** → One enum value per rule. Negligible.
