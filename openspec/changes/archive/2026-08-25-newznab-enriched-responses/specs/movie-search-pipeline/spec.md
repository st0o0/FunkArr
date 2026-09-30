## MODIFIED Requirements

### Requirement: Movie release title with year

MovieSearchActor results SHALL use `ReleaseTitleBuilder.BuildMovie` for release title generation. When the match provides a year, it SHALL be included. The movie name SHALL come from resolved match data when available, falling back to the search query topic.

#### Scenario: Movie with resolved year
- **WHEN** movie matching resolves "Petrocelli" with year=2024, quality=HD1080
- **THEN** the release title SHALL be `Petrocelli.2024.GERMAN.1080p.WEB.h264-FA`

#### Scenario: Movie without year
- **WHEN** movie matching has no year information
- **THEN** the release title SHALL use the fallback format with air date: `Petrocelli.2026.08.10.GERMAN.1080p.WEB.h264-FA`
