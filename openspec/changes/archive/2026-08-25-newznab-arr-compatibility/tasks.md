## 1. Group A — Controller/Serializer (no actor changes)

- [x] 1.1 Add `<limits max="100" default="100" />` element to `NewznabSerializer.SerializeCaps()` after the `<server>` element
- [x] 1.2 Change `SerializeFeed` signature to accept `int total` and `int offset` parameters; update `NewznabResponse` to use these values
- [x] 1.3 Apply `Skip(offset).Take(limit)` pagination in `HandleTvSearch`, passing full result count and offset to `SerializeFeed`
- [x] 1.4 Apply `Skip(offset).Take(limit)` pagination in `HandleMovieSearch` (add `limit`/`offset` parameters), passing full result count and offset to `SerializeFeed`
- [x] 1.5 Update `HandleTextSearch` to pass correct total and offset to `SerializeFeed`

## 2. Group B — SearchResult enrichment

- [x] 2.1 Add `string? ImdbId`, `int? Year`, and `int? ResolvedTvdbId` properties to `SearchResult`
- [x] 2.2 Thread `ImdbId` and `Year` through the movie search pipeline in `SearchRequestActor` — populate from `MatchContext.ImdbId` and `MovieInfoResponse.ReleaseYear` during result assembly
- [x] 2.3 Thread `ResolvedTvdbId` through the TV search pipeline in `SearchRequestActor` — populate from the request's TvdbId or ShowActor entity key during result assembly
- [x] 2.4 Update `NewznabResultMapper.ToRssItem` to emit `imdbid` attribute from `SearchResult.ImdbId` when present
- [x] 2.5 Update `NewznabResultMapper.ToRssItem` to emit `year` attribute from `SearchResult.Year` when present
- [x] 2.6 Update `NewznabResultMapper.ToRssItem` to emit `tvdbid` attribute from `SearchResult.ResolvedTvdbId`, falling back to the controller-provided tvdbId parameter

## 3. Tests and snapshots

- [x] 3.1 Update caps verified snapshot to include `<limits>` element
- [x] 3.2 Update TV search verified snapshot (no change needed — snapshot builds items directly) to reflect tvdbid from SearchResult
- [x] 3.3 Add movie search contract test with imdbid and year attributes in snapshot
- [x] 3.4 Update mapper unit tests for new imdbid, year, and tvdbid attribute emission
- [x] 3.5 Run full test suite and fix any regressions
