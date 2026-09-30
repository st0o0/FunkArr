## MODIFIED Requirements

### Requirement: IEpisodeEnrichmentResponse marker interface

FunkArr.Messages.MetadataResolver SHALL define an `abstract record EpisodeEnrichmentResponse` as the base for all episode enrichment responses, replacing the `IEpisodeEnrichmentResponse` marker interface. `EpisodesEnriched` and `EpisodeEnrichmentFailed` SHALL extend `EpisodeEnrichmentResponse`.

#### Scenario: Response type discrimination

- **WHEN** the TvSearchWorker receives an `EpisodeEnrichmentResponse`
- **THEN** it SHALL pattern-match on `EpisodesEnriched` or `EpisodeEnrichmentFailed`
