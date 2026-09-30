## ADDED Requirements

### Requirement: SabnzbdDownloadService has test coverage
`SabnzbdDownloadService` SHALL have tests covering all public methods using TestProbe actors.

#### Scenario: AddFile success
- **WHEN** `AddFile` is called with a valid NZB file and the actor responds with success
- **THEN** the service SHALL return `SabnzbdResult.Ok` with nzo_ids

#### Scenario: AddFile missing file
- **WHEN** `AddFile` is called with null file
- **THEN** the service SHALL return `SabnzbdResult.Error` with 400

#### Scenario: DeleteFromQueue success
- **WHEN** `DeleteFromQueue` is called with a valid GUID and the actor responds
- **THEN** the service SHALL return `SabnzbdResult.Ok`

#### Scenario: DeleteFromQueue invalid GUID
- **WHEN** `DeleteFromQueue` is called with a non-GUID string
- **THEN** the service SHALL return `SabnzbdResult.Error`

#### Scenario: Actor timeout
- **WHEN** any service method calls Ask and the actor does not respond
- **THEN** the service SHALL return `SabnzbdResult.Error` with 504

### Requirement: SabnzbdQueueService has test coverage
`SabnzbdQueueService` SHALL have tests covering all public methods using TestProbe actors.

#### Scenario: GetQueue success
- **WHEN** `GetQueue` is called and the actor responds with queue data
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping a `QueueResponse`

#### Scenario: GetHistory success
- **WHEN** `GetHistory` is called and the actor responds with history data
- **THEN** the service SHALL return `SabnzbdResult.Ok` wrapping a `HistoryResponse`

#### Scenario: PauseQueue success
- **WHEN** `PauseQueue` is called and the actor responds
- **THEN** the service SHALL return `SabnzbdResult.Ok`

#### Scenario: Actor timeout in queue service
- **WHEN** any queue service method calls Ask and the actor does not respond
- **THEN** the service SHALL return `SabnzbdResult.Error` with 504
