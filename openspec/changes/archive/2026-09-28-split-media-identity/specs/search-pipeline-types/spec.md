## MODIFIED Requirements

### Requirement: MediaIdentity groups external IDs and season/episode

MediaIdentity SHALL be an abstract record in FunkArr.Search containing ImdbId (string?) as the shared base property. Two sealed subtypes SHALL exist:

- `ShowIdentity(string? ImdbId, int? TvdbId, string? Season, string? Episode)` for TV shows
- `MovieIdentity(string? ImdbId, int? TmdbId, int? Year)` for movies

Both subtypes SHALL inherit from MediaIdentity. EnrichedItem.Identity SHALL remain typed as MediaIdentity (polymorphic). All three records SHALL live in `MediaIdentity.cs`.

#### Scenario: TV base identity

- **WHEN** a TvSearch has TvdbId=83214 and ImdbId="tt0806910"
- **THEN** the base identity SHALL be new ShowIdentity("tt0806910", 83214, null, null)

#### Scenario: Movie base identity

- **WHEN** a MovieSearch has TmdbId=550 and ImdbId="tt0137523"
- **THEN** the base identity SHALL be new MovieIdentity("tt0137523", 550, null)

#### Scenario: Show identity patched by enrichment

- **WHEN** enrichment resolves Season="2" and Episode="5" for a TV show
- **THEN** the ShowIdentity SHALL be updated to include Season="2", Episode="5" via with-expression

#### Scenario: Movie identity patched by enrichment

- **WHEN** enrichment resolves Year=2024 for a movie
- **THEN** the MovieIdentity SHALL be updated to include Year=2024 via with-expression

#### Scenario: Movie identity does not carry Season or Episode

- **WHEN** a MovieSearchWorker builds an identity from scoring metadata that includes Season/Episode captures
- **THEN** the MovieIdentity SHALL NOT include Season or Episode fields

### Requirement: EnrichedItem is the central pipeline record

EnrichedItem SHALL be a sealed record in FunkArr.Search containing: Index (int), Source (SourceInfo), Score (double), Matched (bool), HasScoringMetadata (bool), Identity (MediaIdentity), Match (MatchInfo?), Display (ReleaseDisplay?). The Identity property SHALL accept both ShowIdentity and MovieIdentity through the abstract MediaIdentity base type.

#### Scenario: Created from TV scoring without enrichment

- **WHEN** a ScoreCompleted result has Index=3, Score=0.85, Metadata with Season="2", Episode="5"
- **THEN** the EnrichedItem SHALL have Identity as ShowIdentity with Season="2", Episode="5", Match=null

#### Scenario: Created from movie scoring

- **WHEN** a ScoreCompleted result has Index=1, Score=0.9
- **THEN** the EnrichedItem SHALL have Identity as MovieIdentity, Match=null

#### Scenario: Patched by episode enrichment

- **WHEN** an EpisodesEnriched response contains an EnrichedEpisode for Index=3 with Season="2", Episode="5", Confidence=0.9, Method=TitleMatch
- **THEN** the EnrichedItem at Index=3 SHALL be updated with ShowIdentity.Season="2", ShowIdentity.Episode="5", Match=new MatchInfo(0.9, TitleMatch)

#### Scenario: Patched by movie enrichment

- **WHEN** a MoviesEnriched response contains an EnrichedMovie for Index=1 with ImdbId="tt0806910", TmdbId=550, Confidence=0.8, Method=TitleMatch
- **THEN** the EnrichedItem at Index=1 SHALL be updated with MovieIdentity.ImdbId="tt0806910", MovieIdentity.TmdbId=550, Match=new MatchInfo(0.8, TitleMatch)

### Requirement: ReleaseVariant maps to SearchResultItem

SceneRelease.Expand() SHALL pattern-match on the MediaIdentity subtype to build ExternalIds and MatchMetadata. For ShowIdentity, ExternalIds SHALL use TvdbId and ImdbId with TmdbId=null, and Season/Episode from the identity. For MovieIdentity, ExternalIds SHALL use ImdbId and TmdbId with TvdbId=null, and Season=null, Episode=null.

#### Scenario: Show identity maps to MatchMetadata with Season/Episode

- **WHEN** SceneRelease.Expand() processes an EnrichedItem with ShowIdentity(ImdbId="tt0806910", TvdbId=83214, Season="2", Episode="5")
- **THEN** the MatchMetadata SHALL have Ids=ExternalIds(83214, "tt0806910", null), Season="2", Episode="5"

#### Scenario: Movie identity maps to MatchMetadata without Season/Episode

- **WHEN** SceneRelease.Expand() processes an EnrichedItem with MovieIdentity(ImdbId="tt0137523", TmdbId=550, Year=1999)
- **THEN** the MatchMetadata SHALL have Ids=ExternalIds(null, "tt0137523", 550), Season=null, Episode=null
