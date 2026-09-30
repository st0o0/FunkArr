## Context

FunkArr integrates with the *arr* ecosystem (Sonarr, Radarr, Prowlarr) but has no local development environment that includes these services. The existing `Dockerfile` is CI-optimized — it expects pre-published, self-contained binaries and uses a chiseled (distroless) base image with no package manager. Developers currently test against real deployments or configure services manually.

## Goals / Non-Goals

**Goals:**
- One-command dev stack: `docker compose -f docker-compose.dev.yml up --build`
- FunkArr built from source inside Docker (no pre-publish step)
- Sonarr, Radarr, Prowlarr available with pre-set API keys and auth disabled
- Shared media volume so download paths are consistent across all services

**Non-Goals:**
- Automated Prowlarr indexer/download-client registration (manual on first run)
- Replacing the CI `Dockerfile` — it stays unchanged
- PostgreSQL in the dev stack (SQLite is sufficient for development)
- Multi-arch support for the dev Dockerfile (host architecture only)

## Decisions

### 1. Separate `Dockerfile.dev` instead of modifying existing `Dockerfile`

The production Dockerfile uses `aspnet:chiseled-extra` (distroless, no shell, no apt). A dev build needs the full SDK for compilation and a non-chiseled runtime for FFmpeg installation via apt. Combining both flows in one Dockerfile would add complexity with no benefit.

**Alternative considered:** Multi-target Dockerfile with build args to switch between CI and dev mode. Rejected — the two flows share almost nothing and the conditional logic would be harder to maintain.

### 2. `ghcr.io/home-operations/` images for arr services

These images support native env var injection for `config.xml` settings via the `APPNAME__NAMESPACE__SETTING` pattern (e.g. `SONARR__AUTH__APIKEY`). This avoids custom entrypoints or init scripts for API key configuration.

**Alternative considered:** Official LinuxServer.io images. Rejected — they don't support setting API keys via environment variables, requiring either volume-mounted config or init scripts.

### 3. Single shared API key across all services

All services use the same dev API key (`funkarr-dev-api-key`). This simplifies the compose file and eliminates cross-referencing between service configs.

### 4. Auth disabled on arr services

`AUTH__METHOD=None` and `AUTH__REQUIRED=DisabledForLocalAddresses` on all arr services. No login screens during development — direct API and UI access.

### 5. Non-chiseled runtime base for dev Dockerfile

Using `mcr.microsoft.com/dotnet/aspnet:10.0-noble` (not chiseled) so FFmpeg can be installed via apt. The SDK stage uses `mcr.microsoft.com/dotnet/sdk:10.0` for the build.

### 6. FunkArr connects to host network arr services via Docker DNS

Services reference each other by compose service name (`funkarr`, `sonarr`, `radarr`, `prowlarr`). FunkArr's path mapping is not needed since all containers mount the same `/media` volume at the same path.

## Risks / Trade-offs

- **First build is slow** (~2-3 min for NuGet restore + compile) → Docker layer caching mitigates on subsequent builds. COPY of csproj/slnx before source enables restore caching.
- **FFmpeg version differs from CI** → apt-installed FFmpeg vs. the specific version in the CI Dockerfile. Acceptable for development; stream-copy muxing is not version-sensitive.
- **No automated Prowlarr setup** → Developer must manually add FunkArr as indexer + download client on first run. This is a one-time action per volume lifecycle and avoids fragile init scripts.
