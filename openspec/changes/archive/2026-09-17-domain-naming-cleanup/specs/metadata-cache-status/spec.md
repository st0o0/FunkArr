## RENAMED Requirements

### Requirement: Cache status project namespace
FROM: `FunkArr.MetadataResolver`
TO: `FunkArr.Enrichment`

#### Scenario: Project namespace reference
- **WHEN** referencing the cache status project namespace
- **THEN** `FunkArr.Enrichment` SHALL be used instead of `FunkArr.MetadataResolver`

### Requirement: Cache status manager reference
FROM: `MetadataResolverManager`
TO: `EnrichmentManager`

#### Scenario: Manager class reference
- **WHEN** referencing the manager actor for cache status queries
- **THEN** `EnrichmentManager` SHALL be used instead of `MetadataResolverManager`
