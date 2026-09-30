## 1. Rename EpisodeGuide → MetadataResolver

- [x] 1.1 Rename project directory `FunkArr.EpisodeGuide` → `FunkArr.MetadataResolver`, update .csproj filename
- [x] 1.2 Rename test project directory `FunkArr.EpisodeGuide.Tests` → `FunkArr.MetadataResolver.Tests`, update .csproj filename
- [x] 1.3 Update FunkArr.slnx — remove old project references, add new ones
- [x] 1.4 Update FunkArr.csproj (Host) — ProjectReference to FunkArr.MetadataResolver
- [x] 1.5 Rename namespace `FunkArr.EpisodeGuide` → `FunkArr.MetadataResolver` in all .cs files (EpisodeGuideManager, EpisodeResolver, LevenshteinDistance, TvdbClient)
- [x] 1.6 Rename namespace `FunkArr.Messages.EpisodeGuide` → `FunkArr.Messages.MetadataResolver` in all message files
- [x] 1.7 Rename `EpisodeGuideManager` class → `MetadataResolverManager` in actor file and tests
- [x] 1.8 Rename `IEpisodeGuideManager` → `IMetadataResolver` in FunkArr.Core.ActorKeys
- [x] 1.9 Rename `EpisodeGuideOptions` → `MetadataResolverOptions` in FunkArr.Core and appsettings.json
- [x] 1.10 Update AkkaSetupContainer — actor registration to use IMetadataResolver and "metadata-resolver" name
- [x] 1.11 Update ServiceSetupContainer — options binding to "MetadataResolver" section
- [x] 1.12 Update TvSearchWorker — `Context.GetActor<IMetadataResolver>()`, using namespace
- [x] 1.13 Update all test files — namespaces and usings
- [x] 1.14 Build + format + run all tests to verify rename is clean

## 2. Tiered Caching

- [x] 2.1 Create CacheEntry record in FunkArr.MetadataResolver with data, fetchedAt, ttl, provider, id fields
- [x] 2.2 Replace simple ConcurrentDictionary in MetadataResolver actor with typed cache supporting per-entry TTL
- [x] 2.3 Implement active show detection — check if any TVDB episode has aired date in the future → 2 day TTL, else 7 day TTL
- [x] 2.4 Add movie cache entries with 30 day TTL
- [x] 2.5 Add QueryCacheStats message and CacheStatsResult response (entry counts per provider, oldest entry, estimated memory)
- [x] 2.6 Handle QueryCacheStats in MetadataResolver actor
- [x] 2.7 Write tests for tiered caching (TTL selection, expiry, stats)

## 3. TMDB v3 API Client

- [x] 3.1 Add TmdbOptions (ApiKey string) to FunkArr.Core
- [x] 3.2 Create TmdbClient in FunkArr.MetadataResolver — HttpClient with base URL `https://api.themoviedb.org/3/`
- [x] 3.3 Implement GetMovieAsync(int tmdbId) — GET /movie/{id}?api_key=..., returns TmdbMovie (title, original_title, release_date, runtime, imdb_id)
- [x] 3.4 Implement FindByImdbIdAsync(string imdbId) — GET /find/{imdb_id}?api_key=...&external_source=imdb_id, returns TmdbMovie
- [x] 3.5 Implement GetAlternativeTitlesAsync(int tmdbId) — GET /movie/{id}/alternative_titles?api_key=..., returns string[]
- [x] 3.6 Add IsConfigured property (checks ApiKey non-empty)
- [x] 3.7 Add TmdbMovie response record (Id, Title, OriginalTitle, ReleaseDate, Runtime, ImdbId)
- [x] 3.8 Register TmdbClient + TmdbOptions in ServiceSetupContainer
- [x] 3.9 Write tests for TMDB client (IsConfigured, response parsing)

## 4. Movie Resolution Messages

- [x] 4.1 Create MovieCandidate record (Index, Title, AiredAt?, Duration)
- [x] 4.2 Create MovieResolved record (Index, Title, Year, ImdbId?, TmdbId?, Confidence, Strategy)
- [x] 4.3 Create ResolveMovie request (ImdbId string?, TmdbId int?, MovieCandidate[])
- [x] 4.4 Create IMovieResolutionResponse, MoviesResolved(MovieResolved[]), MovieResolutionFailed(Reason)

## 5. Movie Resolution Logic

- [x] 5.1 Create MovieResolver static class in FunkArr.MetadataResolver
- [x] 5.2 Implement TMDB ID lookup strategy — fetch movie by ID, validate title similarity against candidates
- [x] 5.3 Implement IMDB ID lookup strategy — find by IMDB ID, get TMDB details, validate
- [x] 5.4 Implement title fuzzy match — compare candidate title against TMDB title + original_title + alternative_titles using LevenshteinDistance
- [x] 5.5 Implement year validation — check candidate AiredAt year against TMDB release_date year
- [x] 5.6 Write tests for movie resolution (all strategies, year validation, no match)

## 6. MetadataResolver Actor — Router Pool Architecture

- [x] 6.1 Create TvdbResolverActor (pool worker) — receives FetchAndResolveEpisodes, calls TvdbClient, runs EpisodeResolver, replies to Sender, sends CacheUpdate to Parent
- [x] 6.2 Create TmdbResolverActor (pool worker) — receives FetchAndResolveMovie, calls TmdbClient, runs MovieResolver, replies to Sender, sends CacheUpdate to Parent
- [x] 6.3 Create internal messages: FetchAndResolveEpisodes, FetchAndResolveMovie, CacheUpdate (sealed records in MetadataResolver scope)
- [x] 6.4 Refactor MetadataResolver constructor — create _tvdbPool (SmallestMailboxPool size 2) and _tmdbPool (SmallestMailboxPool size 2)
- [x] 6.5 Refactor ResolveEpisodes handler — cache hit: resolve locally + reply; cache miss: forward to _tvdbPool with Sender
- [x] 6.6 Add ResolveMovie handler — cache hit: resolve locally + reply; cache miss: forward to _tmdbPool with Sender
- [x] 6.7 Add CacheUpdate handler — store fetched data in cache with content-aware TTL
- [x] 6.8 Add QueryCacheStats handler — respond with entry counts per provider
- [x] 6.9 Handle unconfigured API keys — respond with failure directly (no pool involvement)
- [x] 6.10 Write actor tests (cache hit local resolve, cache miss pool forward, cache update from worker, stats, unconfigured keys)

## 7. MovieSearchWorker Pipeline Extension

- [x] 7.1 Add IMetadataResolver actor ref to MovieSearchWorker
- [x] 7.2 After ScoreCompleted: construct MovieCandidate[] from scored items
- [x] 7.3 Ask MetadataResolver with ResolveMovie, handle MoviesResolved/Failed
- [x] 7.4 Merge resolved title/year into MetadataSpec for release title building
- [x] 7.5 Graceful fallback on resolution failure (use current behavior)
- [x] 7.6 Pass ResolutionConfidence/Strategy through to SearchResultItem
- [x] 7.7 Write tests for MovieSearchWorker resolution stage

## 8. Configuration & Docker

- [x] 8.1 Add Tmdb section to appsettings.json (ApiKey placeholder)
- [x] 8.2 Add FunkArr__Tmdb__ApiKey to docker-compose.dev.yml
- [x] 8.3 Rename EpisodeGuide section → MetadataResolver in appsettings.json
- [x] 8.4 Final build + format + all tests green
