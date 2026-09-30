## 1. Pipeline Types

- [x] 1.1 Create SourceInfo record with From(MediathekItem) factory in FunkArr.Search
- [x] 1.2 Create MediaIdentity record in FunkArr.Search
- [x] 1.3 Create MatchInfo record in FunkArr.Search
- [x] 1.4 Create EnrichedItem record in FunkArr.Search
- [x] 1.5 Create ReleaseVariant record with Expand() and ToResultItem() in FunkArr.Search
- [x] 1.6 Update VideoQuality.GetVariants to take SourceInfo instead of MediathekItem

## 2. TV Search Worker State

- [x] 2.1 Rewrite TvSearchWorkerState as sealed class with Init, private setters, BaseIdentity
- [x] 2.2 Implement Apply(MediathekQueryCompleted) — project to SourceInfo[]
- [x] 2.3 Implement Apply(RuleSetResolved) — set RuleSetId and MediaName
- [x] 2.4 Implement Apply(ScoreCompleted) — create EnrichedItem[] with Match=null
- [x] 2.5 Implement Apply(EpisodesEnriched) — patch Identity and Match on items
- [x] 2.6 Implement TryGetMediathekQuery(out QueryMediathek?)
- [x] 2.7 Implement TryGetRuleSetRequest(out ResolveRuleSet?)
- [x] 2.8 Implement TryGetScoringRequest(out ScoreItems?)
- [x] 2.9 Implement TryGetEnrichmentRequest(out EnrichEpisodes?)
- [x] 2.10 Implement ToSearchCompleted() — expand variants, map, sort, wrap

## 3. Movie Search Worker State

- [x] 3.1 Rewrite MovieSearchWorkerState as sealed class with Init, private setters, BaseIdentity
- [x] 3.2 Implement Apply(MediathekQueryCompleted) — project to SourceInfo[]
- [x] 3.3 Implement Apply(RuleSetResolved) — set RuleSetId and MediaName
- [x] 3.4 Implement Apply(ScoreCompleted) — create EnrichedItem[] with Match=null
- [x] 3.5 Implement Apply(MoviesEnriched) — patch Identity and Match on items
- [x] 3.6 Implement TryGetMediathekQuery(out QueryMediathek?)
- [x] 3.7 Implement TryGetRuleSetRequest(out ResolveRuleSet?)
- [x] 3.8 Implement TryGetScoringRequest(out ScoreItems?)
- [x] 3.9 Implement TryGetEnrichmentRequest(out EnrichMovies?)
- [x] 3.10 Implement ToSearchCompleted() — expand variants, map, sort, wrap

## 4. TV Search Worker

- [x] 4.1 Add IWithTimers to TvSearchWorker, define private timeout records (MediathekTimeout, RuleSetTimeout, ScoringTimeout, EnrichmentTimeout)
- [x] 4.2 Rewrite initial Receive<TvSearch> to use state.Init + TryGet + Tell + Timer
- [x] 4.3 Rewrite Querying phase — handle MediathekQueryCompleted/MediathekQueryFailed/MediathekTimeout
- [x] 4.4 Rewrite ResolvingRuleSet phase — handle RuleSetResolved/RuleSetFailed/RuleSetTimeout
- [x] 4.5 Rewrite Scoring phase — handle ScoreCompleted/ScoringFailed/ScoringTimeout, use TryGetEnrichmentRequest
- [x] 4.6 Rewrite Enriching phase — handle EpisodesEnriched/EpisodeEnrichmentFailed/EnrichmentTimeout

## 5. Movie Search Worker

- [x] 5.1 Add IWithTimers to MovieSearchWorker, define private timeout records
- [x] 5.2 Rewrite initial Receive<MovieSearch> to use state.Init + TryGet + Tell + Timer
- [x] 5.3 Rewrite Querying phase — handle MediathekQueryCompleted/MediathekQueryFailed/MediathekTimeout
- [x] 5.4 Rewrite ResolvingRuleSet phase — handle RuleSetResolved/RuleSetFailed/RuleSetTimeout
- [x] 5.5 Rewrite Scoring phase — handle ScoreCompleted/ScoringFailed/ScoringTimeout, use TryGetEnrichmentRequest
- [x] 5.6 Rewrite Enriching phase — handle MoviesEnriched/MovieEnrichmentFailed/EnrichmentTimeout

## 6. Cleanup

- [x] 6.1 Delete SearchPipeline.cs and SearchContext record
- [x] 6.2 Remove ResolveQuality extension method (replaced by SourceInfo URL checks)
- [x] 6.3 Verify SearchResultItem fields match spec (MatchConfidence, MatchMethod)

## 7. Tests

- [x] 7.1 Update TvSearchWorkerTests — use Tell+Timer pattern with TestProbe for ReplyTo, verify timer cancel on response
- [x] 7.2 Update MovieSearchWorkerTests — same Tell+Timer pattern updates
- [x] 7.3 Add TvSearchWorkerState unit tests — Apply methods and TryGet methods
- [x] 7.4 Add MovieSearchWorkerState unit tests — Apply methods and TryGet methods
- [x] 7.5 Add ReleaseVariant.Expand tests — variant count, size estimation, title generation
- [x] 7.6 Add SourceInfo.From tests — timestamp conversion, null URL handling
- [x] 7.7 Verify build and dotnet format pass

## 8. Spec Updates

- [x] 8.1 Sync delta specs to main specs (tv-search, movie-search, search-messages)
- [x] 8.2 Add new main specs (search-pipeline-types, search-worker-state)
