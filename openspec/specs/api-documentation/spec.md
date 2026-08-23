## Purpose

OpenAPI specification generation and Scalar interactive documentation UI for the FunkArr API.

## Requirements

### Requirement: OpenAPI specification endpoint
The system SHALL expose an OpenAPI 3.x specification at `/openapi/v1.json` describing all API endpoints, their parameters, request bodies, and response schemas.

#### Scenario: Fetch OpenAPI spec
- **WHEN** a client sends GET to `/openapi/v1.json`
- **THEN** the system SHALL return a valid OpenAPI 3.x JSON document with all controller actions documented

### Requirement: Scalar interactive documentation UI
The system SHALL serve the Scalar API documentation UI at `/scalar/v1`, rendering the OpenAPI specification as an interactive API explorer.

#### Scenario: Access Scalar UI
- **WHEN** a user navigates to `/scalar/v1` in a browser
- **THEN** the system SHALL display the Scalar API documentation interface with all endpoints grouped and explorable

### Requirement: Controller tagging for API grouping
Each controller SHALL be tagged for logical grouping in the API documentation. Protocol emulation controllers SHALL use distinct tags to separate them from the Web UI API.

#### Scenario: Protocol emulation controllers tagged separately
- **WHEN** the OpenAPI spec is rendered
- **THEN** Newznab endpoints SHALL appear under a "Newznab Emulation" tag and SABnzbd endpoints under a "SABnzbd Emulation" tag

#### Scenario: Web UI controllers grouped by domain
- **WHEN** the OpenAPI spec is rendered
- **THEN** Download Queue, Ruleset, Match Intelligence, and Setup/Config endpoints SHALL appear under their respective domain tags

### Requirement: Typed response metadata
All controller actions SHALL declare their response types via `[ProducesResponseType]` attributes so the OpenAPI spec includes accurate response schemas.

#### Scenario: Success response documented
- **WHEN** a controller action returns a typed response DTO
- **THEN** the OpenAPI spec SHALL include the response schema with all properties and types

#### Scenario: Error response documented
- **WHEN** a controller action can return an error
- **THEN** the OpenAPI spec SHALL include the 401 and relevant 4xx response schemas
