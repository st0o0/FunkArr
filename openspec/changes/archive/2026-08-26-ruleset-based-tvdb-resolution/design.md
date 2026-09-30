## Context

TvSearchActor runs two parallel resolution stages: SeriesResolver (TVDB API) and RuleSetActor (local rulesets). Both must complete before the Mediathek fetch proceeds. The search term starts as `request.ShowName ?? request.Query ?? ""` (line 74) and gets overwritten by SeriesResolver if it returns a name (line 105-108). RuleSetActor returns only rules, not the show name — even though it has the name in `MediaReference.Name` from the matched ruleset.

When SeriesResolver fails (TVDB v2 returns 401), the search term stays empty, the Mediathek returns generic results, and no matches occur. The RuleSet knew the answer all along but wasn't asked.

Current message flow:
```
HandleSearch
  searchTerm = request.ShowName ?? request.Query ?? ""    ← often ""
  ├── SeriesResolver.Ask(ResolveTvShow)  → TvShowResolved(null, null)   ← fails silently
  └── RuleSetActor.Ask(GetRulesForTopic) → RulesResponse(rules)        ← has name, doesn't return it
  
  TryAdvanceAfterResolution
    MediathekGateway.Ask(FetchItems(""))  ← empty search = generic junk
```

## Goals / Non-Goals

**Goals:**
- TV search by TVDB ID works without any external API key
- RuleSet-provided show name is the primary search term source
- SeriesResolver continues to provide episode metadata when available
- No new configuration required — works out of the box with community rulesets

**Non-Goals:**
- Migrating TvdbClient from v2 to v4 API (separate future change)
- Adding TMDB as a fallback resolver
- Changing the Newznab API surface
- Modifying persistence DTOs or event schemas

## Decisions

### Decision 1: Extend RulesResponse with ResolvedShowName

Add `string? ResolvedShowName` to `RuleSetActor.RulesResponse`. The `HandleGetRulesForTopic` handler populates it from `MediaReference.Name` when a match is found by topic, alias, or TVDB ID.

**Why not a separate message?** The name and rules come from the same lookup in the same actor. A separate Ask would double the message overhead and add coordination complexity in TvSearchActor.

**Why nullable?** When no ruleset matches (auto-generation case), there's no name to return.

### Decision 2: RuleSet name takes priority in TvSearchActor

In `OnRulesResolved`, if `result.ResolvedShowName` is not null and the current `state.SearchTerm` is empty or generic, overwrite it with the RuleSet name. This mirrors the existing pattern in `OnTvShowResolved` (line 105-108).

Resolution priority:
1. RuleSet `ResolvedShowName` (local, always available for known shows)
2. SeriesResolver `ShowName` (TVDB API, may override RuleSet name if available)
3. Request `ShowName`/`Query` (caller-provided fallback)

SeriesResolver still wins if it returns a name — it may have a more accurate/current name. But the pipeline no longer breaks when it fails.

### Decision 3: Graceful degradation in SeriesResolver

Replace the silent `catch { return null; }` in TvdbClient with explicit error logging. In TvSearchActor, handle `ShowResolveFailed` by logging a warning and setting `state.ShowResolved = true` with null name — letting the pipeline continue with the RuleSet name.

**Alternative considered:** Skip SeriesResolver entirely when no TVDB API key is configured. Rejected because it would require configuration awareness in the actor and lose episode metadata when the API is available.

## Risks / Trade-offs

**[Risk] Shows without rulesets return no results on first search** → Mitigation: Auto-generation already triggers for unknown TVDB IDs (`_generationInProgress`). The first search returns empty, the second search uses the generated ruleset. This is existing behavior, unchanged.

**[Risk] RuleSet name may differ from TVDB canonical name** → Mitigation: Minor issue — the Mediathek search is fuzzy enough. The RuleSet name comes from the community dataset which is already curated for Mediathek matching. This is actually preferable to the TVDB name which may include year suffixes like "heute-show (2009)".

**[Risk] RulesResponse shape change is technically breaking for message serialization** → Mitigation: RulesResponse is an in-process Ask response, not persisted or serialized across nodes. Adding a nullable field is safe.
