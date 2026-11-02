## ADDED Requirements

### Requirement: FeedEvalActor evaluates and gates one group
FeedEvalActor SHALL be a `ReceiveActor` used as a pool routee. It SHALL receive an `EvalGroup` message, Ask the appropriate evaluation worker (TvEvaluation for shows, MovieEvaluation for movies), apply FeedGate.Evaluate to the results, record telemetry for rejections, and reply with a `GroupResult`.

#### Scenario: Successful TV evaluation and gating
- **WHEN** FeedEvalActor receives an EvalGroup with MediaType=Show
- **THEN** it SHALL Ask TvEvaluation via `Context.GetActor<ITvEvaluationRegion>()`, gate the results with FeedGate.Evaluate using minConfidence, and reply with GroupResult containing accepted FeedEntry records

#### Scenario: Successful movie evaluation and gating
- **WHEN** FeedEvalActor receives an EvalGroup with MediaType=Movie
- **THEN** it SHALL Ask MovieEvaluation via `Context.GetActor<IMovieEvaluationRegion>()`, gate the results, and reply with GroupResult

#### Scenario: Evaluation failure
- **WHEN** the Ask to TvEvaluation/MovieEvaluation fails or returns a failed response
- **THEN** FeedEvalActor SHALL reply with GroupResult containing Error set, not crash the actor

#### Scenario: Gating rejects low-confidence results
- **WHEN** FeedGate.Evaluate rejects entries below minConfidence
- **THEN** GroupResult.Accepted SHALL contain only entries that passed the gate

### Requirement: FeedEvalActor records telemetry at the actor boundary
Telemetry for rejected entries (Telemetry.Rejected counter) SHALL be recorded inside FeedEvalActor after calling FeedGate.Evaluate, not inside the FeedGate method or any record method. FeedGate.Evaluate SHALL return rejection counts as data.

#### Scenario: Rejection telemetry recorded by actor
- **WHEN** FeedGate.Evaluate rejects 3 entries with reason "low_score" and 1 with reason "no_subtitle"
- **THEN** FeedEvalActor SHALL call `Telemetry.Rejected.Add(3, reason: "low_score")` and `Telemetry.Rejected.Add(1, reason: "no_subtitle")`

#### Scenario: FeedGate has no telemetry side effects
- **WHEN** inspecting FeedGate.Evaluate
- **THEN** it SHALL NOT reference the Telemetry class or any metrics instruments

### Requirement: FeedEvalActor uses PipeTo with failure handler
FeedEvalActor SHALL use `Ask.PipeTo(Self, success:, failure:)` for evaluation requests. The failure handler SHALL produce an error result, not propagate the exception.

#### Scenario: Evaluation timeout
- **WHEN** the Ask to an evaluation worker times out
- **THEN** the PipeTo failure handler SHALL produce a typed error message that results in a GroupResult with Error set

### Requirement: Scatter-gather evaluation in worker
FeedCrawlWorker's Evaluating phase SHALL send all matched groups as EvalGroup messages to the EvalPool, track the number of pending groups in state, and collect GroupResult messages. When all groups have responded, it SHALL Become(Publishing).

#### Scenario: All groups dispatched at once
- **WHEN** the worker enters the Evaluating phase with 10 matched groups
- **THEN** it SHALL send 10 EvalGroup messages to the EvalPool and set pendingGroups=10

#### Scenario: Group results collected
- **WHEN** all 10 GroupResult messages have been received
- **THEN** the worker SHALL have collected all accepted FeedEntry arrays and all errors, and SHALL Become(Publishing)

#### Scenario: Failed groups counted
- **WHEN** 2 of 10 GroupResult messages have Error set
- **THEN** the worker state SHALL record failedGroups=2

### Requirement: EvalGroup and GroupResult are internal messages
EvalGroup SHALL carry: RuleSetId (string), MediaType, Ids (ExternalIds), Items (MediathekItem[]). GroupResult SHALL carry: RuleSetId (string), Accepted (FeedEntry[]), Error (string?).

#### Scenario: EvalGroup message shape
- **WHEN** constructing an EvalGroup
- **THEN** it SHALL require RuleSetId, MediaType, Ids, and Items as constructor parameters
