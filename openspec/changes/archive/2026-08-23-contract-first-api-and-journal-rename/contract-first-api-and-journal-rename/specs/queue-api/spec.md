## MODIFIED Requirements

### Requirement: Typed response models
Queue and history responses SHALL use generated contract types from `FunkArr.Api.Contracts` instead of hand-written types from `FunkArr.Api.Models`. The generated types SHALL have identical JSON shapes to the current `QueueItemResponse` and `HistoryItemResponse`.

#### Scenario: OpenAPI schema available
- **WHEN** the OpenAPI spec is generated
- **THEN** the queue and history response schemas SHALL include all properties with their types

#### Scenario: Domain-to-contract mapping
- **WHEN** the controller builds response data from actor messages
- **THEN** it SHALL use `.ToContract()` extension methods to produce generated contract types
