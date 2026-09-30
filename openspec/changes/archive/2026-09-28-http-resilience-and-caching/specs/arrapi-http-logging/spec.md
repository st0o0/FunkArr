## ADDED Requirements

### Requirement: HTTP logging SHALL be registered for ArrApi controllers

The application SHALL register `AddHttpLogging()` in service configuration and `UseHttpLogging()` in the middleware pipeline, scoped to ArrApi controller routes.

#### Scenario: HTTP logging middleware is active

- **WHEN** the application starts
- **THEN** HTTP logging SHALL be available in the pipeline for `/index/api` and `/download/api` routes

### Requirement: ArrApi HTTP logging SHALL log headers at Information level

At `Information` log level, ArrApi HTTP logging SHALL capture request method, request path, query string, response status code, and request duration.

#### Scenario: Newznab search request logged at Information

- **WHEN** Prowlarr sends `GET /index/api?t=tvsearch&q=Tatort&tvdbid=123` and the log level is Information
- **THEN** a log entry SHALL contain the method (GET), path (/index/api), query string (t=tvsearch&q=Tatort&tvdbid=123), response status code, and duration

#### Scenario: SABnzbd request logged at Information

- **WHEN** Sonarr sends `GET /download/api?mode=queue` and the log level is Information
- **THEN** a log entry SHALL contain the method (GET), path (/download/api), query string (mode=queue), response status code, and duration

### Requirement: ArrApi HTTP logging SHALL log bodies at Trace level

At `Trace` log level, ArrApi HTTP logging SHALL additionally capture request and response bodies.

#### Scenario: Newznab response body logged at Trace

- **WHEN** a Newznab search request completes and log level is Trace
- **THEN** the log entry SHALL additionally contain the response body (Newznab XML)

#### Scenario: No body logging at Information level

- **WHEN** a request completes and log level is Information (not Trace)
- **THEN** the log entry SHALL NOT contain request or response bodies

### Requirement: Internal API SHALL NOT have HTTP logging

The `/api/*` routes (internal UI API) SHALL NOT have HTTP logging applied.

#### Scenario: Internal API request not logged by HTTP logging middleware

- **WHEN** the Vue UI sends a request to `/api/downloads/queue`
- **THEN** the HTTP logging middleware SHALL NOT produce a log entry for this request
