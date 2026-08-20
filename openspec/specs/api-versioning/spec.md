## Purpose

URL-segment API versioning infrastructure for FunkArr Web UI endpoints, with version-neutral support for protocol emulation controllers.

## Requirements

### Requirement: URL-segment API versioning
The system SHALL use URL-segment versioning for all Web UI API endpoints. The version segment SHALL appear as `/api/v{version}/` in the URL path.

#### Scenario: Versioned Web UI endpoint
- **WHEN** a client sends a request to `/api/v1/queue`
- **THEN** the system SHALL route to the v1 QueueController

#### Scenario: Unversioned request to versioned endpoint
- **WHEN** a client sends a request to `/api/queue` (no version segment)
- **THEN** the system SHALL return HTTP 404

### Requirement: Version-neutral protocol emulation
Newznab and SABnzbd controllers SHALL be marked as version-neutral. Their routes SHALL NOT include a version segment.

#### Scenario: Newznab stays at /api
- **WHEN** a client sends a Newznab request to `/api?t=caps&apikey=key`
- **THEN** the system SHALL route to the NewznabController without requiring a version segment

#### Scenario: SABnzbd stays at /download/api
- **WHEN** a client sends a SABnzbd request to `/download/api?mode=version&apikey=key`
- **THEN** the system SHALL route to the SabnzbdController without requiring a version segment

### Requirement: Default API version
The system SHALL configure v1 as the default API version. The system SHALL report the API version via response headers when `ReportApiVersions` is enabled.

#### Scenario: API version reported in response
- **WHEN** a client sends a request to any versioned endpoint
- **THEN** the response SHALL include an `api-supported-versions` header indicating supported versions
