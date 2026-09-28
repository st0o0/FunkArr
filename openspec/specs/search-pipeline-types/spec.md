## Purpose

Pipeline stage types internal to FunkArr.Search - SourceInfo (Mediathek projection), MediaIdentity hierarchy (abstract base with ShowIdentity/MovieIdentity subtypes for type-safe external IDs), MatchInfo (enrichment result), EnrichedItem (post-scoring central type), and SceneRelease (post-expansion with scene-style release titles).
## Requirements
### Requirement: SourceInfo projects MediathekItem for the pipeline

SourceInfo SHALL be a sealed record in FunkArr.Search that projects a MediathekItem into a pipeline-internal type. It SHALL contain: Channel, Topic, Title, Description (string?), Duration (int), Size (long), AiredAt (DateTimeOffset?), UrlHd (string?), Url (string?), UrlLow (string?), SubtitleUrl (string?). The AiredAt field SHALL be converted from the MediathekItem's unix Timestamp. A static `From(MediathekItem)` factory method SHALL perform the projection.

#### Scenario: Project MediathekItem to SourceInfo

- **WHEN** a MediathekItem has Channel="ARD", Topic="Tatort", Timestamp=1693353600, UrlVideo="https://example.com/video.mp4"
- **THEN** SourceInfo.From SHALL produce a SourceInfo with Channel="ARD", Topic="Tatort", AiredAt=2023-08-30T00:00:00Z, Url="https://example.com/video.mp4"

#### Scenario: Null URLs preserved

- **WHEN** a MediathekItem has UrlVideoHd=null
- **THEN** the SourceInfo SHALL have UrlHd=null

#### Scenario: Zero timestamp produces null AiredAt

- **WHEN** a MediathekItem has Timestamp=0
- **THEN** the SourceInfo SHALL have AiredAt=null

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

### Requirement: MatchInfo captures enrichment result

MatchInfo SHALL be a sealed record in FunkArr.Search containing: Confidence (float), Method (MatchMethod). It SHALL only be present when enrichment has run and produced a match.

#### Scenario: Episode enrichment produces MatchInfo

- **WHEN** an EnrichedEpisode has Confidence=0.95 and Method=MatchMethod.TitleMatch
- **THEN** the corresponding MatchInfo SHALL be new MatchInfo(0.95f, MatchMethod.TitleMatch)

#### Scenario: No enrichment means null MatchInfo

- **WHEN** scoring completes without enrichment
- **THEN** EnrichedItem.Match SHALL be null

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

### Requirement: ReleaseVariant expands EnrichedItem into quality variants

ReleaseVariant SHALL be a sealed record in FunkArr.Search containing: Title (string), Source (SourceInfo), Identity (MediaIdentity), Score (double), Quality (int), Size (long), Match (MatchInfo?). A static `Expand(EnrichedItem, MediaType mediaType, string? mediaName)` method SHALL produce ReleaseVariant[] by expanding the source URLs into quality variants (HD=1080, normal=720, low=480) and generating a release title via ReleaseTitleBuilder for each variant. The `mediaType` parameter SHALL be a `MediaType` enum value, not a string.

#### Scenario: Item with all three quality URLs

- **WHEN** an EnrichedItem has Source.UrlHd, Source.Url, and Source.UrlLow all set
- **THEN** Expand SHALL return 3 ReleaseVariants with Quality 1080, 720, and 480

#### Scenario: Item with only normal quality URL

- **WHEN** an EnrichedItem has Source.UrlHd=null, Source.Url set, Source.UrlLow=null
- **THEN** Expand SHALL return 1 ReleaseVariant with Quality=720

#### Scenario: Item with no URLs

- **WHEN** an EnrichedItem has all URL fields null
- **THEN** Expand SHALL return an empty array

#### Scenario: Size estimation

- **WHEN** Source.Size is 0 or negative
- **THEN** the ReleaseVariant size SHALL be estimated from Duration and quality-specific bitrate

#### Scenario: MediaType enum replaces string parameter

- **WHEN** `ReleaseVariant.Expand` is called for a TV show
- **THEN** the `mediaType` parameter SHALL be `MediaType.Show`, not `"tv"`

### Requirement: ReleaseVariant maps to SearchResultItem

SceneRelease.Expand() SHALL pattern-match on the MediaIdentity subtype to build ExternalIds and MatchMetadata. For ShowIdentity, ExternalIds SHALL use TvdbId and ImdbId with TmdbId=null, and Season/Episode from the identity. For MovieIdentity, ExternalIds SHALL use ImdbId and TmdbId with TvdbId=null, and Season=null, Episode=null.

#### Scenario: Show identity maps to MatchMetadata with Season/Episode

- **WHEN** SceneRelease.Expand() processes an EnrichedItem with ShowIdentity(ImdbId="tt0806910", TvdbId=83214, Season="2", Episode="5")
- **THEN** the MatchMetadata SHALL have Ids=ExternalIds(83214, "tt0806910", null), Season="2", Episode="5"

#### Scenario: Movie identity maps to MatchMetadata without Season/Episode

- **WHEN** SceneRelease.Expand() processes an EnrichedItem with MovieIdentity(ImdbId="tt0137523", TmdbId=550, Year=1999)
- **THEN** the MatchMetadata SHALL have Ids=ExternalIds(null, "tt0137523", 550), Season=null, Episode=null

