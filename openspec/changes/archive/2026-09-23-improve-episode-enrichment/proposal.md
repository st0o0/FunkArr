# Improve Episode Enrichment Matching

## Problem

FunkArr's episode enrichment produces incorrect TVDB matches for series like Tatort where the Mediathek holds 75+ episodes simultaneously. Three distinct issues compound:

1. **Title matching fails on composite TVDB titles**: TVDB uses `"Ermittler - Nr - Episodentitel"` format for Tatort. Levenshtein similarity between short Mediathek titles (e.g. "Bauernsterben") and the full composite title (e.g. "Lindholm - 33 - König in Gelb") is always below the 0.7 threshold. Title matching never succeeds for Tatort.

2. **AirDate matching produces mass false positives**: When title matching fails, `FindByAirdate` kicks in with a 7-day tolerance. All Mediathek items have recent upload dates clustering around the latest episodes' air dates. Result: 13 different Tatort episodes all get assigned S2026E17 because their Mediathek upload date falls within 7 days of E17's air date.

3. **Newznab API returns unfiltered results**: When Sonarr requests `season=2026&ep=17`, FunkArr returns all 150 results instead of only those matching E17. The season/episode parameters are passed to the search command but never used to filter the response.

4. **Season-restricted enrichment misses reruns**: The enrichment actor filters TVDB episodes to the requested season only. Mediathek reruns from other seasons (e.g. "Das Ende der Nacht" from S2025) can never be matched.

Evidence from testing: 42 releases tagged S2026E17 containing 13 different episode titles. 87 releases with date-based names that Sonarr cannot match at all.

## Scope

Three focused improvements to the enrichment and Newznab layers:

### 1. TVDB title segment matching (episode-resolution)

Split composite TVDB episode titles on ` - ` separators and compare the candidate against each segment. Take the best similarity score across all segments.

- `"König in Gelb"` vs `"Lindholm - 33 - König in Gelb"` → splits to `["Lindholm", "33", "König in Gelb"]` → best match: 1.0 against "König in Gelb"
- `"Bauernsterben"` vs same → best segment match: ~0.2 → correctly rejected

This directly fixes the root cause: title matching works correctly, so AirDate fallback is rarely reached.

### 2. Guard AirDate matching with minimum title affinity (episode-resolution)

AirDate matching should only apply when the candidate has at least weak title affinity to the best-matching TVDB episode. Add a minimum title similarity floor (e.g. 0.3) below which AirDate matching is skipped entirely for that candidate.

- `"König in Gelb"` vs E17 "König in Gelb": title segment score 1.0 ≥ 0.3 → AirDate allowed (not needed, title already matched)
- `"Bauernsterben"` vs E17: title segment score 0.2 < 0.3 → AirDate blocked → no false match

This is a safety net: even if segment matching doesn't push a candidate over the title threshold, AirDate can't produce a completely unrelated match.

### 3. Newznab result filtering by season/episode (newznab-indexer-api)

When Sonarr passes `season` and `ep` parameters, filter the cached results to only return items whose enriched Season/Episode attributes match. Items without season/episode attributes (unmatched) are excluded when a specific episode is requested.

Additionally, remove season/episode from the cache key — the Mediathek query is the same regardless of which episode Sonarr wants. Different episode queries should hit the same cached search results.

### 4. Season fallback in enrichment (episode-resolution)

When enrichment against the requested season produces unmatched candidates, run a second pass against all seasons for those candidates. Fallback matches receive a small confidence penalty (×0.9) to prefer same-season matches when both exist.

## Out of scope

- Bijective (1:1) episode assignment — with correct title matching, duplicate assignment is rare enough to not warrant the complexity
- Changes to the scoring engine or ruleset matching
- Daily series support in Newznab `ep` parameter parsing (Sonarr sends `ep=01/01` for daily — a separate issue)
- Accessibility variant filtering (Audiodeskription/Gebärdensprache already handled by ruleset priority rules)

## Risks

- **Title segment splitting assumes ` - ` separator**: This is consistent across all Tatort TVDB entries (1400+ episodes checked), but other series may use different formats. The segment matching should be additive — compare against full title AND segments, take the best score.
- **AirDate floor threshold tuning**: 0.3 is a starting point. Too high blocks legitimate AirDate matches for series with no title overlap. Too low still allows false matches. The threshold should be configurable via EnrichmentConfig.
- **All-season fallback performance**: Tatort has 1400+ episodes across all seasons. Levenshtein on 1400 short strings is fast (~1ms), but with segment splitting it's ~4200 comparisons per candidate. Still negligible.
