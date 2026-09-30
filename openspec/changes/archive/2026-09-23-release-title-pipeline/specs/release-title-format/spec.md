## MODIFIED Requirements

### Requirement: ReleaseTitleBuilder formats scene-style titles

FunkArr.Core SHALL define a `ReleaseTitleBuilder` static class with a `Format` method that produces scene-style release titles from pre-resolved display data. The method SHALL accept a media name, an optional identifier string (S##E##, date, or year), an episode title, and a quality value. The method SHALL perform only formatting (sanitization, dot-separation, quality mapping) — no title resolution, topic stripping, or S##E## deduplication. The format SHALL be compatible with Sonarr/Radarr title parsing.

#### Scenario: TV episode with season and episode

- **WHEN** Format is called with mediaName "Tatort", identifier "S01E05", episodeTitle "Der letzte Schrei", quality 720
- **THEN** the result SHALL be `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: TV daily show with airdate

- **WHEN** Format is called with mediaName "heute-show", identifier "2024-09-20", episodeTitle "vom 20. September 2024", quality 480
- **THEN** the result SHALL be `heute-show.2024-09-20.vom.20.September.2024.GERMAN.480p.WEB.h264-FunkArr`

#### Scenario: TV episode with no identifier

- **WHEN** Format is called with mediaName "Tagesschau", identifier null, episodeTitle "20 Uhr", quality 720
- **THEN** the result SHALL be `Tagesschau.20.Uhr.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: Movie with year

- **WHEN** Format is called with mediaName "Der Alte", identifier "2024", episodeTitle "Todfeinde", quality 1080
- **THEN** the result SHALL be `Der.Alte.2024.Todfeinde.GERMAN.1080p.WEB.h264-FunkArr`

#### Scenario: Movie without year

- **WHEN** Format is called with mediaName "Polizeiruf 110", identifier null, episodeTitle "Blutige Fährte", quality 720
- **THEN** the result SHALL be `Polizeiruf.110.Blutige.Fährte.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: Identifier not duplicated in episode title

- **WHEN** Format is called with mediaName "Leschs Kosmos", identifier "S2026E10", episodeTitle "Jahrhunderthitze", quality 1080
- **THEN** the result SHALL be `Leschs.Kosmos.S2026E10.Jahrhunderthitze.GERMAN.1080p.WEB.h264-FunkArr`
- **AND** S2026E10 SHALL appear exactly once

#### Scenario: MediaName not duplicated in episode title

- **WHEN** Format is called with mediaName "Lieselotte", identifier "S01E30", episodeTitle "hat den Dreh raus", quality 1080
- **THEN** the result SHALL be `Lieselotte.S01E30.hat.den.Dreh.raus.GERMAN.1080p.WEB.h264-FunkArr`
- **AND** "Lieselotte" SHALL appear exactly once

### Requirement: MetadataSpec record carries identification results

FunkArr.Messages.Scoring SHALL define a `MetadataSpec` sealed record carrying extracted identification metadata from scoring rules. All fields SHALL be nullable because not every identification strategy produces all three.

#### Scenario: MetadataSpec fields

- **WHEN** a MetadataSpec is created
- **THEN** it SHALL contain `Season` (string?), `Episode` (string?), `AiredAt` (DateTimeOffset?)

#### Scenario: Season and Episode are strings

- **WHEN** a scoring rule extracts season "2024" and episode "27"
- **THEN** MetadataSpec SHALL store them as strings, preserving zero-padding and year-based seasons

#### Scenario: All fields null

- **WHEN** no identification strategy matched
- **THEN** MetadataSpec SHALL be null on ScoredItem (not a MetadataSpec with all-null fields)

## REMOVED Requirements

### Requirement: StripTopicFromTitle
**Reason**: Topic stripping is no longer needed in the builder. EpisodeTitle arrives pre-resolved from the display pipeline without topic prefixes/suffixes.
**Migration**: Fallback topic stripping logic moves to `ReleaseVariant` as `CleanTitle` for items without scoring/enrichment data.

### Requirement: Season and episode zero-padding
**Reason**: Identifier formatting (S##E##, date, year) moves to `ReleaseVariant.Expand` which calls the existing `FormatSeasonEpisode` utility. The builder receives a pre-formatted identifier string.
**Migration**: `FormatSeasonEpisode` and `PadNumber` remain as public utilities on `ReleaseTitleBuilder` for callers to use.
