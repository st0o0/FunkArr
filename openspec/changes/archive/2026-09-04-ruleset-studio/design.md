## Context

RuleSets are JSON files stored in two directories: community (synced from GitHub) and local (user-created). RuleSetManager (singleton) discovers files at startup and watches both directories via `IFileSystemWatcher`. Changes are debounced (2s) and flushed to RuleSetWorker (sharded) instances which merge community+local layers via `RuleSetMerger` and push `MatchingConfig` to `MatchMagicManager`.

The UI currently provides read-only views: list, detail, scoring history, and scoring trace. `MatchMagicActor` already produces full traces per candidate — filter condition pass/fail with actual values, identification strategy outcomes with failure reasons. This tracing infrastructure is the foundation for the debugger.

`IDataFiles` already supports `WriteAtomic`, `Remove`, and `Exists`. `DataPaths` exposes `LocalRuleSets` path.

## Goals / Non-Goals

**Goals:**
- Client-side search/filter on the ruleset list page
- Visual builder for creating and editing local rulesets
- Backend write API for local ruleset CRUD
- Ad-hoc scoring test endpoint reusing the existing scoring engine
- MediathekViewWeb search proxy for fetching real test candidates
- Regex101-style live debugger with full rule pipeline trace visualization

**Non-Goals:**
- Editing community rulesets directly (always local overlays)
- Ruleset versioning or undo history
- Client-side scoring engine (duplicating MatchMagicActor logic in TypeScript)
- Recursive multi-level filter nesting in the builder UI (one level suffices)
- Bulk import/export

## Decisions

### Write API relies on file watchers for reload

The write API endpoints write JSON to `DataPaths.LocalRuleSets` using `IDataFiles.WriteAtomic`. The existing `IFileSystemWatcher` in `RuleSetManager` detects the change and triggers a debounced reload — no new actor messages needed.

**Alternative considered:** Explicit reload message to RuleSetManager. Rejected because it duplicates what file watchers already do, and the watcher also handles external edits (manual file changes, git pulls).

### Ad-hoc test scoring via TestScoreItems message

`MatchMagicManager` looks up stored `MatchingConfig` by `ruleSetId` for normal `ScoreItems`. The debugger needs to score with an arbitrary config that may not be persisted. A new `TestScoreItems` message carries an inline `MatchingConfig` + `ScoreCandidate[]`. `MatchMagicManager` forwards it to the existing scoring pool as `ExecuteScoring` with `ScoringOrigin.Test`.

`MatchMagicActor` skips `RecordScoringResult` (history persistence) when `Origin == ScoringOrigin.Test`. The endpoint returns `ItemTrace[]` directly from `ScoreCompleted`.

**Alternative considered:** Separate actor pool for test scoring. Rejected — unnecessary duplication, the existing pool handles it fine.

### MediathekViewWeb proxy in FunkArr.Api

`GET /api/mediathek/search?q=...&limit=20` sends a query to `MediathekViewWebManager` (singleton in Search domain) via Akka Ask, maps the response to ScoreCandidate-shaped JSON. This follows the same pattern as existing API endpoints and respects domain isolation (Api does not reference Search directly, only through actor resolution and Messages).

**Alternative considered:** Direct browser fetch to MediathekViewWeb. Rejected — CORS blocks it, and the proxy keeps the API surface consistent.

### Builder saves RawRuleSet JSON format

The builder posts a JSON object matching the `RawRuleSet` format (same structure as community ruleset files). The write endpoint validates by running `RuleSetMerger.Build` before writing. Invalid configs return 400.

### Editing community rulesets creates local overlays

When editing an existing community-only ruleset, the builder loads its merged config but saves to the local directory. The user chooses between standalone mode (local replaces community) and overlay mode (local adds/overrides/disables rules). The `RuleSetMerger` already supports both via `standalone: true` and `disable: [...]` fields.

### Filter builder: one level of grouping

The UI provides all/any/not groups containing flat conditions (field + op + value). No nested groups within groups. This covers real-world usage without recursive UI complexity. The backend and hand-edited JSON still support full recursion.

### Server-side scoring, not client-side

The scoring engine includes regex evaluation with timeouts, German date parsing, Umlaut normalization, and nested filter evaluation with short-circuit semantics. Duplicating in TypeScript creates drift risk. Server round-trip (<100ms) is acceptable for a debug tool where the user clicks "Test" per iteration.

## Risks / Trade-offs

**Debounce delay between write and reload** → After saving via write API, the ruleset is not queryable via the read API for up to 2 seconds (file watcher debounce window). The debugger is unaffected — it uses ad-hoc scoring with inline config. The UI shows a brief "saving" state.

**Concurrent writes to same ruleSetId** → Last-write-wins at file level. Acceptable for v1 (single-user tool). Could add ETags later if needed.

**Invalid JSON via API** → Mitigated by validating with `RuleSetMerger.Build` before writing. 400 response with error details on failure.

**ScoringOrigin.Test enum addition** → Requires adding a value to the `ScoringOrigin` enum in Messages. Non-breaking — existing code only matches known values.
