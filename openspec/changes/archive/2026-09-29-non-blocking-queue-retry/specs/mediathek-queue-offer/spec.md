## ADDED Requirements

### Requirement: Non-blocking queue offer via PipeTo

The MediathekViewWebManager SHALL use `Receive<QueryMediathek>` (not
`ReceiveAsync`) and offer requests to the `Source.Queue` via
`OfferAsync(...).PipeTo(Self)`. The actor SHALL NOT await `OfferAsync` or use
any async handler that blocks the mailbox thread.

#### Scenario: Successful enqueue

- **WHEN** a `QueryMediathek` message is received and the queue has capacity
- **THEN** `OfferAsync` SHALL return `Enqueued` via PipeTo, and the actor SHALL
  continue processing other messages while the stream handles the request

#### Scenario: Queue full

- **WHEN** a `QueryMediathek` message is received and the queue is at capacity
- **THEN** `OfferAsync` SHALL return `Dropped` via PipeTo, and the actor SHALL
  immediately respond to the original sender with `QueryMediathekQueueFull` and
  remove the entry from the pending dictionary

#### Scenario: OfferAsync exception

- **WHEN** `OfferAsync` throws (e.g., stream terminated unexpectedly)
- **THEN** the PipeTo failure handler SHALL route the exception to the actor,
  and the actor SHALL respond to the original sender with
  `QueryMediathekError(exception)` and remove the entry from the pending dictionary

#### Scenario: Actor remains responsive during backpressure

- **WHEN** the queue is full and multiple `QueryMediathek` messages arrive
- **THEN** the actor SHALL process each one immediately (responding with
  `QueueFull` for dropped offers) and continue processing `StreamSuccess` and
  `StreamFailure` messages without delay

### Requirement: DropNew overflow strategy

The `Source.Queue` in MediathekViewWebManager SHALL use
`OverflowStrategy.DropNew`. The queue capacity SHALL remain 64 elements.

#### Scenario: Deterministic offer result

- **WHEN** `OfferAsync` is called on the queue
- **THEN** it SHALL return immediately with either `Enqueued` or `Dropped`,
  never block

#### Scenario: No orphaned pending entries from overflow

- **WHEN** a request is dropped by the overflow strategy
- **THEN** the actor SHALL respond to the caller and clean up the pending
  dictionary entry, leaving no orphaned entries

### Requirement: Internal QueueOfferResult message

The MediathekViewWebManager SHALL define a private record for receiving
`OfferAsync` results via PipeTo. The record SHALL carry the RequestId to
correlate with the pending dictionary entry.

#### Scenario: Success path mapping

- **WHEN** `OfferAsync` completes successfully with `IQueueOfferResult`
- **THEN** the PipeTo `success` lambda SHALL wrap it in the internal result
  message with the corresponding RequestId

#### Scenario: Failure path mapping

- **WHEN** `OfferAsync` throws an exception
- **THEN** the PipeTo `failure` lambda SHALL wrap the exception in the internal
  result message with the corresponding RequestId
