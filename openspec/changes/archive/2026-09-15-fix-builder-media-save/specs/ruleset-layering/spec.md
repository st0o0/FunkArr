## MODIFIED Requirements

### Requirement: Media uses local-wins semantics
When merging, the system SHALL use the local `Media` value if present, otherwise fall back to the community value. Per-field merge SHALL apply: each field (`TvdbId`, `ImdbId`, `TmdbId`, `Name`, `Type`) SHALL use the local value if non-null, otherwise fall back to the community value.

#### Scenario: Local overrides media
- **WHEN** community has media with tvdbId 83214 and local has media with tvdbId 99999
- **THEN** the effective media SHALL have tvdbId 99999

#### Scenario: Local has no media, inherits community
- **WHEN** community has media with tvdbId 83214 and local has no media (null)
- **THEN** the effective media SHALL have tvdbId 83214

#### Scenario: Name preserved during merge
- **WHEN** community has media with name "Tatort" and local has media without name
- **THEN** the effective media SHALL have name "Tatort"

#### Scenario: Type preserved during merge
- **WHEN** community has media with type "show" and local has media without type
- **THEN** the effective media SHALL have type "show"

#### Scenario: Local overrides name
- **WHEN** community has media with name "Tatort" and local has media with name "Tatort Custom"
- **THEN** the effective media SHALL have name "Tatort Custom"
