## Purpose

Message types for episode resolution communication between Search and EpisodeGuide domains.

## Requirements

### Requirement: ResolveEpisodes request message
FunkArr.Messages.MetadataResolver SHALL define a `ResolveEpisodes` sealed record containing: TvdbId (int), Season (int?), Config (ResolutionConfig), Candidates (EpisodeCandidate[]). This message SHALL be sent from TvSearchWorker to MetadataResolver.

#### Scenario: ResolveEpisodes with season filter
- **WHEN** a TV search for Tatort season 2026 produces matched items
- **THEN** a `ResolveEpisodes(TvdbId=83214, Season=2026, Config=..., Candidates=[...])` message SHALL be constructed

#### Scenario: ResolveEpisodes without season
- **WHEN** a TV search does not specify a season
- **THEN** `ResolveEpisodes` SHALL have Season=null and the resolver SHALL consider all seasons

### Requirement: EpisodeCandidate record
FunkArr.Messages.MetadataResolver SHALL define an `EpisodeCandidate` sealed record containing: Index (int), Title (string), ConstructedTitle (string?), AiredAt (DateTimeOffset?), Duration (int), ExistingSeason (string?), ExistingEpisode (string?). The Index field SHALL correspond to the original ScoredItem index for result correlation.

#### Scenario: Candidate from title-construction match
- **WHEN** a Mediathek item "Roomservice" matches via TitleConstruction with constructed title "Roomservice"
- **THEN** the EpisodeCandidate SHALL have Title="Roomservice", ConstructedTitle="Roomservice", ExistingSeason=null, ExistingEpisode=null

#### Scenario: Candidate from regex match
- **WHEN** a Mediathek item matches via RegexCapture with season "2026" and episode "01"
- **THEN** the EpisodeCandidate SHALL have ExistingSeason="2026", ExistingEpisode="01"

#### Scenario: Candidate with airdate
- **WHEN** a Mediathek item has timestamp 1788113728
- **THEN** the EpisodeCandidate SHALL have AiredAt set to the corresponding DateTimeOffset

### Requirement: ResolvedEpisode record
FunkArr.Messages.MetadataResolver SHALL define a `ResolvedEpisode` sealed record containing: Index (int), Season (string), Episode (string), EpisodeName (string), Confidence (float), Strategy (string). The Index field SHALL match the EpisodeCandidate.Index for correlation.

#### Scenario: Resolved via fuzzy title match
- **WHEN** "Roomservice" is matched to a TVDB episode
- **THEN** the ResolvedEpisode SHALL have the correct Season, Episode, EpisodeName, Confidence=similarity, Strategy="FuzzyTitleMatch"

#### Scenario: Resolved via regex pass-through
- **WHEN** an item already has ExistingSeason="2026" and ExistingEpisode="01"
- **THEN** the ResolvedEpisode SHALL have Season="2026", Episode="01", Confidence=1.0, Strategy="RegexExtracted"

### Requirement: ResolutionConfig record
FunkArr.Messages.MetadataResolver SHALL define a `ResolutionConfig` sealed record containing: Strategy (string), Threshold (float), AirdateTolerance (int). Default values SHALL be: Strategy="fuzzy", Threshold=0.7f, AirdateTolerance=7.

#### Scenario: Default resolution config
- **WHEN** a ResolutionConfig is constructed with defaults
- **THEN** Strategy SHALL be "fuzzy", Threshold SHALL be 0.7, AirdateTolerance SHALL be 7

#### Scenario: Custom resolution config
- **WHEN** a RuleSet JSON specifies `"resolution": {"strategy": "strict", "threshold": 0.95, "airdateTolerance": 3}`
- **THEN** the ResolutionConfig SHALL have Strategy="strict", Threshold=0.95, AirdateTolerance=3

### Requirement: EpisodesResolved response message
FunkArr.Messages.MetadataResolver SHALL define an `EpisodesResolved` sealed record containing: Episodes (ResolvedEpisode[]). It SHALL implement `IEpisodeResolutionResponse`.

#### Scenario: Successful resolution
- **WHEN** 5 out of 10 candidates are resolved to TVDB episodes
- **THEN** EpisodesResolved SHALL contain 5 ResolvedEpisode entries

#### Scenario: No candidates resolved
- **WHEN** no candidates could be matched to TVDB episodes
- **THEN** EpisodesResolved SHALL contain an empty array

### Requirement: EpisodeResolutionFailed response message
FunkArr.Messages.MetadataResolver SHALL define an `EpisodeResolutionFailed` sealed record containing: Reason (string). It SHALL implement `IEpisodeResolutionResponse`.

#### Scenario: TVDB unavailable
- **WHEN** the TVDB API is unreachable
- **THEN** EpisodeResolutionFailed SHALL contain Reason describing the error

### Requirement: IEpisodeResolutionResponse marker interface
FunkArr.Messages.MetadataResolver SHALL define an `IEpisodeResolutionResponse` marker interface implemented by `EpisodesResolved` and `EpisodeResolutionFailed`.

#### Scenario: Response type discrimination
- **WHEN** the TvSearchWorker receives an `IEpisodeResolutionResponse`
- **THEN** it SHALL pattern-match on `EpisodesResolved` or `EpisodeResolutionFailed`

### Requirement: IMetadataResolver marker interface
FunkArr.Core SHALL define an `IMetadataResolver` marker interface for actor resolution via `Context.GetActor<IMetadataResolver>()`.

#### Scenario: Marker interface in Core
- **WHEN** FunkArr.Core is compiled
- **THEN** it SHALL contain `IMetadataResolver` as a public interface with no members
