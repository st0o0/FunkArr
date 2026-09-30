# message-naming

## Purpose

The unified naming convention for all message types: commands, queries, responses, events, and config messages.

## ADDED Requirements

### Requirement: Command naming is VerbNoun

Commands SHALL be named VerbNoun with no suffix. Command + its response types SHALL live in one file named `VerbNoun.cs`.

#### Scenario: Search command
- **WHEN** a command initiates a series search
- **THEN** it SHALL be named `SearchSeries`

#### Scenario: File contains command and responses
- **WHEN** `SearchSeries.cs` is examined
- **THEN** it SHALL contain `SearchSeries`, `SearchSeriesResponse`, `SearchSeriesCompleted`, and `SearchSeriesFailed`

### Requirement: Command responses use Completed/Failed pattern

Each command SHALL have an `abstract record VerbNounResponse` with exactly two sealed cases: `VerbNounCompleted(...)` for success and `VerbNounFailed(Exception Cause)` for failure.

#### Scenario: SearchSeries responses
- **WHEN** `SearchSeriesResponse` is examined
- **THEN** it SHALL have `SearchSeriesCompleted` and `SearchSeriesFailed` as its only subtypes

#### Scenario: Ask uses per-command response type
- **WHEN** a caller sends SearchSeries
- **THEN** it SHALL use `Ask<SearchSeriesResponse>` with exhaustive pattern match on the two cases

### Requirement: Query naming uses Query prefix

Queries SHALL be named `QueryNoun`. Query + its response types SHALL live in one file named `QueryNoun.cs`.

#### Scenario: Query command
- **WHEN** a query fetches ruleset detail
- **THEN** it SHALL be named `QueryRuleSetDetail`

### Requirement: Query responses use Result/Failed pattern

Each query SHALL have an `abstract record NounResponse` with `NounResult(...)` for success and `NounFailed(Exception Cause)` for failure.

#### Scenario: RuleSetDetail responses
- **WHEN** `RuleSetDetailResponse` is examined
- **THEN** it SHALL have `RuleSetDetailResult` and `RuleSetDetailFailed` as its only subtypes

### Requirement: No standalone NotFound response types

There SHALL be no `*NotFound` message types. Not-found conditions SHALL be expressed as a `*Failed` response carrying an appropriate exception (e.g. `RuleSetNotFoundException`).

#### Scenario: RuleSet not found
- **WHEN** a QueryRuleSetDetail finds no matching ruleset
- **THEN** the actor SHALL return `RuleSetDetailFailed(new RuleSetNotFoundException(...))`

### Requirement: Events use past-tense naming

Persistence events SHALL use past-tense naming and live in FunkArr.Persistence/Events/. This convention is unchanged.

#### Scenario: Download event
- **WHEN** a download starts
- **THEN** the event SHALL be named `DownloadStarted`

### Requirement: Config messages use Noun naming

Fire-and-forget config messages SHALL use plain noun naming with no verb prefix.

#### Scenario: Matching config push
- **WHEN** a matching config is distributed
- **THEN** the message SHALL be named `MatchingConfig`
