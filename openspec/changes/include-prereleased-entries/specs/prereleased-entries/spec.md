## Purpose

Includes Mediathek entries that are published before their broadcast time in every MediathekViewWeb query, so that pre-released episodes can be found as soon as they are downloadable, without letting them crowd aired episodes out of date-sorted results. This is fixed behaviour without a configuration switch.

## ADDED Requirements

### Requirement: Queries include pre-released entries
Every query the `MediathekViewWebManager` sends to MediathekViewWeb SHALL use `future=true`. `QueryMediathek` SHALL NOT carry a future flag, and there SHALL be no configuration option to disable this.

#### Scenario: Search query
- **WHEN** a TV search sends `QueryMediathek` for topic `Tatort`
- **THEN** the request to MediathekViewWeb SHALL have `future=true`

#### Scenario: Feed query
- **WHEN** the feed ingest queries by `filmlisteTimestamp`
- **THEN** the request to MediathekViewWeb SHALL have `future=true`

### Requirement: Aired window is kept for date-sorted queries
For a `QueryMediathek` sorted by `timestamp` descending, the manager SHALL return at least `Size` entries aired up to one hour from now, unless the results end or 3 pages have been fetched. When a page contains fewer aired entries than `Size`, the manager SHALL fetch the next page (offset advanced by `Size`) and SHALL reply once with all collected entries up to the `Size`-th aired entry, de-duplicated by video URL. Queries with any other sort order SHALL be answered from a single request.

#### Scenario: Few upcoming entries
- **WHEN** the first page of 50 holds 4 upcoming and 46 aired entries and the next page would hold aired entries
- **THEN** the manager SHALL fetch the second page and reply with the 4 upcoming and 50 aired entries

#### Scenario: No upcoming entries
- **WHEN** the first page of 50 holds 50 aired entries
- **THEN** the manager SHALL reply after one request

#### Scenario: Flooded daily show
- **WHEN** a page of 50 holds 48 upcoming and 2 aired entries
- **THEN** the manager SHALL fetch further pages until 50 aired entries are collected or 3 pages were fetched

#### Scenario: Feed sort order
- **WHEN** a query is sorted by `filmlisteTimestamp`
- **THEN** the manager SHALL send exactly one request

#### Scenario: Queue full on a follow-up page
- **WHEN** the queue is full while offering the second page
- **THEN** the manager SHALL reply `QueryMediathekQueueFull` once

### Requirement: Implausible broadcast dates are dropped
The manager SHALL drop entries whose broadcast time is more than 90 days in the future.

#### Scenario: Data error
- **WHEN** MediathekViewWeb returns an entry 1159 days in the future
- **THEN** it SHALL NOT be part of the response

### Requirement: Pre-released entries keep their broadcast date
A pre-released entry SHALL be returned with its scheduled broadcast time as air date, so that date-based episode matching works the same as for broadcast entries.

#### Scenario: Air date of a pre-released entry
- **WHEN** a search returns an entry whose broadcast time is two days in the future
- **THEN** the result's air date SHALL be that future broadcast time
