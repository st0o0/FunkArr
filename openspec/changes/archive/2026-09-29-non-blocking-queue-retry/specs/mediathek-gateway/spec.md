## MODIFIED Requirements

### Requirement: MediathekViewWebManager uses Akka.Streams for concurrency control

The Manager SHALL materialize a single long-lived Akka.Streams pipeline in its
constructor. The pipeline SHALL use `Source.Queue` with `OverflowStrategy.DropNew`
for input, `SelectAsyncUnordered` for throttled HTTP execution, and
`Sink.ActorRef` to route results back to Self. Queue offers SHALL use
`OfferAsync(...).PipeTo(Self)` for non-blocking operation.

#### Scenario: Concurrency limit

- **WHEN** multiple `QueryMediathek` messages arrive concurrently
- **THEN** the stream's `SelectAsyncUnordered(maxConcurrent)` SHALL limit parallel HTTP requests to the configured maximum (default 3)

#### Scenario: Buffer overflow with immediate response

- **WHEN** more requests arrive than the `Source.Queue` buffer can hold (64 elements)
- **THEN** the overflow strategy `DropNew` SHALL cause `OfferAsync` to return `Dropped` immediately, and the actor SHALL respond with `QueryMediathekQueueFull` to the caller

#### Scenario: Stream error resilience

- **WHEN** an unexpected exception occurs in the stream pipeline
- **THEN** the `Deciders.ResumingDecider` supervision strategy SHALL drop the failing element and continue processing, and the actor SHALL log a warning

### Requirement: MediathekViewWebManager is a singleton HTTP gateway

The MediathekViewWebManager SHALL be a Cluster Singleton actor that serves as the
single point of access to the MediathekViewWeb API. The actor SHALL use an
internal Akka.Streams pipeline for concurrency control and request processing.
The actor SHALL delegate HTTP requests and caching to an injected `MediathekClient`
service. The actor SHALL NOT implement `IWithUnboundedStash` or `IWithTimers`.
The actor SHALL use `Receive<T>` (synchronous) handlers, not `ReceiveAsync`.

#### Scenario: Successful query

- **WHEN** a `QueryMediathek` message is received
- **THEN** the Manager SHALL assign a `Guid` RequestId, store the Sender in a pending dictionary keyed by RequestId, offer the request to the queue via `OfferAsync.PipeTo`, and when the stream result arrives, look up the original Sender and respond with a `QueryMediathekCompleted` message

#### Scenario: HTTP error

- **WHEN** the `MediathekClient` call fails with an exception inside the stream pipeline
- **THEN** the exception SHALL be caught in the `SelectAsyncUnordered` stage, routed back to the actor as a `StreamFailure`, and the actor SHALL respond to the original Sender with a `QueryMediathekError` message

#### Scenario: Queue full error

- **WHEN** `OfferAsync` returns `Dropped` (queue at capacity)
- **THEN** the actor SHALL respond to the original Sender with `QueryMediathekQueueFull` and remove the pending dictionary entry

### Requirement: MediathekViewWebManager tracks in-flight requests

The Manager SHALL maintain a `Dictionary<Guid, IActorRef>` mapping RequestIds to original Senders. This dictionary serves as both sender correlation and in-flight request tracking.

#### Scenario: Request lifecycle

- **WHEN** a `QueryMediathek` is received and a stream result arrives for it
- **THEN** the Manager SHALL add an entry to the dictionary when the request is received, and remove the entry when the result is processed (stream response or queue drop)

#### Scenario: Orphaned request from stream supervision

- **WHEN** the stream's `ResumingDecider` drops a failing element
- **THEN** the dictionary entry SHALL remain until the caller's Ask times out (no explicit cleanup required)
