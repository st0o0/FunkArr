## Context

FunkArr.EpisodeGuide was built as a dedicated domain for resolving Mediathek items to TVDB episodes. It works — TvSearchWorker asks the EpisodeGuideManager singleton after scoring, gets resolved season/episode numbers, and builds release titles Sonarr can parse. But the name is wrong (it's a resolver, not a guide), it only handles TV, and the cache uses a flat 12h TTL regardless of content type.

Radarr sends movie searches (`t=movie`) with IMDB/TMDB IDs. The MovieSearchWorker currently returns results without any external metadata validation — the release title is built from the Mediathek title as-is. TMDB enrichment would validate the correct movie title, year, and alternative titles for better Radarr matching.

## Goals / Non-Goals

**Goals:**
- Rename EpisodeGuide → MetadataResolver throughout the codebase
- Add TMDB v3 API client for movie lookups
- Movie resolution in MovieSearchWorker (same pattern as TV)
- Tiered caching with content-aware TTLs
- Cache stats for health check integration

**Non-Goals:**
- TMDB as fallback for TV shows (TVDB stays primary for TV)
- Cache persistence to SQLite
- Automatic RuleSet generation from external metadata
- UmlautAdaptarr-style German title normalization API

## Decisions

### Rename over new project

Rename FunkArr.EpisodeGuide → FunkArr.MetadataResolver in place. No deprecation layer, no compatibility shims. The project is v0.x with no external consumers — clean breaks are fine.

**Alternative:** Create a new project and deprecate. Rejected — two projects for the same thing creates confusion, and we'd still need to delete the old one.

### Router Pool pattern (like MatchMagicManager)

MetadataResolver (Singleton) owns the cache and dispatches to provider-specific worker pools. This avoids the serial bottleneck of a single ReceiveAsync actor — TVDB and TMDB API calls run in parallel across pool workers without blocking each other or the cache.

```
MetadataResolver (Singleton)
├── Cache (Dictionary<CacheKey, CacheEntry>)
├── _tvdbPool: SmallestMailboxPool(2) → TvdbResolverActor
│   └── Fetches TVDB data, runs EpisodeResolver, replies to Sender
│       + sends CacheUpdate to Parent
└── _tmdbPool: SmallestMailboxPool(2) → TmdbResolverActor
    └── Fetches TMDB data, runs MovieResolver, replies to Sender
        + sends CacheUpdate to Parent
```

Flow:
1. ResolveEpisodes/ResolveMovie arrives at MetadataResolver
2. Cache hit → resolve locally in the parent, reply directly (no pool needed)
3. Cache miss → forward to the appropriate pool with original Sender
4. Pool worker: API call → resolve → Tell(Sender, result) + Tell(Parent, CacheUpdate)
5. Parent receives CacheUpdate, stores in cache for next lookup

This matches the proven MatchMagicManager pattern where the Manager holds state (configs/cache) and the pool workers do the CPU/IO-heavy work.

**Alternative considered:** Single actor with ReceiveAsync. Rejected — a TVDB call taking 2s blocks all other messages in the mailbox. With 3 parallel Sonarr searches + 1 Radarr search, total latency would be 4-8s serial instead of <2s parallel.

**Alternative considered:** Child actors per provider. Rejected — Router Pool is simpler (Akka manages the pool lifecycle), and we already have the pattern in MatchMagicManager.

### TMDB v3 for movies

TMDB has comprehensive movie data with simple query-parameter auth (`?api_key=...`). Endpoints needed:
- `GET /3/movie/{id}` — details by TMDB ID (title, original_title, release_date, runtime)
- `GET /3/find/{imdb_id}?external_source=imdb_id` — find by IMDB ID
- `GET /3/movie/{id}/alternative_titles` — alternative titles for fuzzy matching

TVDB's movie data is limited compared to TMDB. No cross-provider fallback initially — TVDB handles TV, TMDB handles movies.

### Tiered cache TTLs

Different content types have different staleness profiles:

| Content Type | TTL | Rationale |
|-------------|-----|-----------|
| Active TV show (has future episodes) | 2 days | New episodes get added frequently |
| Inactive TV show (no upcoming) | 7 days | Data rarely changes |
| Movie | 30 days | Movie metadata almost never changes |
| Default fallback | 12 hours | Safe default for unknown content |

Activeness is determined from TVDB episode data: if any episode has an `aired` date in the future, the show is active. Cache lives in the MetadataResolver parent actor state (Dictionary, not ConcurrentDictionary — single-threaded actor access). Pool workers send CacheUpdate messages back to the parent after fetching. No persistence — cache rebuilds lazily as searches trigger lookups. Memory bounded by unique shows/movies queried (~200 for typical home server, ~50MB estimate).

### Movie resolution strategy

Simpler than TV — no season/episode to find:
1. TMDB ID provided → fetch movie details, validate title similarity with Levenshtein
2. IMDB ID provided → find via IMDB external ID, get TMDB details
3. Neither → match Mediathek title against TMDB search results
4. Validate year from TMDB release_date against Mediathek timestamp year
5. Result: enriched title + year + validated IDs for the release title

Confidence scoring same as TV: Levenshtein similarity 0-1, threshold from RuleSet resolution config.

### Message namespace rename

`FunkArr.Messages.EpisodeGuide` → `FunkArr.Messages.MetadataResolver`. Message TYPE names stay unchanged (ResolveEpisodes, EpisodeCandidate, ResolvedEpisode) — they describe the operation, not the service. New movie messages follow the same pattern: `ResolveMovie`, `MovieCandidate`, `MovieResolved`.

### Cache stats via message

`QueryCacheStats` → `CacheStatsResult` exposes: TVDB entry count, TMDB entry count, total estimated memory, oldest entry timestamp. Consumed by the Setup health check endpoint (future) and potentially the UI dashboard.

## Risks / Trade-offs

**TMDB rate limiting** — free tier allows 40 requests per 10 seconds. Mitigated by 30-day cache TTL for movies. Worst case: first-time bulk search for many movies could hit limits. The TmdbClient should handle 429 responses with retry-after.

**Rename merge conflicts** — atomic rename in a single commit minimizes conflict window. No in-flight branches reference EpisodeGuide directly.

**Cache memory growth** — bounded by unique shows/movies queried. 200 entries × ~250KB average = ~50MB. Acceptable for a home server. No eviction needed beyond TTL expiry.

**Actor naming deviation** — MetadataResolver breaks the *Manager = Singleton convention. "Resolver" is a clearer name for what it does. Documented in CLAUDE.md architecture notes as an accepted deviation.
