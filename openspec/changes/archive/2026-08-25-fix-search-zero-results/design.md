## Context

Specialized Newznab searches (`t=movie`, `t=tvsearch`) use a two-phase protocol through `SearchRequestActor`: resolve search hints from `MovieActor`/`ShowActor`, fetch from Mediathek, then delegate matching back to the actor. When no rules exist in either actor, `HandleMatch` returns `MatchedResults([])` — discarding successfully fetched items. Text search (`t=search`) works because it bypasses rule matching entirely.

Additionally, bare movie search (`t=movie` without `q` or `imdbid`) fails because `MovieActor.ResolveSearch` has no title to build a `SearchHint` from, resulting in an empty Mediathek query.

## Goals / Non-Goals

**Goals:**
- `t=movie` and `t=tvsearch` return results even when no rules exist
- Bare `t=movie` returns recent movies using Mediathek's topic categorization
- Bare `t=tvsearch` returns recent content (parity with bare `t=search`)
- Radarr and Sonarr can add FunkArr as an indexer without pre-existing rules

**Non-Goals:**
- Removing or replacing the rule matching infrastructure
- Changing behavior when rules exist (rules still take priority)
- Adding new persistence events or changing the event schema

## Decisions

### D1: Fallback in HandleMatch instead of in SearchRequestActor

The fallback logic lives in `MovieActor.HandleMatch` and `ShowActor.HandleMatch`, not in `SearchRequestActor`. This keeps the actors as the single authority on how items are matched — `SearchRequestActor` remains a thin orchestrator.

**Alternative considered:** Moving fallback to `SearchRequestActor` after receiving empty `MatchedResults`. Rejected because it would require `SearchRequestActor` to know about `ContentFilter` + `QualityExpander` for matched items (duplicating the text search path) and would blur the responsibility boundary between orchestrator and domain actors.

### D2: Fallback returns items as MatchedItemInfo without episode metadata

When no rules match, the fallback creates `MatchedItemInfo(item, episode: null)` for each item (after ContentFilter). This means results lack episode info (season/episode numbers) but still contain the video URLs, titles, and quality variants. The caller (SearchRequestActor) already handles `MatchedItemInfo` with null episode info — `QualityExpander.ExpandMany` produces results without S/E numbers in this case.

**Alternative considered:** Attempting fuzzy title-based episode extraction in the fallback. Rejected — that's what rule auto-generation already does, and adding a second heuristic path creates maintenance burden without clear benefit.

### D3: Bare movie search via ByTopic("Filme") in SearchRequestActor

For `t=movie` without `q` or `imdbid`, `SearchRequestActor` short-circuits before asking `MovieActor.ResolveSearch`. It builds `MediathekSearchQuery.ByTopic("Filme").ExcludeFuture().Limit(100)` and processes results directly (ContentFilter → QualityExpander → ResultScorer), identical to the text search bare path.

This works because German public broadcasters consistently categorize movies under topics containing "Filme" ("Filme", "Filme in der ARD", "Kino - Filme", "ZDF-tivi Filme"). The MediathekViewWeb API matches "Filme" as a substring in the topic field.

**Alternative considered:** Routing through `MovieActor` with a synthetic entity key. Rejected — there is no meaningful movie identity to resolve for a bare browse query, and routing through the actor would hit the same null-title problem.

### D4: Bare TV search via Latest in SearchRequestActor

For `t=tvsearch` without `q` or `tvdbid`, `SearchRequestActor` short-circuits with `MediathekSearchQuery.Latest().ExcludeFuture().Limit(100)` — same as the existing bare text search path. There is no single topic that reliably identifies "all TV content" in the Mediathek (unlike "Filme" for movies), so returning latest entries is the practical choice.

### D5: Fallback position relative to auto-generation

The fallback activates only after auto-generation has been attempted and still produced no rules (or generated rules that matched nothing). The flow is:

1. Try auto-generation if `rules.Count == 0` and items are non-empty (existing behavior)
2. If rules now exist → evaluate with rules (existing behavior)
3. If still no rules → **new: return items through ContentFilter as fallback**

This preserves the auto-generation trigger — first search for a show/movie still attempts to create rules for future searches.

## Risks / Trade-offs

- **Lower precision in fallback path**: Without rules, results may include unrelated items that happen to match the search term. Mitigated by ContentFilter (accessibility filtering) and by the fact that the Mediathek query already narrows results by topic/search term.
- **Bare movie search topic dependency**: If broadcasters change their topic naming away from "Filme", bare movie search would break. Low risk — this naming has been stable for years and is used by MediathekViewWeb itself.
- **No episode info in fallback**: Sonarr won't see S01E03 labels on fallback results, making automatic episode matching less reliable. Mitigated by auto-generation kicking in on first search — subsequent searches will have rules and full episode info.
