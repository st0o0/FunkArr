## ADDED Requirements

### Requirement: Quality cache storage
The system SHALL cache QualityInfo results keyed by video URL in a thread-safe in-memory store.

#### Scenario: Cache hit
- **WHEN** a URL has been probed previously and the cache entry has not expired
- **THEN** the cached QualityInfo SHALL be returned without any network requests

#### Scenario: Cache miss
- **WHEN** a URL has not been probed or the cache entry has expired
- **THEN** the QualityProbeService SHALL execute probing phases and store the result

#### Scenario: Thread-safe concurrent access
- **WHEN** two concurrent searches request quality for the same URL simultaneously
- **THEN** only one probe SHALL execute and both callers SHALL receive the result

### Requirement: Configurable TTL
The cache TTL SHALL be configurable via `FunkArr__QualityCacheTtlMinutes` with a default of 360 (6 hours).

#### Scenario: Default TTL
- **WHEN** no TTL configuration is set
- **THEN** cache entries SHALL expire after 6 hours

#### Scenario: Custom TTL
- **WHEN** `FunkArr__QualityCacheTtlMinutes` is set to 120
- **THEN** cache entries SHALL expire after 2 hours

#### Scenario: Expired entry triggers re-probe
- **WHEN** a cache entry has exceeded its TTL
- **THEN** the next request for that URL SHALL trigger a fresh probe

### Requirement: Memory bounds
The cache SHALL not grow unbounded. It SHALL evict the oldest entries when a configurable capacity is reached (default: 50,000 entries).

#### Scenario: Capacity reached
- **WHEN** the cache holds 50,000 entries and a new URL is probed
- **THEN** the oldest entry SHALL be evicted to make room

#### Scenario: Cache cleared on restart
- **WHEN** the application restarts
- **THEN** the cache SHALL start empty (no persistence)
