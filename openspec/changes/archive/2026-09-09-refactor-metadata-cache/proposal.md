## Why

The MetadataResolverManager caches resolved results (ResolvedEpisode[], MovieResolved[]) keyed only by provider ID, but the resolved results depend on Season, Config, and Candidates which are not part of the cache key. This means cache hits return wrong results when the same series/movie is requested with different inputs. Additionally, the cache grows unbounded - expired entries are only lazily removed on the next read, never actively evicted.

## What Changes

- Cache raw API responses (TvdbEpisode[], TmdbMovie + alternative titles) instead of resolved results
- Move resolution logic out of pool actors into the Manager - pool actors become pure fetch actors
- Re-resolve on every cache hit using current request parameters (Season, Config, Candidates)
- Add periodic eviction of expired cache entries via a scheduled timer
- Update internal messages to carry raw data instead of resolved results
- Update cache entry records to hold raw API data

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `metadata-cache`: Cache stores raw API data instead of resolved results. Adds active eviction via scheduled timer instead of lazy-only expiry.

## Impact

- `FunkArr.MetadataResolver` project only - no cross-domain changes
- MetadataResolverManager: cache dictionaries, message handlers, new timer
- TvdbResolverActor / TmdbResolverActor: remove resolution, return raw data
- CacheEntry: hold raw API data instead of resolved results
- InternalMessages: carry raw data instead of resolved results
- No API changes, no persistence changes, no message contract changes outside the domain
