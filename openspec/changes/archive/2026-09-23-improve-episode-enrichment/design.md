## Context

FunkArr's episode enrichment maps Mediathek items to TVDB episodes via title similarity and airdate proximity. For anthology series like Tatort (75+ items in the Mediathek, 22 episodes in the current TVDB season), the title matching fails because TVDB uses composite titles (`"Lindholm - 33 - König in Gelb"`) while the Mediathek uses only the episode name (`"König in Gelb"`). The Levenshtein similarity between a short Mediathek title and the full composite TVDB title never reaches the 0.7 threshold, so AirDate matching takes over — which falsely assigns nearly everything to E17 or E18 because all Mediathek upload dates cluster within 7 days of those episodes' air dates.

Separately, the Newznab API ignores season/episode filter parameters when returning search results, and the enrichment actor restricts TVDB episode lookups to a single season, preventing rerun matching.

## Goals / Non-Goals

**Goals:**
- Correct episode assignment for series with composite TVDB titles (Tatort pattern)
- Prevent AirDate matching from producing unrelated false positives
- Newznab API returns only episodes matching the requested season/episode
- Reruns from older seasons can be matched to their correct TVDB episode

**Non-Goals:**
- Bijective (1:1) assignment — correct matching makes this unnecessary
- Daily series `ep` format parsing (`ep=01/01`) — separate concern
- Changes to the scoring engine or ruleset matching
- Accessibility variant filtering (handled by ruleset priority rules)

## Decisions

### Decision 1: TVDB title segment matching via separator split

Split TVDB episode names on ` - ` (space-dash-space) and compare the candidate against the full title AND each segment. Take the highest similarity score.

**Rationale:** Tatort's TVDB format is consistently `"Ermittler - Nr - Episodentitel"` across 1400+ episodes and multiple seasons. The last segment is the actual episode name that matches the Mediathek title. Comparing against segments gives `"König in Gelb"` vs `"König in Gelb"` = 1.0 instead of 0.47 against the full composite title.

**Alternative considered:** Substring containment check — fragile with very short titles ("Ex-It", "Schmerz") that could substring-match unrelated episodes. Levenshtein against segments is more robust because it penalizes length mismatches.

**Implementation:** In `EpisodeEnricher.FindByTitle`, after computing similarity against `episode.Name`, also split `episode.Name` on ` - ` and compute similarity against each segment. Use the maximum score across full title + all segments. This is additive — series without composite titles are unaffected since the full title comparison is always included.

### Decision 2: AirDate guard via minimum title affinity

Add a configurable `MinTitleAffinity` field (float, default 0.3) to `AirdateMatchConfig`. Before `FindByAirdate` runs for a candidate, compute the candidate's best title segment score against all TVDB episodes. If that score is below `MinTitleAffinity`, skip AirDate matching entirely for that candidate.

**Rationale:** AirDate matching is dangerous when the candidate title has zero relation to any TVDB episode — this means the Mediathek item is likely a rerun from another season or a completely different episode. The 0.3 floor is intentionally low: it allows AirDate matching when there's even a weak title hint (e.g., slight misspelling), but blocks it for completely unrelated titles where similarity is typically 0.1-0.2.

**Alternative considered:** Reducing AirDate tolerance from 7 to 1-2 days — this helps but doesn't address the root cause (Mediathek upload dates are not air dates for reruns). The title affinity guard addresses both reruns and fresh uploads.

**Implementation:** `FindByAirdate` receives a new parameter for the pre-computed best title segment score. If below the floor, it returns null immediately. The score computation reuses the same segment-split logic from Decision 1.

### Decision 3: Newznab result filtering in the API layer

Filter cached search results in `NewznabSearchService.Search` when the request includes season AND episode parameters. Only items whose enriched `Season` and `Episode` attributes match the request are returned. Items without season/episode (unmatched) are excluded.

**Rationale:** Filtering belongs in the Newznab layer, not the search pipeline, because: (a) the cache should hold all results for reuse across different episode queries, and (b) different Sonarr requests for the same series should hit the same cache entry.

**Cache key change:** Remove season/episode from the cache key. The Mediathek query is identical regardless of which episode Sonarr requests. New key format: `tv:{query}:{tvdbid}:{imdbid}`. This means a Sonarr search for E17 and a subsequent search for E18 share one cache entry instead of triggering two identical Mediathek queries.

**When no season/ep is provided** (RSS sync, Prowlarr general search): return all results unfiltered, preserving current behavior.

### Decision 4: Two-pass season fallback in enrichment

When enrichment against the requested season leaves unmatched candidates, run a second enrichment pass for those candidates against all TVDB episodes (no season filter). Fallback matches receive a confidence penalty of ×0.9.

**Rationale:** Mediathek reruns from other seasons (e.g., "Das Ende der Nacht" from S2025) should be matchable. The confidence penalty ensures same-season matches are preferred when both exist. The penalty is small (0.9) because a correct cross-season match is far better than no match.

**Implementation:** In `TvdbEnrichmentActor.Handle`, after the first `EpisodeEnricher.Resolve` call, collect unmatched candidate indices. If any remain and a season filter was applied, run `Resolve` again with all episodes. Apply the 0.9 confidence factor to the second-pass results. This keeps `EpisodeEnricher` stateless — the two-pass logic lives in the actor.

## Risks / Trade-offs

**Separator assumption** — ` - ` split assumes this separator is used in TVDB composite titles. Verified across all Tatort seasons (1400+ episodes). For series without composite titles, the split produces one segment equal to the full title, so the behavior is identical to today. → No mitigation needed; additive by design.

**All-season fallback performance** — Tatort has 1400+ episodes. With segment splitting, worst case is ~4200 Levenshtein comparisons per unmatched candidate. Strings are short (typically 5-30 chars), so this is <1ms. → Acceptable; no optimization needed.

**MinTitleAffinity threshold tuning** — 0.3 is a starting point. Per-ruleset override via `EnrichmentConfig.Airdate.MinTitleAffinity` allows tuning without code changes. → Configurable, can be adjusted per series via ruleset.

**Cache key change** — Removing season/episode from the key means the first search for any episode of a series populates the cache for all episode queries. If the Mediathek returns different results for different episode queries (it doesn't — it returns all topic matches), this would be incorrect. → Verified: Mediathek search is topic-based, returns the same results regardless of requested episode.
