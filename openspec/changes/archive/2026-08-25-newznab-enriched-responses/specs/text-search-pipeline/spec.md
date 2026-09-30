## MODIFIED Requirements

### Requirement: Fallback title differentiation

TextSearchActor results SHALL carry `Title` (raw Mediathek episode title) and `Timestamp` (air date) through to the Newznab response. The controller SHALL use these fields via `ReleaseTitleBuilder.BuildFallback` to produce differentiated release titles even without RuleSet matching.

#### Scenario: Text search results are distinguishable
- **WHEN** a text search for "Tatort" returns 5 results with different Mediathek titles ("Köpfe", "Der letzte Schrei", "Stille Wasser", etc.)
- **THEN** each result SHALL have a unique release title like `Tatort.Koepfe.2026.08.17.GERMAN.720p.WEB.h264-FA`

#### Scenario: Browse results are distinguishable
- **WHEN** the RSS browse endpoint returns recent items
- **THEN** each item SHALL have a release title including the Mediathek episode title and air date

#### Scenario: Results with no meaningful title
- **WHEN** a text search result has Title equal to Topic (e.g. both "Petrocelli")
- **THEN** the release title SHALL omit the redundant title and use only topic + date: `Petrocelli.2026.08.10.GERMAN.1080p.WEB.h264-FA`
