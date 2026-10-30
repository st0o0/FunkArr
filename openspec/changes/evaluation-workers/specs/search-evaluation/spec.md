## Purpose

Defines how Mediathek entries are evaluated into releases by dedicated evaluation workers, shared by searches and the RSS feed, and how searches combine aired and pre-released entries.

## ADDED Requirements

### Requirement: Dedicated evaluation workers
The system SHALL evaluate Mediathek entries in sharded evaluation entities (`TvEvaluationWorker`, `MovieEvaluationWorker`) keyed by the evaluation ID. An evaluation SHALL resolve the ruleset from the first entry's topic unless the command carries one, score with the variant policy, enrich when media IDs are known, record history and reply `EvaluateSeriesCompleted` / `EvaluateMoviesCompleted`.

#### Scenario: Evaluation without a resolved ruleset
- **WHEN** `EvaluateSeries` with one Tatort entry and no ruleset is sent to the TV evaluation region
- **THEN** the worker SHALL ask the ruleset resolver for topic `Tatort`
- **AND** after scoring it SHALL reply `EvaluateSeriesCompleted` with the evaluation ID and the generated releases

#### Scenario: Evaluation with a resolved ruleset
- **WHEN** `EvaluateSeries` carries a resolved ruleset
- **THEN** the worker SHALL NOT ask the ruleset resolver and SHALL score with that ruleset

#### Scenario: No ruleset found
- **WHEN** the ruleset resolver replies that no ruleset matches
- **THEN** the worker SHALL reply `EvaluateSeriesCompleted` with unscored releases

#### Scenario: Scoring fails
- **WHEN** scoring fails
- **THEN** the worker SHALL reply `EvaluateSeriesFailed` with the evaluation ID and the cause

#### Scenario: No entries
- **WHEN** `EvaluateMovies` carries no entries
- **THEN** the worker SHALL reply `EvaluateMoviesCompleted` with no releases and total 0

### Requirement: Search workers delegate evaluation
A search worker SHALL resolve the ruleset itself only when the request carries a media ID, query MediathekViewWeb (retrying queue-full responses), and hand the entries to the matching evaluation region together with the media IDs, season and resolved ruleset. It SHALL reply to the search with the evaluated releases under its own search ID.

#### Scenario: Text search
- **WHEN** a TV search for `Tatort` without media IDs returns one Mediathek entry
- **THEN** the search worker SHALL send `EvaluateSeries` with that entry and no ruleset to the TV evaluation region
- **AND** reply `SearchSeriesCompleted` with the search ID and the releases from `EvaluateSeriesCompleted`

#### Scenario: Search by media ID
- **WHEN** a TV search with TVDB ID 83214 resolves the ruleset `tatort`
- **THEN** the `EvaluateSeries` command SHALL carry the resolved ruleset and TVDB ID 83214

#### Scenario: Empty query result
- **WHEN** MediathekViewWeb returns no entries
- **THEN** the search worker SHALL reply with no releases and SHALL NOT send an evaluation

#### Scenario: Evaluation fails
- **WHEN** the evaluation replies `EvaluateMoviesFailed`
- **THEN** the search worker SHALL reply `SearchMovieFailed` with the search ID and the cause

### Requirement: Feed evaluations carry ruleset media IDs
The feed crawler SHALL send each ruleset group to the evaluation region of the group's media type and SHALL pass the media IDs registered for that ruleset (TVDB/IMDB for series, IMDB/TMDB for movies).

#### Scenario: Series group with IDs
- **WHEN** the feed groups entries for ruleset `tatort` registered with TVDB ID 76290 and IMDB ID `tt0806910`
- **THEN** the crawler SHALL send `EvaluateSeries` with source `Feed`, TVDB ID 76290 and IMDB ID `tt0806910` to the TV evaluation region

#### Scenario: Failed group evaluation
- **WHEN** the evaluation of a group replies `EvaluateSeriesFailed`
- **THEN** the feed run SHALL count that group as failed

### Requirement: Pre-released entries are merged into searches
A search SHALL send two otherwise identical MediathekViewWeb queries, one with `future=false` and one with `future=true`, and merge the responses: a queue-full response SHALL win, then an error, otherwise the entries SHALL be de-duplicated by video URL (HD, then normal, then low; channel, title and timestamp when no URL exists), keeping the first occurrence.

#### Scenario: Merged results
- **WHEN** the `future=false` query returns entry A and the `future=true` query returns entries B and A
- **THEN** the evaluation SHALL receive exactly A and B

#### Scenario: One query hits a full queue
- **WHEN** one of the two queries returns queue-full
- **THEN** the search SHALL retry both queries with backoff
