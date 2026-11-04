## ADDED Requirements

### Requirement: TestScoringWorker is a per-request entity actor

The Scoring domain SHALL define a `TestScoringWorker` as a `ReceiveActor` entity registered via `WithEntityRegion<ITestScoringRegion>`. Each instance SHALL handle exactly one `TestScoreWithEnrichment` request: score via `ScoringManager`, optionally enrich via `EnrichmentManager`, reply to the original sender, and self-passivate.

#### Scenario: Worker scores and replies without enrichment

- **WHEN** a `TestScoreWithEnrichment` message arrives with no `EnrichmentConfig`
- **THEN** the worker SHALL send `TestScoreItems` to `IScoringManager`, receive `ScoreCompleted`, reply with `TestScoreWithEnrichmentCompleted` containing the item traces, and self-passivate

#### Scenario: Worker scores with enrichment for a TV show

- **WHEN** a `TestScoreWithEnrichment` message arrives with `EnrichmentConfig`, `MediaType = Show`, and a `TvdbId`
- **THEN** the worker SHALL first score via `IScoringManager`, then send `EnrichEpisodes` to `IEnrichmentManager`, merge enrichment results into item traces, reply with `TestScoreWithEnrichmentCompleted`, and self-passivate

#### Scenario: Worker scores with enrichment for a movie

- **WHEN** a `TestScoreWithEnrichment` message arrives with `EnrichmentConfig`, `MediaType = Movie`, and a `TmdbId` or `ImdbId`
- **THEN** the worker SHALL first score via `IScoringManager`, then send `EnrichMovies` to `IEnrichmentManager`, merge enrichment results into item traces, reply with `TestScoreWithEnrichmentCompleted`, and self-passivate

#### Scenario: Enrichment failure returns scoring results without enrichment

- **WHEN** the enrichment Ask times out or returns a failure response
- **THEN** the worker SHALL reply with `TestScoreWithEnrichmentCompleted` containing the scoring-only item traces (no enrichment traces) and self-passivate

#### Scenario: Scoring failure returns error

- **WHEN** the scoring Ask returns an unexpected response (not `ScoreCompleted`)
- **THEN** the worker SHALL reply with `TestScoreWithEnrichmentFailed` and self-passivate

### Requirement: TestScoringWorker uses PipeTo, not ReceiveAsync

The `TestScoringWorker` SHALL NOT use `ReceiveAsync`. It SHALL use `PipeTo` for async operations (scoring Ask, enrichment Ask) and `Become` for state transitions between waiting phases.

#### Scenario: Scoring Ask uses PipeTo

- **WHEN** the worker sends `TestScoreItems` to `IScoringManager`
- **THEN** it SHALL use `Ask` with `PipeTo` to deliver the response as a message, with `failure:` mapping to `TestScoreWithEnrichmentFailed`

#### Scenario: State transitions via Become

- **WHEN** the worker starts
- **THEN** it SHALL be in a "waiting for score" state; upon receiving `ScoreCompleted` with enrichment needed, it SHALL `Become` a "waiting for enrichment" state

### Requirement: TestScoringWorker self-passivates via Passivate message

After replying to the original sender, the worker SHALL request passivation by sending `new Passivate(PoisonPill.Instance)` to `Context.Parent`. It SHALL NOT call `Context.Stop(Self)` directly.

#### Scenario: Passivation after successful reply

- **WHEN** the worker has sent its response to the original sender
- **THEN** it SHALL send `Passivate(PoisonPill.Instance)` to `Context.Parent`

#### Scenario: Passivation after failure reply

- **WHEN** the worker has sent `TestScoreWithEnrichmentFailed` to the original sender
- **THEN** it SHALL send `Passivate(PoisonPill.Instance)` to `Context.Parent`

### Requirement: ITestScoringRegion registry key

FunkArr.Messages SHALL define `ITestScoringRegion` as a marker interface for the actor registry. The entity region SHALL be registered under this key in `AkkaSetupContainer`.

#### Scenario: Region registered in AkkaSetupContainer

- **WHEN** the actor system starts
- **THEN** `ITestScoringRegion` SHALL be registered via `WithEntityRegion<ITestScoringRegion>` with `EntityIdExtractor` and a passivation timeout of 30 seconds

### Requirement: IWithTestScoringId interface for test scoring messages

FunkArr.Messages SHALL define `IWithTestScoringId` inheriting from `IWithId` and `IWithEntityId`. It SHALL expose a `Guid TestScoringId` property with default implementations for `IWithId.Id` and `IWithEntityId.EntityId`.

#### Scenario: TestScoreWithEnrichment implements IWithTestScoringId

- **WHEN** `TestScoreWithEnrichment` is defined
- **THEN** it SHALL implement `IWithTestScoringId` with a `Guid TestScoringId` property (replacing the current `Guid RequestId`)

### Requirement: ScoringManager no longer handles TestScoreWithEnrichment

The `ScoringManager` SHALL NOT have a `Receive<TestScoreWithEnrichment>` handler or any enrichment-related methods. It SHALL retain `Receive<TestScoreItems>` as a one-liner that forwards to the scoring pool.

#### Scenario: ScoringManager has no ReceiveAsync

- **WHEN** `ScoringManager` is compiled
- **THEN** it SHALL NOT contain any `ReceiveAsync` calls

#### Scenario: ScoringManager retains TestScoreItems forwarding

- **WHEN** `TestScoreItems` is received by `ScoringManager`
- **THEN** it SHALL forward to the scoring router pool with the original sender preserved

### Requirement: API endpoint sends to ITestScoringRegion

The `/rulesets/test` endpoint SHALL resolve `ITestScoringRegion` from the actor registry and send `TestScoreWithEnrichment` to it (instead of `IScoringManager`). The response mapping SHALL remain unchanged.

#### Scenario: Endpoint targets test scoring region

- **WHEN** a POST request arrives at `/rulesets/test`
- **THEN** the handler SHALL resolve `ITestScoringRegion` from `IActorRegistry`, create a `TestScoreWithEnrichment` message with a new `Guid` as `TestScoringId`, and Ask the region

#### Scenario: Response shape unchanged

- **WHEN** the region responds with `TestScoreWithEnrichmentCompleted`
- **THEN** the endpoint SHALL return the same `TestScoreResponse` shape as before
