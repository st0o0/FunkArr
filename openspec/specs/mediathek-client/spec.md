## Purpose

Typed HTTP client service for querying the MediathekViewWeb API, with distributed cache integration and telemetry.

## Requirements

### Requirement: MediathekClient is a typed HTTP client service

The `MediathekClient` SHALL be a plain DI service (not an actor) in the `FunkArr.Search` namespace. It SHALL receive a typed `HttpClient` via constructor injection (registered via `AddHttpClient<MediathekClient>`). The client SHALL accept a pre-built JSON query string, handle HTTP POST to the MediathekViewWeb API, response deserialization, and cache lookup/storage. The client SHALL return an internal `MediathekQueryResult` record (not an actor message). The client SHALL NOT reference `FunkArr.Messages`.

#### Scenario: Successful query with cache miss

- **WHEN** `QueryAsync` is called with a JSON query string and the cache has no matching entry
- **THEN** the client SHALL POST the JSON to the API endpoint, deserialize the response, map results to `MediathekItem` records, cache the `MediathekQueryResult` with a 5-minute TTL, and return it

#### Scenario: Successful query with cache hit

- **WHEN** `QueryAsync` is called with a JSON query string and the cache has a valid entry
- **THEN** the client SHALL return the cached `MediathekQueryResult` without making an HTTP request

#### Scenario: HTTP error

- **WHEN** the HTTP POST fails (network error, non-2xx status)
- **THEN** the client SHALL throw an `HttpRequestException` (not catch and wrap it -- the stream pipeline handles failure mapping)

#### Scenario: Cache key derivation

- **WHEN** a JSON query string is used for cache lookup
- **THEN** the cache key SHALL be `mediathek:{json}` where `{json}` is the exact JSON string passed to `QueryAsync`

### Requirement: MediathekClient uses IDistributedCache

The MediathekClient SHALL accept `IDistributedCache` via constructor injection and use the typed extension methods (`GetAsync<T>`, `SetAsync<T>`) for caching query responses. The cache key SHALL be derived from the JSON query string passed to `QueryAsync` using the format `mediathek:{json}`.

#### Scenario: Cache TTL

- **WHEN** a response is cached
- **THEN** the cache entry SHALL use an absolute expiration of 5 minutes

### Requirement: MediathekClient DI registration

The MediathekClient SHALL be registered as a typed HttpClient via `services.AddHttpClient<MediathekClient>` in `SearchSetupContainer`. The registration SHALL configure the base address, `ExternalApiMetricsHandler`, and `AddStandardResilienceHandler` - matching the existing named client configuration.

#### Scenario: Typed client registration

- **WHEN** the Search domain is configured
- **THEN** `MediathekClient` SHALL be available for injection with a pre-configured HttpClient

### Requirement: MediathekClient telemetry

The MediathekClient SHALL record cache hit and cache miss counters on the `FunkArr.Search` meter using the existing telemetry instruments.

#### Scenario: Cache hit telemetry

- **WHEN** a query is served from cache
- **THEN** the cache hit counter SHALL be incremented

#### Scenario: Cache miss telemetry

- **WHEN** a query requires an HTTP request
- **THEN** the cache miss counter SHALL be incremented
