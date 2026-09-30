## ADDED Requirements

### Requirement: MediathekClient is a typed HTTP client service

The `MediathekClient` SHALL be a plain DI service (not an actor) in the `FunkArr.Search` namespace. It SHALL receive a typed `HttpClient` via constructor injection (registered via `AddHttpClient<MediathekClient>`). The client SHALL handle JSON serialization of query objects, HTTP POST to the MediathekViewWeb API, response deserialization, and cache lookup/storage.

#### Scenario: Successful query with cache miss

- **WHEN** `QueryAsync` is called with a query object and the cache has no matching entry
- **THEN** the client SHALL serialize the query to JSON, POST to the API endpoint, deserialize the response, cache the result with a 5-minute TTL, and return the mapped response

#### Scenario: Successful query with cache hit

- **WHEN** `QueryAsync` is called with a query object and the cache has a valid entry
- **THEN** the client SHALL return the cached response without making an HTTP request

#### Scenario: HTTP error

- **WHEN** the HTTP POST fails (network error, non-2xx status)
- **THEN** the client SHALL throw an `HttpRequestException` (not catch and wrap it - the actor handles failure mapping)

### Requirement: MediathekClient uses IDistributedCache

The MediathekClient SHALL accept `IDistributedCache` via constructor injection and use the typed extension methods (`GetAsync<T>`, `SetAsync<T>`) for caching query responses. The cache key SHALL be derived from the deterministic JSON serialization of the query object.

#### Scenario: Cache key determinism

- **WHEN** two identical query objects are serialized
- **THEN** they SHALL produce the same cache key

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
