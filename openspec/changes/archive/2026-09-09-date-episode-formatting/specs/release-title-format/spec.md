# release-title-format (delta)

## MODIFIED Requirements

### Requirement: ReleaseTitleBuilder formats scene-style titles

FunkArr.Core SHALL define a `ReleaseTitleBuilder` static class with a `Build` method that produces scene-style release titles from media metadata. The format SHALL be compatible with Sonarr/Radarr title parsing.

#### Scenario: TV episode with episode only (no season)

- **WHEN** Build is called with metadata (Season null, Episode "312"), quality 720, category "tv"
- **THEN** the formatted identifier SHALL be `S01E312` (absolute episodes map to Season 01 for Sonarr compatibility)

#### Scenario: TV episode with high absolute number

- **WHEN** Build is called with metadata (Season null, Episode "1666"), quality 1080, category "tv"
- **THEN** the formatted identifier SHALL be `S01E1666`

#### Scenario: TV daily show with airdate only

- **WHEN** Build is called with topic "heute-show", title "heute-show vom 20. September 2024", metadata (AiredAt 2024-09-20), quality 480, category "tv"
- **THEN** the result SHALL be `heute-show.2024-09-20.heute-show.vom.20.September.2024.GERMAN.480p.WEB.h264-FunkArr`

#### Scenario: TV episode with season and episode

- **WHEN** Build is called with topic "Tatort", title "Der letzte Schrei", metadata (Season "01", Episode "05"), quality 720, category "tv"
- **THEN** the result SHALL be `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: TV episode with no metadata

- **WHEN** Build is called with topic "Tagesschau", title "Tagesschau 20 Uhr", metadata null, quality 720, category "tv"
- **THEN** the result SHALL be `Tagesschau.Tagesschau.20.Uhr.GERMAN.720p.WEB.h264-FunkArr`
