## Why

There is no local development environment that includes the *arr* ecosystem services (Sonarr, Radarr, Prowlarr). Developers must either deploy FunkArr to a real setup or manually configure companion services to test indexer/download-client integration. The existing `Dockerfile` is CI-only (expects pre-published binaries), so there is also no way to build and run FunkArr in Docker from source without a separate publish step.

## What Changes

- Add `Dockerfile.dev` — multi-stage build that compiles from source using the .NET SDK, producing a runnable container without requiring `dotnet publish` beforehand.
- Add `docker-compose.dev.yml` — full dev stack with FunkArr (built from `Dockerfile.dev`), Sonarr, Radarr, and Prowlarr using `ghcr.io/home-operations/` images. Shared API key, auth disabled, shared media volume. Prowlarr indexer/download-client registration remains manual on first run.

## Capabilities

### New Capabilities

- `dev-dockerfile`: Multi-stage Dockerfile that builds FunkArr from source using the .NET SDK, intended for local development and the dev compose stack.
- `dev-compose`: Docker Compose file that stands up FunkArr alongside Sonarr, Radarr, and Prowlarr for local integration testing and development.

### Modified Capabilities

_(none)_

## Impact

- **New files only** — no changes to existing code, Dockerfile, or docker-compose.example.yml.
- **Dependencies**: `ghcr.io/home-operations/sonarr`, `ghcr.io/home-operations/radarr`, `ghcr.io/home-operations/prowlarr` container images.
- **Developer workflow**: `docker compose -f docker-compose.dev.yml up --build` gives a full local stack.
