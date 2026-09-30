## ADDED Requirements

### Requirement: Search domain SHALL instrument MediathekViewWeb cache hits and misses

The Search domain Telemetry SHALL expose counters for MediathekViewWeb response cache hits and misses.

#### Scenario: Cache hit counted

- **WHEN** MediathekViewWebManager serves a response from cache
- **THEN** `funkarr.search.mediathek_cache_hits_total` counter SHALL be incremented

#### Scenario: Cache miss counted

- **WHEN** MediathekViewWebManager makes an HTTP request (cache miss)
- **THEN** `funkarr.search.mediathek_cache_misses_total` counter SHALL be incremented
