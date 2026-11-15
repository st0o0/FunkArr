# Design

## Approach

Use the existing MockHandler pattern from TvdbClientCacheTests and TmdbClientErrorTests. For actor-level tests, use Akka.Hosting.TestKit with DI-wired actors (pattern from EnrichmentManagerTests).

### HTTP-level tests (TvdbClient, TmdbClient)
- MockHandler intercepts requests, returns canned JSON responses
- MemoryCache injected directly for cache verification
- Track call counts to verify cache behavior

### Actor-level tests (TvdbEnrichmentActor, TmdbEnrichmentActor)
- Use TestKit with real actors wired via DI
- Wire MockHandler into HttpClient via IHttpClientFactory
- Send messages, assert responses via ExpectMsg

### Config tests
- Exercise EpisodeEnricher.Resolve and MovieEnricher.Resolve with various EnrichmentConfig permutations (already partially covered, extend for runtime Filter mode)
