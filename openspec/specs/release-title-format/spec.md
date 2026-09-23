# release-title-format

## Purpose

Scene-style release title formatting from pre-resolved display data. Defines the MetadataSpec record for carrying identification results and the ReleaseTitleBuilder formatter that produces titles compatible with Sonarr/Radarr parsing.

## Requirements

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

### Requirement: Umlaut preservation

ReleaseTitleBuilder SHALL preserve German Umlauts and special characters (ä, ö, ü, Ä, Ö, Ü, ß) as-is in release titles. No ASCII digraph normalization SHALL be applied.

#### Scenario: Umlauts preserved in title

- **WHEN** Format is called with a mediaName containing "ö"
- **THEN** the "ö" SHALL remain as "ö" in the output, not be replaced with "oe"

#### Scenario: All German special characters preserved

- **WHEN** a title contains ä, ö, ü, Ä, Ö, Ü, ß
- **THEN** they SHALL appear unchanged in the output (NOT ae, oe, ue, Ae, Oe, Ue, ss)

#### Scenario: Eszett preserved

- **WHEN** Format is called with mediaName "Straße", episodeTitle "Spaß"
- **THEN** the result SHALL be `Straße.Spaß.GERMAN.720p.WEB.h264-FunkArr`

### Requirement: Special character handling

ReleaseTitleBuilder SHALL remove characters that are invalid in scene-style titles and replace spaces with dots.

#### Scenario: Spaces become dots

- **WHEN** a title is "Der letzte Schrei"
- **THEN** it SHALL become "Der.letzte.Schrei"

#### Scenario: Special characters removed

- **WHEN** a title contains `/:;"'@#?$%^*+=!<>,()`
- **THEN** those characters SHALL be removed

#### Scenario: Consecutive dots collapsed

- **WHEN** character removal produces "Tatort..Der...Schrei"
- **THEN** it SHALL be collapsed to "Tatort.Der.Schrei"

#### Scenario: Leading and trailing dots stripped

- **WHEN** normalization produces ".Tatort.Der.Schrei."
- **THEN** it SHALL be trimmed to "Tatort.Der.Schrei"

### Requirement: Quality tier mapping

ReleaseTitleBuilder SHALL map integer quality values to scene-style quality labels.

#### Scenario: Known quality values

- **WHEN** quality is 1080
- **THEN** the label SHALL be "1080p"

#### Scenario: Standard definition

- **WHEN** quality is 480
- **THEN** the label SHALL be "480p"

#### Scenario: Low quality

- **WHEN** quality is 270
- **THEN** the label SHALL be "270p"

#### Scenario: HD quality

- **WHEN** quality is 720
- **THEN** the label SHALL be "720p"
