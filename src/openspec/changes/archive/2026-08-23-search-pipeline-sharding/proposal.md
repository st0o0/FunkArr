## Why

The SearchCoordinator is a singleton god-actor that bottlenecks all search requests through a single mailbox. It mixes routing, caching, request coalescing, pipeline orchestration, and metrics in one actor. The ShowResolverWorker combines TVDB and TMDB concerns in a single persistent actor. The QualityProbe makes HTTP calls that are redundant — MediathekViewWeb URLs already encode resolution, codec, and bitrate in their filenames. These design issues prevent parallel search processing and make the search architecture unnecessarily complex.

## What Changes

- **BREAKING**: Replace `SearchCoordinator` singleton with stateless `SearchRouter` that forwards to three ShardRegions
- **BREAKING**: Split `ShowResolverWorker` into `SeriesResolver` (TVDB, singleton) and `MovieResolver` (TMDB, singleton)
- **BREAKING**: Eliminate `QualityProbeWorker` — replace with inline `UrlPatternAnalyzer` + `EstimateSize` (no HTTP probing)
- **BREAKING**: Eliminate `MatchWorker` and `ScoreWorker` actors — inline matching and scoring logic directly into search entities
- Introduce `TvSearchPipeline` ShardRegion (entity per tvdbId) for parallel TV search processing
- Introduce `MovieSearchPipeline` ShardRegion (entity per imdbId/query) for parallel movie search processing
- Introduce `TextSearchPipeline` ShardRegion (entity per query) for parallel text search processing
- Promote `MediathekGatewayWorker` from child actor to singleton for centralized rate-limiting
- Update `RssFeedCoordinator` to use scatter-gather on `TextSearchPipeline` for parallel RSS refresh (~5s instead of ~70s)

## Capabilities

### New Capabilities
- `search-router`: Stateless singleton that extracts entity keys and forwards to the appropriate ShardRegion
- `tv-search-pipeline`: Sharded entity per tvdbId that orchestrates the TV search pipeline (SeriesResolver + Rules + MediathekGateway + inline Match/Score)
- `movie-search-pipeline`: Sharded entity per imdbId that orchestrates the movie search pipeline (MovieResolver + MediathekGateway + inline Match/Score)
- `text-search-pipeline`: Sharded entity per query that orchestrates the text search pipeline (MediathekGateway + inline Match/Score)
- `series-resolver`: Singleton persistent actor for TVDB lookups with 24h cache and request coalescing
- `movie-resolver`: Singleton persistent actor for TMDB lookups with 24h cache and request coalescing

### Modified Capabilities
- `search-coordinator`: Replaced by `search-router` — no longer owns pipeline, cache, or coalescing
- `show-resolver-worker`: Split into `series-resolver` and `movie-resolver` with separate persistence streams
- `quality-probing`: Eliminated as a standalone capability — URL pattern analysis inlined into search entities
- `mediathek-gateway-worker`: Promoted from child actor to singleton for shared rate-limited access
- `newznab-indexer`: RSS feed requests route through `RssFeedCoordinator` → `TextSearchPipeline` scatter-gather
- `rss-feed-cache`: Uses scatter-gather on `TextSearchPipeline` ShardRegion for parallel refresh

## Impact

- **Actors**: SearchCoordinator, ShowResolverWorker, MatchWorker, ScoreWorker, QualityProbeWorker removed. SearchRouter, SeriesResolver, MovieResolver, MediathekGateway promoted to singletons. Three new ShardRegions added.
- **Persistence**: New PersistenceIds `series-resolver` and `movie-resolver` replace `show-resolver`. Clean break — old journal ignored, caches rebuild within 24h.
- **APIs**: No external API changes — Newznab and SABnzbd endpoints unchanged. Internal actor messages change.
- **Configuration**: `FunkArrActorSystemSetup` gains 3 ShardRegion registrations and 4 singleton registrations, loses SearchCoordinator registration.
- **Tests**: SearchCoordinator tests must be rewritten for the new sharded architecture. QualityProbe tests become UrlPatternAnalyzer-only tests.
- **Performance**: Search parallelism (multiple shows searched simultaneously), RSS refresh drops from ~70s to ~5s.
