# Scene Release (Delta)

## MODIFIED Requirements

### Requirement: ForMovie factory resolves movie releases

`SceneRelease` SHALL provide a static `ForMovie(EnrichedItem item, string? mediaName)` method that constructs a SceneRelease for movie content. The identifier SHALL be the enriched movie year from `Identity.Year` when available, falling back to the broadcast year from `Source.AiredAt`. When the resolved `EpisodeTitle` equals `MediaName` (ordinal, case-insensitive), `EpisodeTitle` SHALL be set to empty string to prevent title stuttering.

#### Scenario: Movie with enriched year
- **WHEN** ForMovie is called with Identity.Year=1995 and Source.AiredAt=2026-09-27
- **THEN** Identifier SHALL be "1995" (the TMDB year, not the broadcast year)

#### Scenario: Movie without enrichment falls back to broadcast year
- **WHEN** ForMovie is called with Identity.Year=null and Source.AiredAt=2024-03-15
- **THEN** Identifier SHALL be "2024"

#### Scenario: Movie with no year data
- **WHEN** ForMovie is called with Identity.Year=null and Source.AiredAt=null
- **THEN** Identifier SHALL be null

#### Scenario: Movie title deduplicated when episode title matches media name
- **WHEN** ForMovie is called with MediaName="Schachnovelle" and the resolved EpisodeTitle is also "Schachnovelle"
- **THEN** EpisodeTitle SHALL be empty string, producing "Schachnovelle.2021.GERMAN.1080p.WEB.h264-FunkArr" (no stutter)

#### Scenario: Movie title preserved when episode title differs from media name
- **WHEN** ForMovie is called with MediaName="Der Alte" and EpisodeTitle="Todfeinde"
- **THEN** EpisodeTitle SHALL remain "Todfeinde", producing "Der.Alte.2024.Todfeinde.GERMAN.1080p.WEB.h264-FunkArr"
