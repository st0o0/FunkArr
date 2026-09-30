## 1. HandleMatch Fallback in Actors

- [x] 1.1 Add no-rules fallback in `MovieActor.HandleMatch`: when `rules.Count == 0` after auto-generation attempt, apply `ContentFilter.ShouldSkip` to items and return passing items as `MatchedItemInfo(item, episode: null)` instead of `MatchedResults([])`
- [x] 1.2 Add no-rules fallback in `ShowActor.HandleMatch`: same pattern — when `rules.Count == 0` after auto-generation attempt, apply `ContentFilter.ShouldSkip` and return items as `MatchedItemInfo(item, episode: null)`

## 2. Bare Search in SearchRequestActor

- [x] 2.1 Handle bare movie search (`Search.Movie` with null imdbId and null query) in `SearchRequestActor`: short-circuit with `MediathekSearchQuery.ByTopic("Filme").ExcludeFuture().Limit(100)`, process results through ContentFilter → QualityExpander → ResultScorer
- [x] 2.2 Handle bare TV search — already handled by NewznabController (line 76-79) which redirects bare tvsearch to HandleTextSearch → BrowseActor for latest results
- [x] 2.3 Update `NewznabController` — no changes needed: bare tvsearch already routes to text search (line 76-79), bare movie search already sends SearchRequest.Movie(null, null) which is now handled by SearchRequestActor.HandleBareMovieSearch

## 3. Tests

- [x] 3.1 Add test for `MovieActor.HandleMatch` fallback: verify items are returned through ContentFilter when no rules exist
- [x] 3.2 Add test for `ShowActor.HandleMatch` fallback: verify items are returned through ContentFilter when no rules exist
- [x] 3.3 Add test for bare movie search: verify `ByTopic("Filme")` query is built and results are returned
- [x] 3.4 Add test for bare TV search: verify latest query is built and results are returned
- [x] 3.5 Build and run full test suite to verify no regressions
