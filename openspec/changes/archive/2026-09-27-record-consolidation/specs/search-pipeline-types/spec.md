## MODIFIED Requirements

### Requirement: ReleaseVariant maps to SearchResultItem

ReleaseVariant SHALL have a `ToResultItem()` method that produces a flat SearchResultItem by copying fields directly. The method SHALL construct a `MatchMetadata` from the ReleaseVariant's Identity (TvdbId, ImdbId, TmdbId, Season, Episode) and Match (Confidence, Method) fields. If both Identity has no IDs and Match is null, MatchMetadata SHALL be null. SubtitleUrl SHALL be mapped from Source.SubtitleUrl.

#### Scenario: Trivial 1:1 mapping
- **WHEN** a ReleaseVariant has Title="Tatort.S02E05.Roomservice.GERMAN.720p.WEB.h264-FunkArr", Source.Channel="ARD", Identity.TvdbId=83214, Match.Confidence=0.9
- **THEN** ToResultItem SHALL produce a SearchResultItem with those values in a MatchMetadata property, not as flat fields

#### Scenario: No enrichment produces null MatchMetadata
- **WHEN** a ReleaseVariant has Identity with all null IDs and Match=null
- **THEN** ToResultItem SHALL produce a SearchResultItem with Metadata=null
