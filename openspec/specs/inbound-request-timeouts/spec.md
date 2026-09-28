# inbound-request-timeouts Specification

## Purpose

Per-endpoint request timeout policies for all API route groups, preventing long-running requests from consuming server resources indefinitely.

## Requirements

### Requirement: ASP.NET request timeout middleware SHALL be registered

The application SHALL register `AddRequestTimeouts()` in service configuration and `UseRequestTimeouts()` in the middleware pipeline.

#### Scenario: Middleware is active

- **WHEN** the application starts
- **THEN** the request timeout middleware SHALL be in the pipeline and enforce per-endpoint timeout policies

### Requirement: Each API endpoint group SHALL have an explicit request timeout

Request timeouts SHALL be configured per endpoint group using `WithRequestTimeout`. The framework default 408 (Request Timeout) response SHALL be used.

- `/api/mediathek/search`: 60 seconds
- `/api/downloads/*`: 15 seconds
- `/api/rulesets/*`: 15 seconds
- `/api/system/*`: 15 seconds
- `/index/api/*` (Newznab): 45 seconds
- `/download/api/*` (SABnzbd): 15 seconds
- `/healthz`, `/alive`: 5 seconds

#### Scenario: Mediathek search times out

- **WHEN** a request to `/api/mediathek/search` exceeds 60 seconds
- **THEN** the middleware SHALL cancel the CancellationToken and return HTTP 408

#### Scenario: Downloads endpoint times out

- **WHEN** a request to `/api/downloads/queue` exceeds 15 seconds
- **THEN** the middleware SHALL cancel the CancellationToken and return HTTP 408

#### Scenario: Newznab search times out

- **WHEN** a request to `/index/api?t=tvsearch` exceeds 45 seconds
- **THEN** the middleware SHALL cancel the CancellationToken and return HTTP 408

#### Scenario: Health probe times out

- **WHEN** a request to `/healthz` exceeds 5 seconds
- **THEN** the middleware SHALL cancel the CancellationToken and return HTTP 408

### Requirement: SSE stream endpoint SHALL have request timeout disabled

The `/api/downloads/queue/stream` SSE endpoint SHALL have request timeouts explicitly disabled because it is a long-lived connection.

#### Scenario: SSE stream is not interrupted by timeout

- **WHEN** a client connects to `/api/downloads/queue/stream` and maintains the connection for minutes
- **THEN** the request timeout middleware SHALL NOT cancel the connection

### Requirement: Metrics and infrastructure endpoints SHALL not have request timeouts unless specified

The `/metrics` endpoint SHALL NOT have a request timeout applied.

#### Scenario: Prometheus scrape is not interrupted

- **WHEN** Prometheus scrapes `/metrics`
- **THEN** no request timeout SHALL be enforced by the middleware
