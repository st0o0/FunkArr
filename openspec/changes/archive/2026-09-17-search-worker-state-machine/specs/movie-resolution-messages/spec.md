## MODIFIED Requirements

### Requirement: IMovieResolutionResponse marker interface

FunkArr.Messages.MetadataResolver SHALL define an `abstract record MovieEnrichmentResponse` as the base for all movie enrichment responses, replacing the `IMovieEnrichmentResponse` marker interface. `MoviesEnriched` and `MovieEnrichmentFailed` SHALL extend `MovieEnrichmentResponse`.

#### Scenario: Response type discrimination

- **WHEN** the MovieSearchWorker receives a `MovieEnrichmentResponse`
- **THEN** it SHALL pattern-match on `MoviesEnriched` or `MovieEnrichmentFailed`
