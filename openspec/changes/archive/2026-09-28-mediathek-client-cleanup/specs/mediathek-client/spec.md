## MODIFIED Requirements

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
