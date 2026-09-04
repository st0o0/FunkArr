## ADDED Requirements

### Requirement: TvSearchWorker is a sharded entity

The TvSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search, responds, and passivates.

#### Scenario: Worker creation and passivation

- **WHEN** the TvSearch ShardRegion receives a message with a new SearchId
- **THEN** a new TvSearchWorker instance SHALL be created, process the search, and passivate after responding

### Requirement: TvSearchWorker orchestrates search pipeline

The TvSearchWorker SHALL chain MediathekViewWeb query and MatchMagic scoring using PipeTo, without blocking the actor thread. When only IDs are provided (no query text), the worker SHALL resolve the topic from the RuleSet domain first.

#### Scenario: Successful TV search with query text

- **WHEN** the worker receives a TvSearchCommand with Query="Tatort"
- **THEN** it SHALL build a MediathekQuery with topic-based search and duration minimum of 300 seconds, Ask the MediathekViewWebManager, then Ask the MatchMagicManager for scoring, and Tell the Sender with a SearchCompleted containing scored items

#### Scenario: ID-only TV search

- **WHEN** the worker receives a TvSearchCommand with Query=null and TvdbId=83214
- **THEN** it SHALL Ask the RuleSetResolver with ResolveRuleSet(null, TvdbId: 83214), use the resolved topic to build a MediathekQuery, then proceed with the standard MVW query and scoring flow

#### Scenario: ID-only search with no matching ruleset

- **WHEN** the worker receives a TvSearchCommand with Query=null and TvdbId=99999 and no ruleset maps that ID
- **THEN** the worker SHALL Tell the Sender with SearchCompleted(SearchId, Items: [], Total: 0)

#### Scenario: Text search carries IDs through to results

- **WHEN** the worker receives a TvSearchCommand with Query="Tatort" and TvdbId=83214
- **THEN** the worker SHALL use the text-based flow and set TvdbId=83214 on each SearchResultItem

#### Scenario: MediathekViewWeb query fails

- **WHEN** the MediathekViewWebManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate

#### Scenario: MatchMagic scoring fails

- **WHEN** the MatchMagicManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate

### Requirement: TvSearchWorker builds TV-specific queries

The worker SHALL construct MediathekQuery messages tailored for TV content.

#### Scenario: Query with show name only

- **WHEN** a TvSearchCommand has a Query but no Season or Episode
- **THEN** the MediathekQuery SHALL search the topic field for the query string with duration_min=300

#### Scenario: Query with season and episode

- **WHEN** a TvSearchCommand has Query, Season, and Episode
- **THEN** the MediathekQuery SHALL search the topic field for the query and the title field for episode-related patterns

#### Scenario: Query with only season

- **WHEN** a TvSearchCommand has Query and Season but no Episode
- **THEN** the MediathekQuery SHALL search the topic field for the query string

### Requirement: TvSearchWorker queries MediathekViewWeb with pagination

The TvSearchWorker SHALL use the Limit and Offset values from the incoming TvSearchCommand when constructing the MediathekQuery. If Limit is null, the worker SHALL use a default Size of 50. If Offset is null, the worker SHALL use 0.

#### Scenario: Search with explicit limit and offset

- **WHEN** a TvSearchCommand with Limit=100 and Offset=20 is received
- **THEN** the MediathekQuery SHALL use Size=100 and Offset=20

#### Scenario: Search with null pagination (defaults)

- **WHEN** a TvSearchCommand with Limit=null and Offset=null is received
- **THEN** the MediathekQuery SHALL use Size=50 and Offset=0

#### Scenario: Search with only limit specified

- **WHEN** a TvSearchCommand with Limit=25 and Offset=null is received
- **THEN** the MediathekQuery SHALL use Size=25 and Offset=0

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
