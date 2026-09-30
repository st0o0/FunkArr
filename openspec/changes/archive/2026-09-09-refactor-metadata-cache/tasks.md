## 1. Internal messages and cache entries

- [x] 1.1 Replace EpisodeCacheUpdate/MovieCacheUpdate in InternalMessages.cs with TvdbFetchResult(int TvdbId, TvdbEpisode[] Episodes) and TmdbFetchResult(int TmdbId, TmdbMovie Movie, string[] AltTitles)
- [x] 1.2 Add TvdbFetchFailed(int TvdbId, string Reason) and TmdbFetchFailed(int TmdbId, string Reason) failure messages
- [x] 1.3 Add FetchTvdbEpisodes(int TvdbId) and FetchTmdbMovie(string? ImdbId, int? TmdbId) fetch-only messages (replacing FetchAndResolveEpisodes/FetchAndResolveMovie)
- [x] 1.4 Update CacheEntry.cs: EpisodeCacheEntry stores TvdbEpisode[] only (drop Resolved), MovieCacheEntry stores TmdbMovie + string[] AltTitles (drop Resolved)
- [x] 1.5 Add EvictExpired internal message and PendingEpisodeRequest/PendingMovieRequest records

## 2. Pool actors - strip resolution

- [x] 2.1 Simplify TvdbResolverActor: receive FetchTvdbEpisodes, fetch via TvdbClient, tell Parent TvdbFetchResult or TvdbFetchFailed (remove EpisodeResolver dependency)
- [x] 2.2 Simplify TmdbResolverActor: receive FetchTmdbMovie, fetch via TmdbClient, tell Parent TmdbFetchResult or TmdbFetchFailed (remove MovieResolver dependency)

## 3. MetadataResolverManager - cache and resolve

- [x] 3.1 Add EpisodeResolver and MovieResolver constructor dependencies to MetadataResolverManager
- [x] 3.2 Replace _episodeCache/movieCache value types with raw-data cache entries
- [x] 3.3 Add pending request tracking: Dictionary<int, List<PendingEpisodeRequest>> and Dictionary<int, List<PendingMovieRequest>>
- [x] 3.4 Rewrite HandleResolveEpisodes: cache hit -> filter by season + resolve in-place + respond; cache miss -> check pending, if none in-flight send FetchTvdbEpisodes, stash request
- [x] 3.5 Rewrite HandleResolveMovie: cache hit -> resolve in-place + respond; cache miss -> check pending, if none in-flight send FetchTmdbMovie, stash request
- [x] 3.6 Add HandleTvdbFetchResult: cache raw data, resolve + respond for each pending request, clear pending
- [x] 3.7 Add HandleTmdbFetchResult: cache raw data, resolve + respond for each pending request, clear pending
- [x] 3.8 Add HandleTvdbFetchFailed/HandleTmdbFetchFailed: forward failure to all pending callers, clear pending
- [x] 3.9 Add HandleEvictExpired: iterate both caches, remove expired entries
- [x] 3.10 Schedule EvictExpired timer in constructor via Context.System.Scheduler.ScheduleTellRepeatedly (30 min interval)
- [x] 3.11 Update HandleCacheStats to work with new cache entry types

## 4. Tests

- [x] 4.1 Update MetadataResolverManagerTests for new cache behavior: cache hit re-resolves, different season/candidates return correct results
- [x] 4.2 Add tests for concurrent request deduplication (same ID, multiple callers)
- [x] 4.3 Add tests for fetch failure propagation to all pending callers
- [x] 4.4 Add test for periodic eviction removing expired entries
- [x] 4.5 Run full test suite and dotnet format
