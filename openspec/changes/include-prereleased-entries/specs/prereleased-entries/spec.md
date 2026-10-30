## Purpose

Controls whether Mediathek entries that are published before their broadcast time are included in searches, so that pre-released episodes can be found as soon as they are downloadable.

## ADDED Requirements

### Requirement: Include-future option
The system SHALL read `FunkArr:Search:IncludeFuture` as a boolean. When it is not configured, it SHALL default to `true`. Configuration changes SHALL apply to new searches without a restart.

#### Scenario: Default
- **WHEN** `FunkArr:Search:IncludeFuture` is not configured
- **THEN** the effective value SHALL be `true`

#### Scenario: Disabled
- **WHEN** `FunkArr__Search__IncludeFuture=false` is configured
- **THEN** the effective value SHALL be `false`

### Requirement: Searches honor the include-future option
Every MediathekViewWeb query built for a TV search, a movie search or the internal Mediathek search endpoint SHALL send `future` equal to the effective include-future value.

#### Scenario: TV search includes pre-released entries
- **WHEN** a TV search is executed with the default configuration
- **THEN** the MediathekViewWeb query SHALL have `future=true`

#### Scenario: TV search by resolved topic
- **WHEN** a TV search queries MediathekViewWeb with a topic resolved from a media ID and the default configuration
- **THEN** the MediathekViewWeb query SHALL have `future=true`

#### Scenario: Movie search
- **WHEN** a movie search is executed with `IncludeFuture=false`
- **THEN** the MediathekViewWeb query SHALL have `future=false`

#### Scenario: Internal Mediathek search
- **WHEN** `GET /api/mediathek/search?q=Tatort` is requested with the default configuration
- **THEN** the MediathekViewWeb query SHALL have `future=true`

### Requirement: Pre-released entries keep their broadcast date
A pre-released entry SHALL be returned with its scheduled broadcast time as air date, so that date-based episode matching works the same as for broadcast entries.

#### Scenario: Air date of a pre-released entry
- **WHEN** a search returns an entry whose broadcast time is two days in the future
- **THEN** the result's air date SHALL be that future broadcast time
