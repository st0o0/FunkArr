## MODIFIED Requirements

### Requirement: Scoring messages use primitive candidates

ScoreItems and ScoreCompleted SHALL use flat primitive records for MatchMagic interaction. ScoreItems SHALL include RequestId and ScoringOrigin for correlation and provenance. ScoreCompleted SHALL include RequestId for response correlation.

#### Scenario: ScoreItems record

- **WHEN** items are submitted for scoring
- **THEN** ScoreItems SHALL contain: RequestId (Guid), RuleSetId (string), Origin (ScoringOrigin), Candidates (ScoreCandidate[])

#### Scenario: ScoringOrigin record

- **WHEN** a scoring origin is specified
- **THEN** ScoringOrigin SHALL contain: Source (string), Query (string)

#### Scenario: ScoreCandidate record

- **WHEN** a candidate is prepared for scoring
- **THEN** ScoreCandidate SHALL contain: Title (string), Topic (string), Channel (string), Duration (int), Quality (int), Description (string?), Timestamp (long)

#### Scenario: ScoreCompleted record

- **WHEN** scoring completes
- **THEN** ScoreCompleted SHALL contain: RequestId (Guid), Results (ScoredItem[])

#### Scenario: ScoredItem record

- **WHEN** a scored item is returned
- **THEN** ScoredItem SHALL contain: Index (int) referencing the input position, Score (double), Matched (bool)

### Requirement: SearchWorker resolves ruleSetId via RuleSetResolver

TvSearchWorker and MovieSearchWorker SHALL query the RuleSetResolver for the ruleSetId before sending ScoreItems to MatchMagicManager. They SHALL generate a RequestId, populate ScoringOrigin with the source ("sonarr", "radarr", or "ui") and original query, and include Description and Timestamp from the MediathekItem in ScoreCandidate.

#### Scenario: Successful resolution and scoring

- **WHEN** SearchWorker receives a search command, it asks RuleSetResolver for the ruleSetId
- **THEN** after receiving RuleSetResolved, it creates ScoreItems with a new RequestId, ScoringOrigin(source, query), and candidates including Description and Timestamp from the raw MediathekItems

#### Scenario: Resolution fails

- **WHEN** RuleSetResolver responds with RuleSetNotFound
- **THEN** SearchWorker returns search results with all items scored at 0.0
