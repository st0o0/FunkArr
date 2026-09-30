## Context

Three terms are overloaded in the codebase:
- "Strategy" refers to both rule extraction methods (scoring) and TVDB match labels (resolution)
- "Resolver" refers to ruleset lookup, TVDB/TMDB orchestration, and pure matching logic
- "Identification" refers to both S/E extraction from titles and episode matching via TVDB

## Goals / Non-Goals

**Goals:**
- Each term has exactly one meaning across schema, messages, and domain code
- Project/class/enum names clearly express their function

**Non-Goals:**
- Changing any behavior or logic
- Touching RuleSetResolver (that name is fine - it resolves topics to ruleset IDs)

## Decisions

### Schema: strategy → extraction

The `strategy` field on rules in ruleset.schema.json becomes `extraction`. This is a breaking schema change (acceptable at 0.x). All community ruleset JSON files need updating.

### Project rename: FunkArr.MetadataResolver → FunkArr.MetadataMatching

The project, test project, namespace, and assembly name all change. Solution file (.slnx) updates. This is a mechanical rename across all referencing .csproj files.

### Class renames within MetadataMatching

- `MetadataResolverManager` → `MetadataMatchingManager`
- `EpisodeResolver` → `EpisodeMatcher`
- `MovieResolver` → `MovieMatcher`
- `TvdbResolverActor` → `TvdbMatchingActor`
- `TmdbResolverActor` → `TmdbMatchingActor`

### Message/enum renames

- `IdentificationStrategy` → `ExtractionMethod`
- `IdentificationSpec` → `ExtractionSpec`
- `IMetadataResolver` (actor key) → `IMetadataMatching`

### Keep RuleSetResolver as-is

RuleSetResolver genuinely resolves (looks up) a ruleset for a topic or ID. The name is accurate.

## Risks / Trade-offs

- [Large diff] - Mechanical rename, no logic changes. Risk is low but review surface is wide.
- [Breaking schema change] - All existing ruleset JSONs need updating. Acceptable at 0.x and there are no external consumers yet.
