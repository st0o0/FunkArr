# Capability: dev-compose

## Purpose

Development Docker Compose stack (`docker-compose.dev.yml`) that runs FunkArr alongside Sonarr, Radarr, and Prowlarr for integrated local development and testing.

## Requirements

### Requirement: FunkArr service built from source
The compose file SHALL define a `funkarr` service that builds from `Dockerfile.dev` and exposes port 6969.

#### Scenario: Build and run FunkArr from source
- **WHEN** a developer runs `docker compose -f docker-compose.dev.yml up --build`
- **THEN** FunkArr is compiled from source, starts, and is reachable at `http://localhost:6969`

### Requirement: Sonarr service with pre-set API key
The compose file SHALL include a `sonarr` service using `ghcr.io/home-operations/sonarr` with the API key and auth configuration set via environment variables.

#### Scenario: Sonarr accessible without login
- **WHEN** the dev stack is running
- **THEN** Sonarr is reachable at `http://localhost:8989` without authentication

### Requirement: Radarr service with pre-set API key
The compose file SHALL include a `radarr` service using `ghcr.io/home-operations/radarr` with the API key and auth configuration set via environment variables.

#### Scenario: Radarr accessible without login
- **WHEN** the dev stack is running
- **THEN** Radarr is reachable at `http://localhost:7878` without authentication

### Requirement: Prowlarr service with pre-set API key
The compose file SHALL include a `prowlarr` service using `ghcr.io/home-operations/prowlarr` with the API key and auth configuration set via environment variables.

#### Scenario: Prowlarr accessible without login
- **WHEN** the dev stack is running
- **THEN** Prowlarr is reachable at `http://localhost:9696` without authentication

### Requirement: Shared media volume
The `funkarr`, `sonarr`, and `radarr` services SHALL mount a shared `media` volume at `/media` so download paths are consistent. Prowlarr does NOT mount the media volume (it is an indexer proxy and does not need file access).

#### Scenario: FunkArr download visible to Sonarr
- **WHEN** FunkArr writes a file to `/media/downloads`
- **THEN** Sonarr can access the same file at `/media/downloads`

#### Scenario: Prowlarr has no media mount
- **WHEN** the dev stack is inspected
- **THEN** the Prowlarr service SHALL NOT have a `/media` volume mount

### Requirement: Shared API key
All services SHALL use the same dev API key so FunkArr can authenticate against arr APIs and vice versa without key management.

#### Scenario: FunkArr API key matches Prowlarr
- **WHEN** Prowlarr sends a request to FunkArr's Newznab endpoint with its configured API key
- **THEN** FunkArr accepts the request

### Requirement: Persistent config volumes
Each arr service SHALL have its own named volume for `/config` so configuration (including manually added indexers/download clients) survives container restarts.

#### Scenario: Prowlarr config survives restart
- **WHEN** a developer stops and restarts the dev stack
- **THEN** Prowlarr retains its configured indexers and download clients
