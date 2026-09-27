# integration-test-downloads Specification

## Purpose

Contract tests for the internal Downloads API, verifying queue, history, mutation, and pipeline control endpoints with realistic data and typed response deserialization.

## Requirements

### Requirement: Download API endpoints have contract tests
Every Downloads API endpoint SHALL have at least one integration test with realistic data and typed response deserialization.

#### Scenario: GetQueue with active download
- **WHEN** `GET /api/downloads/queue` is requested and the probe responds with a processing item
- **THEN** the typed `DownloadQueueResponse` SHALL contain the item with correct title, percentage, and status

#### Scenario: GetHistory with entries
- **WHEN** `GET /api/downloads/history` is requested and the probe responds with completed/failed items
- **THEN** the typed `DownloadHistoryResponse` SHALL contain items with correct titles and statuses

#### Scenario: Pause returns success
- **WHEN** `POST /api/downloads/pause` is sent and the probe responds with success
- **THEN** the typed `OperationResult` SHALL have `Success=true`

#### Scenario: Cancel nonexistent returns 404
- **WHEN** `DELETE /api/downloads/queue/{id}` is sent and the probe responds with failure
- **THEN** the response SHALL be 404 with `OperationResult { Success=false }`

#### Scenario: Move with invalid priority returns 400
- **WHEN** `POST /api/downloads/queue/{id}/move` is sent with an invalid priority string
- **THEN** the response SHALL be 400
