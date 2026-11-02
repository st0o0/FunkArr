## Purpose

Lets Sonarr and Radarr grab new Mediathek releases automatically through their RSS sync, by collecting new Mediathek entries, evaluating them like a search, and serving the confident matches as a stable, pageable Newznab feed.

## ADDED Requirements

### Requirement: Mediathek messages carry Filmliste timestamps
`MediathekItem` SHALL carry `FilmlisteTimestamp` (when the entry first appeared in the Filmliste) and `QueryMediathekCompleted` SHALL carry `ListTimestamp` (the `queryInfo.filmlisteTimestamp` of the response, i.e. the last completed Filmliste update). Missing, empty or non-numeric values SHALL map to 0; numeric strings SHALL be parsed.

#### Scenario: Numeric value
- **WHEN** a MediathekViewWeb result has `"filmlisteTimestamp": 1761800000`
- **THEN** the mapped `FilmlisteTimestamp` SHALL be 1761800000

#### Scenario: String and empty values
- **WHEN** a result has `"filmlisteTimestamp": "1761800000"`, `""` or no such field
- **THEN** the mapped value SHALL be 1761800000, 0 and 0 respectively

### Requirement: Search source Feed
`SearchSource` SHALL include `Feed` for searches started by the feed ingest. The persisted and API counterparts SHALL include the same value, so match history entries from the feed can be told apart from user searches.

#### Scenario: Feed source in match history
- **WHEN** the feed ingest evaluates entries for a ruleset
- **THEN** the recorded match history entry SHALL have source `Feed`

### Requirement: Feed configuration
The system SHALL read `FunkArr:Feed` with `Enabled` (default `true`), `Interval` (default 15 minutes), `InitialLookback` (default 48 hours), `PageSize` (default 100), `MaxPages` (default 20), `Retention` (default 14 days), `MinConfidence` (default 0.7) and `MaxConcurrency` (default 4). With `Enabled=false` no ingest SHALL run.

#### Scenario: Defaults
- **WHEN** no `FunkArr:Feed` section is configured
- **THEN** the feed SHALL be enabled with the default values above

#### Scenario: Disabled
- **WHEN** `FunkArr__Feed__Enabled=false` is configured
- **THEN** no MediathekViewWeb query SHALL be sent by the feed ingest

### Requirement: Ingest runs on a timer without overlap
The feed ingest SHALL run once shortly after startup and then again `Interval` after the previous run has finished. A new run SHALL NOT start while a run is in progress. A manual crawl request SHALL start a run immediately unless one is in progress.

#### Scenario: Next run scheduled after completion
- **WHEN** a run finishes
- **THEN** the next run SHALL be scheduled `Interval` later

#### Scenario: Manual crawl during a run
- **WHEN** a manual crawl is requested while a run is in progress
- **THEN** no second run SHALL start

### Requirement: Ingest detects new entries by Filmliste timestamp
Each run SHALL first read the timestamp L of the last completed Filmliste update. If L equals the stored cursor, the run SHALL end without further queries. Otherwise it SHALL page through entries sorted by `filmlisteTimestamp` descending and collect entries with cursor < `filmlisteTimestamp` ≤ L, stopping at the first older entry, at a page that is not full, or after `MaxPages`.

#### Scenario: Nothing new
- **WHEN** L equals the cursor
- **THEN** the run SHALL send exactly one MediathekViewWeb query

#### Scenario: Update still being indexed
- **WHEN** a page contains entries with `filmlisteTimestamp` greater than L
- **THEN** those entries SHALL be skipped in this run

#### Scenario: Entries without timestamp
- **WHEN** an entry has no `filmlisteTimestamp`
- **THEN** it SHALL NOT be treated as new

#### Scenario: First run
- **WHEN** no cursor exists yet
- **THEN** the run SHALL collect entries with `filmlisteTimestamp` within `InitialLookback` before L

#### Scenario: Duplicates across pages
- **WHEN** the same entry appears on two pages of one run
- **THEN** it SHALL be processed once

#### Scenario: Gap
- **WHEN** `MaxPages` is reached before an entry at or below the cursor
- **THEN** the collected entries SHALL be processed and the run SHALL be reported as having a gap

### Requirement: Cursor advances only after successful publishing
The cursor SHALL be set to L and persisted only after every ruleset group of the run has been processed. If a MediathekViewWeb query or a group fails, the cursor SHALL stay unchanged so that the next run repeats the window. After three consecutive failed runs, the cursor SHALL advance anyway and the run SHALL be reported as having a gap.

#### Scenario: Group failure
- **WHEN** the evaluation of one ruleset group fails
- **THEN** the other groups SHALL still be published and the cursor SHALL stay unchanged

#### Scenario: Restart
- **WHEN** FunkArr restarts
- **THEN** the cursor SHALL be restored and the next run SHALL collect everything since it

### Requirement: Entries are grouped by ruleset
New entries SHALL be matched to registered rulesets by topic or alias, case-insensitively. Each group SHALL be evaluated as a TV or movie search depending on the ruleset's media type, defaulting to TV. Entries without a matching ruleset SHALL NOT be published and SHALL be counted.

#### Scenario: Alias match
- **WHEN** an entry's topic equals an alias of ruleset "tatort"
- **THEN** it SHALL be evaluated in the group of ruleset "tatort"

#### Scenario: Unknown topic
- **WHEN** an entry's topic matches no ruleset
- **THEN** it SHALL be counted as unmatched and not evaluated

### Requirement: Feed evaluations use the search pipeline
Each group SHALL be evaluated by the same TV or movie search pipeline as interactive searches, with search source `Feed`, starting after the MediathekViewWeb query step. Scoring, enrichment, the variant policy and match history recording SHALL apply unchanged.

#### Scenario: Same release title as search
- **WHEN** an entry is evaluated by the feed and found by an interactive search
- **THEN** both SHALL produce the same release title and attributes

### Requirement: Publishing gate
A release SHALL be published only if its score is at least `MinConfidence` and, for TV, it has season and episode, or, for movies, it has a TMDB or IMDB ID. Each release SHALL get a stable ID derived from channel and video URL, and `FirstSeenAt` equal to the entry's `filmlisteTimestamp`.

#### Scenario: Unmatched release
- **WHEN** a TV release has no season or episode
- **THEN** it SHALL NOT be published

#### Scenario: Low confidence
- **WHEN** a release has a score below `MinConfidence`
- **THEN** it SHALL NOT be published

### Requirement: Feed store
The feed store SHALL keep published releases deduplicated by stable ID, keeping the first `FirstSeenAt`, persist them across restarts, and remove releases whose `FirstSeenAt` is older than `Retention`.

#### Scenario: Re-added entry
- **WHEN** a release with an existing stable ID is published again with a later `FirstSeenAt`
- **THEN** the stored release SHALL keep its original `FirstSeenAt` and no second entry SHALL exist

#### Scenario: Retention
- **WHEN** a release is older than `Retention`
- **THEN** it SHALL no longer be returned

### Requirement: RSS sync requests are served from the feed
A Newznab request without search criteria SHALL be answered from the feed store, ordered by `FirstSeenAt` descending, filtered by the request's category, paged by `offset` and `limit`. Each item SHALL use the stable ID as GUID and `FirstSeenAt` as `pubDate`. If the feed has no releases for the request's category, the response SHALL contain the validation item instead.

#### Scenario: Sonarr RSS sync
- **WHEN** `t=tvsearch` without `q` is requested and the feed holds TV releases
- **THEN** the response SHALL list them newest first with `total` equal to the number of TV releases

#### Scenario: Stable GUID
- **WHEN** the same release is returned by two RSS requests
- **THEN** its GUID and `pubDate` SHALL be identical

#### Scenario: Empty feed
- **WHEN** the feed holds no movie releases and `t=movie` without `q` is requested
- **THEN** the response SHALL contain the validation item

### Requirement: Feed API and UI
`GET /api/feed` SHALL return the newest published releases and the ingest status (last run time, outcome, new entries, published releases, unmatched entries, gap). `POST /api/feed/crawl` SHALL request a manual crawl and return 202. The web UI SHALL show both on a Feed page.

#### Scenario: Feed status
- **WHEN** `GET /api/feed` is requested after a run
- **THEN** the response SHALL contain the run's outcome and counts

#### Scenario: Manual crawl
- **WHEN** `POST /api/feed/crawl` is requested
- **THEN** the response SHALL be 202 Accepted
