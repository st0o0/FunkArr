## Context

The Setup Guide wizard (Step 3-5: Prowlarr/Sonarr/Radarr config) displays connection info as a reference table. Currently it shows placeholders for host/port and incorrect URL Base values. The health check API already returns `setupConnectionInfo` with paths and default port.

## Goals / Non-Goals

**Goals:**
- Show the actual FunkArr URL (from the browser's current location) in configuration tables
- Display correct URL Base values that account for how Sonarr/Radarr construct the full URL

**Non-Goals:**
- Auto-configuring the *arr services via their APIs (future change)
- Detecting Docker internal hostnames vs external addresses

## Decisions

### Decision: Use browser location for host, health check for port

The browser knows the address the user used to reach FunkArr. Use `window.location.hostname` and the port from the health check response (`setupConnectionInfo.defaultPort`). This handles both Docker and non-Docker setups correctly because the user already reached FunkArr at that address.

### Decision: Fix URL Base to match how Sonarr/Radarr actually construct URLs

Sonarr's SABnzbd client appends `/api` to the URL Base. So:
- URL Base `/download` -> Sonarr calls `/download/api?mode=...` (correct)
- URL Base `/download/api` -> Sonarr calls `/download/api/api?mode=...` (broken)

Prowlarr's Newznab client uses the API Path field directly (no auto-append), so `/index/api` is correct as-is.

## Risks / Trade-offs

**[Risk] Reverse proxy setups** -> If FunkArr is behind a reverse proxy with a different external port, `window.location.port` may not match the internal port. Mitigation: show the value from the browser (which is what the user needs for *arr config) and add a note "adjust if using a reverse proxy".
