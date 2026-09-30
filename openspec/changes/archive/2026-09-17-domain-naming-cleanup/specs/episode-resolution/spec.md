## RENAMED Requirements

### Requirement: Episode resolution project namespace
FROM: `FunkArr.MetadataResolver`
TO: `FunkArr.Enrichment`

#### Scenario: Project namespace reference
- **WHEN** referencing the episode resolution project namespace
- **THEN** `FunkArr.Enrichment` SHALL be used instead of `FunkArr.MetadataResolver`

### Requirement: Episode resolver class
FROM: `EpisodeResolver`
TO: `EpisodeEnricher`

#### Scenario: Resolver class name
- **WHEN** referencing the episode resolution logic class
- **THEN** `EpisodeEnricher` SHALL be used instead of `EpisodeResolver`

### Requirement: TVDB resolver actor
FROM: `TvdbResolverActor`
TO: `TvdbEnrichmentActor`

#### Scenario: TVDB actor class name
- **WHEN** referencing the TVDB child actor for episode resolution
- **THEN** `TvdbEnrichmentActor` SHALL be used instead of `TvdbResolverActor`
