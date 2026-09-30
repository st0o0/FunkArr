# episode-resolution-messages

## MODIFIED Requirements

### Requirement: Episode enrichment response types

`EpisodeEnrichmentResponse` abstract record SHALL be renamed to `EnrichEpisodesResponse`. `EpisodesEnriched` SHALL be renamed to `EnrichEpisodesCompleted`. `EpisodeEnrichmentFailed` SHALL be renamed to `EnrichEpisodesFailed`.

#### Scenario: EnrichEpisodes file
- **WHEN** `EnrichEpisodes.cs` is examined
- **THEN** it SHALL contain `EnrichEpisodes`, `EnrichEpisodesResponse`, `EnrichEpisodesCompleted`, `EnrichEpisodesFailed`
