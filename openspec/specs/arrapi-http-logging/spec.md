# arrapi-http-logging Specification

## Purpose

HTTP request/response logging for ArrApi controller routes, providing diagnostic visibility into Newznab and SABnzbd API traffic at configurable log levels.

## Requirements

### Requirement: HTTP logging SHALL be registered for ArrApi controllers

The application SHALL register `AddHttpLogging()` in service configuration and `UseHttpLogging()` in the middleware pipeline, scoped to ArrApi controller routes.

#### Scenario: HTTP logging middleware is active

- **WHEN** the application starts
- **THEN** HTTP logging SHALL be available in the pipeline for `/index/api` and `/download/api` routes

### Requirement: ArrApi HTTP logging SHALL log headers at Information level

At `Information` log level, ArrApi HTTP logging SHALL capture request method, request path, query string, response status code, and request duration. The `apikey` query parameter value SHALL be redacted from all log output.

#### Scenario: Newznab search request logged at Information

- **WHEN** Prowlarr sends `GET /index/api?t=tvsearch&q=Tatort&tvdbid=123&apikey=secret123` and the log level is Information
- **THEN** a log entry SHALL contain the method (GET), path (/index/api), query string with apikey redacted (t=tvsearch&q=Tatort&tvdbid=123&apikey=[REDACTED]), response status code, and duration

#### Scenario: SABnzbd request logged at Information

- **WHEN** Sonarr sends `GET /download/api?mode=queue&apikey=secret123` and the log level is Information
- **THEN** a log entry SHALL contain the method (GET), path (/download/api), query string with apikey redacted (mode=queue&apikey=[REDACTED]), response status code, and duration

#### Scenario: Non-apikey parameters preserved

- **WHEN** any ArrApi request is logged
- **THEN** all query parameters except `apikey` SHALL be logged with their original values

### Requirement: ArrApi HTTP logging SHALL log bodies at Trace level

At `Trace` log level, ArrApi HTTP logging SHALL additionally capture request and response bodies.

#### Scenario: Newznab response body logged at Trace

- **WHEN** a Newznab search request completes and log level is Trace
- **THEN** the log entry SHALL additionally contain the response body (Newznab XML)

#### Scenario: No body logging at Information level

- **WHEN** a request completes and log level is Information (not Trace)
- **THEN** the log entry SHALL NOT contain request or response bodies

### Requirement: API key redaction SHALL use IHttpLoggingInterceptor

The application SHALL implement `IHttpLoggingInterceptor` to redact the `apikey` query parameter value from HTTP log entries. The `[HttpLogging(HttpLoggingFields.All)]` attribute SHALL be removed from both controllers.

#### Scenario: Interceptor registered

- **WHEN** the application starts
- **THEN** an `IHttpLoggingInterceptor` implementation SHALL be registered that handles apikey redaction

#### Scenario: Controller logging attribute removed

- **WHEN** examining `NewznabController` and `SabnzbdController`
- **THEN** neither SHALL have a `[HttpLogging]` attribute

#### Scenario: Global logging config includes query string

- **WHEN** the global HTTP logging configuration is applied
- **THEN** `RequestQuery` SHALL be included in the logged fields (the interceptor handles redaction)

### Requirement: Internal API SHALL NOT have HTTP logging

The `/api/*` routes (internal UI API) SHALL NOT have HTTP logging applied.

#### Scenario: Internal API request not logged by HTTP logging middleware

- **WHEN** the Vue UI sends a request to `/api/downloads/queue`
- **THEN** the HTTP logging middleware SHALL NOT produce a log entry for this request
