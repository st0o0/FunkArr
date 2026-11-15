## Purpose

Delta spec for setup proxy endpoints — adds useSsl to SABnzbd payloads, validates self-URL, and accepts parsed URI in payload factories.

## MODIFIED Requirements

### Requirement: Create Sonarr download client via proxy
The system SHALL accept `POST /api/setup/sonarr/download-client` with a JSON body containing `url` and `apiKey`. The endpoint SHALL resolve and validate the self-URL using the shared resolution chain. The endpoint SHALL construct a SABnzbd download client payload and POST it to `{url}/api/v3/downloadclient` with the `X-Api-Key` header. The payload SHALL include name `FunkArr`, host and port derived from the validated self-URL URI, `useSsl` derived from the URI scheme (`true` for `https`), URL base `/download`, FunkArr's API key, and category `tv`.

#### Scenario: Successful Sonarr download client creation
- **WHEN** the user sends `POST /api/setup/sonarr/download-client` with a valid Sonarr URL and API key
- **THEN** FunkArr POSTs a SABnzbd download client to Sonarr and returns `{ "success": true }`

#### Scenario: Download client already exists
- **WHEN** Sonarr returns a 400 error indicating the download client already exists
- **THEN** the response is `{ "success": false, "error": "<Sonarr error message>" }`

#### Scenario: HTTPS self-URL sets useSsl
- **WHEN** the resolved self-URL is `https://funkarr.example.com:6969`
- **THEN** the SABnzbd payload SHALL include `useSsl: true`, `host: "funkarr.example.com"`, `port: 6969`

#### Scenario: HTTP self-URL leaves useSsl false
- **WHEN** the resolved self-URL is `http://funkarr:6969`
- **THEN** the SABnzbd payload SHALL include `useSsl: false`

#### Scenario: Invalid self-URL returns error before Arr call
- **WHEN** the resolved self-URL is malformed
- **THEN** the endpoint SHALL return `ArrResourceResponse` with `Success=false` without contacting Sonarr

### Requirement: Create Radarr download client via proxy
The system SHALL accept `POST /api/setup/radarr/download-client` with a JSON body containing `url` and `apiKey`. The endpoint SHALL resolve and validate the self-URL using the shared resolution chain. The payload SHALL include `useSsl` derived from the URI scheme, URL base `/download`, FunkArr's API key, and category `movies`.

#### Scenario: Successful Radarr download client creation
- **WHEN** the user sends `POST /api/setup/radarr/download-client` with a valid Radarr URL and API key
- **THEN** FunkArr POSTs a SABnzbd download client to Radarr and returns `{ "success": true }`

#### Scenario: HTTPS self-URL sets useSsl for Radarr
- **WHEN** the resolved self-URL is `https://funkarr.example.com`
- **THEN** the SABnzbd payload for Radarr SHALL include `useSsl: true`

### Requirement: Create Prowlarr indexer via proxy
The system SHALL accept `POST /api/setup/prowlarr/indexer` with a JSON body containing `url` (the Prowlarr base URL) and `apiKey` (the Prowlarr API key). The endpoint SHALL resolve and validate the self-URL using the shared resolution chain. The endpoint SHALL construct a Newznab indexer payload with FunkArr's connection details and POST it to `{url}/api/v1/indexer` with the `X-Api-Key` header set to the provided API key. On success, the endpoint SHALL return HTTP 200 with the `ArrResourceResponse`. On failure, the response SHALL contain the error.

#### Scenario: Successful Prowlarr indexer creation
- **WHEN** the user sends `POST /api/setup/prowlarr/indexer` with a valid Prowlarr URL and API key
- **THEN** FunkArr POSTs a Newznab indexer to Prowlarr with the validated self-URL

#### Scenario: Prowlarr unreachable
- **WHEN** the user sends `POST /api/setup/prowlarr/indexer` with an unreachable URL
- **THEN** the response is `{ "success": false, "error": "<connection error message>" }`

### Requirement: Create Sonarr indexer via proxy
The system SHALL accept `POST /api/setup/sonarr/indexer`. The endpoint SHALL resolve and validate the self-URL using the shared resolution chain before constructing the Newznab indexer payload.

#### Scenario: Successful Sonarr indexer creation
- **WHEN** the user sends `POST /api/setup/sonarr/indexer` with a valid Sonarr URL and API key
- **THEN** FunkArr POSTs a Newznab indexer to Sonarr using the validated self-URL

### Requirement: Create Radarr indexer via proxy
The system SHALL accept `POST /api/setup/radarr/indexer`. The endpoint SHALL resolve and validate the self-URL using the shared resolution chain before constructing the Newznab indexer payload.

#### Scenario: Successful Radarr indexer creation
- **WHEN** the user sends `POST /api/setup/radarr/indexer` with a valid Radarr URL and API key
- **THEN** FunkArr POSTs a Newznab indexer to Radarr using the validated self-URL

### Requirement: Consistent error envelope
All five proxy endpoints SHALL return HTTP 200 with a JSON body containing `success` (boolean) and optionally `error` (string). Self-URL validation failures SHALL use the same envelope.

#### Scenario: Self-URL validation failure uses error envelope
- **WHEN** the resolved self-URL is malformed on any of the five endpoints
- **THEN** the response is HTTP 200 with `{ "success": false, "error": "Invalid FunkArr URL ..." }`
