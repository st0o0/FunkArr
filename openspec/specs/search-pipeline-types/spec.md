## Purpose

Pipeline stage types internal to FunkArr.Search — SourceInfo (Mediathek projection), MediaIdentity (external IDs + season/episode), MatchInfo (enrichment result), EnrichedItem (post-scoring central type), and ReleaseVariant (post-expansion with variant quality URLs).

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

MediaIdentity SHALL be a sealed record in FunkArr.Search containing: TvdbId (int?), ImdbId (string?), TmdbId (int?), Season (string?), Episode (string?). It SHALL be used as a composed sub-record in EnrichedItem to represent the resolved identity of a search result.

#### Scenario: TV base identity

- **WHEN** a TvSearch has TvdbId=83214 and ImdbId="tt0806910"
- **THEN** the base MediaIdentity SHALL be new MediaIdentity(83214, "tt0806910", null, null, null)

#### Scenario: Identity patched by enrichment

- **WHEN** enrichment resolves Season="2" and Episode="5"
- **THEN** the MediaIdentity SHALL be updated to include Season="2", Episode="5" via with-expression

### Requirement: MatchInfo captures enrichment result

MatchInfo SHALL be a sealed record in FunkArr.Search containing: Confidence (float), Method (MatchMethod). It SHALL only be present when enrichment has run and produced a match.

#### Scenario: Episode enrichment produces MatchInfo

- **WHEN** an EnrichedEpisode has Confidence=0.95 and Method=MatchMethod.TitleMatch
- **THEN** the corresponding MatchInfo SHALL be new MatchInfo(0.95f, MatchMethod.TitleMatch)

#### Scenario: No enrichment means null MatchInfo

- **WHEN** scoring completes without enrichment
- **THEN** EnrichedItem.Match SHALL be null

### Requirement: EnrichedItem is the central pipeline record

EnrichedItem SHALL be a sealed record in FunkArr.Search containing: Index (int), Source (SourceInfo), Score (double), Identity (MediaIdentity), Match (MatchInfo?). It SHALL be the type stored in state after scoring. Items without enrichment SHALL have Match=null.

#### Scenario: Created from scoring without enrichment

- **WHEN** a ScoreCompleted result has Index=3, Score=0.85, Metadata with Season=null
- **THEN** the EnrichedItem SHALL have Index=3, Score=0.85, Identity with base IDs and Season/Episode from metadata, Match=null

#### Scenario: Patched by episode enrichment

- **WHEN** an EpisodesEnriched response contains an EnrichedEpisode for Index=3 with Season="2", Episode="5", Confidence=0.9, Method=TitleMatch
- **THEN** the EnrichedItem at Index=3 SHALL be updated with Identity.Season="2", Identity.Episode="5", Match=new MatchInfo(0.9, TitleMatch)

#### Scenario: Patched by movie enrichment

- **WHEN** a MoviesEnriched response contains an EnrichedMovie for Index=1 with ImdbId="tt0806910", TmdbId=550, Confidence=0.8, Method=TitleMatch
- **THEN** the EnrichedItem at Index=1 SHALL be updated with Identity.ImdbId="tt0806910", Identity.TmdbId=550, Match=new MatchInfo(0.8, TitleMatch)

### Requirement: ReleaseVariant expands EnrichedItem into quality variants

ReleaseVariant SHALL be a sealed record in FunkArr.Search containing: Title (string), Source (SourceInfo), Identity (MediaIdentity), Score (double), Quality (int), Size (long), Match (MatchInfo?). A static `Expand(EnrichedItem, string mediaType, string? mediaName)` method SHALL produce ReleaseVariant[] by expanding the source URLs into quality variants (HD=1080, normal=720, low=480) and generating a release title via ReleaseTitleBuilder for each variant.

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

### Requirement: ReleaseVariant maps to SearchResultItem

ReleaseVariant SHALL have a `ToResultItem()` method that produces a flat SearchResultItem by copying fields directly. This mapping SHALL contain no logic — only field assignment.

#### Scenario: Trivial 1:1 mapping

- **WHEN** a ReleaseVariant has Title="Tatort.S02E05.Roomservice.GERMAN.720p.WEB.h264-FunkArr", Source.Channel="ARD", Identity.TvdbId=83214, Match.Confidence=0.9
- **THEN** ToResultItem SHALL produce a SearchResultItem with the same values in the corresponding flat fields
