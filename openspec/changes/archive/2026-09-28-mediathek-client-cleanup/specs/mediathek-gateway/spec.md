## MODIFIED Requirements

### Requirement: MediathekViewWebManager is a singleton HTTP gateway

The MediathekViewWebManager SHALL be a Cluster Singleton actor that serves as the single point of access to the MediathekViewWeb API. The actor SHALL use an internal Akka.Streams pipeline for concurrency control and request processing. The actor SHALL delegate HTTP requests and caching to an injected `MediathekClient` service. The actor SHALL NOT implement `IWithUnboundedStash` or `IWithTimers`.

#### Scenario: Successful query

- **WHEN** a `QueryMediathek` message is received
- **THEN** the Manager SHALL assign a `Guid` RequestId, store the Sender in a pending dictionary keyed by RequestId, use the `MediathekQueryBuilder` to build a JSON query string, feed it into the stream pipeline, and when the stream result arrives, look up the original Sender and respond with a `QueryMediathekCompleted` message

#### Scenario: HTTP error

- **WHEN** the `MediathekClient` call fails with an exception inside the stream pipeline
- **THEN** the exception SHALL be caught in the `SelectAsyncUnordered` stage, routed back to the actor as a `StreamFailure`, and the actor SHALL respond to the original Sender with a `QueryMediathekFailed` message

### Requirement: MediathekViewWebManager uses Akka.Streams for concurrency control

The Manager SHALL materialize a single long-lived Akka.Streams pipeline in its constructor. The pipeline SHALL use `Source.ActorRef` for input, `Select` for query building, `SelectAsyncUnordered` for throttled HTTP execution, and `Sink.ActorRef` to route results back to Self.

#### Scenario: Concurrency limit

- **WHEN** multiple `QueryMediathek` messages arrive concurrently
- **THEN** the stream's `SelectAsyncUnordered(maxConcurrent)` SHALL limit parallel HTTP requests to the configured maximum (default 3)

#### Scenario: Buffer overflow

- **WHEN** more requests arrive than the `Source.ActorRef` buffer can hold (64 elements)
- **THEN** the overflow strategy `DropNew` SHALL drop the excess request, and the caller SHALL receive no response (Ask timeout acts as circuit breaker)

#### Scenario: Stream error resilience

- **WHEN** an unexpected exception occurs in the stream pipeline
- **THEN** the `Deciders.ResumingDecider` supervision strategy SHALL drop the failing element and continue processing, and the actor SHALL log a warning

### Requirement: MediathekViewWebManager tracks in-flight requests

The Manager SHALL maintain a `Dictionary<Guid, IActorRef>` mapping RequestIds to original Senders. This dictionary serves as both sender correlation and in-flight request tracking.

#### Scenario: Request lifecycle

- **WHEN** a `QueryMediathek` is received and a stream result arrives for it
- **THEN** the Manager SHALL add an entry to the dictionary when the request enters the stream, and remove the entry when the stream result is processed

#### Scenario: Orphaned request

- **WHEN** the stream drops or fails to process a request (e.g., supervision resume)
- **THEN** the dictionary entry SHALL remain until the caller's Ask times out (no explicit cleanup required)

### Requirement: MediathekQueryBuilder maps query messages to API format

The MediathekQueryBuilder SHALL be an internal class (not an actor) that provides a fluent API to construct the MediathekViewWeb JSON request body. It SHALL support all API features as a 1:1 mapping.

#### Scenario: Full query construction

- **WHEN** a MediathekQuery with multiple fields, duration filters, sorting, and pagination is built
- **THEN** the Builder SHALL produce a JSON string matching the API format with `queries` array, `sortBy`, `sortOrder`, `future`, `offset`, `size`, `duration_min`, and `duration_max` fields

#### Scenario: Minimal query

- **WHEN** a MediathekQuery with only one query field is built
- **THEN** the Builder SHALL produce valid JSON with defaults: `sortBy=timestamp`, `sortOrder=desc`, `future=false`, `offset=0`, `size=15`
