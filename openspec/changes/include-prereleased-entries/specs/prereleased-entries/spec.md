## Purpose

Makes Mediathek entries that are published before their broadcast time available to Sonarr and Radarr through the RSS feed, while searches stay limited to aired entries. Sonarr and Radarr only search for aired episodes and available movies, so pre-released entries are only useful in the RSS sync.

## ADDED Requirements

### Requirement: Feed ingest includes pre-released entries
The feed ingest SHALL query MediathekViewWeb with `future=true`, so that entries published before their broadcast time enter the feed as soon as they appear in the Filmliste.

#### Scenario: Pre-released episode
- **WHEN** a new Filmliste update contains a Tatort episode whose broadcast is two days in the future
- **THEN** the feed ingest SHALL collect it and, if it passes evaluation, publish it to the feed

### Requirement: Searches exclude upcoming entries
TV searches, movie searches and the internal Mediathek search SHALL query MediathekViewWeb with `future=false`.

#### Scenario: TV search
- **WHEN** a TV search for `Tatort` is executed
- **THEN** the MediathekViewWeb query SHALL have `future=false`

#### Scenario: Internal Mediathek search
- **WHEN** `GET /api/mediathek/search?q=Tatort` is requested
- **THEN** the MediathekViewWeb query SHALL have `future=false`

### Requirement: Implausible broadcast dates are dropped
The feed ingest SHALL drop entries whose broadcast time is more than 90 days in the future.

#### Scenario: Data error
- **WHEN** an ingest run collects an entry 1159 days in the future
- **THEN** it SHALL NOT be evaluated or published

### Requirement: Pre-released entries keep their broadcast date
A pre-released entry SHALL keep its scheduled broadcast time as air date, so that date-based episode matching works the same as for broadcast entries and Sonarr's air date restriction can hold it back.

#### Scenario: Air date of a pre-released entry
- **WHEN** the feed publishes an entry whose broadcast time is two days in the future
- **THEN** the release's air date SHALL be that future broadcast time
