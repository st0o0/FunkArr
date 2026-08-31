# API Authentication

## Purpose

ApiKey query parameter authentication for indexer and download API endpoints, plus DownloadPath configuration.

## Requirements

### Requirement: ApiKey query parameter authentication
All indexer and download API endpoints SHALL validate the `apikey` query parameter against the configured `FunkArrOptions.ApiKey`. The parameter name SHALL be case-insensitive.

#### Scenario: Valid API key
- **WHEN** a request includes `?apikey=correct-key` and the configured key is "correct-key"
- **THEN** the request SHALL proceed to the endpoint handler

#### Scenario: Missing API key
- **WHEN** a request has no `apikey` query parameter
- **THEN** the system SHALL return HTTP 403 with an error body

#### Scenario: Wrong API key
- **WHEN** a request includes `?apikey=wrong-key` and the configured key is "correct-key"
- **THEN** the system SHALL return HTTP 403 with an error body

#### Scenario: Newznab error format
- **WHEN** authentication fails on an indexer API endpoint
- **THEN** the error body SHALL be Newznab XML: `<error code="100" description="Invalid API Key"/>`

#### Scenario: SABnzbd error format
- **WHEN** authentication fails on a download API endpoint
- **THEN** the error body SHALL be JSON: `{"status":false,"error":"API Key Incorrect"}`

### Requirement: DownloadPath configuration
FunkArrOptions SHALL include a `DownloadPath` property (string, default "downloads") specifying the base directory for completed downloads.

#### Scenario: Default download path
- **WHEN** no DownloadPath is configured
- **THEN** the value SHALL be "downloads"

#### Scenario: Custom download path
- **WHEN** `FunkArr__DownloadPath` environment variable is set to "/media/downloads"
- **THEN** FunkArrOptions.DownloadPath SHALL be "/media/downloads"
