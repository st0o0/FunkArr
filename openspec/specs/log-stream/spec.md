# log-stream Specification

## Purpose
TBD - created by archiving change fix-internal-api-bugs. Update Purpose after archive.
## Requirements
### Requirement: Recent log snapshot
The system SHALL expose `GET /api/system/logs` returning the most recent log entries (up to 500) held by the ring buffer sink, oldest first. Each entry SHALL contain `timestamp`, `level`, `message`, `sourceContext` and `exception`.

#### Scenario: Snapshot after logging
- **WHEN** 3 log events have been emitted and `GET /api/system/logs` is requested
- **THEN** the response is 200 with those 3 entries in emission order

#### Scenario: Ring buffer capacity
- **WHEN** 600 log events have been emitted
- **THEN** the snapshot contains exactly the newest 500

### Requirement: Multi-subscriber live log stream
The system SHALL expose `GET /api/system/logs/stream` as server-sent events. Every connected client SHALL receive every log entry emitted after it connected. Entries emitted while no client is connected SHALL NOT be delivered to a later stream subscriber.

#### Scenario: Two clients receive all entries
- **WHEN** two clients are connected to the stream and 10 log events are emitted
- **THEN** each client receives all 10 entries in order

#### Scenario: No stale replay
- **WHEN** 50 log events are emitted with no client connected and a client then connects
- **THEN** the client receives none of those 50 entries on the stream

#### Scenario: Slow client isolation
- **WHEN** one client stops reading and its per-subscriber buffer is full
- **THEN** that client's oldest buffered entries are dropped
- **AND** other clients and log emission are not blocked

