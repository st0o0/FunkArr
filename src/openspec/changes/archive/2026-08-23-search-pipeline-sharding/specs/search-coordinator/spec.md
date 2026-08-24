## REMOVED Requirements

### Requirement: SearchCoordinator singleton registration
**Reason**: Replaced by `SearchRouter` (stateless singleton) + three ShardRegions (`TvSearchPipeline`, `MovieSearchPipeline`, `TextSearchPipeline`). The god-actor pattern is decomposed into routing, resolution, and per-entity pipeline orchestration.
**Migration**: All code that asks `SearchCoordinator` for search results SHALL ask `SearchRouter` instead, which forwards to the appropriate ShardRegion. Message types (`TvSearchRequest`, `MovieSearchRequest`, `TextSearchRequest`, `SearchResponse`) remain the same.

### Requirement: Topic-level result cache
**Reason**: Caching moves into each search pipeline entity. Each `TvSearchPipeline`, `MovieSearchPipeline`, and `TextSearchPipeline` entity owns its own cache with 55-min TTL.
**Migration**: No external migration needed. Cache is ephemeral and rebuilds on demand.

### Requirement: Topic-level request coalescing
**Reason**: Coalescing is now natural per-entity — each shard entity has its own mailbox, so concurrent requests for the same entity key are automatically serialized and coalesced within the entity.
**Migration**: No external migration needed.

### Requirement: Receive + PipeTo pipeline orchestration
**Reason**: Pipeline orchestration moves into each search entity. The Receive + PipeTo pattern is still used within entities for async steps (Ask to singletons), but CPU-bound steps (Match, Score, ExpandQualities) are inlined as synchronous function calls.
**Migration**: No external migration needed.
