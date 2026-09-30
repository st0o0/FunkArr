## Context

The RuleSet system has two representations that diverged: a JSON schema (`data/community/ruleset.schema.json`) designed ahead of the C# model (`FunkArr.MatchMagic`). The schema includes an `overrides` config that was never implemented in C#, uses a `$type` discriminator that C# ignores, and carries dead concepts (`generated` source layer, `exactMatch` operator). The C# model uses loose strings where enums are appropriate. The override system is broken by design — index-based rule removal breaks when the base ruleset reorders.

The project has two ruleset sources: community (maintained in the repo at `data/community/`) and local (user-created). The `generated` layer no longer exists.

## Goals / Non-Goals

**Goals:**
- Align JSON schema and C# model to a single source of truth (schema leads, C# follows)
- Replace the broken override system with an extend-by-default model using rule IDs
- Clean up dead concepts (`source` field, `$type` discriminator, `exactMatch`, `generated` layer)
- Build a pure `RuleSetResolver` that merges community + local into an effective ruleset

**Non-Goals:**
- Change matching strategies or filter evaluation logic
- Add new FilterOp operators or MatchStrategy values
- Modify the MatchMagicManager actor or its message protocol
- Build a UI for ruleset editing
- Implement ruleset file discovery/loading from disk (that's the RuleSet domain project's job)

## Decisions

### 1. Drop `source` field — derive from file path

**Decision:** Remove `source` from the ruleset JSON format. The runtime determines source by which directory the file was loaded from.

**Rationale:** `source` is redundant with file location and can lie (a file in `community/` claiming `"source": "local"`). The system already knows where it loaded a file from. Removing it eliminates a class of inconsistency bugs.

**Alternative considered:** Keep `source` as documentation-only (not validated). Rejected — a field that exists but isn't authoritative invites confusion.

**Impact on C#:** `RuleSet` record loses the `Source` parameter. Callers that need to know the source track it externally (the resolver knows which directory each file came from).

### 2. Drop `$type` discriminator on filter nodes

**Decision:** Remove the `"$type": "filter"` / `"$type": "group"` discriminator from the JSON schema. Use structural detection: presence of `all`/`any`/`not` means FilterGroup, presence of `field`/`op`/`value` means Filter.

**Rationale:** The C# `FilterNodeJsonConverter` already uses structural detection and ignores `$type`. The discriminator adds boilerplate to hand-written JSON for zero benefit. Structural detection is unambiguous — a node cannot have both `field` and `all`.

**Alternative considered:** Add `$type` support to C# serialization for round-trip fidelity. Rejected — it would mean every serialized filter gets a noisy `$type` field that nobody needs.

### 3. Drop `exactMatch`, keep `eq`

**Decision:** Remove `exactMatch` from the FilterOp enum. Keep `eq` as the sole case-insensitive string equality operator.

**Rationale:** Both do the same thing. The C# model only has `Eq`. No existing community rulesets use `exactMatch` (they all use `eq`).

### 4. `MediaRef.Type` becomes `MediaType` enum

**Decision:** Replace `string Type = "show"` with `MediaType Type = MediaType.Show` where `MediaType` is an enum with values `Show` and `Movie`.

**Rationale:** Only two valid values exist. A string accepts invalid values silently. The enum makes deserialization fail-fast on bad data.

### 5. Rule ID required, kebab-case enforced

**Decision:** Every rule gets a required `id` field. Format: `^[a-z][a-z0-9-]{2,}$` (kebab-case, min 3 chars). IDs must be unique within a ruleset file.

**Rationale:** Rule IDs are the foundation of the override system. Index-based references broke on reorder. Name-based references are stable. Kebab-case is consistent with the rest of the OpenSpec/JSON ecosystem.

**Uniqueness scope:** IDs are unique per-file, not globally. Two different topics can both have a rule named `"season-episode"`.

### 6. Extend-by-default override model

**Decision:** When a local ruleset exists for a topic that also has a community ruleset, the local ruleset automatically extends the community base. Override is opt-out via `standalone: true`.

**Rationale:** The common case is "tweak a few things." Requiring explicit opt-in to extending would mean most local rulesets need boilerplate. Standalone is the exception ("I want full control").

**Merge operations:**
- **Same ID** → local rule replaces community rule entirely
- **New ID** → local rule is added
- **`disable: ["id"]`** → community rule is skipped
- **No local match** → community rule is kept as-is

**Field merge semantics:**

| Field | Merge behavior |
|---|---|
| `topic` | Community canonical (local ignored if different) |
| `aliases` | Union of both sets (deduplicated) |
| `media` | Local wins if present, else community |
| `confidence` | Local wins if present, else community |
| `rules` | ID-based merge (replace/add/disable), then sort by priority |

### 7. Two schema profiles, one JSON Schema file

**Decision:** Use a single JSON Schema with conditional validation (`if`/`then`) to enforce profile rules. Community profile: `standalone`/`disable` forbidden, `media`/`confidence`/`rules` required. Local profile: `media`/`confidence` optional, `rules` can be empty.

**Rationale:** Two separate schema files would duplicate the shared structure. JSON Schema's conditional composition handles this cleanly. The profile is determined by which directory the file is in — the validator passes the profile as a parameter or the CI workflow validates each directory with different `required` constraints.

**Alternative considered:** Single permissive schema, enforce profiles in C#. Rejected — schema validation should catch bad files before they reach the runtime.

**Implementation:** The schema file uses `$defs` for shared types. Top-level `oneOf` branches for community vs local profiles, differentiated by required fields and allowed properties.

### 8. RuleSetResolver as pure function

**Decision:** Implement `RuleSetResolver` as a static class with a pure `Resolve(RuleSet? community, RuleSet? local) → RuleSet` method in `FunkArr.MatchMagic`.

**Rationale:** The merge algorithm is pure logic — no I/O, no side effects, no actor dependencies. A static method is trivially testable. The caller (RuleSet domain project) handles file loading and passes parsed rulesets to the resolver.

**Algorithm:**
1. If only one exists → return it
2. If local has `Standalone == true` → return local
3. Start with community rules
4. Remove rules whose ID is in `local.Disable`
5. For each local rule: same ID → replace, new ID → add
6. Sort merged rules by priority
7. Union aliases, local-wins for confidence/media, community topic

## Risks / Trade-offs

- **Breaking change to all community ruleset files** (need rule IDs, `source` removed) → Mitigated by version 0.x policy allowing clean breaks. Migration is mechanical: add `id` fields, remove `source`. A one-time script can do this.
- **`standalone: true` default-false might surprise users** who expect local to fully override → Mitigated by clear documentation. The behavior is predictable: if you don't use `disable` or same-ID rules, extending is a no-op (your local rules just add to community).
- **Rule ID collisions between community and local** are intentional (replacement) but could be accidental → Mitigated by kebab-case naming convention making IDs descriptive and unlikely to collide by accident.
- **Single JSON Schema with conditional profiles adds schema complexity** → Acceptable tradeoff for not duplicating shared types across two files.

## Open Questions

None — all decisions were made during the explore session.
