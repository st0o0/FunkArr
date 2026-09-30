## RENAMED Requirements

### Requirement: Metadata cache project namespace
FROM: `FunkArr.MetadataResolver`
TO: `FunkArr.Enrichment`

#### Scenario: Project namespace reference
- **WHEN** referencing the metadata cache project namespace
- **THEN** `FunkArr.Enrichment` SHALL be used instead of `FunkArr.MetadataResolver`

### Requirement: Metadata cache manager actor
FROM: `MetadataResolverManager`
TO: `EnrichmentManager`

#### Scenario: Manager actor class name
- **WHEN** referencing the metadata cache manager actor
- **THEN** `EnrichmentManager` SHALL be used instead of `MetadataResolverManager`

### Requirement: Metadata cache manager actor key
FROM: `IMetadataResolver`
TO: `IEnrichmentManager`

#### Scenario: Actor key interface
- **WHEN** referencing the actor key for the metadata/enrichment manager singleton
- **THEN** `IEnrichmentManager` SHALL be used instead of `IMetadataResolver`

### Requirement: Metadata cache manager Akka registration
FROM: `"metadata-resolver"`
TO: `"enrichment-manager"`

#### Scenario: Akka singleton registration name
- **WHEN** registering the enrichment manager singleton in Akka
- **THEN** the name `"enrichment-manager"` SHALL be used instead of `"metadata-resolver"`
