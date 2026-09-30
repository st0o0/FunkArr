## 1. Extend RulesResponse with show name

- [x] 1.1 Add `string? ResolvedShowName` to `RuleSetActor.RulesResponse` record in `src/FunkArr/RuleSet/RuleSetActor.cs`
- [x] 1.2 Update `HandleGetRulesForTopic` to populate `ResolvedShowName` from `MediaReference.Name` of the matched ruleset (topic, alias, or TVDB ID match)
- [x] 1.3 Pass `null` for `ResolvedShowName` when no ruleset matches (empty rules + auto-generation path)

## 2. Update TvSearchActor to use RuleSet name

- [x] 2.1 In `OnRulesResolved`, read `result.ResolvedShowName` and update `state.SearchTerm` if it's not null and the current term is empty or matches the initial fallback
- [x] 2.2 Fix `HandleShowResolveFailed` to continue pipeline (set `ShowResolved = true`, call `TryAdvanceAfterResolution`) instead of aborting with empty results
- [x] 2.3 Verify `TryAdvanceAfterResolution` proceeds correctly when SeriesResolver fails but RuleSet provided a name

## 3. Improve error logging

- [x] 3.1 In `TvdbClient.GetShowAsync`, log non-success HTTP status codes before returning null (include tvdbId and status code)
- [x] 3.2 In `TvdbClient.GetShowAsync` catch block, log the exception message before returning null
- [x] 3.3 In `TvdbClient.GetEpisodesAsync`, apply the same logging improvements

## 4. Tests

- [x] 4.1 Update existing RuleSetActor tests to verify `ResolvedShowName` in `RulesResponse` for topic, alias, and TVDB ID matches
- [x] 4.2 Add TvSearchActor test: RuleSet provides name, SeriesResolver fails → pipeline completes with RuleSet name as search term
- [x] 4.3 Existing test covers both-resolve case (StubSeriesResolver provides "Tatort", wins over RuleSet)
- [x] 4.4 Add TvSearchActor test: neither provides name → empty search term fallback

## 5. Integration verification

- [x] 5.1 Rebuild dev container, trigger tvsearch for Tatort (tvdbid=83214) — resolution works: RuleSet provides "Tatort" name, Mediathek returns 1000 items. Matching returns 0 (pre-existing RuleSet matching issue, separate from this change)
- [x] 5.2 Prowlarr text search returns Tatort results (12 items via t=search)
- [x] 5.3 FunkArr Matches page shows match activity entries
