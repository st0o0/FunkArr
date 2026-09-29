## Purpose

Retry logic in SearchWorkers for retryable mediathek query failures (queue full) with IWithTimers-based backoff.
## Requirements
### Requirement: SearchWorkers retry on retryable mediathek failures

TvSearchWorker and MovieSearchWorker SHALL retry mediathek queries when receiving
a `QueryMediathekQueueFull` response. Retry SHALL use `IWithTimers` and
`Timers.StartSingleTimer` for backoff with bounded attempts.

#### Scenario: First retry after QueueFull

- **WHEN** a worker in `Querying` state receives `QueryMediathekQueueFull` and
  the attempt count is below the maximum
- **THEN** the worker SHALL schedule a retry message to Self using
  `Timers.StartSingleTimer` with the appropriate backoff delay
  and remain in the `Querying` state

#### Scenario: Retry succeeds

- **WHEN** a retry attempt results in `QueryMediathekCompleted`
- **THEN** the worker SHALL process the results normally, continuing to the next
  pipeline stage (RuleSet resolution, Scoring, or direct response)

#### Scenario: All retries exhausted

- **WHEN** a worker receives `QueryMediathekQueueFull` and the attempt count
  has reached the maximum (3 total attempts: 1 initial + 2 retries)
- **THEN** the worker SHALL respond with `SearchSeriesFailed` or
  `SearchMovieFailed` to the original caller

#### Scenario: Terminal failure is not retried

- **WHEN** a worker in `Querying` state receives `QueryMediathekError`
- **THEN** the worker SHALL immediately respond with `SearchSeriesFailed` or
  `SearchMovieFailed` without retrying

#### Scenario: Ask timeout is not retried

- **WHEN** a worker's `Ask` to MediathekViewWebManager times out (PipeTo failure
  with `AskTimeoutException`)
- **THEN** the worker SHALL immediately respond with failure without retrying

### Requirement: Retry backoff schedule

The retry backoff delays SHALL be 500ms for the first retry and 1500ms for the
second retry.

#### Scenario: Backoff timing

- **WHEN** a worker retries after QueueFull
- **THEN** the first retry SHALL be delayed by 500ms and the second retry SHALL
  be delayed by 1500ms

#### Scenario: Total retry budget fits within Ask timeout

- **WHEN** a worker exhausts all retries
- **THEN** the total delay budget (500ms + 1500ms = 2s) SHALL leave sufficient
  headroom within the 15s mediathek Ask timeout for actual query processing

### Requirement: Retry uses private message type

Each SearchWorker SHALL define a private `RetryMediathekQuery` record for
timer-based retry. This message SHALL carry the current attempt number.

#### Scenario: RetryMediathekQuery handling

- **WHEN** a `RetryMediathekQuery` message is received in the `Querying` state
- **THEN** the worker SHALL issue a new `Ask` to the MediathekViewWebManager
  with the original query and increment the attempt counter

#### Scenario: Stale retry after state change

- **WHEN** a `RetryMediathekQuery` arrives but the worker is no longer in the
  `Querying` state (e.g., already moved to Scoring)
- **THEN** the message SHALL be unhandled (default Akka behavior for unmatched
  messages in Become states)
