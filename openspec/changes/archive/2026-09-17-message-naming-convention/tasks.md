## 1. Convention documentation

- [x] 1.1 Add message naming convention section to CLAUDE.md

## 2. Search messages

- [x] 2.1 Rename `TvSearch` → `SearchSeries`, create `SearchSeriesResponse` (Completed/Failed), consolidate into one file
- [x] 2.2 Rename `MovieSearch` → `SearchMovie`, create `SearchMovieResponse` (Completed/Failed), consolidate into one file
- [x] 2.3 Remove `ISearchResponse` interface
- [x] 2.4 Update all Search domain actors and tests for new names
- [x] 2.5 Update `SearchCommand` (gateway) and SearchHandler to use per-command response types

## 3. Scoring messages

- [x] 3.1 Create `ScoreItemsResponse` (Completed/Failed), consolidate into ScoreItems.cs
- [x] 3.2 Create per-query response types: `ScoringDetailResponse`, `ScoringHistoryResponse`, `ScoringStatsResponse`
- [x] 3.3 Remove `IScoringResponse` interface and `ScoringDetailNotFound` type
- [x] 3.4 Rename `RecordScoringResult` → `RecordScoring`
- [x] 3.5 Update MatchMagic domain actors and tests

## 4. RuleSet messages

- [x] 4.1 Create per-query response types: `RuleSetDetailResponse`, `RegisteredRuleSetsResponse`, `RuleSetSummaryResponse`, `RuleSetListWithStatsResponse`
- [x] 4.2 Remove `RuleSetResponse` abstract record and `RuleSetNotFound`
- [x] 4.3 Create `ResolveRuleSetResponse` (Resolved/Failed) for the ResolveRuleSet command
- [x] 4.4 Update RuleSet domain actors and tests

## 5. Mediathek messages

- [x] 5.1 Create `QueryMediathekResponse` (Completed/Failed), consolidate into QueryMediathek.cs
- [x] 5.2 Remove `IMediathekResponse` interface
- [x] 5.3 Update MediathekViewWebManager and Search workers

## 6. MetadataResolver messages

- [x] 6.1 Rename `EpisodesEnriched` → `EnrichEpisodesCompleted`, `EpisodeEnrichmentFailed` → `EnrichEpisodesFailed`, create `EnrichEpisodesResponse`
- [x] 6.2 Rename `MoviesEnriched` → `EnrichMoviesCompleted`, `MovieEnrichmentFailed` → `EnrichMoviesFailed`, create `EnrichMoviesResponse`
- [x] 6.3 Remove old `EpisodeEnrichmentResponse` and `MovieEnrichmentResponse` abstract records
- [x] 6.4 Update MetadataResolver actors and tests

## 7. Download messages

- [x] 7.1 Remove `IDownloadResponse` marker interface (zero consumers)
- [x] 7.2 Review Download command/query naming against convention, rename if needed

## 8. Cross-cutting

- [x] 8.1 Update FunkArr.Api Ask<> calls to per-command/query response types
- [x] 8.2 Update FunkArr.ArrApi SearchHandler and adapters
- [x] 8.3 Verify full build, run all tests, `dotnet format`
