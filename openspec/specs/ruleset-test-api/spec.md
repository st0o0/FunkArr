## Purpose

Ad-hoc scoring test endpoint and MediathekViewWeb search proxy for the RuleSet Studio debugger.

## Requirements

### Requirement: Ad-hoc scoring test endpoint
The system SHALL expose `POST /api/rulesets/test` that runs scoring against an ad-hoc matching config without persisting results. The request body SHALL contain `config` (object with `defaultConfidence` (float) and `rules` (array)) and `candidates` (array of candidate objects). Each candidate SHALL have `title` (string), `topic` (string), `channel` (string), `duration` (int, seconds), `quality` (int), `description` (string, nullable), and `timestamp` (long, unix seconds). The endpoint SHALL transform the config rules from JSON string format to the internal MatchingConfig representation using the same transformation logic as RuleSetMerger, construct ScoreCandidate objects from the candidate array, and send a TestScoreItems message to the MatchMagicManager with a generated RequestId and ScoringOrigin.Test. The response SHALL contain the full `itemTraces` array with the same structure as the scoring detail endpoint.

#### Scenario: Test with matching candidates
- **WHEN** `POST /api/rulesets/test` is called with a config containing a seasonAndEpisodeNumber rule and a candidate whose title contains season/episode patterns
- **THEN** the response is 200 with `itemTraces` containing one entry with `matched: true`, `matchedRuleId`, and `ruleTraces` showing the matched rule

#### Scenario: Test with no matches
- **WHEN** `POST /api/rulesets/test` is called with a config and candidates that don't match any rules
- **THEN** the response is 200 with `itemTraces` where all entries have `matched: false`

#### Scenario: Test with filter trace detail
- **WHEN** `POST /api/rulesets/test` is called and a candidate fails a filter condition
- **THEN** the response includes `ruleTraces` with `outcome: "filterFailed"` and `filterTrace` showing the field, op, expected value, actual value, and `passed: false`

#### Scenario: Test with identification trace detail
- **WHEN** `POST /api/rulesets/test` is called and a candidate passes filters but fails identification
- **THEN** the response includes `ruleTraces` with `outcome: "identificationFailed"` and `identificationTrace` showing the strategy and failure reason

#### Scenario: Test with empty candidates
- **WHEN** `POST /api/rulesets/test` is called with an empty `candidates` array
- **THEN** the response is 200 with an empty `itemTraces` array

#### Scenario: Test with empty rules
- **WHEN** `POST /api/rulesets/test` is called with a config containing no rules
- **THEN** the response is 200 with `itemTraces` where all entries have `matched: false` and empty `ruleTraces`

#### Scenario: Invalid strategy in test config
- **WHEN** `POST /api/rulesets/test` is called with a rule containing an unrecognized strategy string
- **THEN** the rule is skipped during scoring (same behavior as RuleSetMerger)

#### Scenario: Actor timeout
- **WHEN** `POST /api/rulesets/test` is called and the MatchMagicManager does not respond within the timeout
- **THEN** the response is 504

### Requirement: TestScoreItems message and handler
The MatchMagicManager SHALL handle a `TestScoreItems` message that carries an inline `MatchingConfig` and `ScoreCandidate[]` array. Unlike `ScoreItems` which looks up config by RuleSetId, `TestScoreItems` SHALL use the provided config directly. The manager SHALL forward the request to the scoring pool as an `ExecuteScoring` with `ScoringOrigin.Test`.

#### Scenario: TestScoreItems bypasses config lookup
- **WHEN** MatchMagicManager receives a TestScoreItems with an inline config
- **THEN** it uses the provided config instead of looking up by RuleSetId

#### Scenario: TestScoreItems uses Test origin
- **WHEN** MatchMagicManager forwards a TestScoreItems to the scoring pool
- **THEN** the ExecuteScoring message has `Origin: ScoringOrigin.Test`

### Requirement: ScoringOrigin includes Test value
The `ScoringOrigin` enum SHALL include a `Test` value for ad-hoc scoring requests. This value SHALL be used by the test endpoint to distinguish test runs from real scoring.

#### Scenario: Test origin value exists
- **WHEN** the ScoringOrigin enum is inspected
- **THEN** it SHALL include a `Test` member

### Requirement: Test scoring skips history recording
The MatchMagicActor SHALL NOT send `RecordScoringResult` to the history region when the `ScoringOrigin` is `Test`. All other scoring behavior (filter evaluation, identification, trace generation) SHALL remain identical.

#### Scenario: Test scoring produces traces but no history
- **WHEN** MatchMagicActor processes an ExecuteScoring with ScoringOrigin.Test
- **THEN** it SHALL return ScoreCompleted with full item traces to the sender
- **AND** it SHALL NOT send RecordScoringResult to the history region

#### Scenario: Non-test scoring still records history
- **WHEN** MatchMagicActor processes an ExecuteScoring with ScoringOrigin other than Test
- **THEN** it SHALL send RecordScoringResult to the history region as before

### Requirement: MediathekViewWeb search proxy endpoint
The system SHALL expose `GET /api/mediathek/search` that proxies search queries to the MediathekViewWeb API and returns results shaped as scoring candidates. The endpoint SHALL accept a required `q` (string) query parameter and an optional `limit` (int, default 20, max 100) parameter. The endpoint SHALL Ask the MediathekViewWebManager with a MediathekQuery and map the response items to a JSON array of candidate objects with fields `title`, `topic`, `channel`, `duration` (seconds), `quality`, `description`, and `timestamp` (unix seconds).

#### Scenario: Search with results
- **WHEN** `GET /api/mediathek/search?q=tatort` is called and MediathekViewWeb returns 15 items
- **THEN** the response is 200 with a JSON array of 15 candidate objects

#### Scenario: Search with limit
- **WHEN** `GET /api/mediathek/search?q=tatort&limit=5` is called
- **THEN** the query is sent with a size limit of 5 and the response contains at most 5 candidates

#### Scenario: Search with no results
- **WHEN** `GET /api/mediathek/search?q=xyznonexistent` is called and MediathekViewWeb returns 0 items
- **THEN** the response is 200 with an empty JSON array

#### Scenario: Missing query parameter
- **WHEN** `GET /api/mediathek/search` is called without the `q` parameter
- **THEN** the response is 400

#### Scenario: MediathekViewWeb error
- **WHEN** `GET /api/mediathek/search?q=tatort` is called and MediathekViewWebManager responds with MediathekQueryFailed
- **THEN** the response is 502 with an error message

#### Scenario: Actor timeout
- **WHEN** `GET /api/mediathek/search?q=tatort` is called and MediathekViewWebManager does not respond within the timeout
- **THEN** the response is 504

### Requirement: Proxy endpoint candidate mapping
The proxy endpoint SHALL map MediathekItem fields to candidate fields: `title` from item title, `topic` from item topic, `channel` from item channel, `duration` from item duration (already in seconds), `quality` from the highest available quality, `description` from item description (nullable), and `timestamp` from item timestamp (unix seconds). Fields not present in the MediathekItem SHALL default to empty string for strings, 0 for numbers, and null for nullable fields.

#### Scenario: Full item mapping
- **WHEN** a MediathekItem has title "Tatort: Fangschuss", topic "Tatort", channel "Das Erste", duration 5400, description "Ein Fall...", and timestamp 1693000000
- **THEN** the candidate object has matching field values

#### Scenario: Item with missing description
- **WHEN** a MediathekItem has no description
- **THEN** the candidate object has `description: null`
