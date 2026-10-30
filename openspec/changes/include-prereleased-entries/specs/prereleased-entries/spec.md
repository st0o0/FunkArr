## Purpose

Includes Mediathek entries that are published before their broadcast time in searches and the internal Mediathek search, so that pre-released episodes can be found as soon as they are downloadable. This is fixed behaviour without a configuration switch.

## ADDED Requirements

### Requirement: Searches include pre-released entries
TV and movie searches SHALL always include entries published before their broadcast time by sending an additional, otherwise identical MediathekViewWeb query with `future=true` next to the regular `future=false` query (see `search-evaluation`). The internal Mediathek search endpoint and the feed ingest SHALL always query with `future=true`. There SHALL be no configuration option to disable this.

#### Scenario: TV search
- **WHEN** a TV search is executed
- **THEN** it SHALL send one MediathekViewWeb query with `future=false` and one with `future=true`

#### Scenario: TV search by resolved topic
- **WHEN** a TV search queries MediathekViewWeb with a topic resolved from a media ID
- **THEN** both queries SHALL use the resolved topic and one of them SHALL have `future=true`

#### Scenario: Movie search
- **WHEN** a movie search is executed
- **THEN** it SHALL send one MediathekViewWeb query with `future=false` and one with `future=true`

#### Scenario: Internal Mediathek search
- **WHEN** `GET /api/mediathek/search?q=Tatort` is requested
- **THEN** the MediathekViewWeb query SHALL have `future=true`

### Requirement: Pre-released entries keep their broadcast date
A pre-released entry SHALL be returned with its scheduled broadcast time as air date, so that date-based episode matching works the same as for broadcast entries.

#### Scenario: Air date of a pre-released entry
- **WHEN** a search returns an entry whose broadcast time is two days in the future
- **THEN** the result's air date SHALL be that future broadcast time
