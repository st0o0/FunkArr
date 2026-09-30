## Context

The MetadataResolver domain enriches scored mediathek items with structured catalog metadata (TVDB episodes for TV, TMDB data for movies). The current terminology uses "resolution" throughout — messages, result types, strategy labels, and config. This creates confusion because `ResolutionStrategy` means two unrelated things: an input mode (fuzzy/strict/none) and an output label (TitleMatch/AirdateMatch/etc.). Additionally, `ResolutionConfig` is never configured — it's always `new ResolutionConfig()` with defaults.

## Goals / Non-Goals

**Goals:**
- Rename resolution domain to enrichment terminology for clarity
- Introduce `MatchMethod` enum replacing string-based strategy labels
- Remove unused `ResolutionConfig` — resolvers use sensible defaults directly
- Consistent naming across TV and movie result types
- Add `IEnrichmentResult` marker interface for shared result contract

**Non-Goals:**
- Changing resolver logic or match algorithms
- Changing the search pipeline flow (workers still Ask the MetadataResolver)
- Adding new match methods or enrichment capabilities
- Renaming the `FunkArr.MetadataResolver` project itself (the project resolves metadata, the operation on items is enrichment)

## Decisions

### D1: MatchMethod as enum in FunkArr.Messages

The `MatchMethod` enum lives in `FunkArr.Messages.MetadataResolver`, not in the `FunkArr.MetadataResolver` project. This follows the existing pattern: message types live in Messages, domain logic references them.

Values:
- `RegexExtracted` — S/E parsed from title by scoring regex rules
- `TitleMatch` — Levenshtein title similarity (covers both fuzzy episode match and movie title match)
- `AirdateMatch` — matched by air date proximity
- `YearMatch` — movie matched by year + weak title

**Why `TitleMatch` instead of separate `FuzzyTitleMatch`?** The old distinction between "TitleMatch" (movie, exact-ish) and "FuzzyTitleMatch" (TV, Levenshtein) was artificial — both use Levenshtein similarity with a threshold. The confidence score already encodes how fuzzy the match was. One label, one meaning.

**Alternative considered:** Keep separate FuzzyTitleMatch / ExactTitleMatch. Rejected because there's no exact match path — even 1.0 similarity goes through Levenshtein.

### D2: Remove ResolutionConfig entirely

`ResolutionConfig(Strategy="fuzzy", Threshold=0.7f, AirdateTolerance=7)` is:
- Never configured by callers (always `new ResolutionConfig()`)
- Has a `Strategy` field that controls "none"/"strict"/"fuzzy" but "fuzzy" is the only real mode
- Mixes input-mode with threshold constants

The resolvers (`EpisodeResolver`, `MovieResolver`) absorb the defaults directly:
- `EpisodeResolver.Resolve(TvdbEpisode[], EpisodeCandidate[])` — threshold=0.7f, airdateTolerance=7 are internal constants
- The "none" strategy check moves: `TvdbResolverActor` checks a simpler condition or the caller simply doesn't send `EnrichEpisodes` when enrichment isn't needed

If per-RuleSet tuning is needed later, it gets a clean design as part of that feature.

### D3: IEnrichmentResult marker interface

```csharp
public interface IEnrichmentResult
{
    int Index { get; }
    float Confidence { get; }
    MatchMethod Method { get; }
}
```

Both `EnrichedEpisode` and `EnrichedMovie` implement this. Enables generic handling in UI or diagnostics code that only cares about "did it match and how confident?"

### D4: Consistent naming pattern

| Old | New |
|---|---|
| `ResolveEpisodes` | `EnrichEpisodes` |
| `ResolveMovie` | `EnrichMovies` |
| `IEpisodeResolutionResponse` | `IEpisodeEnrichmentResponse` |
| `IMovieResolutionResponse` | `IMovieEnrichmentResponse` |
| `EpisodesResolved` | `EpisodesEnriched` |
| `EpisodeResolutionFailed` | `EpisodeEnrichmentFailed` |
| `MoviesResolved` | `MoviesEnriched` |
| `MovieResolutionFailed` | `MovieEnrichmentFailed` |
| `ResolvedEpisode` | `EnrichedEpisode` |
| `MovieResolved` | `EnrichedMovie` |
| `ResolutionStrategy` (class) | deleted, replaced by `MatchMethod` enum |
| `ResolutionConfig` | deleted |
| `SearchResultItem.ResolutionConfidence` | `SearchResultItem.MatchConfidence` |
| `SearchResultItem.ResolutionStrategy` | `SearchResultItem.MatchMethod` |

### D5: TvdbResolverActor "none" strategy handling

Currently `TvdbResolverActor` checks `msg.Config.Strategy == ResolutionStrategy.None` to skip resolution. With `ResolutionConfig` removed, the `EnrichEpisodes` message no longer carries a mode. The workers simply don't send the enrichment message when enrichment isn't needed (they already check `needsResolution` before asking). The "none" path in `TvdbResolverActor` is dead code today — no caller sets it.

## Risks / Trade-offs

- **[Broad rename]** Many files change in one commit. → Mitigation: Pure rename with no logic changes, easy to review. All changes are compile-verified.
- **[Removing ResolutionConfig]** If per-RuleSet resolution tuning is needed later, we'll need to re-introduce config. → Mitigation: 0.x version, clean break OK. Future config will be better designed with the enrichment vocabulary.
