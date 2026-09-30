## Context

`MediaIdentity` is a flat sealed record in `FunkArr.Search` with seven nullable fields covering both Show and Movie concerns. Both `TvSearchWorkerState` and `MovieSearchWorkerState` construct it via a `BaseIdentity` property and `with` expressions. `SceneRelease` consumes it through `EnrichedItem.Identity` to build scene-style release titles and `MatchMetadata` for the Newznab wire format.

The scoring engine (`ScoringEngine`) is media-type-agnostic and produces `TracedIdentification`/`MetadataSpec` with generic Season/Episode/Title fields. The worker states currently pass these through to `MediaIdentity` regardless of media type, meaning movies can acquire Season/Episode values.

## Goals / Non-Goals

**Goals:**

- Eliminate impossible states: movies with Season/Episode, shows with TmdbId/Year
- Make `BaseIdentity` return type encode media type at compile time
- Keep `EnrichedItem.Identity` polymorphic so shared pipeline code stays unchanged
- Keep wire types (`ExternalIds`, `MatchMetadata`) and scoring trace types flat

**Non-Goals:**

- Splitting `ExternalIds` or `MatchMetadata` (Newznab protocol is flat by design)
- Splitting `TracedIdentification`, `MetadataSpec`, `EnrichmentTrace` (scoring engine is intentionally media-type-agnostic; enrichment traces are behind persistence freeze)
- Changing the Newznab XML output format
- Changing persistence records

## Decisions

### 1. Abstract record base with sealed subtypes

```csharp
public abstract record MediaIdentity(string? ImdbId);

public sealed record ShowIdentity(
    string? ImdbId,
    int? TvdbId,
    string? Season,
    string? Episode) : MediaIdentity(ImdbId);

public sealed record MovieIdentity(
    string? ImdbId,
    int? TmdbId,
    int? Year) : MediaIdentity(ImdbId);
```

**Why abstract record over interface:** Records give `with` expressions, positional deconstruction, and value equality. The base holds `ImdbId` which is shared. An interface would lose `with` and force boilerplate equality.

**Why ImdbId on base:** Both Sonarr and Radarr use IMDb IDs. It's the one truly shared external identifier.

### 2. Worker states return concrete subtypes

`TvSearchWorkerState.BaseIdentity` becomes `ShowIdentity`, `MovieSearchWorkerState.BaseIdentity` becomes `MovieIdentity`. The property return type stays `MediaIdentity` (covariant) so `EnrichedItem` doesn't need to change.

`MovieSearchWorkerState` stops using `with { Season = ..., Episode = ... }` from `MetadataSpec`. Instead it uses `with { Year = ... }` from the aired date or enrichment data.

`TvSearchWorkerState` continues using `with { Season = ..., Episode = ... }` from `MetadataSpec` as before.

### 3. SceneRelease uses pattern matching

`SceneRelease.Expand()` currently accesses `Item.Identity.TvdbId`, `.ImdbId`, `.TmdbId`, `.Season`, `.Episode` directly. After the split, it pattern-matches:

```csharp
var (ids, season, episode) = Item.Identity switch
{
    ShowIdentity si => (new ExternalIds(si.TvdbId, si.ImdbId, null), si.Season, si.Episode),
    MovieIdentity mi => (new ExternalIds(null, mi.ImdbId, mi.TmdbId), null as string, null as string),
    _ => (new ExternalIds(null, Item.Identity.ImdbId, null), null as string, null as string),
};
```

`ForShow` can cast to `ShowIdentity` for Season/Episode access. `ForMovie` can cast to `MovieIdentity` for Year access. Both already know the media type from context.

### 4. Keep all types in same file

`ShowIdentity` and `MovieIdentity` stay in `MediaIdentity.cs` alongside the abstract base. They are small records tightly coupled to the base type.

### 5. Wire types stay flat

`ExternalIds` and `MatchMetadata` in `FunkArr.Messages.Shared` remain sealed records with all-nullable fields. They serve two purposes:
- Newznab XML serialization (protocol is flat, all optional attributes)
- RuleSet registry (a ruleset can be either Show or Movie, discriminated by `MediaType?`)

Splitting would force pattern matching in the ArrApi serializer and RuleSet domain for no safety benefit -- these are projection/lookup types, not domain identity.

## Risks / Trade-offs

- **Exhaustiveness**: C# pattern matching on abstract records doesn't warn on missing cases without a discard. The `_` fallback arm in `Expand()` handles future subtypes gracefully but silently. Acceptable since we control the hierarchy and there won't be a third media type.
- **`with` expressions**: `with` on a base-typed `MediaIdentity` variable creates a copy of the concrete type (C# record behavior), but only base properties are accessible. Worker states already hold the concrete type, so this is not an issue.
- **Test churn**: Tests that construct `MediaIdentity` directly switch to `ShowIdentity`/`MovieIdentity`. Straightforward search-and-replace.
