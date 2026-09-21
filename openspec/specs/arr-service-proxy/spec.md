## Purpose

Proxy endpoints that create indexer and download client entries in external *arr services (Prowlarr, Sonarr, Radarr) on behalf of the user, with a consistent success/error envelope.

## Requirements

### Requirement: Create Prowlarr indexer via proxy
The system SHALL accept `POST /api/setup/prowlarr/indexer` with a JSON body containing `url` (the Prowlarr base URL) and `apiKey` (the Prowlarr API key). The endpoint SHALL construct a Newznab indexer payload with FunkArr's connection details and POST it to `{url}/api/v1/indexer` with the `X-Api-Key` header set to the provided API key. On success, the endpoint SHALL return HTTP 200 with `{ "success": true }`. On failure, the endpoint SHALL return HTTP 200 with `{ "success": false, "error": "<message>" }`.

#### Scenario: Successful Prowlarr indexer creation
- **WHEN** the user sends `POST /api/setup/prowlarr/indexer` with a valid Prowlarr URL and API key
- **THEN** FunkArr POSTs a Newznab indexer to Prowlarr with name `FunkArr`, the FunkArr base URL, API path `/index/api`, FunkArr's API key, and categories 5000 (TV) and 2000 (Movies)
- **THEN** the response is `{ "success": true }`

#### Scenario: Prowlarr unreachable
- **WHEN** the user sends `POST /api/setup/prowlarr/indexer` with an unreachable URL
- **THEN** the response is `{ "success": false, "error": "<connection error message>" }`

#### Scenario: Prowlarr rejects request
- **WHEN** the Prowlarr API returns a non-success status (e.g., 400 for duplicate indexer)
- **THEN** the response is `{ "success": false, "error": "<arr service error message>" }`

### Requirement: Create Sonarr indexer via proxy
The system SHALL accept `POST /api/setup/sonarr/indexer` with a JSON body containing `url` (the Sonarr base URL) and `apiKey` (the Sonarr API key). The endpoint SHALL construct a Newznab indexer payload and POST it to `{url}/api/v3/indexer` with the `X-Api-Key` header. The indexer payload SHALL include name `FunkArr`, the FunkArr base URL with `/index/api` path, FunkArr's API key, and categories 5030 (TV/SD) and 5040 (TV/HD).

#### Scenario: Successful Sonarr indexer creation
- **WHEN** the user sends `POST /api/setup/sonarr/indexer` with a valid Sonarr URL and API key
- **THEN** FunkArr POSTs a Newznab indexer to Sonarr and returns `{ "success": true }`

#### Scenario: Sonarr API key invalid
- **WHEN** the Sonarr API returns HTTP 401
- **THEN** the response is `{ "success": false, "error": "Unauthorized -- check your Sonarr API key" }`

### Requirement: Create Sonarr download client via proxy
The system SHALL accept `POST /api/setup/sonarr/download-client` with a JSON body containing `url` and `apiKey`. The endpoint SHALL construct a SABnzbd download client payload and POST it to `{url}/api/v3/downloadclient` with the `X-Api-Key` header. The payload SHALL include name `FunkArr`, host and port derived from FunkArr's own URL, URL base `/download`, FunkArr's API key, and category `tv`.

#### Scenario: Successful Sonarr download client creation
- **WHEN** the user sends `POST /api/setup/sonarr/download-client` with a valid Sonarr URL and API key
- **THEN** FunkArr POSTs a SABnzbd download client to Sonarr and returns `{ "success": true }`

#### Scenario: Download client already exists
- **WHEN** Sonarr returns a 400 error indicating the download client already exists
- **THEN** the response is `{ "success": false, "error": "<Sonarr error message>" }`

### Requirement: Create Radarr indexer via proxy
The system SHALL accept `POST /api/setup/radarr/indexer` with a JSON body containing `url` and `apiKey`. The endpoint SHALL construct a Newznab indexer payload and POST it to `{url}/api/v3/indexer` with the `X-Api-Key` header. The indexer payload SHALL include name `FunkArr`, the FunkArr base URL with `/index/api` path, FunkArr's API key, and categories 2030 (Movies/SD) and 2040 (Movies/HD).

#### Scenario: Successful Radarr indexer creation
- **WHEN** the user sends `POST /api/setup/radarr/indexer` with a valid Radarr URL and API key
- **THEN** FunkArr POSTs a Newznab indexer to Radarr and returns `{ "success": true }`

### Requirement: Create Radarr download client via proxy
The system SHALL accept `POST /api/setup/radarr/download-client` with a JSON body containing `url` and `apiKey`. The endpoint SHALL construct a SABnzbd download client payload and POST it to `{url}/api/v3/downloadclient` with the `X-Api-Key` header. The payload SHALL include name `FunkArr`, host and port from FunkArr's own URL, URL base `/download`, FunkArr's API key, and category `movies`.

#### Scenario: Successful Radarr download client creation
- **WHEN** the user sends `POST /api/setup/radarr/download-client` with a valid Radarr URL and API key
- **THEN** FunkArr POSTs a SABnzbd download client to Radarr and returns `{ "success": true }`

### Requirement: Consistent error envelope
All five proxy endpoints SHALL return HTTP 200 with a JSON body containing `success` (boolean) and optionally `error` (string). The endpoint SHALL NOT propagate the arr service's HTTP status code to the caller. Connection timeouts SHALL use a 10-second limit.

#### Scenario: Timeout handling
- **WHEN** the arr service does not respond within 10 seconds
- **THEN** the response is `{ "success": false, "error": "Connection timed out" }`

#### Scenario: Malformed URL
- **WHEN** the request body contains an invalid URL (e.g., missing scheme)
- **THEN** the response is `{ "success": false, "error": "<validation message>" }`
