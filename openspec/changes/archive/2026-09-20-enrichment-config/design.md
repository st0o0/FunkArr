## Context

EpisodeEnricher and MovieEnricher match Mediathek items against TVDB/TMDB metadata using hardcoded thresholds and a fixed strategy order. The scoring engine already builds a clean "constructed title" from TitleParts rules but drops it in `BuildMetadata()` — the `EpisodeCandidate.ConstructedTitle` field is never populated. Different shows need different enrichment strategies: date-titled shows (Sendung mit der Maus) need airdate-first matching, shows with clean titles (Tatort) work well with title-first, and shows where scoring regex already extracts perfect S/E (Mord mit Aussicht) don't need enrichment at all.

## Goals / Non-Goals

**Goals:**
- Make enrichment thresholds, method ordering, and runtime mode configurable per ruleset
- Wire the scoring engine's constructed title through to the enrichers
- Use proper enums for all finite option sets (methods, runtime mode)
- Maintain full backwards compatibility — existing rulesets without `enrichment` section work identically to today

**Non-Goals:**
- UI for editing enrichment config (future work)
- Per-rule enrichment config (enrichment is a show property, not a rule property)
- Description-based matching (MediathekArr has it, too niche for now)
- Changing the enrichment actor pool topology (pools stay separate)

## Decisions

### EnrichmentConfig travels through RuleSetResolver, not ScoringManager

**Decision:** Route `EnrichmentConfig` through `RegisterRuleSet` → `RuleSetResolverState` → `RuleSetResolved`, not through `MatchingConfig` → `ScoringManager` → `ScoreCompleted`.

**Rationale:** Enrichment config is not a scoring concern. ScoringManager stores `MatchingConfig` for rule evaluation — adding unrelated enrichment data to it violates single responsibility. The resolver already holds per-ruleset metadata (mediaName, mediaType) and is the natural home for per-show config that the search workers need.

**Alternative considered:** Attaching to `MatchingConfig` and echoing back in `ScoreCompleted` — simpler data path but muddies the scoring domain with enrichment concerns.

### Fully populated records, defaults in one factory method

**Decision:** `EnrichmentConfig` and sub-records have no nullable fields. All defaults are applied once in `RuleSetMerger.BuildEnrichmentConfig()` from the nullable `RawEnrichment` JSON model. Downstream code never null-checks.

**Rationale:** Eliminates shotgun null-checking across enrichers, actors, and worker states. One place to change defaults. Same pattern as how `MatchingConfig` is built from `RawRuleSet`.

### Enums for finite option sets

**Decision:** `EnrichmentMethod` (Title, Airdate) and `RuntimeMode` (Tiebreaker, Filter) are enums with `JsonStringEnumConverter` and camelCase `JsonStringEnumMemberName`.

**Rationale:** Compiler-checked exhaustive switch expressions in enrichers. Consistent with existing enums (`IdentificationStrategy`, `FilterField`, `FilterOp`, `TitlePartType`). JSON serialization pattern is established.

### MetadataSpec gains ConstructedTitle

**Decision:** Add `string? ConstructedTitle` to `MetadataSpec`. `ScoringEngine.BuildMetadata()` preserves `TracedIdentification.Title` instead of dropping it.

**Rationale:** The scoring engine already does the hard work of building clean episode names from TitleParts rules. The enricher needs this for accurate Levenshtein matching but currently gets the raw Mediathek title. This is the minimal change to wire through existing data.

### Methods array controls fallback order

**Decision:** `EnrichmentConfig.Methods` is an `EnrichmentMethod[]` that the enricher walks in order. `RegexExtracted` always runs first unconditionally (it's not in the array — it's a fast-path for pre-resolved S/E). Default: `[Title, Airdate]`.

**Rationale:** Different shows need different strategies. Date-titled shows set `[Airdate]`, title-based shows keep the default. The array approach is more flexible than a single "prefer" enum and maps cleanly to a foreach + switch expression.

## Risks / Trade-offs

**[Risk] Enrichment config bloats RuleSetResolverState** → The config is a small record (~6 fields). One `ImmutableDictionary<string, EnrichmentConfig>` is negligible compared to the existing lookup/id/topic/mediaName/mediaType dictionaries. Mitigation: no action needed.

**[Risk] Breaking change to MetadataSpec record** → Adding `ConstructedTitle` with a default value (`null`) is backwards compatible for existing callers. `MetadataSpec` is an in-memory message, not persisted. Mitigation: no migration needed.

**[Risk] Existing rulesets need updating** → No. All enrichment fields are optional. `BuildEnrichmentConfig(null)` returns the current hardcoded defaults. Zero existing rulesets need changes.
