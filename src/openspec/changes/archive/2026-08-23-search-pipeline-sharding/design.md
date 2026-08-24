## Context

The current search architecture funnels all requests through a single `SearchCoordinator` actor that owns caching, coalescing, pipeline orchestration, and metrics. This makes it a bottleneck and a god-actor. The `ShowResolverWorker` mixes TVDB and TMDB concerns. The `QualityProbeWorker` makes HTTP calls (HEAD/Range) to determine video quality, but MediathekViewWeb URLs already encode resolution, codec, and bitrate in their filenames (e.g., `avc-1080.mp4`, `1920x1080-50p-5000kbit.mp4`).

FunkArr already uses Cluster Sharding for `DownloadCoordinator` and `DownloadRequestTracker`. The search pipeline is a natural fit for the same pattern: each unique search topic/query becomes a shard entity with its own mailbox, enabling true parallel processing.

## Goals / Non-Goals

**Goals:**
- Parallel search processing — multiple TV/movie/text searches execute simultaneously
- Clean separation of concerns — routing, resolution, searching, matching as distinct actors
- Eliminate redundant HTTP probing — use URL pattern analysis instead
- RSS feed refresh in seconds instead of ~70s
- Consistent architecture with existing shard patterns (DownloadCoordinator)

**Non-Goals:**
- Multi-node distribution (single-node cluster sharding is sufficient)
- Persisting search result caches (ephemeral, rebuild on restart)
- Changing the external Newznab/SABnzbd API contracts
- Changing the MediathekViewWeb query format

## Decisions

### 1. Three separate ShardRegions for three search types

Each search type has a fundamentally different pipeline (TV needs TVDB resolution + rules, Movie needs TMDB resolution, Text needs neither). Separate ShardRegions make this explicit with type-safe messages and independent entity lifecycles.

**Entity keys:**
- `TvSearchPipeline`: `tvdbId` (integer) — the entity becomes the "show owner" that caches episodes, rules, and results. Different season/episode requests for the same show land at the same entity and get filtered per-caller.
- `MovieSearchPipeline`: `imdbId` when available, otherwise `query:{searchTerm}` — coalesces lookups for the same movie.
- `TextSearchPipeline`: `query` (string) — simple cache per search term. RSS feed uses this region.

**Why not one ShardRegion with a type prefix:** Different state shapes, different persistence tiers, different passivation strategies. A union entity would need switch statements for every operation.

**Alternative considered:** Router pools instead of sharding. Sharding wins because it provides automatic entity lifecycle (passivation), natural per-key coalescing, and consistency with existing patterns.

### 2. Singletons for external API access (rate-limiting boundary)

Actors that talk to external HTTP APIs become singletons registered via `ActorRegistry`:

- **MediathekGateway** — sole access point to MediathekViewWeb. All search entities ask this singleton. Rate-limiting and request coalescing happen here.
- **SeriesResolver** — sole access point to TVDB API. Persistent actor (T2) with 24h cache and request coalescing for concurrent lookups of the same tvdbId.
- **MovieResolver** — sole access point to TMDB API. Persistent actor (T2) with 24h cache and request coalescing for concurrent lookups of the same imdbId.

**Why promote MediathekGateway from child to singleton:** As a child of SearchCoordinator, it was implicitly shared. As a singleton, the sharing is explicit and all search entities can access it via `ActorRegistry`.

**Why split ShowResolver:** TVDB and TMDB are independent APIs with independent rate limits, independent caches, and independent persistence streams. Mixing them forced a combined snapshot format and made recovery handle both event types.

### 3. Eliminate QualityProbe — inline UrlPatternAnalyzer

MediathekViewWeb URLs contain quality information in their filenames:
- ARD: `.avc-1080.mp4`, `.avc-720.mp4`, `.avc-360.mp4`
- DasErste: `1920x1080-50p-5000kbit.mp4`
- ZDF: `2000k_p15v14` (profile-based)
- Arte: `_1080p_` patterns

`UrlPatternAnalyzer` already exists and extracts resolution, codec, and bitrate from these patterns without any HTTP calls. The HTTP probing (HEAD for Content-Length, Range for MP4 atom parsing) provides marginal value:
- Real file size vs. estimated: irrelevant for Sonarr/Radarr quality cutoff decisions
- MP4 container parsing: redundant when URL pattern already gives resolution+codec

Probing is replaced by: `UrlPatternAnalyzer.Analyze(url)` + `EstimateSize(duration, bitrate)`. Both are pure CPU functions (<1ms).

### 4. Inline Match and Score — no actor needed

`MatchWorker` calls `MatchingPipeline.Execute()` (a static method) and `ScoreWorker` calls `ScoreResult()` (a static method). Both are CPU-bound computations that complete in <1ms. They were separate actors only because the old SearchCoordinator needed PipeTo to keep the mailbox open for coalescing. With sharded entities, each entity has its own mailbox — coalescing is per-entity — so these can be direct function calls.

### 5. SearchRouter as thin stateless singleton

`SearchRouter` replaces `SearchCoordinator`. It:
- Receives `TvSearchRequest`, `MovieSearchRequest`, `TextSearchRequest`
- Extracts the entity key
- Forwards to the appropriate ShardRegion
- Has no state, no cache, no pipeline logic

### 6. RssFeedCoordinator scatter-gather

Instead of asking `SearchCoordinator` sequentially for 60 topics with 1s delays, `RssFeedCoordinator` sends all topic queries to the `TextSearchPipeline` ShardRegion in parallel. Each query routes to its own entity. Results are collected with a timeout and aggregated.

### 7. Persistence migration — clean break

`ShowResolverWorker` used PersistenceId `show-resolver` with mixed events. New actors use:
- `SeriesResolver`: PersistenceId `series-resolver`
- `MovieResolver`: PersistenceId `movie-resolver`

The old journal is ignored. Caches rebuild within 24h from API calls. This is acceptable at version 0.x.

## Risks / Trade-offs

- **[More ShardRegions]** 3 new regions + 2 existing = 5 total. → Acceptable for single-node. ShardRegion overhead is minimal.
- **[Loss of HTTP probing accuracy]** Estimated file sizes instead of real Content-Length. → Sonarr/Radarr don't use file size for quality decisions. Bitrate-based estimates are sufficient.
- **[HLS streams]** If MediathekViewWeb ever returns HLS URLs without MP4 alternatives, UrlPatternAnalyzer cannot determine quality. → Not observed in current data. Can be added back as a fallback if needed.
- **[Entity key collisions]** Two different shows with the same tvdbId are impossible (tvdbId is unique). Two text searches for "Tatort" are the same entity (correct behavior). → No risk.
- **[MediathekGateway as bottleneck]** All entities ask the same singleton. → The gateway already handles coalescing and caching. The bottleneck is MediathekViewWeb's response time, not the actor mailbox.
- **[Persistence migration]** Old `show-resolver` journal becomes orphaned. → No cleanup needed, SQLite/Postgres handles this. Cache rebuilds organically.
