## RENAMED Requirements

### Requirement: Movie resolution project namespace
FROM: `FunkArr.MetadataResolver`
TO: `FunkArr.Enrichment`

#### Scenario: Project namespace reference
- **WHEN** referencing the movie resolution project namespace
- **THEN** `FunkArr.Enrichment` SHALL be used instead of `FunkArr.MetadataResolver`

### Requirement: Movie resolver class
FROM: `MovieResolver`
TO: `MovieEnricher`

#### Scenario: Resolver class name
- **WHEN** referencing the movie resolution logic class
- **THEN** `MovieEnricher` SHALL be used instead of `MovieResolver`

### Requirement: TMDB resolver actor
FROM: `TmdbResolverActor`
TO: `TmdbEnrichmentActor`

#### Scenario: TMDB actor class name
- **WHEN** referencing the TMDB child actor for movie resolution
- **THEN** `TmdbEnrichmentActor` SHALL be used instead of `TmdbResolverActor`
