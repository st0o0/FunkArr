## 1. Title Segment Matching

- [x] 1.1 Add segment-split logic to `EpisodeEnricher.FindByTitle` — split TVDB episode names on ` - `, compute Levenshtein similarity against each segment, take max score across full title + segments. Apply same logic to `ConstructedTitle` comparison and tiebreaker computation.
- [x] 1.2 Add tests for segment matching in `FunkArr.Enrichment.Tests` — composite title exact segment match, composite title no segment match, non-composite title unchanged behavior, ConstructedTitle against segments, tiebreaker with segment scores.

## 2. AirDate Title Affinity Guard

- [x] 2.1 Add `MinTitleAffinity` field (float) to `AirdateMatchConfig` record in `FunkArr.Messages/Enrichment/EnrichmentConfig.cs`. Default 0.3.
- [x] 2.2 Update `EpisodeEnricher.FindByAirdate` to accept the pre-computed best title segment score and skip matching when score < `MinTitleAffinity`. Update `ResolveCandidate` to compute the best title segment score before calling `FindByAirdate` and pass it through.
- [x] 2.3 Update ruleset deserialization to handle the new `MinTitleAffinity` field with default fallback. Update any test fixtures that construct `AirdateMatchConfig`.
- [x] 2.4 Add tests for the AirDate guard — blocked when title affinity below floor, allowed when above floor, MinTitleAffinity=0 preserves old behavior.

## 3. Season Fallback Enrichment

- [x] 3.1 Update `TvdbEnrichmentActor.Handle` — after first-pass enrichment with season filter, collect unmatched candidate indices. If any remain and a season filter was applied, run `EpisodeEnricher.Resolve` again with all episodes for only the unmatched candidates. Apply confidence × 0.9 to fallback results.
- [x] 3.2 Add tests for season fallback — rerun matched from different season, same-season matches not re-processed, no fallback when Season=null, confidence penalty applied, all matched in first pass skips fallback.

## 4. Newznab Result Filtering

- [x] 4.1 Update `SearchResultCache.BuildKey` — remove season and episode from the TV search cache key. New format: `tv:{query}:{tvdbid}:{imdbid}`.
- [x] 4.2 Add result filtering in `NewznabSearchService.Search` — after cache retrieval and before pagination, filter by season/episode when request params are present. Filter uses the `Season`/`Episode` fields on `SearchResultItem`. Items without season/episode are excluded when filtering is active.
- [x] 4.3 Update pagination metadata — `total` in the RSS response must reflect the post-filter count, not the cached total.
- [x] 4.4 Add tests for Newznab filtering — filter by season+episode, filter by season only, no filter when params absent, unmatched items excluded, pagination after filtering, cache key shared across episode queries.
