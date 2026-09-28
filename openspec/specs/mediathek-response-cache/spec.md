# mediathek-response-cache Specification

## Purpose

In-actor response cache for MediathekViewWebManager, reducing redundant HTTP requests for identical queries within a configurable TTL window.

## Requirements

### Requirement: MediathekViewWebManager SHALL cache API responses

The MediathekViewWebManager SHALL maintain an in-actor response cache keyed by the serialized query JSON. On cache hit, the actor SHALL respond immediately without consuming a concurrency slot or making an HTTP request.

#### Scenario: Cache miss triggers HTTP request

- **WHEN** a QueryMediathek message arrives and no matching cache entry exists
- **THEN** the Manager SHALL proceed with the normal HTTP flow (consume concurrency slot, POST to API, cache result, respond)

#### Scenario: Cache hit returns immediately

- **WHEN** a QueryMediathek message arrives and a non-expired cache entry exists for the same query JSON
- **THEN** the Manager SHALL respond with the cached QueryMediathekCompleted immediately, without consuming a concurrency slot

#### Scenario: Cache hit does not affect concurrency count

- **WHEN** all concurrency slots are occupied and a cached query arrives
- **THEN** the Manager SHALL respond from cache without stashing the message

### Requirement: Cache entries SHALL expire after a configurable TTL

Cache entries SHALL have a default TTL of 5 minutes. Expired entries SHALL be treated as cache misses.

#### Scenario: Entry within TTL

- **WHEN** a query arrives 3 minutes after the same query was cached (TTL = 5 min)
- **THEN** the cached response SHALL be returned

#### Scenario: Entry past TTL

- **WHEN** a query arrives 6 minutes after the same query was cached (TTL = 5 min)
- **THEN** the entry SHALL be treated as a miss and a new HTTP request SHALL be made

### Requirement: Cache key SHALL be the serialized query JSON

The cache key SHALL be the JSON string produced by `MediathekQueryBuilder.Build()` for the incoming QueryMediathek message. This ensures that queries with identical API payloads share cache entries regardless of how they were constructed.

#### Scenario: Same query from different search types

- **WHEN** TvSearchWorker sends a query with `topic=Tatort, durationMin=300, size=15` and another TvSearchWorker sends an identical query
- **THEN** both SHALL map to the same cache key and the second SHALL be a cache hit

#### Scenario: Different offset produces different cache key

- **WHEN** one query has `offset=0` and another has `offset=15` but otherwise identical fields
- **THEN** they SHALL have different cache keys and be cached independently

### Requirement: Cache SHALL be cleaned up periodically

The MediathekViewWebManager SHALL use `IWithTimers` to schedule periodic cache cleanup that removes expired entries.

#### Scenario: Periodic cleanup runs

- **WHEN** the cleanup timer fires
- **THEN** all cache entries whose expiry has passed SHALL be removed from the dictionary

#### Scenario: Cleanup does not remove valid entries

- **WHEN** the cleanup timer fires and entries are within TTL
- **THEN** those entries SHALL remain in the cache
