## RENAMED Requirements

### Requirement: Episode resolution message namespace
FROM: `FunkArr.Messages.MetadataResolver`
TO: `FunkArr.Messages.Enrichment`

#### Scenario: Message namespace reference
- **WHEN** referencing the namespace for episode enrichment messages
- **THEN** `FunkArr.Messages.Enrichment` SHALL be used instead of `FunkArr.Messages.MetadataResolver`

#### Scenario: Message type names unchanged
- **WHEN** referencing message types (EnrichEpisodes, EnrichedEpisode, EpisodeCandidate, EpisodeEnrichmentResponse, EpisodeEnrichmentFailed, EpisodesEnriched)
- **THEN** the type names SHALL remain unchanged; only the namespace moves
