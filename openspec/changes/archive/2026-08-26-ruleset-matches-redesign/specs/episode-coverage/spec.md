## ADDED Requirements

### Requirement: Episode coverage tracking in ShowActor
The `ShowActor` SHALL maintain a `HashSet<(int Season, int Episode)>` of all episodes that have ever been successfully matched, plus a `Dictionary<(int Season, int Episode), DateTime>` recording the last-seen timestamp for each matched episode. When a match evaluation produces matched items with season/episode info, any newly seen (season, episode) pairs SHALL be persisted via an `EpisodeMatched` event.

#### Scenario: First match of an episode
- **WHEN** a match evaluation matches item "Tatort: Der letzte Schrei" to S01E1245 for the first time
- **THEN** the actor SHALL persist an `EpisodeMatched` event with Season=1, Episode=1245, Timestamp=now and add (1, 1245) to the matched episodes set

#### Scenario: Repeated match of same episode
- **WHEN** a match evaluation matches S01E1245 again and it is already in the matched episodes set
- **THEN** the actor SHALL update the last-seen timestamp in state but SHALL NOT persist a new `EpisodeMatched` event

#### Scenario: Recovery restores coverage
- **WHEN** the ShowActor recovers from its journal
- **THEN** the matched episodes set and last-seen timestamps SHALL be restored from replayed `EpisodeMatched` events

### Requirement: Episode coverage tracking in MovieActor
The `MovieActor` SHALL track whether the movie has ever been successfully matched, recording the first-matched and last-seen timestamps.

#### Scenario: Movie first matched
- **WHEN** a match evaluation produces a successful movie match for the first time
- **THEN** the actor SHALL persist an `EpisodeMatched` event with a flag indicating movie-level match and Timestamp=now

#### Scenario: Movie matched again
- **WHEN** the movie is matched again and already marked as matched
- **THEN** the actor SHALL update the last-seen timestamp without persisting a new event

### Requirement: Episode coverage API endpoint
The system SHALL expose `GET /api/v1/rulesets/{tvdbId}/coverage` returning the episode coverage data for a show.

#### Scenario: Coverage for a show with matches
- **WHEN** `GET /api/v1/rulesets/83214/coverage` is called and ShowActor(83214) has matched 142 of 148 known TVDB episodes
- **THEN** the endpoint SHALL return `{ totalKnownEpisodes: 148, matchedEpisodes: 142, coveragePercent: 95.9, seasons: [...] }` where each season lists episodes with matched/unmatched status and last-seen timestamps

#### Scenario: Coverage for a show with no matches
- **WHEN** `GET /api/v1/rulesets/329324/coverage` is called and no episodes have been matched
- **THEN** the endpoint SHALL return `{ totalKnownEpisodes: N, matchedEpisodes: 0, coveragePercent: 0.0, seasons: [...] }` with all episodes marked as unmatched

#### Scenario: Coverage for unknown show
- **WHEN** `GET /api/v1/rulesets/999999/coverage` is called for a non-existent show
- **THEN** the endpoint SHALL return HTTP 404

#### Scenario: Coverage requires TVDB data
- **WHEN** `GET /api/v1/rulesets/{tvdbId}/coverage` is called and the ShowActor has matched episodes but TVDB episode data is not yet cached
- **THEN** the actor SHALL resolve TVDB data via the gateway before responding, to provide the full episode list for coverage calculation

### Requirement: Movie coverage API endpoint
The system SHALL expose `GET /api/v1/rulesets/movies/{id}/coverage` returning movie-level coverage (matched yes/no, first-matched, last-seen).

#### Scenario: Movie coverage matched
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/coverage` is called and the movie has been matched
- **THEN** the endpoint SHALL return `{ matched: true, firstMatched: "...", lastSeen: "..." }`

#### Scenario: Movie coverage not matched
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/coverage` is called and the movie has never been matched
- **THEN** the endpoint SHALL return `{ matched: false }`
