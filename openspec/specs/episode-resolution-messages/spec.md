## Purpose

Message types for episode enrichment communication between Search and MetadataResolver domains.

## Requirements

### Requirement: MatchMethod enum
FunkArr.Messages.MetadataResolver SHALL define a `MatchMethod` enum with values: `RegexExtracted`, `TitleMatch`, `AirdateMatch`, `YearMatch`. This enum replaces all string-based strategy labels.

#### Scenario: MatchMethod values
- **WHEN** `MatchMethod` is inspected
- **THEN** it SHALL contain exactly: RegexExtracted, TitleMatch, AirdateMatch, YearMatch

### Requirement: IEnrichmentResult marker interface
FunkArr.Messages.MetadataResolver SHALL define an `IEnrichmentResult` interface with properties: `int Index`, `float Confidence`, `MatchMethod Method`. Both `EnrichedEpisode` and `EnrichedMovie` SHALL implement this interface.

#### Scenario: Common enrichment result properties
- **WHEN** an IEnrichmentResult is received
- **THEN** it SHALL expose Index, Confidence, and Method regardless of whether it is an episode or movie enrichment

### Requirement: ResolveEpisodes request message
FunkArr.Messages.MetadataResolver SHALL define an `EnrichEpisodes` sealed record containing: TvdbId (int), Season (int?), Candidates (EpisodeCandidate[]). This message SHALL be sent from TvSearchWorker to MetadataResolver.

#### Scenario: EnrichEpisodes with season filter
- **WHEN** a TV search for Tatort season 2026 produces matched items
- **THEN** an `EnrichEpisodes(TvdbId=83214, Season=2026, Candidates=[...])` message SHALL be constructed

#### Scenario: EnrichEpisodes without season
- **WHEN** a TV search does not specify a season
- **THEN** `EnrichEpisodes` SHALL have Season=null and the resolver SHALL consider all seasons

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
FunkArr.Messages.MetadataResolver SHALL define an `EnrichedEpisode` sealed record containing: Index (int), Season (string), Episode (string), EpisodeName (string), Confidence (float), Method (MatchMethod). The Index field SHALL match the EpisodeCandidate.Index for correlation. EnrichedEpisode SHALL implement IEnrichmentResult.

#### Scenario: Enriched via title match
- **WHEN** "Roomservice" is matched to a TVDB episode
- **THEN** the EnrichedEpisode SHALL have the correct Season, Episode, EpisodeName, Confidence=similarity, Method=MatchMethod.TitleMatch

#### Scenario: Enriched via regex pass-through
- **WHEN** an item already has ExistingSeason="2026" and ExistingEpisode="01"
- **THEN** the EnrichedEpisode SHALL have Season="2026", Episode="01", Confidence=1.0, Method=MatchMethod.RegexExtracted

### Requirement: EpisodesResolved response message
FunkArr.Messages.MetadataResolver SHALL define an `EpisodesEnriched` sealed record containing: Episodes (EnrichedEpisode[]). It SHALL extend `EpisodeEnrichmentResponse`.

#### Scenario: Successful enrichment
- **WHEN** 5 out of 10 candidates are enriched to TVDB episodes
- **THEN** EpisodesEnriched SHALL contain 5 EnrichedEpisode entries

#### Scenario: No candidates enriched
- **WHEN** no candidates could be matched to TVDB episodes
- **THEN** EpisodesEnriched SHALL contain an empty array

### Requirement: EpisodeResolutionFailed response message
FunkArr.Messages.MetadataResolver SHALL define an `EpisodeEnrichmentFailed` sealed record containing: Cause (Exception). It SHALL extend `EpisodeEnrichmentResponse`.

#### Scenario: TVDB unavailable
- **WHEN** the TVDB API is unreachable
- **THEN** EpisodeEnrichmentFailed SHALL contain Cause describing the error

### Requirement: IEpisodeEnrichmentResponse marker interface
FunkArr.Messages.MetadataResolver SHALL define an `abstract record EpisodeEnrichmentResponse` as the base for all episode enrichment responses, replacing the `IEpisodeEnrichmentResponse` marker interface. `EpisodesEnriched` and `EpisodeEnrichmentFailed` SHALL extend `EpisodeEnrichmentResponse`.

#### Scenario: Response type discrimination
- **WHEN** the TvSearchWorker receives an `EpisodeEnrichmentResponse`
- **THEN** it SHALL pattern-match on `EpisodesEnriched` or `EpisodeEnrichmentFailed`

### Requirement: IMetadataResolver marker interface
FunkArr.Core SHALL define an `IMetadataResolver` marker interface for actor resolution via `Context.GetActor<IMetadataResolver>()`.

#### Scenario: Marker interface in Core
- **WHEN** FunkArr.Core is compiled
- **THEN** it SHALL contain `IMetadataResolver` as a public interface with no members
