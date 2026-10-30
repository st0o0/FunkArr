# Design

## Context

- `TvSearchWorkerState` / `MovieSearchWorkerState` turn `MediathekItem`s into `SourceInfo` and later expand **all** items into releases in `ToSearchCompleted`, including items with `Matched=false`. Without a ruleset or matching config, `UnscoredItems()` returns every source unscored.
- `ScoringEngine.Score` evaluates ruleset rules per `ScoreCandidate` and produces `ScoredItem`s plus `ItemTrace`s, which Search sends to History.
- Settings in FunkArr are configuration-bound options (`IOptionsMonitor<T>`), shown read-only in the UI (`/api/downloads/settings`, `/api/system/routes`).

See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**
- One detection and one policy decision per item, applied identically on every search path.
- Excluded items stay visible in match history with a reason.

**Non-Goals:**
- Editing the policy from the UI (FunkArr has no writable settings yet).
- A "variant only as fallback" mode (see #203 out of scope).
- Detecting the actual original language of an original version.

## Decisions

### Detect in Search at item intake, not in Scoring

Issue #203 placed detection in Scoring. Implementation showed that scoring does not see every item (no ruleset, no matching config), and that `Matched=false` items are still returned. The only point every item passes is `SourceInfo.From`, in Search. So:

- `VariantDetector` (static, pure; moved with the evaluation pipeline to `FunkArr.Feed` by `evaluation-workers`) computes `MediathekVariant` from title and topic. `SourceInfo` carries it.
- The worker states filter excluded items in `ToSearchCompleted`, covering scored, unmatched and unscored results with one rule.
- Search marks excluded candidates with `ScoreCandidate.ExcludedVariant`. Scoring only records them: it skips rule evaluation and writes the reason into the trace. The policy itself never reaches Scoring.

*Alternative:* detection in Scoring plus a second detection for unscored paths. Rejected because it duplicates the logic across two domains, which domain isolation forbids sharing.

### Title and topic only, fixed precedence

Descriptions often say "also available with audio description", which would mark standard versions as excluded. Precedence AudioDescription > SignLanguage > PlainLanguage > OriginalVersion: an OV item with audio description is primarily an accessibility variant. "Hörfassung" is ARD's term for audio description, so it maps to `AudioDescription` rather than a separate variant. Patterns are compiled regexes with word boundaries, case-insensitive, with a timeout like the scoring engine's.

### Policy as options

`VariantOptions` (`FunkArr:Variants`, in Core) has one `VariantPolicy` property per non-standard variant, defaulting to `Exclude`, plus `IsExcluded(MediathekVariant)`. Invalid enum values fail at startup through `ValidateOnStart` (the configuration binder names the key). Workers receive `IOptionsMonitor<VariantOptions>` through DI and snapshot `CurrentValue` per search, so changes apply to new searches. The constructor parameter is optional, so existing tests that create workers directly keep working; they get the default policy.

### Release title tag

`SceneRelease.FormatTitle` inserts a tag before the language token: `AD`, `DGS`, `EinfacheSprache`; `OriginalVersion` emits `OV` **instead of** `GERMAN`. Sonarr and Radarr then treat the release as non-German (they assume English without a language token), which is closer to reality than `GERMAN`. The exact original language stays unknown (non-goal).

### Trace and persistence

- `ItemTrace` gets `MediathekVariant? ExcludedVariant = null` (appended, optional).
- `PersistedItemTrace` gets `PersistedMediathekVariant? ExcludedVariant = null` (extend-only). Mapping uses the existing int-cast pattern. Old events deserialize with `null`.
- API `ItemTrace` gets the same field. `GET /api/system/variants` returns the effective policy. Both use numeric enums, like the rest of the API.

### UI

- Settings page: a read-only "Variants" section listing each variant with Allowed/Excluded.
- Scoring detail: an "Excluded: <variant>" badge instead of the rule list for excluded items.
- i18n keys in en and de; de-AT and de-CH only override individual keys and fall back to de.

## Risks / Trade-offs

- [Default exclusion changes existing results] → Documented in the docs and release notes. A single setting restores the old behavior per variant.
- [False positives on short tokens (AD, OV, DGS)] → Tokens only match as whole words in parentheses, after "mit", or standalone. Unit tests cover known false-positive titles ("ADAC", "OVB").
- [Excluded items count as rejected in scoring metrics] → Acceptable. They are rejected. No new metric is added.
- [Mediathek titles not covered by the patterns] → Patterns live in one place and are easy to extend. Unknown markers fall back to `Standard`, which is today's behavior.

## Migration Plan

No data migration: old history events read with `ExcludedVariant = null`. Rollback: revert. Users who want variants set `FunkArr__Variants__<Variant>=Allow`.
