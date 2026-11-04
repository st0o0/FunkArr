## MODIFIED Requirements

### Requirement: MediathekClient uses IMemoryCache

The MediathekClient SHALL accept `IMemoryCache` via constructor injection (replacing `IDistributedCache`) and use it for caching query responses. Cache entries SHALL use `AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)` and `Size = 1`. The cache key format SHALL remain `mediathek:{json}`.

#### Scenario: Cache miss fetches from API

- **WHEN** `QueryAsync` is called with a JSON query string and no cache entry exists
- **THEN** the client SHALL POST to the MediathekViewWeb API, store the `MediathekQueryResult` in `IMemoryCache` with 5-minute absolute expiration and Size = 1, and return it

#### Scenario: Cache hit returns stored object

- **WHEN** `QueryAsync` is called with a JSON query string and a cache entry exists
- **THEN** the client SHALL return the cached `MediathekQueryResult` without making an HTTP request

#### Scenario: Cache key derivation unchanged

- **WHEN** a JSON query string is used for cache lookup
- **THEN** the cache key SHALL be `mediathek:{json}` where `{json}` is the exact JSON string passed to `QueryAsync`

### Requirement: MediathekClient telemetry

The MediathekClient SHALL record cache hit and cache miss counters on the `FunkArr.Search` meter using the existing telemetry instruments.

#### Scenario: Cache hit telemetry

- **WHEN** a query is served from cache
- **THEN** the cache hit counter SHALL be incremented

#### Scenario: Cache miss telemetry

- **WHEN** a query requires an HTTP request
- **THEN** the cache miss counter SHALL be incremented

## REMOVED Requirements

### Requirement: MediathekClient uses IDistributedCache

**Reason**: Replaced by IMemoryCache — no JSON serialization overhead, consistent with other cache consumers.
**Migration**: Change constructor parameter from `IDistributedCache` to `IMemoryCache`. Replace `GetAsync<T>`/`SetAsync<T>` extension methods with `TryGetValue`/`Set`.
