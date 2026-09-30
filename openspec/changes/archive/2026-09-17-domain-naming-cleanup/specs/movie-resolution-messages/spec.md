## RENAMED Requirements

### Requirement: Movie resolution message namespace
FROM: `FunkArr.Messages.MetadataResolver`
TO: `FunkArr.Messages.Enrichment`

#### Scenario: Message namespace reference
- **WHEN** referencing the namespace for movie enrichment messages
- **THEN** `FunkArr.Messages.Enrichment` SHALL be used instead of `FunkArr.Messages.MetadataResolver`

#### Scenario: Message type names unchanged
- **WHEN** referencing message types (EnrichMovies, EnrichedMovie, MovieCandidate, MovieEnrichmentResponse, MovieEnrichmentFailed, MoviesEnriched, IEnrichmentResult, MatchMethod, QueryCacheStats, CacheStatsResult)
- **THEN** the type names SHALL remain unchanged; only the namespace moves
