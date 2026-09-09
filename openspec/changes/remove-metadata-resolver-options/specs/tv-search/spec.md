## MODIFIED Requirements

### Requirement: TvSearchWorker episode resolution stage
After receiving ScoreCompleted, the TvSearchWorker SHALL check if any matched items lack Season/Episode metadata. If unresolved items exist AND the search has a TvdbId, the worker SHALL construct EpisodeCandidates and Ask the MetadataResolver. The resolution config SHALL use the `ResolutionConfig` record defaults (`new ResolutionConfig()`).

#### Scenario: All items have season/episode from regex
- **WHEN** all matched ScoredItems have MetadataSpec with Season and Episode set
- **THEN** the worker SHALL skip episode resolution and proceed directly to ToScoredResult

#### Scenario: Some items lack season/episode
- **WHEN** matched ScoredItems include items with MetadataSpec.Season=null
- **THEN** the worker SHALL construct EpisodeCandidates and Ask the MetadataResolver

#### Scenario: Resolution succeeds
- **WHEN** the MetadataResolver responds with EpisodesResolved
- **THEN** the worker SHALL merge resolved Season/Episode into the ScoredItems

#### Scenario: Resolution fails
- **WHEN** the MetadataResolver responds with EpisodeResolutionFailed
- **THEN** the worker SHALL proceed with existing metadata
