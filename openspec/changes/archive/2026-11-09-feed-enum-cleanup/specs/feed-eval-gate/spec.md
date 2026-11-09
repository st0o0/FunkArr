## ADDED Requirements

### Requirement: Typed rejection reasons

`FeedGate` SHALL use a `RejectionReason` enum from `FunkArr.Messages.Feed`
instead of string constants for rejection reasons. The enum SHALL have members
`Unknown = 0, LowConfidence = 1, MissingEpisode = 2, MissingId = 3,
UnknownSource = 4, MissingTimestamp = 5`.

#### Scenario: GateResult uses RejectionReason enum

- **WHEN** `GateResult.RejectedByReason` is inspected
- **THEN** its type SHALL be `IReadOnlyDictionary<RejectionReason, int>`

#### Scenario: FeedGate returns enum values

- **WHEN** a result is rejected for low confidence
- **THEN** the rejection reason SHALL be `RejectionReason.LowConfidence`

#### Scenario: FeedGate string constants removed

- **WHEN** `FeedGate` is inspected
- **THEN** it SHALL NOT contain string constants `LowConfidence`, `MissingEpisode`, `MissingId`, `UnknownSource`, or `MissingTimestamp`

#### Scenario: GroupResult uses RejectionReason enum

- **WHEN** `GroupResult` in `FeedEvalMessages` is inspected
- **THEN** `RejectedByReason` SHALL be `IReadOnlyDictionary<RejectionReason, int>?`

#### Scenario: Telemetry tag uses enum ToString

- **WHEN** rejection metrics are recorded in `FeedEvalActor`
- **THEN** the `reason` tag SHALL use the enum member name (e.g. `"LowConfidence"`)

### Requirement: RejectionReason persistence enum

A `PersistedRejectionReason` enum SHALL exist in `FunkArr.Persistence` with
members `Unknown = 0, LowConfidence = 1, MissingEpisode = 2, MissingId = 3,
UnknownSource = 4, MissingTimestamp = 5` for forward-compatibility.

#### Scenario: PersistedRejectionReason follows conventions

- **WHEN** `PersistedRejectionReason` is inspected
- **THEN** it SHALL have `Unknown = 0` and explicit int values for all members
