# feed-eval-gate Specification

## Purpose

Defines evaluation and gating of matched groups via FeedCrawlPageActor inline processing and FeedGate.

## Requirements

### Requirement: Typed rejection reasons

`FeedGate` SHALL use a `RejectionReason` enum from `FunkArr.Messages.Feed` instead of string constants for rejection reasons. The enum SHALL have members `Unknown = 0, LowConfidence = 1, MissingEpisode = 2, MissingId = 3, UnknownSource = 4, MissingTimestamp = 5`.

#### Scenario: GateResult uses RejectionReason enum

- **WHEN** `GateResult.RejectedByReason` is inspected
- **THEN** its type SHALL be `IReadOnlyDictionary<RejectionReason, int>`

#### Scenario: FeedGate returns enum values

- **WHEN** a result is rejected for low confidence
- **THEN** the rejection reason SHALL be `RejectionReason.LowConfidence`

#### Scenario: Telemetry tag uses enum ToString

- **WHEN** rejection metrics are recorded
- **THEN** the `reason` tag SHALL use the enum member name (e.g. `"LowConfidence"`)

### Requirement: Evaluation and gating inside page actor

Evaluation (Ask to TvEvaluationRegion / MovieEvaluationRegion) and gating (FeedGate.Evaluate) SHALL be performed inline by FeedCrawlPageActor during its Evaluating phase. FeedEvalActor SHALL be removed. The page actor groups items by ruleset, fires parallel evaluation Asks, collects results, and applies FeedGate per group — all within a single pool routee.

#### Scenario: Per-group evaluation

- **WHEN** a page has items matching 3 different rulesets
- **THEN** FeedCrawlPageActor SHALL fire 3 evaluation Asks (one per group)
- **AND** collect all 3 results before applying FeedGate per group
- **AND** aggregate accepted entries into the PageProcessed response

#### Scenario: Movie vs TV routing

- **WHEN** a group has MediaType.Movie
- **THEN** FeedCrawlPageActor SHALL Ask MovieEvaluationRegion with EvaluateMovies
- **WHEN** a group has MediaType.Show
- **THEN** FeedCrawlPageActor SHALL Ask TvEvaluationRegion with EvaluateSeries

#### Scenario: No stale-sender risk

- **WHEN** FeedCrawlPageActor is in Evaluating state
- **THEN** it SHALL NOT accept new ProcessPage messages
- **AND** the captured sender reference from the original ProcessPage SHALL be used for the reply

### Requirement: RejectionReason persistence enum

A `PersistedRejectionReason` enum SHALL exist in `FunkArr.Persistence` with members `Unknown = 0, LowConfidence = 1, MissingEpisode = 2, MissingId = 3, UnknownSource = 4, MissingTimestamp = 5` for forward-compatibility.

#### Scenario: PersistedRejectionReason follows conventions

- **WHEN** `PersistedRejectionReason` is inspected
- **THEN** it SHALL have `Unknown = 0` and explicit int values for all members
