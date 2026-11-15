# Enrichment Test Coverage

## Why

The enrichment domain has basic unit tests for enricher logic and client config validation, but lacks coverage for HTTP-level behavior: pagination, re-authentication, cache interactions, error propagation through actors, and config-driven behavior at the actor level. QA Plan Part C (sections C.1-C.3) identifies 25+ test cases.

## What

Add tests covering:
- **C.1 TVDB flow**: Multi-page pagination, 401 re-auth success/failure, cache hit skips HTTP, season filter in actor, season fallback penalty in actor
- **C.2 TMDB flow**: FindByImdbId success path, cache hit skips HTTP, alt-title caching  
- **C.3 Enrichment config via actors**: Disabled config, title-only method, no TVDB/TMDB ID, null config defaults

## Scope

Test-only change. No production code modifications.
