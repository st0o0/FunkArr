# release-title-format

## Purpose

Scene-style release title construction from media metadata. Defines the MetadataSpec record for carrying identification results and the ReleaseTitleBuilder formatter that produces titles compatible with Sonarr/Radarr parsing.

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

FunkArr.Core SHALL define a `ReleaseTitleBuilder` static class with a `Build` method that produces scene-style release titles from media metadata. The format SHALL be compatible with Sonarr/Radarr title parsing.

#### Scenario: TV episode with season and episode

- **WHEN** Build is called with topic "Tatort", title "Der letzte Schrei", metadata (Season "01", Episode "05"), quality 720, category "tv"
- **THEN** the result SHALL be `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: TV daily show with airdate only

- **WHEN** Build is called with topic "heute-show", title "heute-show vom 20. September 2024", metadata (AiredAt 2024-09-20), quality 480, category "tv"
- **THEN** the result SHALL be `heute-show.2024-09-20.heute-show.vom.20.September.2024.GERMAN.480p.WEB.h264-FunkArr`

#### Scenario: TV episode with no metadata

- **WHEN** Build is called with topic "Tagesschau", title "Tagesschau 20 Uhr", metadata null, quality 720, category "tv"
- **THEN** the result SHALL be `Tagesschau.Tagesschau.20.Uhr.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: Movie with airdate (year extraction)

- **WHEN** Build is called with topic "Der Alte", title "Todfeinde", metadata (AiredAt 2024-03-15), quality 1080, category "movie"
- **THEN** the result SHALL be `Der.Alte.2024.Todfeinde.GERMAN.1080p.WEB.h264-FunkArr`

#### Scenario: Movie without airdate

- **WHEN** Build is called with topic "Polizeiruf 110", title "Blutige Fährte", metadata null, quality 720, category "movie"
- **THEN** the result SHALL be `Polizeiruf.110.Blutige.Faehrte.GERMAN.720p.WEB.h264-FunkArr`

### Requirement: Umlaut normalization

ReleaseTitleBuilder SHALL normalize German umlauts and special characters to ASCII equivalents before formatting.

#### Scenario: Standard umlaut replacement

- **WHEN** a title contains "Überführung"
- **THEN** it SHALL be normalized to "Ueberfuehrung"

#### Scenario: All umlaut mappings

- **WHEN** a title contains ä, ö, ü, Ä, Ö, Ü, ß
- **THEN** they SHALL be replaced with ae, oe, ue, Ae, Oe, Ue, ss respectively

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

### Requirement: Season and episode zero-padding

ReleaseTitleBuilder SHALL zero-pad season and episode numbers to at least 2 digits, preserving longer values.

#### Scenario: Single digit season and episode

- **WHEN** season is "1" and episode is "5"
- **THEN** the formatted S/E SHALL be "S01E05"

#### Scenario: Already padded values

- **WHEN** season is "01" and episode is "27"
- **THEN** the formatted S/E SHALL be "S01E27"

#### Scenario: Year-based season

- **WHEN** season is "2024" and episode is "27"
- **THEN** the formatted S/E SHALL be "S2024E27"

#### Scenario: Absolute episode number (no season)

- **WHEN** season is null and episode is "312"
- **THEN** the formatted identifier SHALL be "E312" (no season prefix)
