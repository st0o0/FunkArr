## ADDED Requirements

### Requirement: TvSearchWorker resolves episodes after scoring
After receiving ScoreCompleted, the TvSearchWorker SHALL check if any matched items lack Season/Episode metadata (MetadataSpec.Season is null AND MetadataSpec.Episode is null). If unresolved items exist AND the search has a TvdbId, the worker SHALL construct EpisodeCandidates from the scored items and Ask the EpisodeGuideManager to resolve them. The resolution config SHALL be obtained from the MatchingConfig (if available) or use defaults.

#### Scenario: All items have season/episode from regex
- **WHEN** all matched ScoredItems have MetadataSpec with Season and Episode set (regex-extracted)
- **THEN** the worker SHALL skip episode resolution and proceed directly to ToScoredResult

#### Scenario: Some items lack season/episode
- **WHEN** matched ScoredItems include items with MetadataSpec.Season=null (title-constructed matches)
- **THEN** the worker SHALL construct EpisodeCandidates for those items and Ask the EpisodeGuideManager

#### Scenario: No TvdbId available
- **WHEN** the search has no TvdbId (text-only search)
- **THEN** the worker SHALL skip episode resolution (TVDB lookup requires an ID)

#### Scenario: Resolution succeeds
- **WHEN** the EpisodeGuideManager responds with EpisodesResolved containing resolved episodes
- **THEN** the worker SHALL merge the resolved Season/Episode/EpisodeName into the corresponding ScoredItems' MetadataSpec before calling ToScoredResult

#### Scenario: Resolution fails
- **WHEN** the EpisodeGuideManager responds with EpisodeResolutionFailed
- **THEN** the worker SHALL proceed with ToScoredResult using the existing metadata (airdate-based titles)

#### Scenario: Resolution times out
- **WHEN** the EpisodeGuideManager Ask times out (default 15 seconds)
- **THEN** the worker SHALL proceed with ToScoredResult using the existing metadata

### Requirement: TvSearchWorker constructs EpisodeCandidates from scored items
The worker SHALL build EpisodeCandidate records from matched ScoredItems by extracting: Index from ScoredItem.Index, Title from the original MediathekItem, ConstructedTitle from TracedIdentification.Title (if available in scoring trace), AiredAt from MetadataSpec.AiredAt, Duration from the MediathekItem, ExistingSeason/ExistingEpisode from MetadataSpec.

#### Scenario: Candidate from title-construction match
- **WHEN** a ScoredItem matched via TitleConstruction with MetadataSpec(Season=null, Episode=null, AiredAt=2026-08-30)
- **THEN** the EpisodeCandidate SHALL have ExistingSeason=null, ExistingEpisode=null, AiredAt=2026-08-30

#### Scenario: Candidate with existing regex season/episode
- **WHEN** a ScoredItem matched via RegexCapture with MetadataSpec(Season="2026", Episode="01", AiredAt=2026-02-27)
- **THEN** the EpisodeCandidate SHALL have ExistingSeason="2026", ExistingEpisode="01"

### Requirement: TvSearchWorker merges resolved episodes into metadata
After receiving EpisodesResolved, the worker SHALL update the MetadataSpec for each resolved item by setting Season and Episode from the ResolvedEpisode. The AiredAt SHALL be preserved from the original metadata.

#### Scenario: Merge resolved season/episode
- **WHEN** a ResolvedEpisode has Index=3, Season="2026", Episode="09"
- **THEN** the ScoredItem at index 3 SHALL have its MetadataSpec updated to Season="2026", Episode="09" while preserving the original AiredAt

#### Scenario: Unresolved items retain original metadata
- **WHEN** an item at index 5 has no corresponding ResolvedEpisode
- **THEN** the ScoredItem at index 5 SHALL keep its original MetadataSpec unchanged
