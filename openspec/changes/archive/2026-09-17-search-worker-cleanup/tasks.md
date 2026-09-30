## 1. SearchRequest base record

- [x] 1.1 Add `abstract record SearchRequest(Guid SearchId, string Source, string? Query, int? Limit, int? Offset) : IWithSearchId` to FunkArr.Messages
- [x] 1.2 Make `SearchSeries` inherit from `SearchRequest`, remove duplicated fields
- [x] 1.3 Make `SearchMovie` inherit from `SearchRequest`, remove duplicated fields
- [x] 1.4 Update SearchManager to work with the new message hierarchy (no behavioral change)
- [x] 1.5 Build and verify no compile errors

## 2. TvSearchWorker Ask+PipeTo

- [x] 2.1 Remove `IWithTimers`, `Timers` property, and all 4 private timeout records from TvSearchWorker
- [x] 2.2 Replace Tell+Timer in constructor (SearchSeries handler) with Ask+PipeTo for Mediathek and RuleSet steps
- [x] 2.3 Replace Tell+Timer in Querying state — handle QueryMediathekCompleted and QueryMediathekFailed only
- [x] 2.4 Replace Tell+Timer in ResolvingRuleSet state — handle RuleSetResolved and RuleSetFailed only
- [x] 2.5 Replace Tell+Timer in Scoring state — handle ScoreCompleted and ScoringFailed only
- [x] 2.6 Replace Tell+Timer in Enriching state — handle EnrichEpisodesCompleted and EnrichEpisodesFailed only
- [x] 2.7 Update TvSearchWorkerTests for Ask+PipeTo pattern (ExpectMsg on Ask instead of Tell, no timer assertions)

## 3. MovieSearchWorker Ask+PipeTo

- [x] 3.1 Remove `IWithTimers`, `Timers` property, and all 4 private timeout records from MovieSearchWorker
- [x] 3.2 Replace Tell+Timer in constructor (SearchMovie handler) with Ask+PipeTo for Mediathek and RuleSet steps
- [x] 3.3 Replace Tell+Timer in Querying state — handle QueryMediathekCompleted and QueryMediathekFailed only
- [x] 3.4 Replace Tell+Timer in ResolvingRuleSet state — handle RuleSetResolved and RuleSetFailed only
- [x] 3.5 Replace Tell+Timer in Scoring state — handle ScoreCompleted and ScoringFailed only
- [x] 3.6 Replace Tell+Timer in Enriching state — handle EnrichMoviesCompleted and EnrichMoviesFailed only
- [x] 3.7 Update MovieSearchWorkerTests for Ask+PipeTo pattern

## 4. Enrichers to static

- [x] 4.1 Convert EpisodeEnricher to static class with static Resolve method
- [x] 4.2 Update EpisodeEnricherTests — remove `new EpisodeEnricher()`, call `EpisodeEnricher.Resolve(...)` directly
- [x] 4.3 Convert MovieEnricher to static class with static Resolve method
- [x] 4.4 Update MovieEnricherTests — remove `new MovieEnricher()`, call `MovieEnricher.Resolve(...)` directly

## 5. Verification

- [x] 5.1 Run `dotnet build src/FunkArr.slnx`
- [x] 5.2 Run `dotnet format src/FunkArr.slnx`
- [x] 5.3 Run all Search and Enrichment test projects
