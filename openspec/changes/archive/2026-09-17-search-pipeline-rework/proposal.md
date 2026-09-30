## Why

The search pipeline mixes transformation logic, decision-making, and message construction across SearchPipeline (god-class with 3 BuildScoredResult overloads), SearchWorkerState (record with extension methods), and the SearchWorkers themselves (conditional NeedsEnrichment checks, Ask+PipeTo+failure lambda chains). MediathekItem leaks through the entire pipeline, SearchResultItem is a flat bag carrying data from all stages, and enrichment merge logic is buried inside "Build" methods. This makes the pipeline hard to reason about, extend, and test.

## What Changes

- Introduce typed pipeline stage records: SourceInfo (Mediathek projection), MediaIdentity (external IDs), MatchInfo (enrichment result), EnrichedItem (post-scoring), ReleaseVariant (post-expansion)
- Convert TvSearchWorkerState and MovieSearchWorkerState from records with extension methods to classes with Apply methods that own all transformation and decision logic (Pathfinder-style TryGet pattern)
- Replace Ask+PipeTo+failure lambdas in workers with Tell+Timer — workers become pure orchestrators that receive messages, call state, and tell/reply
- Delete SearchPipeline.cs and SearchContext — replaced by state class methods
- VideoQuality.GetVariants operates on SourceInfo instead of MediathekItem
- ReleaseVariant.ToResultItem() provides trivial 1:1 mapping to SearchResultItem (API DTO stays flat)

## Capabilities

### New Capabilities

- `search-pipeline-types`: Pipeline stage types (SourceInfo, MediaIdentity, MatchInfo, EnrichedItem, ReleaseVariant) and variant expansion logic
- `search-worker-state`: State classes with Apply methods for pipeline transformations and TryGet methods for routing decisions

### Modified Capabilities

- `tv-search`: Worker becomes pure orchestrator using Tell+Timer, delegates all logic to state
- `movie-search`: Worker becomes pure orchestrator using Tell+Timer, delegates all logic to state
- `episode-resolution`: Integration into pipeline via state.Apply(EpisodesEnriched) patching EnrichedItem identity+match
- `movie-resolution`: Integration into pipeline via state.Apply(MoviesEnriched) patching EnrichedItem identity+match
- `search-messages`: SearchResultItem field names stabilized (MatchConfidence, MatchMethod)

## Impact

- **FunkArr.Search**: Major refactor — new types, rewritten workers and state classes, deleted SearchPipeline.cs
- **FunkArr.Messages**: Minor — SearchResultItem field names (already in-flight in uncommitted changes)
- **FunkArr.Search.Tests**: TvSearchWorkerTests and MovieSearchWorkerTests need updates for Tell+Timer pattern
- **No API contract changes**: SearchResultItem stays flat, Newznab/SABnzbd adapters unaffected
- **No MetadataResolver changes**: EnrichedEpisode/EnrichedMovie/EnrichEpisodes/EnrichMovies messages stay as-is
