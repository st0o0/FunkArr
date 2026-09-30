## MODIFIED Requirements

### Requirement: TvSearchWorker episode resolution stage
After receiving ScoreCompleted, the TvSearchWorker SHALL check if any matched items lack Season/Episode metadata (MetadataSpec.Season is null AND MetadataSpec.Episode is null). If unresolved items exist AND the search has a TvdbId, the worker SHALL construct EpisodeCandidates from the scored items and Ask the MetadataResolver to resolve them. The resolution config SHALL be obtained from the MatchingConfig (if available) or use defaults.

#### Scenario: All items have season/episode from regex
- **WHEN** all matched ScoredItems have MetadataSpec with Season and Episode set (regex-extracted)
- **THEN** the worker SHALL skip episode resolution and proceed directly to ToScoredResult

#### Scenario: Some items lack season/episode
- **WHEN** matched ScoredItems include items with MetadataSpec.Season=null (title-constructed matches)
- **THEN** the worker SHALL construct EpisodeCandidates for those items and Ask the MetadataResolver

#### Scenario: No TvdbId available
- **WHEN** the search has no TvdbId (query-only search)
- **THEN** the worker SHALL skip episode resolution (TVDB lookup requires an ID)

#### Scenario: Resolution succeeds
- **WHEN** the MetadataResolver responds with EpisodesResolved containing resolved episodes
- **THEN** the worker SHALL merge the resolved Season/Episode/EpisodeName into the corresponding ScoredItems' MetadataSpec before calling ToScoredResult

#### Scenario: Resolution fails
- **WHEN** the MetadataResolver responds with EpisodeResolutionFailed
- **THEN** the worker SHALL proceed with ToScoredResult using the existing metadata (airdate-based titles)

#### Scenario: Resolution times out
- **WHEN** the MetadataResolver Ask times out (default 15 seconds)
- **THEN** the worker SHALL proceed with ToScoredResult using the existing metadata

### Requirement: TvSearchWorker uses IMetadataResolver
The TvSearchWorker SHALL resolve the MetadataResolver singleton via `Context.GetActor<IMetadataResolver>()` (renamed from `IEpisodeGuideManager`).

#### Scenario: Actor resolution
- **WHEN** TvSearchWorker is constructed
- **THEN** it SHALL resolve `IMetadataResolver` for episode resolution requests
