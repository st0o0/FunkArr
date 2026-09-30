## Context

FunkArr exposes two API surfaces for the *arr ecosystem: a Newznab XML indexer at `/index/api` (used by Prowlarr) and a SABnzbd JSON download client at `/download/api` (used by Sonarr/Radarr). Users must manually configure these URLs, API paths, and API keys in each *arr application. There is currently no way for FunkArr to verify its own operational readiness or help users with this configuration.

The internal API (`/api/*`) has no authentication — it is the UI backend, served on the same host. The *arr-facing APIs authenticate via `?apikey=` query parameter.

## Goals / Non-Goals

**Goals:**
- Provide a self-check endpoint that verifies all FunkArr prerequisites are met.
- Show health status on the dashboard so users see problems at a glance.
- Walk users through *arr configuration with exact, copy-pasteable field values.
- Match the actual field layout of Prowlarr/Sonarr/Radarr (URL vs API Path vs Host/Port/URL Base).

**Non-Goals:**
- No storing or testing of external *arr service URLs/API keys.
- No Prowlarr-to-Sonarr/Radarr sync verification.
- No programmatic configuration of *arr apps via their APIs.
- No actor-level health checks (only HTTP-level and filesystem).

## Decisions

### 1. Self-check runs server-side, returns structured JSON

The health check endpoint (`GET /api/health/setup`) runs all checks server-side and returns a single JSON response. This keeps the frontend simple and ensures checks like MediathekViewWeb connectivity and filesystem access work correctly in Docker environments where the browser can't reach those resources.

Alternative: Client-side checks from the browser. Rejected because the browser runs on the user's machine, not in Docker — it can't test filesystem paths or internal network connectivity.

### 2. Checks are non-cached and run on every request

Each `GET /api/health/setup` call runs all checks fresh. The checks are lightweight (HTTP HEAD, directory exists, config reads) and the endpoint is only called from the dashboard/setup UI, not under load.

Alternative: Cache with TTL. Rejected as premature — the checks are fast and correctness matters more than response time here.

### 3. API key shown in cleartext on the setup guide endpoint

The setup guide needs the API key for copy-to-clipboard. Since the internal API (`/api/*`) has no auth and is only accessible from the UI, exposing the key in the health response is acceptable. The dashboard widget shows only a masked version; the full key is available in the same response for the guide to use.

Alternative: Separate endpoint for the key. Rejected — adds complexity for no security gain since `/api/*` is already unprotected.

### 4. Connection info uses placeholder format, not auto-detection

The guide shows `<funkarr-host>` and `<funkarr-port>` as placeholders rather than attempting to auto-detect the base URL from request headers. Users know their own Docker hostnames/ports, and `Host` header detection is unreliable behind reverse proxies.

Alternative: Auto-detect from `Host` header. Rejected — fragile in Docker/reverse proxy setups, the most common deployment.

### 5. Per-service field layout matches actual *arr UI

Each *arr app has a different field layout:
- **Prowlarr** (Custom Newznab): URL + API Path as separate fields
- **Sonarr/Radarr** (SABnzbd): Host + Port + URL Base as separate fields

The guide must show the exact field names and values for each, not a generic "enter this URL" instruction.

### 6. Frontend stepper as dedicated route

The setup guide lives at `/setup` as a full-page stepper component, not a modal or drawer. This gives enough space for the per-service instructions and allows direct linking/bookmarking.

The dashboard shows a compact health widget that links to `/setup` for the full guide.

### 7. FFmpeg check is non-critical

FFmpeg availability is checked but reported as a warning, not a blocker. FunkArr can search and serve NZBs without FFmpeg — it's only needed for the download/remux pipeline.

## Risks / Trade-offs

- **MediathekViewWeb check adds external dependency to health endpoint** → The check uses HTTP HEAD with a short timeout (3s). If MediathekViewWeb is slow, the entire health response is delayed. Mitigation: run checks concurrently with `Task.WhenAll`.
- **API key in cleartext on unauthed endpoint** → Acceptable because the internal API is already unprotected and only accessible from the same host. If auth is added to `/api/*` later, this endpoint would be covered automatically.
- **Guide content is static/hardcoded** → The per-service instructions are baked into the Vue component. If Prowlarr/Sonarr/Radarr change their UIs, the guide needs manual updates. Mitigation: keep instructions minimal (field names + values, no screenshots).

## Open Questions

None — scope is well-defined from the exploration session.
